using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using Newtonsoft.Json;
using System.Text;
using Query.Cnx;
using Microsoft.Extensions.Configuration;

namespace ApiGuardian.Infrastructure.Repositories;

public class CasosEspecialesRepository : ICasosEspecialesRepository
{
    private readonly DapperContext _context;
    private readonly DapperContextSqlServer _contextSql;
    private readonly ILogService _log;
    private string NOMBREARCHIVO = "CasosEspecialesRepository.CS";
    private readonly IConfiguration _configuration;

    public CasosEspecialesRepository(DapperContext context, ILogService log, IConfiguration configuration, DapperContextSqlServer contextSql)
    {
        _context = context;
        _log = log;
        _configuration = configuration;
        _contextSql = contextSql;
    }
    public async Task<(IEnumerable<ItemVentaCnx> VentasCasosEspeciales, bool Success, string Mensaje)> GetVentasCasosEspeciales(string LogTransaccionId,string Usuario, string Inicio, string Fin)
    {
        string nombreMetodo = "GetVentasCasosEspeciales()";

        var query = ScriptCnx.QueryVentaCnx(_configuration, true);

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}, Usuario:{Usuario}, Inicio:{Inicio}], Fin:{Fin}");

        try
        {
            using var connection = _contextSql.CreateConnection();

            var Lista = await connection.QueryAsync<ItemVentaCnx>(query, new {Inicio, Fin});

            bool success = true;
            string mensaje = success ? "Casos especiales obtenidos correctamente." : "No se encontraron casos especiales.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Fin de metodo [mensaje: {mensaje}]");

            return (Lista ?? Enumerable.Empty<ItemVentaCnx>(), success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (Enumerable.Empty<ItemVentaCnx>(), false, $"Error al obtener los casos especiales: {ex.Message}");
        } 
    }

}
