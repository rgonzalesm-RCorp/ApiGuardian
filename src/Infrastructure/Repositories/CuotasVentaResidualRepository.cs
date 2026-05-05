using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using Newtonsoft.Json;
using Query.Cnx;

namespace ApiGuardian.Infrastructure.Repositories;

public class CuotasVentaResidualRepository : ICuotasVentaResidualRepository
{
    private readonly DapperContext _context;
    private readonly DapperContextSqlServer  _contextSqlServer;
    private readonly ILogService _log;
    private string NOMBREARCHIVO = "ControlProcesoRepository.CS";
    public CuotasVentaResidualRepository(DapperContext context, ILogService log, DapperContextSqlServer contextSqlServer)
    {
        _context = context;
        _log = log;
        _contextSqlServer = contextSqlServer;
    }
    public async Task<(bool Success, string Mensaje, IEnumerable<VentaResidual> ListadoCuotasVentasResidual)> GetCuotasVentasResidual(string LogTransaccionId, string Usuario, string Inicio, string Fin)
    {
        string query = ScriptCnx.GetQueryVentaResidual;
         string nombreMetodo = "GetObetenerContactoVentasMes()";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");
        try
        {
            using var connection = _contextSqlServer.CreateConnection();

            var Lista = await connection.QueryAsync<VentaResidual>(query, new {Inicio, Fin});

            bool success = true;
            string mensaje = success ? "cuotas ventas residual obtenidos correctamente." : "No se encontraron cuotas ventas residual.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}]");

            return ( success, mensaje, Lista ?? new List<VentaResidual>());
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al obtener los tipos de descuento: {ex.Message}", Enumerable.Empty<VentaResidual>());
        }
    }
}
