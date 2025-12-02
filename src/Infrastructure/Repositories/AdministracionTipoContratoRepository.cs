using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using Newtonsoft.Json;

namespace ApiGuardian.Infrastructure.Repositories;

public class AdministracionTipoContratoRepository : IAdministracionTipoContratoRepository
{
    private readonly DapperContext _context;
    private readonly ILogService _log;
    private readonly string NOMBREARCHIVO = "AdministracionTipoContratoRepository.cs";

    public AdministracionTipoContratoRepository(DapperContext context, ILogService log)
    {
        _context = context;
        _log = log;
    }

    // ======================================
    // GET LISTADO
    // ======================================
    public async Task<(IEnumerable<AdministracionTipoContratoABM> TipoContrato, bool Success, string Mensaje)>
        GetTipoContrato(string LogTransaccionId)
    {
        string metodo = "GetTipoContrato()";

        const string query = @"
            SELECT 
                ltipocontrato_id AS LTipoContratoId,
                UPPER(snombre) AS SNombre,
                susuarioadd AS Usuario
            FROM administraciontipocontrato
            ORDER BY ltipocontrato_id DESC;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo, $"Inicio [script:{query}]");

        try
        {
            using var con = _context.CreateConnection();
            var lista = await con.QueryAsync<AdministracionTipoContratoABM>(query);

            bool success = lista.Any();
            string mensaje = success ? "Datos obtenidos." : "No se encontraron registros.";

            return (lista, success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, metodo, "Error", ex);
            return (Enumerable.Empty<AdministracionTipoContratoABM>(), false, ex.Message);
        }
    }

    // ======================================
    // PAGINACION
    // ======================================
    public async Task<(IEnumerable<AdministracionTipoContratoABM> TipoContrato, bool Success, string Mensaje, int Total)>
        GetTipoContratoPagination(string LogTransaccionId, int page, int pageSize, string? search)
    {
        string metodo = "GetTipoContratoPagination()";

        const string query = @"
            SELECT 
                ltipocontrato_id AS LTipoContratoId,
                UPPER(snombre) AS SNombre,
                susuarioadd AS Usuario
            FROM administraciontipocontrato
            ORDER BY ltipocontrato_id DESC
            LIMIT @pageSize OFFSET @page;
        ";

        const string countQuery = "SELECT COUNT(*) FROM administraciontipocontrato;";

        try
        {
            using var con = _context.CreateConnection();

            var lista = await con.QueryAsync<AdministracionTipoContratoABM>(query, new { page, pageSize });
            var total = await con.ExecuteScalarAsync<int>(countQuery);

            bool success = lista.Any();
            string mensaje = success ? "Datos obtenidos." : "No se encontraron registros.";

            return (lista, success, mensaje, total);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, metodo, "Error", ex);
            return (Enumerable.Empty<AdministracionTipoContratoABM>(), false, ex.Message, 0);
        }
    }

    // ======================================
    // INSERTAR
    // ======================================
    public async Task<(bool Success, string Mensaje)> GuardarTipoContrato(string LogTransaccionId, AdministracionTipoContratoABM data)
    {
        string metodo = "GuardarTipoContrato()";

        const string nextIdQuery = @"SELECT IFNULL(MAX(ltipocontrato_id),0)+1 FROM administraciontipocontrato;";
        const string insertQuery = @"
            INSERT INTO administraciontipocontrato
            (ltipocontrato_id, snombre, susuarioadd, dtfechaadd, susuariomod, dtfechamod)
            VALUES
            (@nextId, @SNombre, @Usuario, NOW(), @Usuario, NOW());
        ";

        try
        {
            using var con = _context.CreateConnection();

            var nextId = await con.ExecuteScalarAsync<int>(nextIdQuery);

            var result = await con.ExecuteAsync(insertQuery, new {
                nextId,
                data.SNombre,
                data.Usuario
            });

            return result > 0
                ? (true, "Registro insertado.")
                : (false, "No se insertó ningún registro.");
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, metodo, "Error", ex);
            return (false, ex.Message);
        }
    }

    // ======================================
    // MODIFICAR
    // ======================================
    public async Task<(bool Success, string Mensaje)> ModificarTipoContrato(string LogTransaccionId, AdministracionTipoContratoABM data)
    {
        string metodo = "ModificarTipoContrato()";

        const string query = @"
            UPDATE administraciontipocontrato
            SET snombre=@SNombre,
                susuariomod=@Usuario,
                dtfechamod=NOW()
            WHERE ltipocontrato_id=@LTipoContratoId;
        ";

        try
        {
            using var con = _context.CreateConnection();
            var result = await con.ExecuteAsync(query, data);

            return result > 0
                ? (true, "Registro actualizado.")
                : (false, "No se pudo actualizar.");
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, metodo, "Error", ex);
            return (false, ex.Message);
        }
    }

    // ======================================
    // ELIMINAR
    // ======================================
    public async Task<(bool Success, string Mensaje)> EliminarTipoContrato(string LogTransaccionId, int LTipoContratoId)
    {
        string metodo = "EliminarTipoContrato()";

        const string query = @"DELETE FROM administraciontipocontrato WHERE ltipocontrato_id = @LTipoContratoId;";

        try
        {
            using var con = _context.CreateConnection();
            var result = await con.ExecuteAsync(query, new { LTipoContratoId });

            return result > 0
                ? (true, "Registro eliminado.")
                : (false, "No se pudo eliminar.");
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, metodo, "Error", ex);
            return (false, ex.Message);
        }
    }
}
