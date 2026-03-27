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
                        select cvp.PC_ConfigVtaPersonalId, cvp.LCiclo_id, cvp.Estado , cvp.usuarioadd Usuario, ac.snombre Ciclo
                        from pc_configvtapersonal  cvp
                        inner join administracionciclo ac on ac.lciclo_id = cvp.lciclo_id
                        where cvp.estado = 1 and cvp.lciclo_id > 0 order by cvp.LCiclo_id desc, cvp.PC_ConfigVtaPersonalId desc;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var pc_ConfigVtaPersonal = await connection.QueryAsync<PC_ConfigVtaPersonal>(query);
            foreach (var item in pc_ConfigVtaPersonal)
            {
                string queryComplejo = $@"
                                        SELECT cvpc.PC_ConfigVtaPersonalComplejoId, cvpc.PC_ConfigVtaPersonalId
                                        , cvpc.LComplejo_id, cvpc.usuarioadd Usuario, ac.snombre Complejo
                                        FROM pc_configvtapersonalcomplejo cvpc 
                                        inner join administracioncomplejo ac on cvpc.lcomplejo_id = ac.lcomplejo_id
                                        where cvpc.estado = 1 
                                        and cvpc.PC_ConfigVtaPersonalId = @PC_ConfigVtaPersonalId";
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
    public async Task<(bool Success, string Mensaje)> DeleteConfiguracionComisionVentaPersonal(string LogTransaccionId, string usuario, int PC_ConfigVtaPersonalId)
    {
        string nombreMetodo = "DeleteConfiguracionComisionVentaPersonal()";
        string query = @"
            update  pc_configvtapersonal set estado = 0, usuariomod = @usuario, fechamod = now() where PC_ConfigVtaPersonalId = @PC_ConfigVtaPersonalId;
        ";
        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");
        try
        {
            
            using var connection = _context.CreateConnection();

            var rows = await connection.ExecuteAsync(query, new
            {
                usuario
                , PC_ConfigVtaPersonalId
            });

            bool success = rows > 0;
            string mensaje = success ? "Configuracion eliminada correctamente" : "No se encontró al configuracion a eliminar";
            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Fin de metodo [mensaje: {mensaje}, rowsAffected:{rows}]");
            
            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al eliminar la confiuracion: {ex.Message}");
        }
    }
    public async Task<(bool Success, string Mensaje, IEnumerable<PC_VerificarListaComplejos> Listado)> VerificarComplejos(string LogTransaccionId, string complejosId, int LCicloId)
    {
        string nombreMetodo = "VerificarComplejos()";

        string query = $@"SELECT 
                            CVP.lciclo_id LCicloId
                            , CVPC.lcomplejo_id LComplejoId
                            , AC.SNOMBRE Complejo
                            FROM  pc_configvtapersonal CVP 
                            INNER JOIN pc_configvtapersonalcomplejo CVPC ON CVP.PC_ConfigVtaPersonalId = CVPC.PC_ConfigVtaPersonalId and CVP.estado = 1 and  CVPC.estado = 1
                            INNER JOIN administracioncomplejo AC on AC.lcomplejo_id = CVPC.lcomplejo_id
                            WHERE CVP.lciclo_id = @LCicloId and CVPC.lcomplejo_id in ({complejosId});
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var pc_ListaComplejo = await connection.QueryAsync<PC_VerificarListaComplejos>(query, new{LCicloId});
         
            bool success = true;
            string mensaje = success ? "Lista obtenidas correctamente." : "no se pudo obtener la lista.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}]");

            return (success, mensaje, pc_ListaComplejo );
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al obtener el listado: {ex.Message}", Enumerable.Empty<PC_VerificarListaComplejos>());
        }
    }
}

