using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;

namespace ApiGuardian.Infrastructure.Repositories;

public class AdministracionDescuentoCicloTipoRepository : IAdministracionDescuentoCicloTipoRepository
{
    private readonly DapperContext _context;
    private readonly ILogService _log;
    private readonly string NOMBREARCHIVO = "AdministracionDescuentoCicloTipoRepository.cs";

    public AdministracionDescuentoCicloTipoRepository(DapperContext context, ILogService log)
    {
        _context = context;
        _log = log;
    }

    // ============================================================
    // LISTADO COMPLETO
    // ============================================================
    public async Task<(IEnumerable<AdministracionDescuentoCicloTipo> Tipos, bool Success, string Mensaje)>
        GetDescuentoCicloTipo(string LogTransaccionId)
    {
        string metodo = "GetDescuentoCicloTipo()";

        const string query = @"
            SELECT 
                ldescuentociclotipo_id AS LDescuentoCicloTipoId,
                UPPER(snombre) AS SNombre
            FROM administraciondescuentociclotipo
            ORDER BY ldescuentociclotipo_id DESC;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo, $"Inicio de consulta. Script: {query}");

        try
        {
            using var con = _context.CreateConnection();
            var lista = await con.QueryAsync<AdministracionDescuentoCicloTipo>(query);

            bool success = lista.Any();
            string mensaje = success ? "Datos obtenidos correctamente." : "No se encontraron registros.";

            return (lista, success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, metodo, "Error en consulta", ex);
            return (Enumerable.Empty<AdministracionDescuentoCicloTipo>(), false, ex.Message);
        }
    }

    // ============================================================
    // PAGINACIÓN
    // ============================================================
    public async Task<(IEnumerable<AdministracionDescuentoCicloTipo> Tipos, bool Success, string Mensaje, int Total)>
        GetDescuentoCicloTipoPagination(string LogTransaccionId, int page, int pageSize, string? search)
    {
        string metodo = "GetDescuentoCicloTipoPagination()";

        const string query = @"
            SELECT 
                ldescuentociclotipo_id AS LDescuentoCicloTipoId,
                UPPER(snombre) AS SNombre
            FROM administraciondescuentociclotipo
            ORDER BY ldescuentociclotipo_id DESC
            LIMIT @pageSize OFFSET @page;
        ";

        const string countQuery = "SELECT COUNT(*) FROM administraciondescuentociclotipo;";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo,
            $"Inicio paginación. Script: {query} Parámetros: page={page}, pageSize={pageSize}");

        try
        {
            using var con = _context.CreateConnection();

            var lista = await con.QueryAsync<AdministracionDescuentoCicloTipo>(query, new { page, pageSize });
            var total = await con.ExecuteScalarAsync<int>(countQuery);

            bool success = lista.Any();
            string mensaje = success ? "Datos paginados obtenidos." : "No se encontraron datos.";

            return (lista, success, mensaje, total);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, metodo, "Error en paginación", ex);
            return (Enumerable.Empty<AdministracionDescuentoCicloTipo>(), false, ex.Message, 0);
        }
    }

    // ============================================================
    // INSERTAR
    // ============================================================
    public async Task<(bool Success, string Mensaje)> GuardarDescuentoCicloTipo(string LogTransaccionId, AdministracionDescuentoCicloTipo data)
    {
        string metodo = "GuardarDescuentoCicloTipo()";

        const string nextIdQuery = @"SELECT IFNULL(MAX(ldescuentociclotipo_id),0)+1 FROM administraciondescuentociclotipo;";

        const string insertQuery = @"
            INSERT INTO administraciondescuentociclotipo
            (ldescuentociclotipo_id, snombre, susuarioadd, dtfechaadd, susuariomod, dtfechamod)
            VALUES
            (@nextId, @SNombre, @Usuario, NOW(), @Usuario, NOW());
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo,
            $"Inicio de inserción. Script nextId: {nextIdQuery} Script insert: {insertQuery} Data: {System.Text.Json.JsonSerializer.Serialize(data)}");

        try
        {
            using var con = _context.CreateConnection();
            int nextId = await con.ExecuteScalarAsync<int>(nextIdQuery);

            var result = await con.ExecuteAsync(insertQuery, new
            {
                nextId,
                data.SNombre,
                data.Usuario
            });

            bool success = result > 0;
            string mensaje = success ? "Registro insertado correctamente." : "No se pudo insertar.";

            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, metodo, "Error insertando registro", ex);
            return (false, ex.Message);
        }
    }

    // ============================================================
    // MODIFICAR
    // ============================================================
    public async Task<(bool Success, string Mensaje)> ModificarDescuentoCicloTipo(string LogTransaccionId, AdministracionDescuentoCicloTipo data)
    {
        string metodo = "ModificarDescuentoCicloTipo()";

        const string query = @"
            UPDATE administraciondescuentociclotipo
            SET snombre=@SNombre,
                susuariomod=@Usuario,
                dtfechamod=NOW()
            WHERE ldescuentociclotipo_id=@LDescuentoCicloTipoId;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo,
            $"Inicio de actualización. Script: {query} Data: {System.Text.Json.JsonSerializer.Serialize(data)}");

        try
        {
            using var con = _context.CreateConnection();
            int result = await con.ExecuteAsync(query, data);

            bool success = result > 0;
            string mensaje = success ? "Registro actualizado correctamente." : "No se pudo actualizar.";

            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, metodo, "Error actualizando registro", ex);
            return (false, ex.Message);
        }
    }

    // ============================================================
    // ELIMINAR
    // ============================================================
    public async Task<(bool Success, string Mensaje)> EliminarDescuentoCicloTipo(string LogTransaccionId, int LDescuentoCicloTipoId)
    {
        string metodo = "EliminarDescuentoCicloTipo()";

        const string query = @"DELETE FROM administraciondescuentociclotipo WHERE ldescuentociclotipo_id=@LDescuentoCicloTipoId;";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo,
            $"Inicio de eliminación. Script: {query} Parámetro: {LDescuentoCicloTipoId}");

        try
        {
            using var con = _context.CreateConnection();
            int result = await con.ExecuteAsync(query, new { LDescuentoCicloTipoId });

            bool success = result > 0;
            string mensaje = success ? "Registro eliminado correctamente." : "No se pudo eliminar.";

            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, metodo, "Error eliminando registro", ex);
            return (false, ex.Message);
        }
    }
}
