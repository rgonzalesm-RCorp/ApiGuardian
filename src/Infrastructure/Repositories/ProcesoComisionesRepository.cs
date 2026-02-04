using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using Newtonsoft.Json;

namespace ApiGuardian.Infrastructure.Repositories;

public class ProcesoComisionesRepository : IProcesoComisionesRepository
{
    private readonly DapperContext _context;
    private readonly ILogService _log;
    private string NOMBREARCHIVO = "ProcesoComisionesRepository.CS";
    public ProcesoComisionesRepository(DapperContext context, ILogService log)
    {
        _context = context;
        _log = log;
    }
    public async Task<(ItemProcesoJon Data, bool Success, string Mensaje)> GetProceso(string LogTransaccionId, string Proceso)
    {
        string nombreMetodo = "GetProceso()";

        string query = $@"
            select 
            J.proceso Proceso
            , J.estado Estado
            , C.dtfechainicio Inicio
            , C.dtfechafin Fin
            from administracionjob J
            inner join administracionciclo C on C.lciclo_id = J.lciclo_id and J.estado = 1 
            WHERE J.proceso = '{Proceso}'
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var procesoJob = await connection.QueryFirstOrDefaultAsync<ItemProcesoJon>(query);

            bool success = true;
            string mensaje = success ? "Tipos de descuento obtenidos correctamente." : "No se encontraron tipos de descuento.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, tiposDescuento:{JsonConvert.SerializeObject(procesoJob, Formatting.Indented)}]");

            return (procesoJob, success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (new ItemProcesoJon(), false, $"Error al obtener los tipos de descuento: {ex.Message}");
        }
    }

}
