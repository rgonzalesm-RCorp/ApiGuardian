using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using Newtonsoft.Json;

namespace ApiGuardian.Infrastructure.Repositories;

public class ConfiguracionProcesoComisionesRepository : IConfiguracionProcesoComisionesRepository
{
    private readonly DapperContext _context;
    private readonly ILogService _log;
    private string NOMBREARCHIVO = "ConfiguracionProcesoComisionesRepository.cs";
    public ConfiguracionProcesoComisionesRepository(DapperContext context, ILogService log)
    {
        _context = context;
        _log = log;
    }
    public async Task<(bool Success, string Mensaje)> GuardarConfiguracionComisionVentaPersonal(string LogTransaccionId, PC_ConfigVtaPersonal pC_ConfigVtaPersonal)
    {
        string nombreMetodo = "GuardarConfiguracionComisionVentaPersonal()";

        string query = $@"INSERT INTO pc_configvtapersonal 
                            (PC_ConfigVtaPersonalId, lciclo_id, estado, fechaadd, usuarioadd, fechamod, usuariomod)
                        VALUES
                            (0, @LCiclo_id, 1, NOW(), @Usuario, NOW(), @Usuario);
                        SELECT IFNULL(MAX(PC_ConfigVtaPersonalId), 0) FROM pc_configvtapersonal;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var ConfiguracionVentaPersonalId = await connection.ExecuteScalarAsync<int>(query, new
            {
                pC_ConfigVtaPersonal.LCiclo_id,
                pC_ConfigVtaPersonal.Usuario
            });
            foreach (var item in pC_ConfigVtaPersonal.Complejos)
            {
                string queryComplejo = $@"INSERT INTO pc_configvtapersonalcomplejo 
                                            (pc_configvtapersonalcomplejoId, PC_ConfigVtaPersonalId, lcomplejo_id, estado, fechaadd, usuarioadd, fechamod, usuariomod)
                                        VALUES
                                            (0, @ConfiguracionVentaPersonalId, @LComplejo_id, 1, NOW(), @Usuario, NOW(), @Usuario)";
                var responseComplejo = await connection.QueryAsync(queryComplejo, new
                {
                    ConfiguracionVentaPersonalId,
                    item.LComplejo_id,
                    item.Usuario
                });
            }
            foreach (var item in pC_ConfigVtaPersonal.Inicials)
            {
                string queryIncial = $@"insert into pc_configvtapersonalinicial
                        (pc_configvtapersonalinicialId, pc_configvtapersonalId, inicial_desde, inicial_hasta, comision, estado, fechaadd, usuarioadd, fechamod, usuariomod)
                    VALUES
                        (0, @ConfiguracionVentaPersonalId, @Inicial_desde, @Inicial_hasta, @Comision, 1, NOW(), @Usuario, NOW(), @Usuario)";
                var responseInicial = await connection.QueryAsync(queryIncial, new
                {
                    ConfiguracionVentaPersonalId,
                    item.Inicial_desde,
                    item.Inicial_hasta, 
                    item.Comision,
                    item.Usuario
                });
            }

            bool success = true;
            string mensaje = success ? "Configuracion de venta personal guardado correctamente." : "Configuracion de venta personal no se guardo correctamente.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}]");

            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al obtener al guardar la configuracion de venta personal: {ex.Message}");
        }
    }
    public async Task<(bool Success, string Mensaje, IEnumerable<PC_ConfigVtaPersonal> pC_ConfigVtaPersonal)> GETConfiguracionComisionVentaPersonal(string LogTransaccionId)
    {
        string nombreMetodo = "GuardarConfiguracionComisionVentaPersonal()";

        string query = $@"
            select PC_ConfigVtaPersonalId, LCiclo_id, Estado , usuarioadd Usuario from pc_configvtapersonal where estado = 1 and lciclo_id > 0;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var pc_ConfigVtaPersonal = await connection.QueryAsync<PC_ConfigVtaPersonal>(query);
            foreach (var item in pc_ConfigVtaPersonal)
            {
                string queryComplejo = $@"
                                        SELECT PC_ConfigVtaPersonalComplejoId, PC_ConfigVtaPersonalId
                                        , LComplejo_id, usuarioadd Usuario
                                        FROM pc_configvtapersonalcomplejo where estado = 1 
                                        and PC_ConfigVtaPersonalId = @PC_ConfigVtaPersonalId";
                string queryInicial = @"SELECT 
                                            PC_ConfigVtaPersonalInicialId
                                            , PC_ConfigVtaPersonalId
                                            , Inicial_desde
                                            , Inicial_hasta
                                            , Comision
                                            , usuarioadd Usuario
                                            from pc_configvtapersonalinicial
                                        where estado = 1 and PC_ConfigVtaPersonalId = @PC_ConfigVtaPersonalId";
                var responseComplejo = await connection.QueryAsync<PC_ConfigVtaPersonalComplejo>(queryComplejo, new
                {
                    item.PC_ConfigVtaPersonalId
                });
                item.Complejos = responseComplejo.ToList();

                var responseInicial = await connection.QueryAsync<PC_ConfigVtaPersonalInicial>(queryInicial, new
                {
                    item.PC_ConfigVtaPersonalId
                });
                item.Inicials = responseInicial.ToList();
            }


            bool success = true;
            string mensaje = success ? "Configuracion de venta personal guardado correctamente." : "Configuracion de venta personal no se guardo correctamente.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}]");

            return (success, mensaje, pc_ConfigVtaPersonal );
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al obtener al guardar la configuracion de venta personal: {ex.Message}", Enumerable.Empty<PC_ConfigVtaPersonal>());
        }
    }
}

