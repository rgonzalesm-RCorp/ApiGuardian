using ApiGuardian.Domain.DTO;
using Dapper;

namespace ApiGuardian.Infrastructure.Repositories.Simulacion;

// Monte Sion de la simulación. SQL copiado de MonteSionRepository.GuardarRangosAsync (R2).
// Diferencias:
//  - En vez de rechazar el ciclo si ya tiene datos, borra los datos Monte Sion del ciclo y los vuelve a insertar
//    (el RESET ya los borra; aquí se repite para que el paso se pueda ejecutar solo).
//  - No ejecuta UPDATE administracioncontacto.lnivel_id: la simulación no cambia el rango real del contacto.
//  - Sin resultados no es error: deja el ciclo sin datos Monte Sion y devuelve OK.
public sealed partial class SimulacionRepositorio
{
    private static readonly string[] SqlDeleteMonteSion =
    [
        "DELETE FROM t_nuevos_rangos_montesion_2025 WHERE lciclo_id = @LCicloId;",
        "DELETE FROM reportesmontesion WHERE lciclo_id = @LCicloId;",
        "DELETE FROM t_plan_montesion WHERE lciclo_id = @LCicloId;",
    ];

    private const string SqlMonteSionNiveles = "SELECT lnivel_id AS NivelId, snombre AS Nombre FROM administracionnivel;";

    private const string SqlMonteSionContactos = """
        SELECT contacto.lcontacto_id AS ContactoId, contacto.scodigo AS Codigo,
               contacto.snombrecompleto AS Nombre, contacto.scedulaidentidad AS Ci,
               contacto.stelefonomovil AS Celular, contacto.stelefonofijo AS Fijo,
               contacto.lpais_id AS PaisId, contacto.sciudad AS Ciudad, pais.snombre AS Pais
        FROM administracioncontacto contacto
        INNER JOIN basepais pais ON pais.lpais_id = contacto.lpais_id
        WHERE contacto.lcontacto_id IN @ContactoIds;
        """;

    private const string SqlMonteSionVentasPersonales = """
        SELECT grupo.lasesor_id AS ContactoId,
               COALESCE(SUM(CASE WHEN grupo.lgeneracion = 0 THEN contrato.dprecio ELSE 0 END), 0) AS Vendido,
               SUM(CASE WHEN grupo.lgeneracion = 0 THEN 1 ELSE 0 END) AS Cantidad
        FROM administracionventagrupo grupo
        INNER JOIN administracioncontrato contrato ON contrato.lcontrato_id = grupo.lcontrato_id
        WHERE grupo.lciclo_id = @CicloId AND grupo.lasesor_id IN @ContactoIds
        GROUP BY grupo.lasesor_id;
        """;

    private const string SqlMonteSionInsertPlan = """
        INSERT INTO t_plan_montesion
        (susuarioadd, dtfechaadd, susuariomod, dtfechamod, id_plan_montesion, scodigo, Nivel, lcontacto_id, Nombre,
         Vendido, CantVP, Venta_Grupo, CantVG, Nivel_Conseguido, Monto_Pagar, Porcentaje_Liderazgo, lciclo_id)
        VALUES (@Usuario, NOW(), @Usuario, NOW(), @Id, @Codigo, @NivelActual, @ContactoId, @Nombre,
                @Vendido, @CantVP, @VentaGrupo, @CantVG, @NivelAlcanzado, @Monto, @PorcentajeLiderazgo, @CicloId);
        """;

    private const string SqlMonteSionInsertReporte = """
        INSERT INTO reportesmontesion
        (susuarioadd, dtfechaadd, susuariomod, dtfechamod, reportemontesion_id, lciclo_id, id_nro, nroascensos,
         lcontacto_id, nivel_consolidado_mes, lpuntosmesrango, nivel_ciclo, puntosacumulados, subieron_nivel,
         niveles_escalados, Produccion, Monto, Nivel_VME, puntos_VME, Observacion)
        VALUES (@Usuario, NOW(), @Usuario, NOW(), @ReporteId, @CicloId, @IdNro, @NroAscensos,
                @ContactoId, @NivelActual, 0, @NivelAlcanzado, @PuntosAcumulados, @SubioNivel,
                @NivelesEscalados, @Produccion, @Monto, NULL, NULL, @Observacion);
        """;

    private const string SqlMonteSionInsertAscenso = """
        INSERT INTO t_nuevos_rangos_montesion_2025
        (ID, lcontacto_id, Mes, NOMBRE, CI, CELU, FIJO, lpais_id, sciudad, Pais, nivel, RANGO,
         nivel_ciclo, Rango_Alcanzado, niveles_escalados, lciclo_id)
        VALUES (@Id, @ContactoId, @Mes, @Nombre, @Ci, @Celular, @Fijo, @PaisId, @Ciudad, @Pais, @NivelActual,
                @RangoActual, @NivelAlcanzado, @RangoAlcanzado, @NivelesEscalados, @CicloId);
        """;

    public async Task<(bool Success, string Mensaje, int Rangos, int Ascensos)> ReemplazarMonteSionAsync(
        string logId, int cicloId, string usuario, IReadOnlyCollection<MonteSionResultadoRango> resultados)
    {
        const string metodo = nameof(ReemplazarMonteSionAsync);
        try
        {
            using var connection = _context.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            foreach (var sql in SqlDeleteMonteSion)
                await connection.ExecuteAsync(sql, new { LCicloId = cicloId }, transaction);

            if (resultados.Count == 0)
            {
                transaction.Commit();
                return (true, "Ningún emprendedor califica a un rango Monte Sion en el ciclo.", 0, 0);
            }

            var contactoIds = resultados.Select(item => item.EmprendedorId).Distinct().ToArray();
            var niveles = (await connection.QueryAsync<NivelMonteSionSimulacion>(SqlMonteSionNiveles, transaction: transaction)).ToList();
            var nivelesPorId = niveles.ToDictionary(nivel => nivel.NivelId);
            var contactos = (await connection.QueryAsync<ContactoMonteSionSimulacion>(SqlMonteSionContactos, new { ContactoIds = contactoIds }, transaction))
                .ToDictionary(item => item.ContactoId);
            var ventasPersonales = (await connection.QueryAsync<VentaPersonalMonteSionSimulacion>(SqlMonteSionVentasPersonales, new { CicloId = cicloId, ContactoIds = contactoIds }, transaction))
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
                return (false, $"Existen niveles calculados no válidos para los contactos: {string.Join(", ", nivelesInvalidos)}.", 0, 0);
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
                var monto = resultado.Beneficio?.BonoAPagar ?? 0m;
                var porcentajeLiderazgo = resultado.RangoEvaluado?.PorcentajeLiderazgo ?? 0m;

                await connection.ExecuteAsync(SqlMonteSionInsertPlan, new
                {
                    Usuario = usuario,
                    Id = ++proximoPlanId,
                    contacto.Codigo,
                    NivelActual = nivelActual,
                    ContactoId = resultado.EmprendedorId,
                    contacto.Nombre,
                    Vendido = ventaPersonal?.Vendido ?? 0m,
                    CantVP = ventaPersonal?.Cantidad ?? 0,
                    VentaGrupo = resultado.ProduccionTotalRed,
                    CantVG = cantidadGrupo,
                    NivelAlcanzado = nivelAlcanzado,
                    Monto = decimal.ToInt32(decimal.Truncate(monto)),
                    PorcentajeLiderazgo = porcentajeLiderazgo,
                    CicloId = cicloId,
                }, transaction);

                await connection.ExecuteAsync(SqlMonteSionInsertReporte, new
                {
                    Usuario = usuario,
                    ReporteId = ++proximoReporteId,
                    CicloId = cicloId,
                    IdNro = ++proximoIdNro,
                    NroAscensos = subioNivel ? ++proximoAscenso : 0,
                    ContactoId = resultado.EmprendedorId,
                    NivelActual = nivelActual,
                    NivelAlcanzado = nivelAlcanzado,
                    SubioNivel = subioNivel ? 1 : 0,
                    NivelesEscalados = nivelesEscalados,
                    Produccion = resultado.ProduccionTotalRed,
                    PuntosAcumulados = resultado.ProduccionValidaTotal,
                    Monto = monto,
                    Observacion = resultado.Beneficio?.Motivo.Length > 100 ? resultado.Beneficio.Motivo[..100] : resultado.Beneficio?.Motivo,
                }, transaction);

                // Sin UPDATE administracioncontacto: el ascenso queda registrado, el rango real no cambia.
                if (!subioNivel) continue;

                var rangoActual = niveles.FirstOrDefault(item => item.NivelId == nivelActual)?.Nombre ?? "Asesor Comercial";
                var rangoAlcanzado = niveles.First(item => item.NivelId == nivelAlcanzado).Nombre;
                await connection.ExecuteAsync(SqlMonteSionInsertAscenso, new
                {
                    Id = ++proximoNuevoRangoId,
                    ContactoId = resultado.EmprendedorId,
                    Mes = mes,
                    contacto.Nombre,
                    contacto.Ci,
                    contacto.Celular,
                    contacto.Fijo,
                    contacto.PaisId,
                    contacto.Ciudad,
                    contacto.Pais,
                    NivelActual = nivelActual,
                    RangoActual = rangoActual,
                    NivelAlcanzado = nivelAlcanzado,
                    RangoAlcanzado = rangoAlcanzado,
                    NivelesEscalados = nivelesEscalados,
                    CicloId = cicloId,
                }, transaction);
            }

            transaction.Commit();
            return (true, "Información Monte Sion de la simulación guardada correctamente.", resultados.Count, proximoAscenso);
        }
        catch (Exception ex)
        {
            _log.Error(logId, NOMBRE_ARCHIVO, metodo, $"Error al guardar Monte Sion de la simulación: {ex.Message}");
            return (false, $"Error al guardar Monte Sion de la simulación: {ex.Message}", 0, 0);
        }
    }

    private sealed class NivelMonteSionSimulacion { public int NivelId { get; init; } public string Nombre { get; init; } = string.Empty; }
    private sealed class ContactoMonteSionSimulacion
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
    private sealed class VentaPersonalMonteSionSimulacion { public int ContactoId { get; init; } public decimal Vendido { get; init; } public int Cantidad { get; init; } }
}
