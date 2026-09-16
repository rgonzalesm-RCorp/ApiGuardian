using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.DTO;
using ApiGuardian.Infrastructure.Persistence;
using Dapper;
using System.Globalization;
using System.Text;

namespace ApiGuardian.Infrastructure.Repositories;

public sealed class MonteSionRepository : IMonteSionRepository
{
    private const string NombreArchivo = "MonteSionRepository.cs";
    private readonly DapperContext _context;
    private readonly ILogService _log;

    public MonteSionRepository(DapperContext context, ILogService log)
    {
        _context = context;
        _log = log;
    }

    public async Task<(IEnumerable<MonteSionRangoActual> Data, bool Success, string Mensaje)> ObtenerRangosActualesAsync(string logTransaccionId, int cicloId)
    {
        const string sql = """
            SELECT
                plan.lcontacto_id AS EmprendedorId,
                COALESCE(plan.Nivel, 0) AS NivelActualId,
                COALESCE(nivel.snombre, 'Asesor Comercial') AS RangoActual
            FROM t_plan_montesion plan
            LEFT JOIN administracionnivel nivel ON nivel.lnivel_id = plan.Nivel
            INNER JOIN (
                SELECT lcontacto_id, MAX(id_plan_montesion) AS IdPlanMonteSion
                FROM t_plan_montesion
                GROUP BY lcontacto_id
            ) ultimo ON ultimo.IdPlanMonteSion = plan.id_plan_montesion;
            """;

        try
        {
            using var connection = _context.CreateConnection();
            var data = await connection.QueryAsync<MonteSionRangoActual>(sql, new { CicloId = cicloId });
            return (data, true, "Rangos actuales obtenidos correctamente.");
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId, NombreArchivo, nameof(ObtenerRangosActualesAsync), "Error al obtener rangos actuales Monte Sion.", ex);
            return (Enumerable.Empty<MonteSionRangoActual>(), false, $"Error al obtener rangos actuales: {ex.Message}");
        }
    }

    public async Task<(IEnumerable<MonteSionNivel> Data, bool Success, string Mensaje)> ObtenerNivelesAsync(string logTransaccionId)
    {
        const string sql = """
            SELECT lnivel_id AS NivelId, snombre AS Nombre, ddesde AS ProduccionDesde, dhasta AS ProduccionHasta,
                   dbono AS BonoUsd, dbonomembresia AS PorcentajeLiderazgo, VME AS VmeMonto
            FROM administracionnivel
            ORDER BY lnivel_id;
            """;
        try
        {
            using var connection = _context.CreateConnection();
            var data = await connection.QueryAsync<MonteSionNivel>(sql);
            return (data, true, "Niveles Monte Sion obtenidos correctamente.");
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId, NombreArchivo, nameof(ObtenerNivelesAsync), "Error al obtener niveles Monte Sion.", ex);
            return (Enumerable.Empty<MonteSionNivel>(), false, $"Error al obtener niveles: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Mensaje, MonteSionGuardadoResultado Data)> GuardarRangosAsync(
        string logTransaccionId,
        int cicloId,
        string usuario,
        IReadOnlyCollection<MonteSionResultadoRango> resultados)
    {
        var salida = new MonteSionGuardadoResultado { CicloId = cicloId };
        if (cicloId <= 0 || resultados.Count == 0)
            return (false, "No existen rangos calculados para guardar.", salida);

        const string datosExistentesSql = """
            SELECT GROUP_CONCAT(Tabla ORDER BY Tabla SEPARATOR ', ')
            FROM (
                SELECT 't_plan_montesion' AS Tabla
                FROM DUAL
                WHERE EXISTS (SELECT 1 FROM t_plan_montesion WHERE lciclo_id = @CicloId)
                UNION ALL
                SELECT 'reportesmontesion' AS Tabla
                FROM DUAL
                WHERE EXISTS (SELECT 1 FROM reportesmontesion WHERE lciclo_id = @CicloId)
                UNION ALL
                SELECT 't_nuevos_rangos_montesion_2025' AS Tabla
                FROM DUAL
                WHERE EXISTS (SELECT 1 FROM t_nuevos_rangos_montesion_2025 WHERE lciclo_id = @CicloId)
            ) AS TablasConCiclo;
            """;
        const string nivelesSql = "SELECT lnivel_id AS NivelId, snombre AS Nombre FROM administracionnivel;";
        const string contactosSql = """
            SELECT contacto.lcontacto_id AS ContactoId, contacto.scodigo AS Codigo,
                   contacto.snombrecompleto AS Nombre, contacto.scedulaidentidad AS Ci,
                   contacto.stelefonomovil AS Celular, contacto.stelefonofijo AS Fijo,
                   contacto.lpais_id AS PaisId, contacto.sciudad AS Ciudad, pais.snombre AS Pais
            FROM administracioncontacto contacto
            INNER JOIN basepais pais ON pais.lpais_id = contacto.lpais_id
            WHERE contacto.lcontacto_id IN @ContactoIds;
            """;
        const string ventasPersonalesSql = """
            SELECT grupo.lasesor_id AS ContactoId,
                   COALESCE(SUM(CASE WHEN grupo.lgeneracion = 0 THEN contrato.dprecio ELSE 0 END), 0) AS Vendido,
                   SUM(CASE WHEN grupo.lgeneracion = 0 THEN 1 ELSE 0 END) AS Cantidad
            FROM administracionventagrupo grupo
            INNER JOIN administracioncontrato contrato ON contrato.lcontrato_id = grupo.lcontrato_id
            WHERE grupo.lciclo_id = @CicloId AND grupo.lasesor_id IN @ContactoIds
            GROUP BY grupo.lasesor_id;
            """;

        try
        {
            using var connection = _context.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            var tablasConCiclo = await connection.ExecuteScalarAsync<string?>(datosExistentesSql, new { CicloId = cicloId }, transaction);
            if (!string.IsNullOrWhiteSpace(tablasConCiclo))
            {
                transaction.Rollback();
                return (false, $"El ciclo {cicloId} ya fue registrado en: {tablasConCiclo}. No se duplicaron registros.", salida);
            }

            var contactoIds = resultados.Select(item => item.EmprendedorId).Distinct().ToArray();
            var niveles = (await connection.QueryAsync<NivelMonteSion>(nivelesSql, transaction: transaction)).ToList();
            var nivelesPorId = niveles.ToDictionary(nivel => nivel.NivelId);
            var contactos = (await connection.QueryAsync<ContactoMonteSion>(contactosSql, new { ContactoIds = contactoIds }, transaction))
                .ToDictionary(item => item.ContactoId);
            var ventasPersonales = (await connection.QueryAsync<VentaPersonalMonteSion>(ventasPersonalesSql, new { CicloId = cicloId, ContactoIds = contactoIds }, transaction))
                .ToDictionary(item => item.ContactoId);
            var mes = await connection.ExecuteScalarAsync<string>("SELECT snombre FROM administracionciclo WHERE lciclo_id = @CicloId;", new { CicloId = cicloId }, transaction) ?? cicloId.ToString();

            var nivelesInvalidos = resultados
                .Where(item => item.NivelActualId < 0 || item.NivelAlcanzadoId < 0
                    || !nivelesPorId.ContainsKey(item.NivelActualId)
                    || (item.Califica && !nivelesPorId.ContainsKey(item.NivelAlcanzadoId)))
                .Select(item => item.EmprendedorId)
                .Distinct()
                .ToList();
            if (nivelesInvalidos.Count > 0)
            {
                transaction.Rollback();
                return (false, $"Existen niveles calculados no válidos para los contactos: {string.Join(", ", nivelesInvalidos)}.", salida);
            }

            var proximoPlanId = await connection.ExecuteScalarAsync<int>("SELECT COALESCE(MAX(id_plan_montesion), 0) FROM t_plan_montesion;", transaction: transaction);
            var proximoReporteId = await connection.ExecuteScalarAsync<int>("SELECT COALESCE(MAX(reportemontesion_id), 0) FROM reportesmontesion;", transaction: transaction);
            var proximoIdNro = await connection.ExecuteScalarAsync<int>("SELECT COALESCE(MAX(id_nro), 0) FROM reportesmontesion;", transaction: transaction);
            var proximoNuevoRangoId = await connection.ExecuteScalarAsync<long>("SELECT COALESCE(MAX(ID), 0) FROM t_nuevos_rangos_montesion_2025;", transaction: transaction);
            var proximoAscenso = 0;

            foreach (var resultado in resultados)
            {
                if (!contactos.TryGetValue(resultado.EmprendedorId, out var contacto))
                    throw new InvalidOperationException($"No existe el contacto {resultado.EmprendedorId}.");

                var nivelActual = resultado.NivelActualId;
                var nivelAlcanzado = resultado.NivelAlcanzadoId;
                var subioNivel = nivelAlcanzado > nivelActual;
                var nivelesEscalados = subioNivel ? nivelAlcanzado - nivelActual : 0;
                var ventaPersonal = ventasPersonales.GetValueOrDefault(resultado.EmprendedorId);
                var cantidadGrupo = resultado.RangoEvaluado?.Equipos.Sum(equipo => equipo.VentasPorNivel.Sum(venta => venta.NumerosVenta.Count)) ?? 0;
                var monto = resultado.Beneficio?.BonoAPagarUsd ?? 0m;
                var porcentajeLiderazgo = resultado.RangoEvaluado?.PorcentajeLiderazgo ?? 0m;

                const string planSql = """
                    INSERT INTO t_plan_montesion
                    (susuarioadd, dtfechaadd, susuariomod, dtfechamod, id_plan_montesion, scodigo, Nivel, lcontacto_id, Nombre,
                     Vendido, CantVP, Venta_Grupo, CantVG, Nivel_Conseguido, Monto_Pagar, Porcentaje_Liderazgo, lciclo_id)
                    VALUES (@Usuario, NOW(), @Usuario, NOW(), @Id, @Codigo, @NivelActual, @ContactoId, @Nombre,
                            @Vendido, @CantVP, @VentaGrupo, @CantVG, @NivelAlcanzado, @Monto, @PorcentajeLiderazgo, @CicloId);
                    """;
                await connection.ExecuteAsync(planSql, new
                {
                    Usuario = usuario, Id = ++proximoPlanId, contacto.Codigo, NivelActual = nivelActual,
                    ContactoId = resultado.EmprendedorId, contacto.Nombre, Vendido = ventaPersonal?.Vendido ?? 0m,
                    CantVP = ventaPersonal?.Cantidad ?? 0, VentaGrupo = resultado.ProduccionTotalRed, CantVG = cantidadGrupo,
                    NivelAlcanzado = nivelAlcanzado, Monto = decimal.ToInt32(decimal.Truncate(monto)), PorcentajeLiderazgo = porcentajeLiderazgo, CicloId = cicloId
                }, transaction);

                const string reporteSql = """
                    INSERT INTO reportesmontesion
                    (susuarioadd, dtfechaadd, susuariomod, dtfechamod, reportemontesion_id, lciclo_id, id_nro, nroascensos,
                     lcontacto_id, nivel_consolidado_mes, lpuntosmesrango, nivel_ciclo, puntosacumulados, subieron_nivel,
                     niveles_escalados, Produccion, Monto, Nivel_VME, puntos_VME, Observacion)
                    VALUES (@Usuario, NOW(), @Usuario, NOW(), @ReporteId, @CicloId, @IdNro, @NroAscensos,
                            @ContactoId, @NivelActual, 0, @NivelAlcanzado, @PuntosAcumulados, @SubioNivel,
                            @NivelesEscalados, @Produccion, @Monto, NULL, NULL, @Observacion);
                    """;
                await connection.ExecuteAsync(reporteSql, new
                {
                    Usuario = usuario, ReporteId = ++proximoReporteId, CicloId = cicloId, IdNro = ++proximoIdNro,
                    NroAscensos = subioNivel ? ++proximoAscenso : 0, ContactoId = resultado.EmprendedorId,
                    NivelActual = nivelActual, NivelAlcanzado = nivelAlcanzado, SubioNivel = subioNivel ? 1 : 0,
                    NivelesEscalados = nivelesEscalados, Produccion = resultado.ProduccionTotalRed,
                    PuntosAcumulados = resultado.ProduccionValidaTotal, Monto = monto,
                    Observacion = resultado.Beneficio?.Motivo.Length > 100 ? resultado.Beneficio.Motivo[..100] : resultado.Beneficio?.Motivo
                }, transaction);

                if (!subioNivel) continue;

                var rangoActual = niveles.FirstOrDefault(item => item.NivelId == nivelActual)?.Nombre ?? "Asesor Comercial";
                var rangoAlcanzado = niveles.First(item => item.NivelId == nivelAlcanzado).Nombre;
                const string ascensoSql = """
                    INSERT INTO t_nuevos_rangos_montesion_2025
                    (ID, lcontacto_id, Mes, NOMBRE, CI, CELU, FIJO, lpais_id, sciudad, Pais, nivel, RANGO,
                     nivel_ciclo, Rango_Alcanzado, niveles_escalados, lciclo_id)
                    VALUES (@Id, @ContactoId, @Mes, @Nombre, @Ci, @Celular, @Fijo, @PaisId, @Ciudad, @Pais, @NivelActual,
                            @RangoActual, @NivelAlcanzado, @RangoAlcanzado, @NivelesEscalados, @CicloId);
                    """;
                await connection.ExecuteAsync(ascensoSql, new
                {
                    Id = ++proximoNuevoRangoId, ContactoId = resultado.EmprendedorId, Mes = mes, contacto.Nombre, contacto.Ci,
                    contacto.Celular, contacto.Fijo, contacto.PaisId, contacto.Ciudad, contacto.Pais, NivelActual = nivelActual,
                    RangoActual = rangoActual, NivelAlcanzado = nivelAlcanzado, RangoAlcanzado = rangoAlcanzado,
                    NivelesEscalados = nivelesEscalados, CicloId = cicloId
                }, transaction);
            }

            transaction.Commit();
            salida.RegistrosPlan = resultados.Count;
            salida.RegistrosReporte = resultados.Count;
            salida.Ascensos = proximoAscenso;
            return (true, "Información Monte Sion guardada correctamente.", salida);
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId, NombreArchivo, nameof(GuardarRangosAsync), "Error al guardar rangos Monte Sion.", ex);
            return (false, $"Error al guardar rangos Monte Sion: {ex.Message}", salida);
        }
    }

    private static string Normalizar(string? texto)
    {
        var descompuesto = (texto ?? string.Empty).Trim().Normalize(NormalizationForm.FormD);
        var normalizado = string.Concat(descompuesto.Where(caracter => CharUnicodeInfo.GetUnicodeCategory(caracter) != UnicodeCategory.NonSpacingMark))
            .Normalize(NormalizationForm.FormC)
            .ToUpperInvariant();

        return normalizado switch
        {
            "LIDER" => "LEADER",
            "ZAFIRO" => "SAPPHIRE",
            "ESMERALDA" => "EMERALD",
            "DIAMANTE" => "DIAMOND",
            "EMBAJADOR REGIONAL" => "REGIONAL AMBASSADOR",
            "EMBAJADOR NACIONAL" => "NATIONAL AMBASSADOR",
            "EMBAJADOR INTERNACIONAL" => "INTERNATIONAL AMBASSADOR",
            _ => normalizado,
        };
    }

    private sealed class NivelMonteSion { public int NivelId { get; init; } public string Nombre { get; init; } = string.Empty; }
    private sealed class NivelAnteriorMonteSion { public int ContactoId { get; init; } public int Nivel { get; init; } }
    private sealed class ContactoMonteSion
    {
        public int ContactoId { get; init; }
        public string? Codigo { get; init; }
        public string Nombre { get; init; } = string.Empty;
        public string? Ci { get; init; }
        public string? Celular { get; init; }
        public string? Fijo { get; init; }
        public long PaisId { get; init; }
        public string? Ciudad { get; init; }
        public string Pais { get; init; } = string.Empty;
    }
    private sealed class VentaPersonalMonteSion { public int ContactoId { get; init; } public decimal Vendido { get; init; } public int Cantidad { get; init; } }

    public async Task<(IEnumerable<MonteSionContactoRed> Contactos, IEnumerable<MonteSionContactoRed> Evaluables, IEnumerable<MonteSionProduccionContacto> Producciones, IEnumerable<MonteSionRangoHistorico> HistorialRangos, IEnumerable<int> EmprendedoresPrimeraVenta, bool Success, string Mensaje)>
        ObtenerDatosCicloAsync(string logTransaccionId, int cicloId)
    {
        const string metodo = "ObtenerDatosCicloAsync";
        const string contactosQuery = @"
            SELECT lcontacto_id AS EmprendedorId, scodigo AS Codigo, snombrecompleto AS EmprendedorNombre,
                   dtfecharegistro AS FechaRegistro
            FROM administracioncontacto contacto
            WHERE contacto.cbaja = 0;";
        const string evaluablesQuery = @"
            SELECT
                vendedor.lcontacto_id AS EmprendedorId,
                vendedor.scodigo AS Codigo,
                vendedor.snombrecompleto AS EmprendedorNombre,
                vendedor.dtfecharegistro AS FechaRegistro
            FROM administracioncontacto vendedor
            INNER JOIN (
                SELECT lcontacto_id AS VendedorId
                FROM administracionventapersonal
                WHERE lciclo_id = @CicloId
            ) x ON x.VendedorId = vendedor.lcontacto_id
            WHERE vendedor.cbaja = 0;";
        const string produccionQuery = @"
            SELECT
                red.lasesor_id AS EmprendedorId,
                red.lcontacto_id AS VendedorId,
                red.Nivel,
                CASE
                    WHEN contrato.ltipocontrato_id IN (1, 2) THEN COALESCE(contrato.dprecio, 0)
                    ELSE 0
                END AS Produccion,
                contrato.snroventa AS NumeroVenta
            FROM red_comprimida red
            INNER JOIN administracioncontrato contrato ON contrato.lasesor_id = red.lcontacto_id
            INNER JOIN administracionciclo ciclo ON ciclo.lciclo_id = red.lciclo_id
            WHERE red.lciclo_id = @CicloId
              AND red.Nivel BETWEEN 1 AND 7
              AND contrato.dtfecha >= DATE(ciclo.dtfechainicio)
              AND contrato.dtfecha < DATE_ADD(DATE(ciclo.dtfechafin), INTERVAL 1 DAY);";
        const string historialRangosQuery = @"
            SELECT DISTINCT
                historial.lcontacto_id AS EmprendedorId,
                nivel.snombre AS Rango
            FROM reportesmontesion historial
            INNER JOIN administracionnivel nivel ON nivel.lnivel_id = historial.nivel_ciclo
            WHERE historial.lciclo_id <> @CicloId;";
        const string primeraVentaQuery = """
            SELECT venta.lcontacto_id
            FROM administracionventapersonal venta
            INNER JOIN administracioncontrato contrato ON contrato.lcontrato_id = venta.lcontrato_id
            WHERE venta.lciclo_id = @CicloId
              AND contrato.lasesor_id = venta.lcontacto_id
              AND contrato.lasesor_id > 0
            GROUP BY venta.lcontacto_id
            HAVING (
                SELECT COUNT(*)
                FROM administracioncontrato historial
                WHERE historial.lasesor_id = venta.lcontacto_id
            ) = 1;
            """;

        try
        {
            using var connection = _context.CreateConnection();
            var contactos = (await connection.QueryAsync<MonteSionContactoRed>(contactosQuery)).ToList();
            var evaluables = (await connection.QueryAsync<MonteSionContactoRed>(evaluablesQuery, new { CicloId = cicloId })).ToList();
            var producciones = (await connection.QueryAsync<MonteSionProduccionContacto>(produccionQuery, new { CicloId = cicloId })).ToList();
            var historialRangos = (await connection.QueryAsync<MonteSionRangoHistorico>(historialRangosQuery, new { CicloId = cicloId })).ToList();
            var emprendedoresPrimeraVenta = (await connection.QueryAsync<int>(primeraVentaQuery, new { CicloId = cicloId })).ToList();
            _log.Info(logTransaccionId, NombreArchivo, metodo,
                $"Datos MonteSion obtenidos. Ciclo: {cicloId}, emprendedores evaluables: {evaluables.Count}.");

            return (contactos, evaluables, producciones, historialRangos, emprendedoresPrimeraVenta, true, "Producción de red obtenida correctamente.");
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId, NombreArchivo, metodo, "Error al obtener producción de red.", ex);
            return (Enumerable.Empty<MonteSionContactoRed>(), Enumerable.Empty<MonteSionContactoRed>(), Enumerable.Empty<MonteSionProduccionContacto>(), Enumerable.Empty<MonteSionRangoHistorico>(), Enumerable.Empty<int>(), false,
                $"Error al obtener producción de red: {ex.Message}");
        }
    }

}
