using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using Newtonsoft.Json;

namespace ApiGuardian.Infrastructure.Repositories;

public class AdministracionBancoRepository : IAdministracionBancoRepository
{
    private readonly DapperContext _context;
    private readonly ILogService _log;
    private string NOMBREARCHIVO = "AdministracionBancoRepository.cs";
    public AdministracionBancoRepository(DapperContext context, ILogService log)
    {
        _context = context;
        _log = log;
    }

    public async Task<(IEnumerable<ListaAdministracionBanco> Data, bool Success, string Mensaje)> GetAllBanco(string LogTransaccionId)
    {
        string nombreMetodo = "GetAllBanco()";

        const string query = @"
            SELECT 
                ab.lbanco_id AS LBancoId,
                ab.snombre AS SNombre,
                ab.scodigo AS SCodigo,
                ab.estado AS Estado,
                ab.fechaadd AS FechaAdd,
                ab.usuarioadd AS UsuarioAdd,
                ab.fechamod AS FechaMod,
                ab.usuariomod AS UsuarioMod,
                am.snombre AS Moneda,
                am.lmoneda_id AS LMonedaId
            FROM administracionbanco ab
            INNER JOIN administracionmoneda am ON am.lmoneda_id = ab.lmoneda_id
            WHERE ab.estado = 1;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var data = await connection.QueryAsync<ListaAdministracionBanco>(query);

            bool success = data != null && data.Any();
            string mensaje = success ? "Datos obtenidos correctamente." : "No se encontraron registros.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, total:{data?.Count()}, data:{JsonConvert.SerializeObject(data, Formatting.Indented)}]");

            return (data ?? Enumerable.Empty<ListaAdministracionBanco>(), success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (Enumerable.Empty<ListaAdministracionBanco>(), false, $"Error al obtener bancos: {ex.Message}");
        }
    }
    public async Task<(bool Success, string Mensaje)> UpdateBanco(string LogTransaccionId, AdministracionBanco data)
    {
        string nombreMetodo = "UpdateBanco()";

        const string query = @"
            UPDATE administracionbanco 
            SET
                snombre = @SNombre,
                scodigo = @SCodigo,
                estado = @Estado,
                fechamod = NOW(),
                usuariomod = @Usuario,
                lmoneda_id = @LMonedaId
            WHERE lbanco_id = @LBancoId;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var rows = await connection.ExecuteAsync(query, new
            {
                data.SNombre,
                data.SCodigo,
                data.Estado,
                data.Usuario,
                data.LBancoId,
                data.LMonedaId
            });

            bool success = rows > 0;
            string mensaje = success ? "Registro actualizado correctamente." : "No se encontró el registro o no se realizaron cambios.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, rowsAffected:{rows}, data:{JsonConvert.SerializeObject(data, Formatting.Indented)}]");

            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al actualizar banco: {ex.Message}");
        }
    }
    public async Task<(bool Success, string Mensaje)> InsertBanco(string LogTransaccionId, AdministracionBanco data)
    {
        string nombreMetodo = "InsertBanco()";

        const string query = @"
            INSERT INTO administracionbanco 
            (snombre, scodigo, estado, usuarioadd, usuariomod, fechaadd, fechamod, lmoneda_id)
            VALUES 
            (@SNombre, @SCodigo, @Estado, @Usuario, @Usuario, NOW(), NOW(), @LMonedaId);
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var rows = await connection.ExecuteAsync(query, new
            {
                data.SNombre,
                data.SCodigo,
                data.Estado,
                Usuario = data.Usuario,
                data.LMonedaId
            });

            bool success = rows > 0;
            string mensaje = success ? "Registro insertado correctamente." : "No se realizó el guardado.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, rowsAffected:{rows}, data:{JsonConvert.SerializeObject(data, Formatting.Indented)}]");

            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al insertar banco: {ex.Message}");
        }
    }
    public async Task<(bool Success, string Mensaje)> DeleteBanco(string LogTransaccionId, int LBancoId, string? usuario)
    {
        string nombreMetodo = "DeleteBanco()";

        const string query = @"
            UPDATE administracionbanco 
            SET
                estado = 0,
                usuariomod = @Usuario,
                fechamod = NOW()
            WHERE lbanco_id = @LBancoId;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var rows = await connection.ExecuteAsync(query, new
            {
                LBancoId,
                Usuario = usuario
            });

            bool success = rows > 0;
            string mensaje = success ? "Registro eliminado correctamente." : "No se encontró el registro o no se realizaron cambios.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, rowsAffected:{rows}, usuario:{usuario}]");

            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al eliminar banco: {ex.Message}");
        }
    }
    public async Task<(IEnumerable<ListaMoneda> Data, bool Success, string Mensaje)> GetAllMoneda(string LogTransaccionId)
    {
        string nombreMetodo = "GetAllMoneda()";

        const string query = @"
            SELECT 
                lmoneda_id AS LMonedaId, 
                snombre AS SNombre 
            FROM administracionmoneda;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var data = await connection.QueryAsync<ListaMoneda>(query);

            bool success = data != null && data.Any();
            string mensaje = success ? "Datos obtenidos correctamente." : "No se encontraron registros.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, data:{JsonConvert.SerializeObject(data, Formatting.Indented)}]");

            return (data ?? Enumerable.Empty<ListaMoneda>(), success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (Enumerable.Empty<ListaMoneda>(), false, $"Error al obtener monedas: {ex.Message}");
        }
    }
 
}
