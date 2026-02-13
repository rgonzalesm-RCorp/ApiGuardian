using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using Newtonsoft.Json;

namespace ApiGuardian.Infrastructure.Repositories;

public class ProcesoComisionesRepository : IProcesoComisionesRepository
{
    private readonly DapperContext _context;
    private readonly ILogService _log;
    private string NOMBREARCHIVO = "ProcesoComisionesRepository.CS";
    public ProcesoComisionesRepository(DapperContext context, ILogService log)
    {
        _context = context;
        _log = log;
    }
    public async Task<(ItemProcesoJon Data, bool Success, string Mensaje)> GetProceso(string LogTransaccionId, string Proceso)
    {
        string nombreMetodo = "GetProceso()";

        string query = $@"
            select 
            J.proceso Proceso
            , J.estado Estado
            , C.dtfechainicio Inicio
            , C.dtfechafin Fin
            from administracionjob J
            inner join administracionciclo C on C.lciclo_id = J.lciclo_id and J.estado = 1 
            WHERE J.proceso = '{Proceso}'
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var procesoJob = await connection.QueryFirstOrDefaultAsync<ItemProcesoJon>(query);

            bool success = true;
            string mensaje = success ? "Tipos de descuento obtenidos correctamente." : "No se encontraron tipos de descuento.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, tiposDescuento:{JsonConvert.SerializeObject(procesoJob, Formatting.Indented)}]");

            return (procesoJob, success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (new ItemProcesoJon(), false, $"Error al obtener los tipos de descuento: {ex.Message}");
        }
    }
    public async Task<(IEnumerable<VentaPersonalComisionDto> Data, bool Success, string Mensaje)> GetCalculoVentaPersonal(string LogTransaccionId,string Usuario, string Inicio, string Fin, int LCicloId)
    {
        string nombreMetodo = "CalcularVentaPersonal()";
        try
        {
            string query = @"SELECT 
                                    c.lcontrato_id
                                    , c.lasesor_id lcontacta_id
                                    , ac.scedulaidentidad
                                    , ac.snombrecompleto
                                    , cp.snombre proyecto
                                    , c.snroventa
                                    , c.dprecio 
                                    , c.porcentaje_inicial PorcentajeInicial
                                    , c.dcuota_inicial inicial
                                    , CF.PorcentajeComision dporcentajecomision
                                    , (c.dcuota_inicial * CF.PorcentajeComision ) / 100 dcomision
                                    , CF.LCicloId lciclo_id
                                    , ASCC.lsemana_id
                                    , ASCC.lnrosemana
                                FROM administracioncontrato C
                                LEFT JOIN (
                                    SELECT 
                                        CVP.lciclo_id LCicloId
                                        , CVPC.lcomplejo_id LComplejoId
                                        , CVPI.inicial_desde PorcentajeInicialDesde
                                        , CVPI.inicial_hasta PorcentajeInicialHasta
                                        , CVPI.comision PorcentajeComision
                                        , AC.dtfechainicio Inicio
                                        , AC.dtfechafin Fin
                                    FROM  pc_configvtapersonal CVP 
                                    INNER JOIN pc_configvtapersonalcomplejo CVPC ON CVP.PC_ConfigVtaPersonalId = CVPC.PC_ConfigVtaPersonalId and CVP.estado = 1 and  CVPC.estado = 1
                                    INNER JOIN pc_configvtapersonalinicial CVPI ON CVP.PC_ConfigVtaPersonalId = CVPI.PC_ConfigVtaPersonalId and CVPI.estado = 1
                                    INNER JOIN administracionciclo AC ON AC.lciclo_id = CVP.lciclo_id
                                ) CF on CF.LComplejoId = C.lcomplejo_id AND c.porcentaje_inicial BETWEEN CF.PorcentajeInicialDesde and CF.PorcentajeInicialHasta
                                INNER JOIN administracioncontacto AC on AC.lcontacto_id = C.lasesor_id
                                INNER JOIN administracioncomplejo  CP on cp.lcomplejo_id = c.lcomplejo_id AND CF.LCicloId = @LCicloId
                                INNER JOIN administracionsemanaciclo ASCC ON ASCC.lciclo_id = CF.LCicloId 
                                WHERE dtfecha BETWEEN @Inicio and @Fin";
            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}, Usuario: {Usuario}, Inicio:{Inicio}, Fin:{Fin}, LCicloId:{LCicloId}]");
            using var connection = _context.CreateConnection();

            var ventaPersonal = await connection.QueryAsync<VentaPersonalComisionDto>(query, new {Inicio, Fin, LCicloId});

            bool success = ventaPersonal.Count() > 0 ? true : false ;
            string mensaje = success ? "Ventas personales obtenidos correctamente." : "No se encontraron ventas personales.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}]");

            return (ventaPersonal, success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (Enumerable.Empty<VentaPersonalComisionDto>(), false, $"Error al obtener ventas personales: {ex.Message}");
        }
    }
}
