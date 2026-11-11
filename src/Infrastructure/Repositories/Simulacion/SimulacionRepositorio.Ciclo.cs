using Dapper;

namespace ApiGuardian.Infrastructure.Repositories.Simulacion;

// Lectura del ciclo habilitado para simulación y abierto. Solo SELECT.
public sealed partial class SimulacionRepositorio
{
    public sealed class CicloReferencia
    {
        public int LCicloId { get; set; }
        public string Inicio { get; set; } = string.Empty;
        public string Fin { get; set; } = string.Empty;
    }

    private const string SqlUltimoCiclo = @"
        SELECT lciclo_id AS LCicloId,
               DATE_FORMAT(dtfechainicio, '%Y%m%d') AS Inicio,
               DATE_FORMAT(dtfechafin, '%Y%m%d') AS Fin
        FROM administracionciclo
        WHERE simulacion = 1
          AND lestado = 0
        ORDER BY lciclo_id DESC
        LIMIT 1;";

    public async Task<CicloReferencia?> ObtenerUltimoCicloAsync(string logId)
    {
        const string metodo = nameof(ObtenerUltimoCicloAsync);
        try
        {
            using var connection = _context.CreateConnection();
            var ciclo = await connection.QueryFirstOrDefaultAsync<CicloReferencia>(SqlUltimoCiclo);

            if (ciclo is null || string.IsNullOrWhiteSpace(ciclo.Inicio) || string.IsNullOrWhiteSpace(ciclo.Fin))
            {
                _log.Warning(logId, NOMBRE_ARCHIVO, metodo, "No hay ciclo con fechas configuradas.");
                return null;
            }

            return ciclo;
        }
        catch (Exception ex)
        {
            _log.Error(logId, NOMBRE_ARCHIVO, metodo, $"Error al leer el ciclo de referencia: {ex.Message}");
            return null;
        }
    }
}
