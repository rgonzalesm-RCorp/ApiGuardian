using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using Newtonsoft.Json;

namespace ApiGuardian.Infrastructure.Repositories;

public class RedesRepository : IRedesRepository
{
    private readonly DapperContext _context;
    private readonly ILogService _log;
    private string NOMBREARCHIVO = "ControlProcesoRepository.CS";
    public RedesRepository(DapperContext context, ILogService log)
    {
        _context = context;
        _log = log;
    }
    public async Task<(bool Success, string Mensaje, IEnumerable<ItemContactoActivo> ListadoContactosActivos)> GetObetenerContactoVentasMes(string LogTransaccionId, string Usuario, string Inicio, string Fin)
    {
         string nombreMetodo = "GetObetenerContactoVentasMes()";

        string query = $@" select DISTINCT lcontacto_id  LContactoId, lasesor_id  LPatrocinadorId from administracioncontrato where dtfecha BETWEEN @Inicio and @Fin ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var Lista = await connection.QueryAsync<ItemContactoActivo>(query, new {Inicio, Fin});

            bool success = true;
            string mensaje = success ? "Tipos de descuento obtenidos correctamente." : "No se encontraron tipos de descuento.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}]");

            return ( success, mensaje, Lista ?? new List<ItemContactoActivo>());
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al obtener los tipos de descuento: {ex.Message}", Enumerable.Empty<ItemContactoActivo>());
        }
    }
    public async Task<(bool Success, string Mensaje, int PatrocinadorId)> GetObetenerPatrocinador(string LogTransaccionId, string Usuario, int LContactoId)
    {
  
        string nombreMetodo = "GetObetenerPatrocinador()";

        string query = $@" select DISTINCT lpatrocinante_id LContactoId from administracioncontacto where lcontacto_id = @LContactoId";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var PatrocinadorId = await connection.ExecuteScalarAsync<int>(query, new {LContactoId});

            bool success = true;
            string mensaje = success ? "PatrocinadoId obtenidos correctamente." : "No se encontraron el PatrocinadorId.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}]");

            return ( success, mensaje, PatrocinadorId);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al obtener el PatrocinadorId: {ex.Message}", 0);
        }
    }
}
