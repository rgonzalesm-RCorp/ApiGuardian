using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using Newtonsoft.Json;

namespace ApiGuardian.Infrastructure.Repositories;

public class ControlProcesoRepository : IControlProcesoRepository
{
    private readonly DapperContext _context;
    private readonly ILogService _log;
    private string NOMBREARCHIVO = "ControlProcesoRepository.CS";
    public ControlProcesoRepository(DapperContext context, ILogService log)
    {
        _context = context;
        _log = log;
    }
    public async Task<(ItemControlProceso Data, bool Success, string Mensaje)> GetControlProceso(string LogTransaccionId, string Usuario, string Paso, int LCicloId)
    {
         string nombreMetodo = "GetProceso()";

        string query = $@"select * from ControlProceso where paso = @Paso and lciclo_id = @LCicloId";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var proceso = await connection.QueryFirstOrDefaultAsync<ItemControlProceso>(query, new {Paso, LCicloId});

            bool success = true;
            string mensaje = success ? "Tipos de descuento obtenidos correctamente." : "No se encontraron tipos de descuento.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, tiposDescuento:{JsonConvert.SerializeObject(proceso, Formatting.Indented)}]");

            return (proceso ?? new ItemControlProceso(), success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (new ItemControlProceso(), false, $"Error al obtener los tipos de descuento: {ex.Message}");
        }
    }
    public async Task<(bool Success, string Mensaje)> GuardarControlProceso(string LogTransaccionId, string Usuario, ItemControlProceso Data)
    {
        string metodo = "GuardarControlProceso()";

        const string insertQuery = @"INSERT INTO ControlProceso (
                                        lciclo_id,
                                        paso,
                                        inicio,
                                        fin,
                                        estado,
                                        fechaadd,
                                        usuarioadd,
                                        fechamod,
                                        usuariomod
                                    )
                                    VALUES (
                                        @lciclo_id,
                                        @paso,
                                        @inicio,
                                        @fin,
                                        @estado,
                                        @fechaadd,
                                        @usuarioadd,
                                        @fechamod,
                                        @usuariomod
                                    );";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo, $"Inicio inserción. Script: { insertQuery}");

        try
        {
            using var con = _context.CreateConnection();

            var rows = await con.ExecuteAsync(insertQuery, Data);

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
    public async Task<(bool Success, string Mensaje)> UpdateControlProceso(string LogTransaccionId, string Usuario, string Paso, int LCicloId)
    {
        string nombreMetodo = "UpdateControlProceso()";

        const string query = @"
            UPDATE ControlProceso SET fechamod = NOW(), usuariomod = @Usuario, fin = NOW() WHERE paso = @Paso AND lciclo_id = @LCicloId    
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var rows = await connection.ExecuteAsync(query, new
            {
                Usuario, Paso, LCicloId
            });

            bool success = rows > 0;
            string mensaje = success ? "Registro actualizado correctamente." : "No se encontró el registro o no se realizaron cambios.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, rowsAffected:{rows}, Usuario:{Usuario}, Paso:{Paso}, LCicloId: {LCicloId} ]");

            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al actualizar el registro: {ex.Message}");
        }
    }
}
