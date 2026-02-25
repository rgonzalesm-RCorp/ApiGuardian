using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using Newtonsoft.Json;

namespace ApiGuardian.Infrastructure.Repositories;

public class AdministracionVentaPersonalRepository : IAdministracionVentaPersonalRepository
{
    private readonly DapperContext _context;
    private readonly ILogService _log;
    private string NOMBREARCHIVO = "AdministracionVentaPersonalRepository.cs";
    public AdministracionVentaPersonalRepository(DapperContext context, ILogService log)
    {
        _context = context;
        _log = log;
    }
    public async Task<( bool Success, string Mensaje)> InsertVentaPersonal(string LogTransaccionId, List<AdministracionVentaPersonal> data)
    {
        string nombreMetodo = "InsertVentaPersonal()";
        const string nextIdQuery = @"SELECT IFNULL(MAX(lventapersonal_id), 0)
            FROM administracionventapersonal;
        ";

        const string query = @"INSERT INTO administracionventapersonal (
                    susuarioadd,
                    dtfechaadd,
                    susuariomod,
                    dtfechamod,
                    lventapersonal_id,
                    dtfechacalculo,
                    lciclo_id,
                    lcontacto_id,
                    dpreciolote,
                    dporcentajecomision,
                    dcomision,
                    lcontrato_id,
                    ddescuentoatencion,
                    ddescuentotramite,
                    ddescuentoreferido,
                    latencion_id,
                    ltramite_id,
                    lreferido_id,
                    ddescuentolote,
                    snotadescuentolote,
                    cventapagada,
                    dtfechapago,
                    lnrosemana,
                    dporcentajeretencion,
                    dmontoretencion,
                    cpresentafactura,
                    dtotaapagar,
                    lsemana_id
                )
                VALUES (
                    @susuarioadd,
                    @dtfechaadd,
                    @susuariomod,
                    @dtfechamod,
                    @lventapersonal_id,
                    @dtfechacalculo,
                    @lciclo_id,
                    @lcontacto_id,
                    @dpreciolote,
                    @dporcentajecomision,
                    @dcomision,
                    @lcontrato_id,
                    @ddescuentoatencion,
                    @ddescuentotramite,
                    @ddescuentoreferido,
                    @latencion_id,
                    @ltramite_id,
                    @lreferido_id,
                    @ddescuentolote,
                    @snotadescuentolote,
                    @cventapagada,
                    @dtfechapago,
                    @lnrosemana,
                    @dporcentajeretencion,
                    @dmontoretencion,
                    @cpresentafactura,
                    @dtotaapagar,
                    @lsemana_id
                );
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();
            var nextId = await connection.ExecuteScalarAsync<int>(nextIdQuery);

            foreach (var item in data)
            {
                nextId++;
                item.lventapersonal_id = nextId;
            }

            var rows = await connection.ExecuteAsync(query, data);

            bool success = rows > 0;
            string mensaje = success ? "Comisiones registrados correctamente." : "No se realizó el guardado.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, rowsAffected:{rows}, data:{JsonConvert.SerializeObject(data, Formatting.Indented)}]");

            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al insertar las comisiones: {ex.Message}");
        }
    }

}
