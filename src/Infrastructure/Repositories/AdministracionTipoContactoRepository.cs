using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using Newtonsoft.Json;

namespace ApiGuardian.Infrastructure.Repositories;

public class AdministracionTipoContactoRepository : IAdministracionTipoContactoRepository
{
    private readonly DapperContext _context;
    private readonly ILogService _log;
    private string NOMBREARCHIVO = "AdministracionTipoContactoRepository.cs";

    public AdministracionTipoContactoRepository(DapperContext context, ILogService log)
    {
        _context = context;
        _log = log;
    }

    // =====================================
    // GET LISTADO
    // =====================================
    public async Task<(IEnumerable<AdministracionTipoContacto> TipoContacto, bool Success, string Mensaje)> 
        GetTipoContacto(string LogTransaccionId)
    {
        string nombreMetodo = "GetTipoContacto()";

        const string query = @"
            SELECT 
                ltipocontacto_id AS LTipoContactoId,
                snombre AS SNombre,
                dporcentajeyo AS DPorcentajeYo,
                dporcentaje1g AS DPorcentaje1G,
                dporcentaje2g AS DPorcentaje2G,
                dporcentaje3g AS DPorcentaje3G,
                dporcentaje4g AS DPorcentaje4G,
                dporcentaje5g AS DPorcentaje5G,
                dporcentaje6g AS DPorcentaje6G,
                dporcentaje7g AS DPorcentaje7G,
                dcostomembresia AS DCostoMembresia,
                susuarioadd AS Usuario
            FROM administraciontipocontacto
            ORDER BY ltipocontacto_id DESC;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio método [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();
            var lista = await connection.QueryAsync<AdministracionTipoContacto>(query);

            bool success = lista.Any();
            string mensaje = success
                ? "Tipo de contactos obtenidos correctamente."
                : "No se encontraron registros.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin método: {mensaje}, data: {JsonConvert.SerializeObject(lista, Formatting.Indented)}");

            return (lista, success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Error", ex);
            return (Enumerable.Empty<AdministracionTipoContacto>(), false, ex.Message);
        }
    }

    // =====================================
    // GET PAGINACION
    // =====================================
    public async Task<(IEnumerable<AdministracionTipoContacto> TipoContacto, bool Success, string Mensaje, int Total)>
        GetTipoContactoPagination(string LogTransaccionId, int page, int pageSize, string? search)
    {
        string nombreMetodo = "GetTipoContactoPagination()";

        const string query = @"
            SELECT 
                ltipocontacto_id AS LTipoContactoId,
                snombre AS SNombre,
                dporcentajeyo AS DPorcentajeYo,
                dporcentaje1g AS DPorcentaje1G,
                dporcentaje2g AS DPorcentaje2G,
                dporcentaje3g AS DPorcentaje3G,
                dporcentaje4g AS DPorcentaje4G,
                dporcentaje5g AS DPorcentaje5G,
                dporcentaje6g AS DPorcentaje6G,
                dporcentaje7g AS DPorcentaje7G,
                dcostomembresia AS DCostoMembresia,
                susuarioadd AS Usuario
            FROM administraciontipocontacto
            ORDER BY ltipocontacto_id DESC
            LIMIT @pageSize OFFSET @page;
        ";

        const string countQuery = @"SELECT COUNT(*) FROM administraciontipocontacto;";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Inicio de método");

        try
        {
            using var connection = _context.CreateConnection();

            var lista = await connection.QueryAsync<AdministracionTipoContacto>(query, new { page, pageSize });
            int total = await connection.ExecuteScalarAsync<int>(countQuery);

            bool success = lista.Any();
            string mensaje = success ? "Datos obtenidos correctamente." : "No se encontraron registros.";

            return (lista, success, mensaje, total);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Error", ex);
            return (Enumerable.Empty<AdministracionTipoContacto>(), false, ex.Message, 0);
        }
    }

    // =====================================
    // INSERT
    // =====================================
    public async Task<(bool Success, string Mensaje)> GuardarTipoContacto(string LogTransaccionId, AdministracionTipoContacto data)
    {
        string nombreMetodo = "GuardarTipoContacto()";

        const string nextIdQuery = @"SELECT IFNULL(MAX(ltipocontacto_id),0) + 1 FROM administraciontipocontacto;";

        const string query = @"
            INSERT INTO administraciontipocontacto
            (ltipocontacto_id, snombre,
             dporcentajeyo, dporcentaje1g, dporcentaje2g, dporcentaje3g,
             dporcentaje4g, dporcentaje5g, dporcentaje6g, dporcentaje7g,
             dcostomembresia,
             susuarioadd, dtfechaadd, susuariomod, dtfechamod)
            VALUES
            (@nextId, @SNombre,
             @DPorcentajeYo, @DPorcentaje1G, @DPorcentaje2G, @DPorcentaje3G,
             @DPorcentaje4G, @DPorcentaje5G, @DPorcentaje6G, @DPorcentaje7G,
             @DCostoMembresia,
             @Usuario, NOW(), @Usuario, NOW());
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
            $"Inicio método [data: {JsonConvert.SerializeObject(data, Formatting.Indented)}]");

        try
        {
            using var connection = _context.CreateConnection();
            var nextId = await connection.ExecuteScalarAsync<int>(nextIdQuery);

            var rows = await connection.ExecuteAsync(query, new
            {
                nextId,
                data.SNombre,
                data.DPorcentajeYo,
                data.DPorcentaje1G,
                data.DPorcentaje2G,
                data.DPorcentaje3G,
                data.DPorcentaje4G,
                data.DPorcentaje5G,
                data.DPorcentaje6G,
                data.DPorcentaje7G,
                data.DCostoMembresia,
                data.Usuario
            });

            bool success = rows > 0;
            string mensaje = success ? "Registro insertado correctamente." : "No se insertó ningún registro.";

            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Error", ex);
            return (false, ex.Message);
        }
    }

    // =====================================
    // UPDATE
    // =====================================
    public async Task<(bool Success, string Mensaje)> ModificarTipoContacto(string LogTransaccionId, AdministracionTipoContacto data)
    {
        string nombreMetodo = "ModificarTipoContacto()";

        const string query = @"
            UPDATE administraciontipocontacto
            SET
                snombre = @SNombre,
                dporcentajeyo = @DPorcentajeYo,
                dporcentaje1g = @DPorcentaje1G,
                dporcentaje2g = @DPorcentaje2G,
                dporcentaje3g = @DPorcentaje3G,
                dporcentaje4g = @DPorcentaje4G,
                dporcentaje5g = @DPorcentaje5G,
                dporcentaje6g = @DPorcentaje6G,
                dporcentaje7g = @DPorcentaje7G,
                dcostomembresia = @DCostoMembresia,
                susuariomod = @Usuario,
                dtfechamod = NOW()
            WHERE ltipocontacto_id = @LTipoContactoId;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
            $"Inicio método [data: {JsonConvert.SerializeObject(data, Formatting.Indented)}]");

        try
        {
            using var connection = _context.CreateConnection();
            var rows = await connection.ExecuteAsync(query, data);

            bool success = rows > 0;
            string mensaje = success ? "Registro actualizado correctamente." : "No se actualizó ningún registro.";

            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Error", ex);
            return (false, ex.Message);
        }
    }

    // =====================================
    // DELETE
    // =====================================
    public async Task<(bool Success, string Mensaje)> EliminarTipoContacto(string LogTransaccionId, int LTipoContactoId)
    {
        string nombreMetodo = "EliminarTipoContacto()";

        const string query = @"DELETE FROM administraciontipocontacto WHERE ltipocontacto_id = @LTipoContactoId;";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
            $"Inicio método [LTipoContactoId: {LTipoContactoId}]");

        try
        {
            using var connection = _context.CreateConnection();
            var rows = await connection.ExecuteAsync(query, new { LTipoContactoId });

            bool success = rows > 0;
            string mensaje = success ? "Registro eliminado correctamente." : "No se pudo eliminar el registro.";

            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Error", ex);
            return (false, ex.Message);
        }
    }
}
