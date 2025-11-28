using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using Newtonsoft.Json;

namespace ApiGuardian.Infrastructure.Repositories;

public class AdministracionBuscarAsesorRepository : IAdministracionBuscarAsesorRepository
{
    private readonly DapperContext _context;
    private readonly ILogService _log;
    private string NOMBREARCHIVO = "AdministracionBuscarAsesorRepository.cs";
    public AdministracionBuscarAsesorRepository(DapperContext context,ILogService log)
    {
        _context = context;
        _log = log;
    }

    public async Task<(IEnumerable<ListaBuscarAsesor> DataFijos, IEnumerable<ListaBuscarAsesor> DataActivos, bool Success, string Mensaje)> GetAsesoreSieteNiveles(string LogTransaccionId, int lContactoId)
    {
        string nombreMetodo = "GetAsesoreSieteNiveles()";

        const string queryFijos = @"CALL sp_GetPadresHasta7Fijos(@LContactoId);";
        const string queryActivos = @"CALL sp_GetPadresHasta7Activos(@LContactoId);";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [queryFijos: {queryFijos}, queryActivos: {queryActivos}]");

        try
        {
            using var connection = _context.CreateConnection();

            var resultFijos = await connection.QueryAsync<ListaBuscarAsesor>(queryFijos, new { LContactoId = lContactoId });
            var resultActivos = await connection.QueryAsync<ListaBuscarAsesor>(queryActivos, new { LContactoId = lContactoId });

            bool success = (resultFijos != null && resultFijos.Any()) || (resultActivos != null && resultActivos.Any());
            string mensaje = success ? "Datos obtenidos correctamente." : "No se encontraron registros.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, dataFijos:{JsonConvert.SerializeObject(resultFijos, Formatting.Indented)}, dataActivos:{JsonConvert.SerializeObject(resultActivos, Formatting.Indented)}]");

            return (resultFijos ?? Enumerable.Empty<ListaBuscarAsesor>(), resultActivos ?? Enumerable.Empty<ListaBuscarAsesor>(), success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (Enumerable.Empty<ListaBuscarAsesor>(), Enumerable.Empty<ListaBuscarAsesor>(), false, $"Error al obtener asesores hasta 7 niveles: {ex.Message}");
        }
    }

 
}
