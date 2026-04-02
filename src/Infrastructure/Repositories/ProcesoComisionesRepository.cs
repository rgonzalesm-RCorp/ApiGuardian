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
    public async Task<(IEnumerable<VentaPersonalComisionDto> Data, IEnumerable<VentaPersonalComisionDto> ListaVtaPersonal, bool Success, string Mensaje)> GetCalculoVentaPersonal(string LogTransaccionId,string Usuario, string Inicio, string Fin, int LCicloId)
    {
        string nombreMetodo = "CalcularVentaPersonal()";
        try
        {
            string query = @"SELECT 
                                    c.lcontrato_id
                                    , c.lasesor_id lcontacta_id
                                    , c.dtfecha fechaVenta
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
                                ) CF on CF.LComplejoId = C.lcomplejo_id AND c.porcentaje_inicial BETWEEN CF.PorcentajeInicialDesde and CF.PorcentajeInicialHasta AND CF.LCicloId = @LCicloId
                                INNER JOIN administracioncontacto AC on AC.lcontacto_id = C.lasesor_id
                                INNER JOIN administracioncomplejo  CP on cp.lcomplejo_id = c.lcomplejo_id 
                                LEFT JOIN administracionsemanaciclo ASCC ON ASCC.lciclo_id = CF.LCicloId 
                                WHERE dtfecha BETWEEN @Inicio and @Fin order by c.lcontrato_id desc";
            string queryVtaPersonl = @"SELECT 
                                    c.lcontrato_id
                                    , c.lasesor_id lcontacta_id
                                    , c.dtfecha fechaVenta
                                    , vp.dtfechaadd fechaCalculo
                                    , ac.scedulaidentidad
                                    , ac.snombrecompleto
                                    , cp.snombre proyecto
                                    , c.snroventa
                                    , c.dprecio 
                                    , c.porcentaje_inicial PorcentajeInicial
                                    , c.dcuota_inicial inicial
                                    , VP.dporcentajecomision dporcentajecomision
                                    , VP.dcomision dcomision
                                    , VP.lciclo_id lciclo_id
                                    , ASCC.lsemana_id
                                    , ASCC.lnrosemana
                                FROM administracioncontrato C
                                INNER JOIN administracioncontacto AC on AC.lcontacto_id = C.lasesor_id
                                INNER JOIN administracioncomplejo CP on cp.lcomplejo_id = c.lcomplejo_id 
                                INNER JOIN administracionventapersonal VP ON VP.lcontrato_id = C.lcontrato_id AND VP.lciclo_id = @LCicloId
                                INNER JOIN administracionsemanaciclo ASCC ON ASCC.lciclo_id = @LCicloId 
                                order by c.lcontrato_id desc
            ";
            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}, Usuario: {Usuario}, Inicio:{Inicio}, Fin:{Fin}, LCicloId:{LCicloId}]");
            using var connection = _context.CreateConnection();

            var ventaPersonal = await connection.QueryAsync<VentaPersonalComisionDto>(query, new {Inicio, Fin, LCicloId});
            var ventaPersonalCalulada = await connection.QueryAsync<VentaPersonalComisionDto>(queryVtaPersonl, new {LCicloId});

            bool success = ventaPersonal.Count() > 0 ? true : false ;
            string mensaje = success ? "Ventas personales obtenidos correctamente." : "No se encontraron ventas personales.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}]");

            return (ventaPersonal,ventaPersonalCalulada, success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (Enumerable.Empty<VentaPersonalComisionDto>(), Enumerable.Empty<VentaPersonalComisionDto>(), false, $"Error al obtener ventas personales: {ex.Message}");
        }
    }
    public async Task<(bool Success, string Mensaje)> GuardarVtaRezagadas(string LogTransaccionId, List<ItemVentaCnx> Data, string Usuario)
    {
         string metodo = "GuardarVtaRezagadas()";

        const string insertQuery = @"INSERT INTO VentaRezagadasCiclo (
                    empresaId, lContratoId, dFecha, sManzano, sLote, dPrecio, lComplejoId, idVenta,
                    lote,  suv, precioInicial, sCuotaInicial, idCliente, telefonoFijo,
                    telefonoMovil, correo, fechaNacimiento, direccion,
                    idPaisResidencia, sCedulaIdentidad, sCiudad, fechaRegistro,
                    sNombreCompleto, sTelefonoOficina, sContrasena, vendedorId,
                    telefonoFijoVendedor, telefonoMovilVendedor, correoVendedor, fechaNacimientoVendedor,
                    direccionVendedor, idPaisResidenciaVendedor, sCedulaIdentidadVendedor, fechaRegistroVendedor,
                    sNombreCompletoVendedor, sTelefonoOficinaVendedor, sContrasenaVendedor, sCiudadVendedor,
                    complejo, tipoVenta, porcentajeCuotaInicial, EstadoVentaRezagadasCicloId, FechaRegistroGrd
                ) VALUES (
                    @empresaId, @lContratoId, @dFecha,  @sManzano, @sLote, @dPrecio, @lComplejoId, @idVenta,
                    @lote,  @suv, @precioInicial, @sCuotaInicial, @idCliente, @telefonoFijo,
                    @telefonoMovil, @correo, @fechaNacimiento, @direccion,
                    @idPaisResidencia, @sCedulaIdentidad, @sCiudad, @fechaRegistro,
                    @sNombreCompleto, @sTelefonoOficina, @sContrasena, @vendedorId,
                    @telefonoFijoVendedor, @telefonoMovilVendedor, @correoVendedor, @fechaNacimientoVendedor,
                    @direccionVendedor, @idPaisResidenciaVendedor, @sCedulaIdentidadVendedor, @fechaRegistroVendedor,
                    @sNombreCompletoVendedor, @sTelefonoOficinaVendedor, @sContrasenaVendedor, @sCiudadVendedor,
                    @complejo, @tipoVenta, @porcentajeCuotaInicial, @EstadoVentaRezagadasCicloId, @FechaRegistroGrd
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

    public async Task<(bool Success, string Mensaje, IEnumerable<ItemVentaCnx> Data)> GetVtaRezada(string LogTransaccionId, string Usuario)
    {
        string nombreMetodo = "GetVtaRezada()";

        string query = $@"select * from VentaRezagadasCiclo WHERE EstadoVentaRezagadasCicloId = 1;";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var procesoJob = await connection.QueryAsync<ItemVentaCnx>(query);

            bool success = true;
            string mensaje = success ? "Ventas rezzagadas obtenidos correctamente." : "No se encontraron las ventas rezagadas.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, VentasRezagadas:{JsonConvert.SerializeObject(procesoJob, Formatting.Indented)}]");

            return (success, mensaje, procesoJob);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return ( false, $"Error al obtener las ventas rezagadas: {ex.Message}", Enumerable.Empty<ItemVentaCnx>());
        }
    }
    public async Task<(bool Success, string Mensaje)> UpdateVtaRezagadas(string LogTransaccionId, ItemVentaCnx Data, string Usuario)
    {
        string nombreMetodo = "UpdateVtaRezagadas()";

        const string query = @"
            update VentaRezagadasCiclo set EstadoVentaRezagadasCicloId = 2, FechaProceso = NOW() where idVenta = @IdVenta and lote = @Lote
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var rows = await connection.ExecuteAsync(query, new
            {
                Data.IdVenta,
                Data.Lote
            });

            bool success = rows > 0;
            string mensaje = success ? "Registro actualizado correctamente." : "No se encontró el registro o no se realizaron cambios.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, rowsAffected:{rows}, data:{JsonConvert.SerializeObject(Data, Formatting.Indented)}]");

            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al actualizar banco: {ex.Message}");
        }
    }

    public async Task<(IEnumerable<ItemComisionVentaGrupoDto> Data, bool Success, string Mensaje)> GetCalculoVentaGrupo(string LogTransaccionId,string Usuario, string Inicio, string Fin, int LCicloId)
    {
        string nombreMetodo = "GetCalculoVentaGrupo()";
        try
        {
            string query = @"select 
                                    AD.SNombreCompleto nombreVendedor, t1.*, cm.porcentaje , 
                                    CASE WHEN t1.dCuotaInicial <= 0.00 then 0 else
                                    t1.dCuotaInicial * cm.porcentaje / 100 
                                    end
                                    comision ,
                                    CASE WHEN t1.dCuotaInicial <= 0.00 then 0 else
                                    1
                                    end esCero
                                from 
                                (select DISTINCT lasesor_id from administracioncontrato where dtfecha BETWEEN @Inicio and @Fin ) t 
                                inner join (
                                
                                SELECT 
                                    C.lasesor_id lVendedorId,
                                    ACT.lcontacto_id lGanadorId,
                                    ACT.SNombreCompleto nombreGanador,
                                    c.snroventa sNroVenta,
                                    c.lContrato_id lContratoId,
                                    c.dcuota_inicial dCuotaInicial,
                                    c.dtfecha,
                                    1 Nivel
                                FROM administracioncontrato C 
                                JOIN red_sion RS ON RS.lContacto_id = c.lasesor_id
                                JOIN administracioncontacto ACT ON ACT.lcontacto_id = RS.Nivel_1
                                WHERE c.dtfecha BETWEEN @Inicio and @Fin AND ACT.cbaja = 0

                                UNION ALL

                                SELECT 
                                    C.lasesor_id lVendedorId,
                                    ACT.lcontacto_id lGanadorId,
                                    ACT.SNombreCompleto nombeGanador,
                                    c.snroventa sNroVenta,
                                    c.lContrato_id lContratoId,
                                    c.dcuota_inicial dCuotaInicial,
                                    c.dtfecha,
                                    2 Nivel
                                FROM administracioncontrato C 
                                JOIN red_sion RS ON RS.lContacto_id = c.lasesor_id
                                JOIN administracioncontacto ACT ON ACT.lcontacto_id = RS.Nivel_2
                                WHERE c.dtfecha BETWEEN @Inicio and @Fin AND ACT.cbaja = 0

                                UNION ALL

                                SELECT 
                                    C.lasesor_id lVendedorId,
                                    ACT.lcontacto_id lGanadorId,
                                    ACT.SNombreCompleto nombeGanador,
                                    c.snroventa sNroVenta,
                                    c.lContrato_id lContratoId,
                                    c.dcuota_inicial dCuotaInicial,
                                    c.dtfecha,
                                    3 Nivel
                                FROM administracioncontrato C 
                                JOIN red_sion RS ON RS.lContacto_id = c.lasesor_id
                                JOIN administracioncontacto ACT ON ACT.lcontacto_id = RS.Nivel_3
                                WHERE c.dtfecha BETWEEN @Inicio and @Fin AND ACT.cbaja = 0

                                UNION ALL

                                SELECT 
                                    C.lasesor_id lVendedorId,
                                    ACT.lcontacto_id lGanadorId,
                                    ACT.SNombreCompleto nombeGanador,
                                    c.snroventa sNroVenta,
                                    c.lContrato_id lContratoId,
                                    c.dcuota_inicial dCuotaInicial,
                                    c.dtfecha,
                                    4 Nivel
                                FROM administracioncontrato C 
                                JOIN red_sion RS ON RS.lContacto_id = c.lasesor_id
                                JOIN administracioncontacto ACT ON ACT.lcontacto_id = RS.Nivel_4
                                WHERE c.dtfecha BETWEEN @Inicio and @Fin AND ACT.cbaja = 0

                                UNION ALL

                                SELECT 
                                    C.lasesor_id lVendedorId,
                                    ACT.lcontacto_id lGanadorId,
                                    ACT.SNombreCompleto nombeGanador,
                                    c.snroventa sNroVenta,
                                    c.lContrato_id lContratoId,
                                    c.dcuota_inicial dCuotaInicial,
                                    c.dtfecha,
                                    5 Nivel
                                FROM administracioncontrato C 
                                JOIN red_sion RS ON RS.lContacto_id = c.lasesor_id
                                JOIN administracioncontacto ACT ON ACT.lcontacto_id = RS.Nivel_5
                                WHERE c.dtfecha BETWEEN @Inicio and @Fin AND ACT.cbaja = 0

                                UNION ALL

                                SELECT 
                                    C.lasesor_id lVendedorId,
                                    ACT.lcontacto_id lGanadorId,
                                    ACT.SNombreCompleto nombeGanador,
                                    c.snroventa sNroVenta,
                                    c.lContrato_id lContratoId,
                                    c.dcuota_inicial dCuotaInicial,
                                    c.dtfecha,
                                    6 Nivel
                                FROM administracioncontrato C 
                                JOIN red_sion RS ON RS.lContacto_id = c.lasesor_id
                                JOIN administracioncontacto ACT ON ACT.lcontacto_id = RS.Nivel_6
                                WHERE c.dtfecha BETWEEN @Inicio and @Fin AND ACT.cbaja = 0

                                UNION ALL

                                SELECT 
                                    C.lasesor_id lVendedorId,
                                    ACT.lcontacto_id lGanadorId,
                                    ACT.SNombreCompleto nombeGanador,
                                    c.snroventa sNroVenta,
                                    c.lContrato_id lContratoId,
                                    c.dcuota_inicial dCuotaInicial,
                                    c.dtfecha,
                                    7 Nivel
                                FROM administracioncontrato C 
                                JOIN red_sion RS ON RS.lContacto_id = c.lasesor_id
                                JOIN administracioncontacto ACT ON ACT.lcontacto_id = RS.Nivel_7
                                WHERE c.dtfecha BETWEEN @Inicio and @Fin AND ACT.cbaja = 0

                            ) t1 on t.lasesor_id = t1.lGanadorId
                            inner join (
                                select 1 Nivel, dporcentaje1g porcentaje from administraciontipocontacto where ltipocontacto_id = 9
                                union ALL
                                select 2, dporcentaje2g from administraciontipocontacto where ltipocontacto_id = 9
                                union ALL
                                select 3, dporcentaje3g from administraciontipocontacto where ltipocontacto_id = 9
                                union ALL
                                select 4, dporcentaje4g from administraciontipocontacto where ltipocontacto_id = 9
                                union ALL
                                select 5, dporcentaje5g from administraciontipocontacto where ltipocontacto_id = 9
                                union ALL
                                select 6, dporcentaje6g from administraciontipocontacto where ltipocontacto_id = 9
                                union ALL
                                select 7, dporcentaje7g from administraciontipocontacto where ltipocontacto_id = 9
                            ) cm on cm.nivel = t1.nivel
                            INNER JOIN administracioncontacto AD on AD.lcontacto_id = t1.lVendedorId
                            where t1.dtfecha BETWEEN @Inicio and @Fin ORDER BY t1.dtfecha desc";
            
            query = @"select 
                        vend.SNombreCompleto nombreVendedor
                        , vend.lcontacto_id lVendedorId
                        , gan.lcontacto_id lGanadorId
                        , gan.SNombreCompleto nombreGanador
                        , ACTR.snroventa sNroVenta
                        , ACTR.lcontrato_id lContratoId
                        , ACTR.dcuota_inicial dCuotaInicial
                        , ACTR.dtfecha 
                        , RC.nivel
                        , cm.porcentaje
                        , CASE WHEN actr.dcuota_inicial <= 0.00 then 0 else ACTR.dcuota_inicial * cm.porcentaje / 100 end comision
                        , CASE WHEN actr.dcuota_inicial <= 0.00 then 0 else 1 end esCero
                    from red_comprimida RC 
                    INNER join administracioncontrato ACTR on ACTR.lasesor_id = rc.lcontacto_id and RC.lciclo_id = @LCicloId

                    INNER JOIN administracioncontacto VEND on VEND.lcontacto_id = rc.lcontacto_id
                    INNER JOIN administracioncontacto GAN on GAN.lcontacto_id = rc.lasesor_id
                    INNER JOIN (
                                    select 1 Nivel, dporcentaje1g porcentaje from administraciontipocontacto where ltipocontacto_id = 9
                                    union ALL
                                    select 2, dporcentaje2g from administraciontipocontacto where ltipocontacto_id = 9
                                    union ALL
                                    select 3, dporcentaje3g from administraciontipocontacto where ltipocontacto_id = 9
                                    union ALL
                                    select 4, dporcentaje4g from administraciontipocontacto where ltipocontacto_id = 9
                                    union ALL
                                    select 5, dporcentaje5g from administraciontipocontacto where ltipocontacto_id = 9
                                    union ALL
                                    select 6, dporcentaje6g from administraciontipocontacto where ltipocontacto_id = 9
                                    union ALL
                                    select 7, dporcentaje7g from administraciontipocontacto where ltipocontacto_id = 9
                                ) cm on cm.Nivel = RC.nivel
                    where ACTR. dtfecha BETWEEN @Inicio and @Fin ";
            
            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}, Usuario: {Usuario}, Inicio:{Inicio}, Fin:{Fin}, LCicloId:{LCicloId}]");
            using var connection = _context.CreateConnection();

            var ventaGrupo = await connection.QueryAsync<ItemComisionVentaGrupoDto>(query, new {Inicio, Fin, LCicloId});

            bool success = ventaGrupo.Count() > 0 ? true : false ;
            string mensaje = success ? "Ventas personales obtenidos correctamente." : "No se encontraron ventas personales.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}]");

            return (ventaGrupo, success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (Enumerable.Empty<ItemComisionVentaGrupoDto>(), false, $"Error al obtener ventas personales: {ex.Message}");
        }
    }

}
