using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using Newtonsoft.Json;

namespace ApiGuardian.Infrastructure.Repositories;

public class AdministracionComplejoRepository : IAdministracionComplejoRepository
{
    private readonly DapperContext _context;
    private readonly ILogService _log;
    private string NOMBREARCHIVO = "AdministracionComplejoRepository.cs";

    public AdministracionComplejoRepository(DapperContext context, ILogService log)
    {
        _context = context;
        _log = log;
    }

    public async Task<(IEnumerable<AdministracionComplejoABM> Complejos, bool Success, string Mensaje)> GetComplejo(string LogTransaccionId)
    {
        string nombreMetodo = "GetComplejo()";

        const string query = @"
            SELECT 
                lcomplejo_id AS LComplejoId,
                scodigo AS SCodigo,
                snombre AS SNombre,
                cestado AS CEstado,
                lempresa_id AS LEmpresaId,
                dporcentajeyo AS DPorcentajeYo,
                dporcentaje1g AS DPorcentaje1G,
                dporcentaje2g AS DPorcentaje2G,
                dporcentaje3g AS DPorcentaje3G,
                dporcentaje4g AS DPorcentaje4G,
                dporcentaje5g AS DPorcentaje5G,
                dporcentaje6g AS DPorcentaje6G,
                dporcentaje7g AS DPorcentaje7G,
                susuarioadd AS Usuario
            FROM administracioncomplejo
            ORDER BY lcomplejo_id DESC;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio script: {query}");

        try
        {
            using var connection = _context.CreateConnection();
            var result = await connection.QueryAsync<AdministracionComplejoABM>(query);

            bool success = result != null && result.Any();
            string mensaje = success ? "Datos obtenidos correctamente." : "No se encontraron registros.";

            return (result ?? Enumerable.Empty<AdministracionComplejoABM>(), success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de método", ex);
            return (Enumerable.Empty<AdministracionComplejoABM>(), false, ex.Message);
        }
    }

    public async Task<(IEnumerable<AdministracionComplejoABM> Complejos, bool Success, string Mensaje, int Total)> GetComplejoPagination(string LogTransaccionId, int page, int pageSize, string? search)
    {
        string nombreMetodo = "GetComplejoPagination()";

        const string query = @"
            SELECT 
                lcomplejo_id AS LComplejoId,
                scodigo AS SCodigo,
                snombre AS SNombre,
                cestado AS CEstado,
                lempresa_id AS LEmpresaId,
                dporcentajeyo AS DPorcentajeYo,
                dporcentaje1g AS DPorcentaje1G,
                dporcentaje2g AS DPorcentaje2G,
                dporcentaje3g AS DPorcentaje3G,
                dporcentaje4g AS DPorcentaje4G,
                dporcentaje5g AS DPorcentaje5G,
                dporcentaje6g AS DPorcentaje6G,
                dporcentaje7g AS DPorcentaje7G,
                susuarioadd AS Usuario
            FROM administracioncomplejo
            ORDER BY lcomplejo_id DESC
            LIMIT @pageSize OFFSET @page;
        ";

        const string countQuery = @"SELECT COUNT(*) FROM administracioncomplejo;";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio script: {query}");

        try
        {
            using var connection = _context.CreateConnection();
            var data = await connection.QueryAsync<AdministracionComplejoABM>(query, new { page, pageSize });
            var total = await connection.ExecuteScalarAsync<int>(countQuery);

            bool success = data.Any();
            string mensaje = success ? "Datos obtenidos." : "No hay registros.";

            return (data, success, mensaje, total);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Error", ex);
            return (Enumerable.Empty<AdministracionComplejoABM>(), false, ex.Message, 0);
        }
    }

    public async Task<(bool Success, string Mensaje)> GuardarComplejo(string LogTransaccionId, AdministracionComplejoABM data)
    {
        string nombreMetodo = "GuardarComplejo()";

        const string nextIdQuery = @"SELECT IFNULL(MAX(lcomplejo_id),0) + 1 FROM administracioncomplejo;";

        const string query = @"
            INSERT INTO administracioncomplejo
            (lcomplejo_id, scodigo, snombre, cestado, lempresa_id,
             dporcentajeyo, dporcentaje1g, dporcentaje2g, dporcentaje3g,
             dporcentaje4g, dporcentaje5g, dporcentaje6g, dporcentaje7g,
             susuarioadd, dtfechaadd, susuariomod, dtfechamod)
            VALUES
            (@nextId, @SCodigo, @SNombre, @CEstado, @LEmpresaId,
             @DPorcentajeYo, @DPorcentaje1G, @DPorcentaje2G, @DPorcentaje3G,
             @DPorcentaje4G, @DPorcentaje5G, @DPorcentaje6G, @DPorcentaje7G,
             @Usuario, NOW(), @Usuario, NOW());
        ";
         _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var conn = _context.CreateConnection();
            var nextId = await conn.ExecuteScalarAsync<int>(nextIdQuery);

            var rows = await conn.ExecuteAsync(query, new
            {
                nextId,
                data.SCodigo,
                data.SNombre,
                data.CEstado,
                data.LEmpresaId,
                data.DPorcentajeYo,
                data.DPorcentaje1G,
                data.DPorcentaje2G,
                data.DPorcentaje3G,
                data.DPorcentaje4G,
                data.DPorcentaje5G,
                data.DPorcentaje6G,
                data.DPorcentaje7G,
                data.Usuario
            });

            bool success = rows > 0;
            string mensaje = success ? "Registro insertado correctamente." : "No se insertó ningún registro.";
            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Fin de metodo [mensaje: {mensaje}]");
            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Mensaje)> ModificarComplejo(string LogTransaccionId, AdministracionComplejoABM data)
    {
        string nombreMetodo = "ModificarComplejo()";

        const string query = @"
            UPDATE administracioncomplejo SET
                scodigo = @SCodigo,
                snombre = @SNombre,
                cestado = @CEstado,
                lempresa_id = @LEmpresaId,
                dporcentajeyo = @DPorcentajeYo,
                dporcentaje1g = @DPorcentaje1G,
                dporcentaje2g = @DPorcentaje2G,
                dporcentaje3g = @DPorcentaje3G,
                dporcentaje4g = @DPorcentaje4G,
                dporcentaje5g = @DPorcentaje5G,
                dporcentaje6g = @DPorcentaje6G,
                dporcentaje7g = @DPorcentaje7G,
                susuariomod = @Usuario,
                dtfechamod = NOW()
            WHERE lcomplejo_id = @LComplejoId;
        ";
        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var conn = _context.CreateConnection();
            var rows = await conn.ExecuteAsync(query, data);

            bool success = rows > 0;
            string mensaje = success ? "Complejo modificado correctamente" : "No se encontró el nivel a modificar";
            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Fin de metodo [mensaje: {mensaje}, rowsAffected:{rows}]");
            
            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Mensaje)> EliminarComplejo(string LogTransaccionId, int LComplejoId)
    {
        string metodo = "EliminarComplejo()";

        const string query = @"DELETE FROM administracioncomplejo WHERE lcomplejo_id = @LComplejoId;";
        _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var conn = _context.CreateConnection();
            var rows = await conn.ExecuteAsync(query, new { LComplejoId });

            bool success = rows > 0;
            string mensaje = success ? "Registro eliminado correctamente." : "No se pudo eliminar el registro.";
            _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo, $"Fin de metodo [mensaje: {mensaje}]");

            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, metodo, "Fin de metodo", ex);
            return (false, ex.Message);
        }
    }
}
