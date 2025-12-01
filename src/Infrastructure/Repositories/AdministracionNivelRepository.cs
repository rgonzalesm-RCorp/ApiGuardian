using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using Newtonsoft.Json;

namespace ApiGuardian.Infrastructure.Repositories;

public class AdministracionNivelRepository : IAdministracionNivelRepository
{
    private readonly DapperContext _context;
    private readonly ILogService _log;
    private string NOMBREARCHIVO = "AdministracionNivelRepository.cs";
    public AdministracionNivelRepository(DapperContext context, ILogService log)
    {
        _context = context;
        _log = log;
    }
    public async Task<(IEnumerable<AdministracionNivel> Nivel, bool Success, string Mensaje)> GetNivel(string LogTransaccionId)
    {
         string nombreMetodo = "GetNivel()";

        const string query = @"
            SELECT 
                lnivel_id AS LNivelId, 
                UPPER(ssigla) AS SSigla, 
                UPPER(snombre) AS SNombre,
                ddesde Desde,
                dhasta Hasta,
                dbono Bono,
                dbonomembresia BonoMembresia,
                VME,
                UPPER(susuarioadd) Usuario 
            FROM administracionnivel
            ORDER BY lnivel_id DESC;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var niveles = await connection.QueryAsync<AdministracionNivel>(query);

            bool success = niveles != null && niveles.Any();
            string mensaje = success ? "Niveles obtenidos correctamente." : "No se encontraron niveles.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Fin de metodo [mensaje: {mensaje}, niveles:{JsonConvert.SerializeObject(niveles, Formatting.Indented)}]");

            return (niveles ?? Enumerable.Empty<AdministracionNivel>(), success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (Enumerable.Empty<AdministracionNivel>(), false, $"Error al obtener los niveles: {ex.Message}");
        }
    }
    public async Task<(bool Success, string Mensaje)> GuardarNivel(string LogTransaccionId, AdministracionNivel Nivel)
    {
        string nombreMetodo = "GuardarNivel()";
        const string query = @"
            INSERT INTO administracionnivel
                (lnivel_id, ssigla, snombre, ddesde, dhasta, dbono, dbonomembresia, vme, susuarioadd, dtfechaadd, susuariomod, dtfechamod)
            VALUES
                (@nextId, @SSigla, @SNombre, @Desde, @Hasta, @Bono, @BonoMembresia, @VME, @Usuario, NOW(), @Usuario, NOW());
        ";
        const string nextIdQuery = @"
            SELECT IFNULL(MAX(lnivel_id), 0) + 1 
            FROM administracionnivel;
        ";
         _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");
        try
        {
            using var connection = _context.CreateConnection();
            var nextId = await connection.ExecuteScalarAsync<int>(nextIdQuery);

            var rowsAffected = await connection.ExecuteAsync(query, new
            {
                Nivel.SSigla,
                Nivel.SNombre,
                Nivel.Desde,
                Nivel.Hasta,
                Nivel.Bono,
                Nivel.BonoMembresia,
                Nivel.VME,
                Nivel.Usuario,
                nextId
            });

            bool success = rowsAffected > 0;
            string mensaje = success ? "Registro insertado correctamente." : "No se insertó ningún registro.";
            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Fin de metodo [mensaje: {mensaje}]");
            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al guardar el nivel: {ex.Message}");
        }
    }
    public async Task<(bool Success, string Mensaje)> ModificarNivel(string LogTransaccionId, AdministracionNivel Nivel)
    {
        string nombreMetodo = "ModificarNivel()";
        string query = @"
            UPDATE administracionnivel
            SET 
                ssigla = @SSigla,
                snombre = @SNombre,
                ddesde = @Desde,
                dhasta = @Hasta,
                dbono = @Bono,
                dbonomembresia = @BonoMembresia,
                vme = @VME,
                susuariomod = @Usuario,
                dtfechamod = NOW()
            WHERE lnivel_id = @LNivelId;
        ";
        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");
        try
        {
            
            using var connection = _context.CreateConnection();

            var rows = await connection.ExecuteAsync(query, new
            {
                Nivel.SSigla,
                Nivel.SNombre,
                Nivel.Desde,
                Nivel.Hasta,
                Nivel.Bono,
                Nivel.BonoMembresia,
                Nivel.VME,
                Nivel.Usuario,
                Nivel.LNivelId
            });

            bool success = rows > 0;
            string mensaje = success ? "Nivel modificado correctamente" : "No se encontró el nivel a modificar";
            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Fin de metodo [mensaje: {mensaje}, rowsAffected:{rows}]");
            
            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al modificar el nivel: {ex.Message}");
        }
    }
    public async Task<(bool Success, string Mensaje)> EliminarNivel(string LogTransaccionId, int LNivelId)
    {
        string nombreMetodo = "EliminarNivel()";
        const string query = @"
                DELETE FROM  administracionnivel 
                WHERE lnivel_id = @LNivelId;
        ";
        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

         try
        {
            

            using var connection = _context.CreateConnection();

            var rows = await connection.ExecuteAsync(query, new
            {
                LNivelId,
                Usuario = LogTransaccionId
            });

            bool success = rows > 0;
            string mensaje = success ? "Registro eliminado correctamente." : "No se pudo eliminar el registro.";
            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Fin de metodo [mensaje: {mensaje}]");

            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al eliminar el nivel: {ex.Message}");
        }
    }
    public async Task<(IEnumerable<AdministracionNivel> Nivel, bool Success, string Mensaje, int Total)> GetNivelPagination(string LogTransaccionId, int page, int pageSize, string? search)
    {
         string nombreMetodo = "GetNivel()";

        const string query = @"
            SELECT 
                lnivel_id AS LNivelId, 
                UPPER(ssigla) AS SSigla, 
                UPPER(snombre) AS SNombre,
                ddesde Desde,
                dhasta Hasta,
                dbono Bono,
                dbonomembresia BonoMembresia,
                VME,
                UPPER(susuarioadd) Usuario 
            FROM administracionnivel
            ORDER BY lnivel_id DESC
            LIMIT @pageSize OFFSET @page;
        ";
        const string countQuery = @"
            SELECT 
                COUNT(*)
            FROM administracionnivel;
        ";
        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var niveles = await connection.QueryAsync<AdministracionNivel>(query, new {page, pageSize});
            var total = await connection.ExecuteScalarAsync<int>(countQuery);

            bool success = niveles != null && niveles.Any();
            string mensaje = success ? "Niveles obtenidos correctamente." : "No se encontraron niveles.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Fin de metodo [mensaje: {mensaje}, niveles:{JsonConvert.SerializeObject(niveles, Formatting.Indented)}]");

            return (niveles ?? Enumerable.Empty<AdministracionNivel>(), success, mensaje, total);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (Enumerable.Empty<AdministracionNivel>(), false, $"Error al obtener los niveles: {ex.Message}", 0);
        }
    }
}
