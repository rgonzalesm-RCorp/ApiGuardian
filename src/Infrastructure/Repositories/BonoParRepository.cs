using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using Newtonsoft.Json;
using Query.Grd;

namespace ApiGuardian.Infrastructure.Repositories;

public class BonoParRepository : IBonoParRepository
{
    private readonly DapperContext _context;
    private readonly ILogService _log;
    private string NOMBREARCHIVO = "BonoParRepository.CS";
    public BonoParRepository(DapperContext context, ILogService log)
    {
        _context = context;
        _log = log;
    }
    public async Task<(IEnumerable<ItemBonoPar> Data, bool Success, string Mensaje)> GetBonoPar(string LogTransaccionId, string Usuario, string Inicio, string Fin)
    {
        string nombreMetodo = "GetBonoPar()";

        string query = ScriptGrd.QueryBonoPar();
        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [QueryBonoPar script: {query} Usuario: {Usuario}]");

        try
        {
            using var connection = _context.CreateConnection();

            var Lista = await connection.QueryAsync<ItemBonoPar>(query, new {Inicio, Fin});
            string LContratoIdString = string.Join(",", 
                Lista.Select(x => x.LContratoId)
                    .Where(x => !string.IsNullOrEmpty(x))
            );
            query = ScriptGrd.QueryDetalleBonoPar(LContratoIdString);
            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"QueryDetalleBonoPar [script: {query} Usuario: {Usuario}]");

            var ListaDetalleBonoPar =  await connection.QueryAsync<ItemBonoParDetalle>(query);
            foreach (var item in Lista)
            {
                item.ListaDetalleBonoPar = ListaDetalleBonoPar.Where(x => x.LContactoGanadorId == item.LContctoGanadorId).ToList();
            }

            bool success = true;
            string mensaje = success ? "Tipos de descuento obtenidos correctamente." : "No se encontraron tipos de descuento.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, Usuario: {Usuario}]");

            return (Lista ?? Enumerable.Empty<ItemBonoPar>(), success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (Enumerable.Empty<ItemBonoPar>(),false, $"Error al obtener la comision de bono par: {ex.Message}");
        }
    }

}
