using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using System.Text.Json;

namespace ApiGuardian.Infrastructure.Repositories;

public class AdministracionSemanaRepository : IAdministracionSemanaRepository
{
    private readonly DapperContext _context;
    private readonly ILogService _log;
    private readonly string NOMBREARCHIVO = "AdministracionSemanaRepository.cs";

    public AdministracionSemanaRepository(DapperContext context, ILogService log)
    {
        _context = context;
        _log = log;
    }

    // ======================================
    public async Task<(IEnumerable<AdministracionSemana> Semana, bool Success, string Mensaje)> GetSemana(string LogTransaccionId)
    {
        string metodo = "GetSemana()";
        const string query = @"
            SELECT
                idsemana AS LSemanaId,
                UPPER(lnombre)   AS SNombre,
                susuarioadd AS Usuario
            FROM administracionsemana
            ORDER BY idsemana DESC;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo, $"Inicio consulta. Script: {query}");

        try
        {
            using var con = _context.CreateConnection();
            var lista = await con.QueryAsync<AdministracionSemana>(query);

            bool success = lista != null && lista.Any();
            string mensaje = success ? "Semanas obtenidas correctamente." : "No se encontraron registros.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo, $"Fin consulta. Success={success}. Mensaje: {mensaje}");

            return (lista ?? Enumerable.Empty<AdministracionSemana>(), success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, metodo, "Error al obtener semanas", ex);
            return (Enumerable.Empty<AdministracionSemana>(), false, ex.Message);
        }
    }

    // ======================================
    public async Task<(IEnumerable<AdministracionSemana> Semana, bool Success, string Mensaje, int Total)>
        GetSemanaPagination(string LogTransaccionId, int page, int pageSize, string? search)
    {
        string metodo = "GetSemanaPagination()";
        const string query = @"
            SELECT
                idsemana AS LSemanaId,
                UPPER(lnombre)   AS SNombre,
                susuarioadd AS Usuario
            FROM administracionsemana
            ORDER BY idsemana DESC
            LIMIT @pageSize OFFSET @page;
        ";
        const string countQuery = @"SELECT COUNT(*) FROM administracionsemana;";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo, $"Inicio paginación. Script: {query} Parámetros: page={page}, pageSize={pageSize}, search={search}");

        try
        {
            using var con = _context.CreateConnection();

            var lista = await con.QueryAsync<AdministracionSemana>(query, new { page, pageSize });
            var total = await con.ExecuteScalarAsync<int>(countQuery);

            bool success = lista != null && lista.Any();
            string mensaje = success ? "Semanas paginadas obtenidas." : "No se encontraron registros.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo, $"Fin paginación. Total={total}, Success={success}");

            return (lista ?? Enumerable.Empty<AdministracionSemana>(), success, mensaje, total);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, metodo, "Error en paginación", ex);
            return (Enumerable.Empty<AdministracionSemana>(), false, ex.Message, 0);
        }
    }

    // ======================================
    public async Task<(bool Success, string Mensaje)> GuardarSemana(string LogTransaccionId, AdministracionSemana data)
    {
        string metodo = "GuardarSemana()";

        const string nextIdQuery = @"SELECT IFNULL(MAX(idsemana),0)+1 FROM administracionsemana;";
        const string insertQuery = @"
            INSERT INTO administracionsemana
            (idsemana, lnombre, susuarioadd, dtfechaadd, susuariomod, dtfechamod)
            VALUES
            (@nextId, @SNombre, @Usuario, NOW(), @Usuario, NOW());
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo, $"Inicio inserción. Data: {JsonSerializer.Serialize(data)} ScriptNextId: {nextIdQuery}");

        try
        {
            using var con = _context.CreateConnection();
            var nextId = await con.ExecuteScalarAsync<int>(nextIdQuery);

            var rows = await con.ExecuteAsync(insertQuery, new
            {
                nextId,
                data.SNombre,
                data.Usuario
            });

            bool success = rows > 0;
            string mensaje = success ? "Registro insertado correctamente." : "No se insertó el registro.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo, $"Fin inserción. Success={success}. Mensaje:{mensaje}");

            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, metodo, "Error insertando semana", ex);
            return (false, ex.Message);
        }
    }

    // ======================================
    public async Task<(bool Success, string Mensaje)> ModificarSemana(string LogTransaccionId, AdministracionSemana data)
    {
        string metodo = "ModificarSemana()";
        const string query = @"
            UPDATE administracionsemana
            SET lnombre = @SNombre,
                susuariomod = @Usuario,
                dtfechamod = NOW()
            WHERE idsemana = @LSemanaId;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo, $"Inicio actualización. Data: {JsonSerializer.Serialize(data)} Script: {query}");

        try
        {
            using var con = _context.CreateConnection();
            var rows = await con.ExecuteAsync(query, data);

            bool success = rows > 0;
            string mensaje = success ? "Registro actualizado correctamente." : "No se encontró el registro a actualizar.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo, $"Fin actualización. Success={success}, Rows={rows}");

            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, metodo, "Error actualizando semana", ex);
            return (false, ex.Message);
        }
    }

    // ======================================
    public async Task<(bool Success, string Mensaje)> EliminarSemana(string LogTransaccionId, int LSemanaId)
    {
        string metodo = "EliminarSemana()";
        const string query = @"DELETE FROM administracionsemana WHERE idsemana = @LSemanaId;";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo, $"Inicio eliminación. Script: {query} Param: LSemanaId={LSemanaId}");

        try
        {
            using var con = _context.CreateConnection();
            var rows = await con.ExecuteAsync(query, new { LSemanaId });

            bool success = rows > 0;
            string mensaje = success ? "Registro eliminado correctamente." : "No se pudo eliminar el registro.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo, $"Fin eliminación. Success={success}");

            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, metodo, "Error eliminando semana", ex);
            return (false, ex.Message);
        }
    }
}
