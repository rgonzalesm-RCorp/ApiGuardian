using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using Newtonsoft.Json;

namespace ApiGuardian.Infrastructure.Repositories;

public class AdministracionObservacionComisionRepository : IAdministracionObservacionComisionRepository
{
    private readonly DapperContext _context;
    private readonly ILogService _log;
    private string NOMBREARCHIVO = "UtilsRepository.CS";
    public AdministracionObservacionComisionRepository(DapperContext context, ILogService log)
    {
        _context = context;
        _log = log;
    }
    public async Task<(IEnumerable<ListaAdministracionObservacionComision> Data, bool Success, string Mensaje, int Total)> GetAllAdministracionCObservacionComisionAsync(string LogTransaccionId, int page, int pageSize, string? search, int lCicloId)
    {
        string nombreMetodo = "GetAllAdministracionCObservacionComisionAsync()";

        const string queryData = @"
            SELECT 
                AOC.lobservacioncomision_id AS ObservacionId,
                AOC.dtfechaadd AS Fecha,
                AOC.lcontacto_id AS LContactoId,
                ACT.snombrecompleto AS SNombreCompleto,
                AC.lciclo_id AS LCicloId,
                UPPER(AC.snombre) AS Ciclo,
                AOC.sobservacion AS SObservacion
            FROM administracionobservacioncomision AS AOC
            INNER JOIN administracionciclo AS AC ON AC.lciclo_id = AOC.lciclo_id
            INNER JOIN administracioncontacto AS ACT ON ACT.lcontacto_id = AOC.lcontacto_id
            WHERE AOC.lciclo_id = @lCicloId
            AND (@search IS NULL OR ACT.snombrecompleto LIKE CONCAT('%', @search, '%'))
            ORDER BY AOC.dtfechaadd DESC
            LIMIT @pageSize OFFSET @page;
        ";

        const string queryCount = @"
            SELECT COUNT(*)
            FROM administracionobservacioncomision AS AOC
            INNER JOIN administracionciclo AS AC ON AC.lciclo_id = AOC.lciclo_id
            INNER JOIN administracioncontacto AS ACT ON ACT.lcontacto_id = AOC.lcontacto_id
            WHERE AOC.lciclo_id = @lCicloId
            AND (@search IS NULL OR ACT.snombrecompleto LIKE CONCAT('%', @search, '%'));
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [scriptData: {queryData}]");
        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [scriptCount: {queryCount}]");

        try
        {
            using var connection = _context.CreateConnection();

            int total = await connection.ExecuteScalarAsync<int>(queryCount, new { lCicloId, search });

            var data = await connection.QueryAsync<ListaAdministracionObservacionComision>(
                queryData, new { lCicloId, search, pageSize, page }
            );

            bool success = data != null && data.Any();
            string mensaje = success
                ? "Registros obtenidos correctamente."
                : "No se encontraron registros para los criterios especificados.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, total:{total}, data:{JsonConvert.SerializeObject(data, Formatting.Indented)}]");

            return (data ?? Enumerable.Empty<ListaAdministracionObservacionComision>(), success, mensaje, total);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (Enumerable.Empty<ListaAdministracionObservacionComision>(), false, $"Error al obtener observaciones: {ex.Message}", 0);
        }
    }
    public async Task<(bool succes, string mensaje)> InsertAdministracionObservacionComision(string LogTransaccionId, AdministracionObservacionComision data)
    {
        string nombreMetodo = "InsertAdministracionObservacionComision()";

        const string query = @"
            INSERT INTO administracionobservacioncomision (
                susuarioadd,
                dtfechaadd,
                lobservacioncomision_id,
                susuariomod,
                dtfechamod,
                lcontacto_id,
                lciclo_id,
                sobservacion
            )
            SELECT 
                @usuario,
                NOW(),
                IFNULL(MAX_ID, 0) + 1,
                @usuario,
                NOW(),
                @LContactoId,
                @LCicloId,
                @SObservacion
            FROM (SELECT MAX(lobservacioncomision_id) AS MAX_ID 
                FROM administracionobservacioncomision) AS sub;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var rowsAffected = await connection.ExecuteAsync(query, new
            {
                usuario = data.usuario,
                data.LContactoId,
                data.LCicloId,
                data.SObservacion
            });

            bool success = rowsAffected > 0;
            string mensaje = success ? "Registro insertado correctamente." : "No se insertó ningún registro.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, rowsAffected:{rowsAffected}, data:{JsonConvert.SerializeObject(data, Formatting.Indented)}]");

            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al insertar observación de comisión: {ex.Message}");
        }
    }
    public async Task<(bool succes, string mensaje)> UpdateAdministracionObservacionComision(string LogTransaccionId, AdministracionObservacionComision data)
    {
        string nombreMetodo = "UpdateAdministracionObservacionComision()";

        const string query = @"
            UPDATE administracionobservacioncomision 
            SET 
                susuariomod = @Usuario, 
                dtfechamod = NOW(), 
                lcontacto_id = @LContactoId, 
                lciclo_id = @LCicloId, 
                sobservacion = @SObservacion
            WHERE lobservacioncomision_id = @LObservacionId;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var rowsAffected = await connection.ExecuteAsync(query, new
            {
                LObservacionId = data.lObservacionId,
                data.LContactoId,
                data.LCicloId,
                Usuario = data.usuario,
                data.SObservacion
            });

            bool success = rowsAffected > 0;
            string mensaje = success
                ? "Registro actualizado correctamente."
                : "No se encontró ningún registro con el ID especificado.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, rowsAffected:{rowsAffected}, data:{JsonConvert.SerializeObject(data, Formatting.Indented)}]");

            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al actualizar observación de comisión: {ex.Message}");
        }
    }
    public async Task<(bool succes, string mensaje)> DeleteAdministracionObservacionComision(string LogTransaccionId, int lObservacionId, string? usuario)
    {
        string nombreMetodo = "DeleteAdministracionObservacionComision()";

        if (lObservacionId <= 0)
        {
            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Fin/Inicio de metodo [El ID proporcionado no es válido.]");
            return (false, "El ID proporcionado no es válido.");
        }

        const string query = @"
            DELETE FROM administracionobservacioncomision
            WHERE lobservacioncomision_id = @lObservacionId;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var rowsAffected = await connection.ExecuteAsync(query, new { lObservacionId });

            bool success = rowsAffected > 0;
            string mensaje = success
                ? "Registro eliminado correctamente."
                : "No se encontró ningún registro con el ID especificado.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, rowsAffected:{rowsAffected}, usuario:{usuario}]");

            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Ocurrió un error al eliminar el registro: {ex.Message}");
        }
    }
}
