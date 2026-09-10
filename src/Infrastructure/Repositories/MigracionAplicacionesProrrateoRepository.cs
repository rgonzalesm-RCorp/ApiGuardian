using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Infrastructure.Persistence;
using Dapper;
using System.Globalization;

namespace ApiGuardian.Infrastructure.Repositories;

public sealed class MigracionAplicacionesProrrateoRepository : IMigracionAplicacionesProrrateoRepository
{
    private const int TipoDescuentoAplicacionesId = 1;
    private readonly DapperContext _guardianContext;
    private readonly DapperContextSqlServer _sqlContext;
    private readonly IAdministracionComplejoRepository _administracionComplejoRepository;

    public MigracionAplicacionesProrrateoRepository(
        DapperContext guardianContext,
        DapperContextSqlServer sqlContext,
        IAdministracionComplejoRepository administracionComplejoRepository
    )
    {
        _guardianContext = guardianContext;
        _sqlContext = sqlContext;
        _administracionComplejoRepository = administracionComplejoRepository;
    }

    public async Task<(ResultadoMigracionAplicacionesProrrateo Datos, bool Exito, string Mensaje)> VistaPreviaAsync(SolicitudMigracionAplicacionesProrrateo solicitud)
    {
        var validacion = ValidarSolicitud(solicitud);
        if (validacion is not null)
            return (new ResultadoMigracionAplicacionesProrrateo(), false, validacion);

        var preparado = await PrepararAsync(solicitud);
        return (preparado.Resultado, preparado.Resultado.Errores.Count == 0, preparado.Resultado.Errores.Count == 0
            ? "Vista previa generada correctamente."
            : "La vista previa encontró registros que no pueden migrarse.");
    }

    public async Task<(ResultadoMigracionAplicacionesProrrateo Datos, bool Exito, string Mensaje)> EjecutarAsync(SolicitudMigracionAplicacionesProrrateo solicitud)
        => await EjecutarInternoAsync(solicitud, limpiarDescuentosCiclo: false);

    public async Task<(ResultadoMigracionAplicacionesProrrateo Datos, bool Exito, string Mensaje)> EjecutarDesdeCeroAsync(SolicitudMigracionAplicacionesProrrateo solicitud)
        => await EjecutarInternoAsync(solicitud, limpiarDescuentosCiclo: true);

    private async Task<(ResultadoMigracionAplicacionesProrrateo Datos, bool Exito, string Mensaje)> EjecutarInternoAsync(
        SolicitudMigracionAplicacionesProrrateo solicitud,
        bool limpiarDescuentosCiclo
    )
    {
        var validacion = ValidarSolicitud(solicitud);
        if (validacion is not null)
            return (new ResultadoMigracionAplicacionesProrrateo(), false, validacion);

        var preparado = await PrepararAsync(solicitud);
        var resultado = preparado.Resultado;
        if (resultado.Errores.Count > 0)
            return (resultado, false, "No se insertó información: existen contactos o complejos sin homologar.");

        var cicloDestino = solicitud.Ciclo;
        using var conexion = _guardianContext.CreateConnection();
        conexion.Open();
        using var transaccion = conexion.BeginTransaction();
        try
        {
            // Evita que el resumen sdetalles se trunque al consolidar múltiples descuentos.
            await conexion.ExecuteAsync("SET SESSION group_concat_max_len = 1048576;", transaction: transaccion);

            if (limpiarDescuentosCiclo)
            {
                await conexion.ExecuteAsync("""
                    DELETE detalle
                    FROM administraciondescuentociclodetalle detalle
                    INNER JOIN administraciondescuentociclo encabezado
                        ON encabezado.ldescuentociclo_id = detalle.ldescuentociclo_id
                    WHERE encabezado.lciclo_id = @CicloDestino;
                    """, new { CicloDestino = cicloDestino }, transaccion);

                await conexion.ExecuteAsync(
                    "DELETE FROM administraciondescuentociclo WHERE lciclo_id = @CicloDestino;",
                    new { CicloDestino = cicloDestino },
                    transaccion
                );
            }

            var encabezados = (await conexion.QueryAsync<EncabezadoDescuento>(
                "SELECT ldescuentociclo_id Id, lcontacto_id ContactoId FROM administraciondescuentociclo WHERE lciclo_id = @CicloDestino;",
                new { CicloDestino = cicloDestino }, transaccion)).ToDictionary(item => item.ContactoId, item => item.Id);

            foreach (var fila in preparado.Filas)
            {
                if (!encabezados.TryGetValue(fila.ContactoId, out var descuentoCicloId))
                {
                    descuentoCicloId = await conexion.QuerySingleAsync<int>("""
                        INSERT INTO administraciondescuentociclo
                            (susuarioadd, dtfechaadd, susuariomod, dtfechamod, ldescuentociclo_id, lciclo_id, lcontacto_id, dtotal, sdetalles, lsemana_id)
                        SELECT @Usuario, NOW(), @Usuario, NOW(), COALESCE(MAX(ldescuentociclo_id), 0) + 1, @CicloDestino, @ContactoId, 0, '', 1
                        FROM administraciondescuentociclo;
                        SELECT MAX(ldescuentociclo_id) FROM administraciondescuentociclo;
                        """,
                        new { solicitud.Usuario, CicloDestino = cicloDestino, fila.ContactoId }, transaccion);
                    encabezados[fila.ContactoId] = descuentoCicloId;
                }

                await conexion.ExecuteAsync("""
                    INSERT INTO administraciondescuentociclodetalle
                        (susuarioadd, dtfechaadd, susuariomod, dtfechamod, ldescuentociclodetalle_id, ldescuentociclo_id,
                         ldescuentociclotipo_id, lcomplejo_id, smanzano, slote, suv, dmonto, sobservacion)
                    SELECT @Usuario, NOW(), @Usuario, NOW(), COALESCE(MAX(ldescuentociclodetalle_id), 0) + 1, @DescuentoCicloId,
                           @TipoDescuentoId, @ComplejoId, @Manzano, @Lote, @Uv, @Monto, @Observacion
                    FROM administraciondescuentociclodetalle;
                    """,
                    new { solicitud.Usuario, DescuentoCicloId = descuentoCicloId, TipoDescuentoId = TipoDescuentoAplicacionesId, fila.ComplejoId, fila.Manzano, fila.Lote, fila.Uv, fila.Monto, fila.Observacion }, transaccion);
                resultado.RegistrosInsertados++;
            }

            foreach (var contactoId in encabezados.Keys)
            {
                await conexion.ExecuteAsync("""
                    UPDATE administraciondescuentociclo encabezado
                    SET dtotal = COALESCE((SELECT SUM(detalle.dmonto) FROM administraciondescuentociclodetalle detalle WHERE detalle.ldescuentociclo_id = encabezado.ldescuentociclo_id), 0),
                        sdetalles = COALESCE((SELECT GROUP_CONCAT(CONCAT(detalle.sobservacion, ' - Monto: ', detalle.dmonto, ' $us. - ', detalle.suv) SEPARATOR ',') FROM administraciondescuentociclodetalle detalle WHERE detalle.ldescuentociclo_id = encabezado.ldescuentociclo_id), '')
                    WHERE encabezado.lciclo_id = @CicloDestino AND encabezado.lcontacto_id = @ContactoId;
                    """, new { CicloDestino = cicloDestino, ContactoId = contactoId }, transaccion);
            }

            resultado.ProrrateosInsertados = await RegenerarProrrateoAsync(
                cicloDestino,
                solicitud.Usuario,
                conexion,
                transaccion,
                preparado.Prorrateos
            );

            transaccion.Commit();
            return (
                resultado,
                true,
                $"Migración completada. Descuentos insertados: {resultado.RegistrosInsertados}. Prorrateos insertados: {resultado.ProrrateosInsertados}."
            );
        }
        catch (Exception ex)
        {
            transaccion.Rollback();
            resultado.Errores.Add(ex.Message);
            return (resultado, false, "La migración fue revertida por un error; no se guardaron cambios.");
        }
    }

    private async Task<(ResultadoMigracionAplicacionesProrrateo Resultado, List<FilaMigracion> Filas, List<FilaProrrateoOrigen> Prorrateos)> PrepararAsync(SolicitudMigracionAplicacionesProrrateo solicitud)
    {
        var cicloDestino = solicitud.Ciclo;
        using var origen = _sqlContext.CreateConnection();
        var origenFilas = (await origen.QueryAsync<FilaOrigen>(
            SqlOrigen,
            new { solicitud.FechaInicio, CicloOrigen = cicloDestino }
        )).ToList();
        var resultado = new ResultadoMigracionAplicacionesProrrateo { RegistrosOrigen = origenFilas.Count };

        using var guardian = _guardianContext.CreateConnection();
        var tipoExiste = await guardian.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM administraciondescuentociclotipo WHERE ldescuentociclotipo_id = @Id;", new { Id = TipoDescuentoAplicacionesId });
        if (tipoExiste == 0)
        {
            resultado.Errores.Add($"No existe el tipo de descuento de aplicaciones {TipoDescuentoAplicacionesId}.");
            return (resultado, [], []);
        }

        var contactos = (await guardian.QueryAsync<ContactoGuardian>("SELECT lcontacto_id Id, TRIM(scedulaidentidad) Documento FROM administracioncontacto WHERE cbaja = 0;"))
            .Where(item => !string.IsNullOrWhiteSpace(item.Documento))
            .GroupBy(item => item.Documento, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(item => item.Key, item => item.First().Id, StringComparer.OrdinalIgnoreCase);
        // Se reutiliza exactamente la misma homologación que usa la migración de ventas:
        // empresa_complejo.id_almacen_conexion (CNX) -> empresa_complejo.complejo_id (Guardian).
        var respuestaHomologacion = await _administracionComplejoRepository.GetHomologacionComplejoGrdCnx("MIGRACION_APLICACIONES_PRORRATEO");
        if (!respuestaHomologacion.Success)
        {
            resultado.Errores.Add($"No se pudo obtener la homologación de proyectos usada por la migración de ventas: {respuestaHomologacion.Mensaje}");
            return (resultado, [], []);
        }

        var homologacionesDuplicadas = respuestaHomologacion.Data
            .GroupBy(item => item.LComplejoIdCX)
            .Where(grupo => grupo.Select(item => item.LComplejoId).Distinct().Count() != 1)
            .ToList();
        //foreach (var duplicado in homologacionesDuplicadas)
        //  resultado.Errores.Add($"IDALMACEN CNX con homologación ambigua: {duplicado.Key}.");

        if (resultado.Errores.Count > 0)
            return (resultado, [], []);

        var complejos = respuestaHomologacion.Data
            .GroupBy(item => item.LComplejoIdCX)
            .ToDictionary(item => item.Key, item => item.First().LComplejoId);

        var prorrateosOrigen = (await origen.QueryAsync<ProrrateoConexionOrigen>(
            SqlProrrateoOrigen,
            new { CicloOrigen = cicloDestino }
        )).ToList();
        resultado.ProrrateosOrigen = prorrateosOrigen.Count;

        var empresasGuardian = (await origen.QueryAsync<MapeoEmpresaConexion>(SqlMapeoEmpresaConexion))
            .GroupBy(item => item.EmpresaConexionId)
            .ToDictionary(item => item.Key, item => item.Select(x => x.EmpresaGuardianId).Distinct().ToList());

        var prorrateos = new List<FilaProrrateoOrigen>();
        foreach (var prorrateoOrigen in prorrateosOrigen)
        {
            var documento = prorrateoOrigen.DocumentoCliente?.Trim() ?? string.Empty;
            if (!contactos.TryGetValue(documento, out var contactoId))
            {
                resultado.Errores.Add($"Prorrateo 4.12 sin contacto Guardian: CI {documento}.");
                continue;
            }

            if (!empresasGuardian.TryGetValue(prorrateoOrigen.EmpresaPrestaId, out var empresaGuardian) || empresaGuardian.Count != 1)
            {
                resultado.Errores.Add($"EmpresaPresta sin equivalencia única en Guardian: {prorrateoOrigen.EmpresaPrestaId} (CI {documento}).");
                continue;
            }

            prorrateos.Add(new FilaProrrateoOrigen(
                contactoId,
                NormalizarEmpresaTemporal(empresaGuardian[0]),
                prorrateoOrigen.Monto
            ));
        }

        if (resultado.Errores.Count > 0)
            return (resultado, [], []);

        var existentes = (await guardian.QueryAsync<DetalleExistente>("""
            SELECT encabezado.lcontacto_id ContactoId, detalle.lcomplejo_id ComplejoId, detalle.smanzano Manzano, detalle.slote Lote, detalle.suv Uv, detalle.dmonto Monto, detalle.sobservacion Observacion
            FROM administraciondescuentociclo encabezado
            INNER JOIN administraciondescuentociclodetalle detalle ON detalle.ldescuentociclo_id = encabezado.ldescuentociclo_id
            WHERE encabezado.lciclo_id = @CicloDestino AND detalle.ldescuentociclotipo_id = @TipoDescuentoId;
            """, new { CicloDestino = cicloDestino, TipoDescuentoId = TipoDescuentoAplicacionesId })).Select(ClaveDetalle).ToHashSet();

        var filas = new List<FilaMigracion>();
        foreach (var fila in origenFilas)
        {
            var ci = fila.CiCliente?.Trim() ?? string.Empty;
            if (!contactos.TryGetValue(ci, out var contactoId))
            {
                resultado.Errores.Add($"CI sin contacto Guardian: {ci} (recibo CNX {fila.IdRecibo}).");
                continue;
            }
            if (!complejos.TryGetValue(fila.IdAlmacen, out var complejoId))
            {
                resultado.Errores.Add($"IDALMACEN CNX sin homologación Guardian: {fila.IdAlmacen} (recibo CNX {fila.IdRecibo}).");
                continue;
            }

            var glosa = (fila.Observaciones ?? string.Empty).TrimEnd();
            if (fila.EmpresaId is 32 or 33)
            {
                var montoBob = decimal.Round(fila.Monto * 6.96m, 0, MidpointRounding.AwayFromZero);
                glosa = $"{glosa} - Equivalente BOB {montoBob:N0} (TC 6.96)";
            }

            var migracion = new FilaMigracion(
                contactoId,
                complejoId,
                (fila.Manzano ?? string.Empty).TrimEnd(),
                (fila.Lote ?? string.Empty).TrimEnd(),
                (fila.Lotes ?? string.Empty).TrimEnd(),
                fila.Monto,
                glosa
            );
            if (!existentes.Add(ClaveDetalle(migracion)))
            {
                resultado.RegistrosDuplicados++;
                continue;
            }
            filas.Add(migracion);
        }
        resultado.RegistrosListos = filas.Count;
        return (resultado, filas, prorrateos);
    }

    private static async Task<int> RegenerarProrrateoAsync(
        int ciclo,
        string usuario,
        System.Data.IDbConnection conexion,
        System.Data.IDbTransaction transaccion,
        IReadOnlyCollection<FilaProrrateoOrigen> prorrateosOrigen
    )
    {
        var comisiones = (await conexion.QueryAsync<ProrrateoCalculado>(
            SqlComisionesProrrateo,
            new { Ciclo = ciclo },
            transaccion
        )).ToList();

        var descuentosProrrateo = prorrateosOrigen
            .GroupBy(item => (item.ContactoId, item.EmpresaId))
            .ToDictionary(
                grupo => grupo.Key,
                grupo => decimal.Round(grupo.Sum(item => item.Monto), 2, MidpointRounding.AwayFromZero)
            );

        var comisionesPorClave = comisiones
            .Select(item => (item.ContactoId, item.EmpresaId))
            .ToHashSet();
        var descuentosSinComision = descuentosProrrateo.Keys
            .Where(clave => !comisionesPorClave.Contains(clave))
            .ToList();
        if (descuentosSinComision.Count > 0)
        {
            var detalle = string.Join(", ", descuentosSinComision.Take(10).Select(item => $"contacto {item.ContactoId}/empresa {item.EmpresaId}"));
            throw new InvalidOperationException(
                $"Existen descuentos de AplicacionesProrrateo sin comisión para el mismo cliente y empresa: {detalle}."
            );
        }

        await conexion.ExecuteAsync(
            "DELETE FROM administracioncomisionprorrateo WHERE lciclo_id = @Ciclo;",
            new { Ciclo = ciclo },
            transaccion
        );

        // dmonto es el saldo a pagar por cliente y empresa:
        // todas las comisiones - retención - descuento de AplicacionesProrrateo.
        var filas = comisiones
            .Select(comision => new ProrrateoCalculado
            {
                ContactoId = comision.ContactoId,
                EmpresaId = comision.EmpresaId,
                Monto = decimal.Round(
                    comision.Monto - descuentosProrrateo.GetValueOrDefault((comision.ContactoId, comision.EmpresaId)),
                    2,
                    MidpointRounding.AwayFromZero
                )
            })
            .Where(item => item.Monto > 0)
            .ToList();
        if (filas.Count == 0)
            return 0;

        var siguienteId = await conexion.ExecuteScalarAsync<int>(
            "SELECT COALESCE(MAX(lprorrateo_id), 0) FROM administracioncomisionprorrateo;",
            transaction: transaccion
        );

        foreach (var fila in filas)
        {
            siguienteId++;
            await conexion.ExecuteAsync("""
                INSERT INTO administracioncomisionprorrateo
                    (susuarioadd, dtfechaadd, susuariomod, dtfechamod,
                     lprorrateo_id, lciclo_id, lcontacto_id, lempresa_id_temp, dmonto)
                VALUES
                    (@Usuario, NOW(), @Usuario, NOW(),
                     @ProrrateoId, @Ciclo, @ContactoId, @EmpresaId, @Monto);
                """,
                new
                {
                    ProrrateoId = siguienteId,
                    Usuario = usuario,
                    Ciclo = ciclo,
                    fila.ContactoId,
                    fila.EmpresaId,
                    fila.Monto
                },
                transaccion
            );
        }

        return filas.Count;
    }

    private static string? ValidarSolicitud(SolicitudMigracionAplicacionesProrrateo solicitud)
    {
        if (solicitud.Ciclo <= 0 || string.IsNullOrWhiteSpace(solicitud.FechaInicio))
            return "Ciclo y FechaInicio son obligatorios.";

        return DateTime.TryParseExact(
            solicitud.FechaInicio,
            "yyyyMMdd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _
        )
            ? null
            : "FechaInicio debe tener el formato yyyyMMdd; por ejemplo: 20260901.";
    }

    // administracioncomisionprorrateo conserva 20 y 13 como IDs temporales;
    // los reportes los muestran como las empresas Guardian 21 y 14 respectivamente.
    private static int NormalizarEmpresaTemporal(int empresaGuardianId) => empresaGuardianId switch
    {
        21 => 20,
        14 => 13,
        _ => empresaGuardianId
    };

    private static string ClaveDetalle(DetalleExistente fila) => $"{fila.ContactoId}|{fila.ComplejoId}|{fila.Manzano}|{fila.Lote}|{fila.Uv}|{fila.Monto}|{fila.Observacion}";
    private static string ClaveDetalle(FilaMigracion fila) => $"{fila.ContactoId}|{fila.ComplejoId}|{fila.Manzano}|{fila.Lote}|{fila.Uv}|{fila.Monto}|{fila.Observacion}";

    private const string SqlOrigen = """
        SELECT AP.CI_Cliente CiCliente, C.IDEMPRESA EmpresaId, C.IDALMACEN IdAlmacen, '' Manzano, '' Lote, C.LOTES Lotes, AP.Monto Monto, RTRIM(C.CONCEPTO1) Observaciones, C.IDRECIBO IdRecibo
        FROM BDQISHUR.dbo.AplicacionesPagos AP
        INNER JOIN (
            SELECT IDEMPRESA, IDRECIBO, MAX(LOTES) LOTES, MAX(IDALMACEN) IDALMACEN, MAX(CONCEPTO1) CONCEPTO1
            FROM (
                SELECT 8 IDEMPRESA, R.*, VC.LOTES, V.IDALMACEN FROM BDConexionADVEL.dbo.INRECIBO R INNER JOIN BDConexionADVEL.dbo.INVENTA V ON V.IDVENTA = R.IDVENTA INNER JOIN BDConexionADVEL.dbo.INVENTA_CCN VC ON VC.IDVENTA = V.IDVENTA WHERE R.MODUSER = 'COMI' AND R.FECHA >= @FechaInicio
                UNION SELECT 2, R.*, VC.LOTES, V.IDALMACEN FROM BDConexionQUINTAS.dbo.INRECIBO R INNER JOIN BDConexionQUINTAS.dbo.INVENTA V ON V.IDVENTA = R.IDVENTA INNER JOIN BDConexionQUINTAS.dbo.INVENTA_CCN VC ON VC.IDVENTA = V.IDVENTA WHERE R.MODUSER = 'COMI' AND R.FECHA >= @FechaInicio
                UNION SELECT 12, R.*, VC.LOTES, V.IDALMACEN FROM BDConexionJAYIL.dbo.INRECIBO R INNER JOIN BDConexionJAYIL.dbo.INVENTA V ON V.IDVENTA = R.IDVENTA INNER JOIN BDConexionJAYIL.dbo.INVENTA_CCN VC ON VC.IDVENTA = V.IDVENTA WHERE R.MODUSER = 'COMI' AND R.FECHA >= @FechaInicio
                UNION SELECT 3, R.*, VC.LOTES, V.IDALMACEN FROM BDConexionZURIEL.dbo.INRECIBO R INNER JOIN BDConexionZURIEL.dbo.INVENTA V ON V.IDVENTA = R.IDVENTA INNER JOIN BDConexionZURIEL.dbo.INVENTA_CCN VC ON VC.IDVENTA = V.IDVENTA WHERE R.MODUSER = 'COMI' AND R.FECHA >= @FechaInicio
                UNION SELECT 33, R.*, VC.LOTES, V.IDALMACEN FROM BDConexionADVELbs.dbo.INRECIBO R INNER JOIN BDConexionADVELbs.dbo.INVENTA V ON V.IDVENTA = R.IDVENTA INNER JOIN BDConexionADVELbs.dbo.INVENTA_CCN VC ON VC.IDVENTA = V.IDVENTA WHERE R.MODUSER = 'COMI' AND R.FECHA >= @FechaInicio
                UNION SELECT 32, R.*, VC.LOTES, V.IDALMACEN FROM BDConexionARARAT.dbo.INRECIBO R INNER JOIN BDConexionARARAT.dbo.INVENTA V ON V.IDVENTA = R.IDVENTA INNER JOIN BDConexionARARAT.dbo.INVENTA_CCN VC ON VC.IDVENTA = V.IDVENTA WHERE R.MODUSER = 'COMI' AND R.FECHA >= @FechaInicio
            ) recibos
            GROUP BY IDEMPRESA, IDRECIBO
        ) C ON C.IDEMPRESA = AP.Id_Empresa AND C.IDRECIBO = AP.Id_Recibo
        WHERE AP.Ciclo = @CicloOrigen
        ;
        """;

    private const string SqlProrrateoOrigen = """
        SELECT
            LTRIM(RTRIM(CiCliente)) DocumentoCliente,
            EmpresaPresta EmpresaPrestaId,
            Monto
        FROM BDQISHUR.dbo.AplicacionesProrrateo
        WHERE Ciclo = @CicloOrigen;
        """;

    private const string SqlMapeoEmpresaConexion = """
        SELECT
            IDBD EmpresaConexionId,
            lempresa_id EmpresaGuardianId
        FROM BDQISHUR.dbo.AplicacionesEmpresaGuardianAsumeSion
        WHERE IDBD IN (8, 2, 12, 33, 32, 3)
          AND empresa <> 'MEXICO'
          AND lempresa_id NOT IN (15, 18, 10);
        """;

    private const string SqlComisionesProrrateo = """
        SELECT
            retencion.lcontacto_id ContactoId,
            CASE retencion.idempresa
                WHEN 21 THEN 20
                WHEN 14 THEN 13
                ELSE retencion.idempresa
            END EmpresaId,
            ROUND(SUM(COALESCE(retencion.montocomision, 0)) - SUM(COALESCE(retencion.montoretencion, 0)), 2) Monto
        FROM tbl_retencionempresa retencion
        INNER JOIN administracioncontacto contacto
            ON contacto.lcontacto_id = retencion.lcontacto_id
           AND contacto.cbaja = 0
        WHERE retencion.lciclo_id = @Ciclo
          AND retencion.lcontacto_id > 3
          AND retencion.lcontacto_id <> 6474
        GROUP BY
            retencion.lcontacto_id,
            CASE retencion.idempresa
                WHEN 21 THEN 20
                WHEN 14 THEN 13
                ELSE retencion.idempresa
            END;
        """;

    private sealed class FilaOrigen { public string? CiCliente { get; set; } public int EmpresaId { get; set; } public int IdAlmacen { get; set; } public string? Manzano { get; set; } public string? Lote { get; set; } public string? Lotes { get; set; } public decimal Monto { get; set; } public string? Observaciones { get; set; } public int IdRecibo { get; set; } }
    private sealed class ContactoGuardian { public int Id { get; set; } public string Documento { get; set; } = string.Empty; }
    private sealed class EncabezadoDescuento { public int Id { get; set; } public int ContactoId { get; set; } }
    private sealed class DetalleExistente { public int ContactoId { get; set; } public int ComplejoId { get; set; } public string? Manzano { get; set; } public string? Lote { get; set; } public string? Uv { get; set; } public decimal Monto { get; set; } public string? Observacion { get; set; } }
    private sealed class ProrrateoCalculado { public int ContactoId { get; set; } public int EmpresaId { get; set; } public decimal Monto { get; set; } }
    private sealed class ProrrateoConexionOrigen { public string? DocumentoCliente { get; set; } public int EmpresaPrestaId { get; set; } public decimal Monto { get; set; } }
    private sealed class MapeoEmpresaConexion { public int EmpresaConexionId { get; set; } public int EmpresaGuardianId { get; set; } }
    private sealed record FilaProrrateoOrigen(int ContactoId, int EmpresaId, decimal Monto);
    private sealed record FilaMigracion(int ContactoId, int ComplejoId, string Manzano, string Lote, string Uv, decimal Monto, string Observacion);
}
