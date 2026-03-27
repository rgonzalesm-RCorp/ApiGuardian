using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using Newtonsoft.Json;

namespace ApiGuardian.Infrastructure.Repositories;

public class AdministracionVentaGrupoRepository : IAdministracionVentaGrupoRepository
{
    private readonly DapperContext _context;
    private readonly ILogService _log;
    private string NOMBREARCHIVO = "AdministracionVentaPersonalRepository.cs";
    public AdministracionVentaGrupoRepository(DapperContext context, ILogService log)
    {
        _context = context;
        _log = log;
    }
    public async Task<( bool Success, string Mensaje)> InsertAdministracionVentaGrupo(string LogTransaccionId, List<ItemVentaGrupo> data)
    {
        string nombreMetodo = "InsertAdministracionVentaGrupo()";
        const string nextIdQuery = @"SELECT IFNULL(MAX(lventagrupo_id), 0)
            FROM administracionventagrupo;
        ";

        const string query = @"INSERT INTO administracionventagrupo
                                (
                                    susuarioadd,
                                    dtfechaadd,
                                    susuariomod,
                                    dtfechamod,
                                    lventagrupo_id,
                                    dtfechacalculo,
                                    lciclo_id,
                                    lcontacto_id,
                                    lgeneracion,
                                    lasesor_id,
                                    dporcentajecomision,
                                    dcomision,
                                    dventapersonal,
                                    cestado,
                                    dventapersonalinicial,
                                    lcontrato_id,
                                    lnrosemana,
                                    lsemana_id
                                )
                                VALUES
                                (
                                    @usuario,
                                    NOW(),
                                    @usuario,
                                    NOW(),
                                    @lventagrupo_id,
                                    NOW(),
                                    @lciclo_id,
                                    @lcontacto_id,
                                    @lgeneracion,
                                    @lasesor_id,
                                    @dporcentajecomision,
                                    @dcomision,
                                    @dventapersonal,
                                    1,
                                    @dventapersonalinicial,
                                    @lcontrato_id,
                                    @lnrosemana,
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
                item.lventagrupo_id = nextId;
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
