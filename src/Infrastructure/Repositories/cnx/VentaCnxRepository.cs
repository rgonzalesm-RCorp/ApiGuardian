using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using Newtonsoft.Json;
using Query.Cnx;
using Microsoft.Extensions.Configuration;

namespace ApiGuardian.Infrastructure.Repositories;

public class VentaCnxRepository : IVentasCnxRepository
{
    private readonly DapperContextSqlServer _context;
    private readonly ILogService _log;
    private readonly IConfiguration _configuration;
    private string NOMBREARCHIVO = "AdministracionBancoRepository.cs";
    public VentaCnxRepository(DapperContextSqlServer context, ILogService log, IConfiguration configuration)
    {
        _context = context;
        _log = log;
        _configuration = configuration;
    }
    public async Task<(IEnumerable<ItemVentaCnx> Data, bool Success, string Mensaje)> GetVentaCnx(string LogTransaccionId, string inicio, string fin)
    {
        string nombreMetodo = "GetVentaCnx()";

        var query = ScriptCnx.QueryVentaCnx(_configuration);

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var data = await connection.QueryAsync<ItemVentaCnx>(query.ToString());

            bool success = data != null && data.Any();
            string mensaje = success ? "Datos obtenidos correctamente." : "No se encontraron registros.";

            //_log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Fin de metodo [mensaje: {mensaje}, data:{JsonConvert.SerializeObject(data, Formatting.Indented)}]");

            return (data ?? Enumerable.Empty<ItemVentaCnx>(), success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (Enumerable.Empty<ItemVentaCnx>(), false, $"Error al obtener monedas: {ex.Message}");
        }
        
    }

    public async Task<(ItemVentaCnx Data, bool Success, string Mensaje)> GetClienteDocId(string LogTransaccionId, string docId)
    {
        string nombreMetodo = "GetClienteDocId()";

        var query = ScriptCnx.QueryCllienteDocId();

        //_log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var data = await connection.QueryFirstOrDefaultAsync<ItemVentaCnx>(query, new{docId});

            return (data, true, "Consulta realizada correctamente.");
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            ItemVentaCnx  d =  new ItemVentaCnx();
            return (d, false, "Error al consultar contactos.");
        }
    }
}
