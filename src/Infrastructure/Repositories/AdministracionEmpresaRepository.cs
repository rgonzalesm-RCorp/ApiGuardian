using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using System.Text.Json;

namespace ApiGuardian.Infrastructure.Repositories;

public class AdministracionEmpresaRepository : IAdministracionEmpresaRepository
{
    private readonly DapperContext _context;
    private readonly ILogService _log;
    private readonly string NOMBREARCHIVO = "AdministracionEmpresaRepository.cs";

    public AdministracionEmpresaRepository(DapperContext context, ILogService log)
    {
        _context = context;
        _log = log;
    }

    // ============================================================
    // LISTADO
    // ============================================================
    public async Task<(IEnumerable<AdministracionEmpresa> Empresas, bool Success, string Mensaje)>
        GetEmpresas(string LogTransaccionId)
    {
        string metodo = "GetEmpresas()";

        const string query = @"
            SELECT 
                lempresa_id AS LEmpresaId,
                UPPER(snombre) AS SNombre,
                snit AS SNIT,
                fecha_creacion AS FechaCreacion,
                UPPER(empresa) AS Empresa
            FROM administracionempresa
            ORDER BY lempresa_id DESC;";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo, $"Inicio consulta. Script: {query}");

        try
        {
            using var con = _context.CreateConnection();
            var lista = await con.QueryAsync<AdministracionEmpresa>(query);

            bool success = lista.Any();
            string mensaje = success ? "Datos obtenidos." : "No hay registros.";

            return (lista, success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, metodo, "Error ejecutando consulta", ex);
            return (Enumerable.Empty<AdministracionEmpresa>(), false, ex.Message);
        }
    }

    // ============================================================
    // PAGINACIÓN
    // ============================================================
    public async Task<(IEnumerable<AdministracionEmpresa> Empresas, bool Success, string Mensaje, int Total)>
        GetEmpresasPagination(string LogTransaccionId, int page, int pageSize, string? search)
    {
        string metodo = "GetEmpresasPagination()";

        const string query = @"
            SELECT 
                lempresa_id AS LEmpresaId,
                UPPER(snombre) AS SNombre,
                snit AS SNIT,
                fecha_creacion AS FechaCreacion,
                UPPER(empresa) AS Empresa
            FROM administracionempresa
            ORDER BY lempresa_id DESC
            LIMIT @pageSize OFFSET @page;";

        const string countQuery = @"SELECT COUNT(*) FROM administracionempresa;";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo,
            $"Inicio paginación. Script: {query} page={page}, pageSize={pageSize}, search={search}");

        try
        {
            using var con = _context.CreateConnection();
            var lista = await con.QueryAsync<AdministracionEmpresa>(query, new { page, pageSize });
            int total = await con.ExecuteScalarAsync<int>(countQuery);

            bool success = lista.Any();
            string mensaje = success ? "Datos obtenidos." : "No se encontraron datos.";

            return (lista, success, mensaje, total);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, metodo, "Error paginando", ex);
            return (Enumerable.Empty<AdministracionEmpresa>(), false, ex.Message, 0);
        }
    }

    // ============================================================
    // INSERTAR
    // ============================================================
    public async Task<(bool Success, string Mensaje)> GuardarEmpresa(string LogTransaccionId, AdministracionEmpresa data)
    {
        string metodo = "GuardarEmpresa()";

        const string nextIdQuery = @"SELECT IFNULL(MAX(lempresa_id),0)+1 FROM administracionempresa;";

        const string insertQuery = @"
            INSERT INTO administracionempresa
            (lempresa_id, snombre, snit, fecha_creacion, empresa, susuarioadd, dtfechaadd, susuariomod, dtfechamod)
            VALUES
            (@nextId, UPPER(@SNombre), @SNIT, NOW(), UPPER(@Empresa), @Usuario, NOW(), @Usuario, NOW());";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo,
            $"Inicio inserción. Data: {JsonSerializer.Serialize(data)}");

        try
        {
            using var con = _context.CreateConnection();

            int nextId = await con.ExecuteScalarAsync<int>(nextIdQuery);

            int result = await con.ExecuteAsync(insertQuery, new
            {
                nextId,
                data.SNombre,
                data.SNIT,
                data.Empresa,
                data.Usuario
            });

            bool success = result > 0;
            string mensaje = success ? "Empresa registrada." : "No se pudo registrar.";

            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, metodo, "Error insertando empresa", ex);
            return (false, ex.Message);
        }
    }
    public async Task<(bool Success, string Mensaje)> ModificarEmpresa(string LogTransaccionId, AdministracionEmpresa data)
    {
        string metodo = "ModificarEmpresa()";

        const string query = @"
            UPDATE administracionempresa
            SET snombre=UPPER(@SNombre),
                snit=@SNIT,
                empresa=UPPER(@Empresa),
                susuariomod=@Usuario,
                dtfechamod=NOW()
            WHERE lempresa_id=@LEmpresaId;";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo,
            $"Inicio actualización. Data: {JsonSerializer.Serialize(data)}");

        try
        {
            using var con = _context.CreateConnection();
            int result = await con.ExecuteAsync(query, data);

            bool success = result > 0;
            string mensaje = success ? "Empresa actualizada." : "No se pudo actualizar.";

            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, metodo, "Error actualizando empresa", ex);
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Mensaje)> EliminarEmpresa(string LogTransaccionId, int LEmpresaId)
    {
        string metodo = "EliminarEmpresa()";

        const string query = @"DELETE FROM administracionempresa WHERE lempresa_id=@LEmpresaId;";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo, $"Inicio eliminación ID={LEmpresaId}");

        try
        {
            using var con = _context.CreateConnection();
            int result = await con.ExecuteAsync(query, new { LEmpresaId });

            bool success = result > 0;
            string mensaje = success ? "Empresa eliminada." : "No se pudo eliminar.";

            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, metodo, "Error eliminando empresa", ex);
            return (false, ex.Message);
        }
    }
}
