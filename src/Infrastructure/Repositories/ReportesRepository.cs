using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using Newtonsoft.Json;
using ApiGuardian.Models;

namespace ApiGuardian.Infrastructure.Repositories;

public class ReportesRepository : IReportesRepository
{
    private readonly DapperContext _context;
    private readonly ILogService _log;
    private string NOMBREARCHIVO = "ReportesRepository.CS";

    #region SQL_SCRIPTS_REPORTE_COMISIONES

    private const string QUERY_VENTA_PERSONAL = @"
        SELECT 
            c.*,
            CASE 
                WHEN b.t = c.CuotaInicial THEN b.Cuotas
                WHEN d.m = c.CuotaInicial1 THEN d.Inicial
                WHEN (c.lCiclo_id >= 56 AND c.Comision <> 0 AND c.Porcentaje <> 0) THEN 'CUOTA INICIAL'
                WHEN (ty.m >= 1 OR d.m <= 12) THEN ty.Inicial
                ELSE ''
            END AS Tipo
        FROM (
            SELECT 
                cr.dtFecha AS Fecha,
                CONCAT(RTRIM(cr.sNroVenta), ' - ', vt.sNombreCompleto, ' (', vt.sCodigo, ')') AS NumeroVenta,
                vp.dPrecioLote CuotaInicial,
                vp.dPorcentajeComision Porcentaje,
                vp.dComision Comision,
                cr.dcuota_inicial AS CuotaInicial1,
                cr.lcontrato_id,
                vp.lCiclo_id,
                cp.lcomplejo_id AS lComplejoid,
                vp.lSemana_id,
                vp.lContacto_id
            FROM AdministracionVentaPersonal vp
            JOIN AdministracionCiclo cl ON vp.lCiclo_id = cl.lCiclo_id
            JOIN AdministracionContacto ct ON vp.lContacto_id = ct.lContacto_id
            JOIN AdministracionContrato cr ON vp.lContrato_id = cr.lContrato_id
            JOIN AdministracionContacto vt ON cr.lContacto_id = vt.lContacto_id
            JOIN AdministracionComplejo cp ON cr.lComplejo_id = cp.lComplejo_id
            JOIN AdministracionSemanaCiclo sc ON vp.lSemana_id = sc.lSemana_id AND vp.lCiclo_id = sc.lCiclo_id
            JOIN AdministracionNivel an  ON ct.lNivel_id = an.lNivel_id

            WHERE 
                vp.lCiclo_id = @lCicloId
                AND vp.lContacto_id = @lCotactoId
                AND (
                    (vp.lciclo_id >= 55 AND 
                        vp.lcontacto_id <> ( SELECT lcontacto_id FROM administracioncontacto WHERE scedulaidentidad = '4823437')
                    )
                    OR
                    (vp.lciclo_id < 55 AND 
                        vp.lcontacto_id IN ( SELECT lcontacto_id FROM administracioncontacto WHERE lcontacto_id > 3 )
                    )
                )
        ) AS c

        /* Cuotas pagadas */
        LEFT JOIN (
            SELECT CONCAT('CUOTA # ', a.nro_cuotas) AS Cuotas, a.Monto_Pagar AS t, vp.lContacto_id, vp.lContrato_id
            FROM tbl_promocion_pagados a
            JOIN administracionventapersonal vp ON a.lcontrato_id = vp.lcontrato_id AND a.lciclo_id = vp.lciclo_id
            WHERE vp.lciclo_id >= 56 AND vp.lCiclo_id = @lCicloId AND vp.lContacto_id = @lCotactoId
        ) AS b ON b.lContacto_id = c.lContacto_id AND b.lContrato_id = c.lContrato_id

        /* Cuota inicial por promo */
        LEFT JOIN (
            SELECT 'Cuota Inicial' AS Inicial, vp.dpreciolote AS m, vp.lContacto_id, vp.lContrato_id
            FROM administracionganadorespromo4semana a
            JOIN administracionventapersonal vp  ON a.lcontrato_id = vp.lcontrato_id AND a.lciclo_id = vp.lciclo_id
            WHERE vp.lciclo_id >= 56 AND vp.dcomision <> 0 AND vp.lCiclo_id = @lCicloId AND vp.lContacto_id = @lCotactoId
        ) AS d ON d.lContacto_id = c.lContacto_id AND d.lContrato_id = c.lContrato_id

        /* Cuota residual */
        LEFT JOIN (
            SELECT  'Cuo. Residual' AS Inicial, vp.dpreciolote AS m, vp.lContacto_id, vp.lContrato_id
            FROM t_productos_detalle_cuotas a
            JOIN administracionventapersonal vp ON a.lcontrato_id = vp.lcontrato_id AND a.lciclo_id = vp.lciclo_id
            WHERE vp.lCiclo_id = @lCicloId AND vp.lContacto_id = @lCotactoId
            GROUP BY vp.lcontrato_id, vp.lciclo_id
        ) AS ty ON ty.lContacto_id = c.lContacto_id AND ty.lContrato_id = c.lContrato_id

    ";

    private const string QUERY_VENTA_GRUPO = @"
        SELECT 
            c.*,
            ATC.abr Tipo,
            CASE
                WHEN c.lciclo_id >= 80 
                    THEN 'CUOTA INICIAL'
                ELSE ''
            END AS BasadoEn

        FROM (
            SELECT 
                
                CONCAT(rtrim(ct.sNombreCompleto), ' (', ct.sCodigo, ')') AS Asesor,
                vg.lGeneracion Generacion,
                vg.dVentaPersonal VentaPersonal,
                vg.dPorcentajeComision PorcentajeComision,
                vg.dComision Comision,
                vg.lciclo_id,
                vg.lContacto_id,
                vg.lContrato_id,
                cr.ltipocontrato_id
            FROM AdministracionVentaGrupo vg
            JOIN AdministracionContacto ct ON vg.lAsesor_id = ct.lContacto_id
            JOIN AdministracionContrato cr ON cr.lContrato_id = vg.lContrato_id
            WHERE vg.lciclo_id =  @lCicloId AND vg.lcontacto_id = @lCotactoId
        ) AS c
        INNER JOIN administraciontipocontrato ATC ON ATC.ltipocontrato_id = C.ltipocontrato_id

        ORDER BY c.Generacion;

    ";

    private const string QUERY_BONO_RESIDUAL = @"
        SELECT 
            ltipobono Tipo,
            lporcentajemora1g PocentajeGeneracion,
            1 Bxl,
            ltotalterrenossinmora Terrenos,
            dtotalbono Total
        FROM administracionbonoresidual ABR
        WHERE ABR.lcontacto_id = @lCotactoId AND lciclo_id = @lCicloId
    ";

    private const string QUERY_BONO_LIDERAZGO = @"
        SELECT 
            COUNT(*) Cantidad,
            SUM(pagar) Comision
        FROM t_bono_liderazgo 
        WHERE vendedores_id = @lCotactoId AND lciclo_id = @lCicloId
    ";

    private const string QUERY_ENCABEZADO = @"
        SELECT 
            (SELECT CONCAT(RTRIM(snombrecompleto), ' (', SCodigo, ')') 
                FROM administracioncontacto 
                WHERE lcontacto_id = @lCotactoId) NombreCompleto,
            UPPER(SNombre) Ciclo,
            dtfechainicio Inicio,
            dtfechafin Fin,
            PERIOD_DIFF(
                DATE_FORMAT(CURDATE(), '%Y%m'),
                DATE_FORMAT( (SELECT MIN(crr.dtfecha) FROM administracioncontrato crr WHERE crr.lasesor_id = @lCotactoId), '%Y%m')
            ) AS MesActividad
        FROM administracionciclo 
        WHERE lciclo_id = @lCicloId
    ";

    private const string QUERY_BONO_CARRERA = @"
        SELECT 
            AN.snombre NivelCiclo,
            ABC.dbonoporlote CantidadVentas
        FROM administracionbonocarrera ABC
        INNER JOIN administracionnivel AN ON AN.lnivel_id = ABC.lnivel_id
        WHERE ABC.lcontacto_id = @lCotactoId AND ABC.lciclo_id = @lCicloId
    ";

    #endregion
    #region SCRIPT_REPORTE_APLICACIONES"
    private const string QUERY_APLICACIONES = @"
        SELECT 
            vp.lcontacto_id LContactoId
            , vp.scodigo SCodigo
            , vp.scedulaidentidad SCedulaIdentidad
            , vp.snombrecompleto SNombreCompleto
            , vp.comisionVP ComisionVP
            , IFNULL(VG.comisionVG, 0) ComisionVG
            , IFNULL(BR.comisionBR, 0) ComisionBR
            , IFNULL(BL.comisionBL, 0) ComisionBL
            , IFNULL(RT.retencion, 0) Retencion
            , IFNULL(RT.porcentajeRetecion, 0) PorcentajeRetencion
            , IFNULL(DS.descuento, 0)Descuento
        FROM (
            SELECT sum(dcomision)comisionVP, a.lcontacto_id, b.snombrecompleto, b.scodigo, b.scedulaidentidad  FROM administracionventapersonal a
            inner JOIN administracioncontacto b on a.lcontacto_id = b.lcontacto_id
            WHERE a.lciclo_id = @lCicloId group by a.lcontacto_id
        ) VP
        LEFT JOIN (
            SELECT  sum(dcomision)comisionVG, lcontacto_id FROM administracionventagrupo WHERE lciclo_id = @lCicloId GROUP BY lcontacto_id
        ) VG ON VP.lcontacto_id = VG.lcontacto_id
        LEFT JOIN (
            SELECT  SUM(dtotalbono) comisionBR, lcontacto_id FROM administracionbonoresidual WHERE lciclo_id = @lCicloId GROUP BY lcontacto_id 
        ) BR ON VP.lcontacto_id = BR.lcontacto_id
        LEFT JOIN (
            SELECT  SUM(pagar) comisionBL, vendedores_id lcontacto_id FROM t_bono_liderazgo  WHERE lciclo_id = @lCicloId group by vendedores_id
        ) BL ON VP.lcontacto_id = BL.lcontacto_id
        LEFT JOIN(
            SELECT IFNULL(SUM(montoretencion),0) retencion,IFNULL(MAX(porcentajeret),0) porcentajeRetecion ,  lcontacto_id FROM tbl_retencionempresa WHERE lciclo_id = @lCicloId GROUP BY lcontacto_id
        ) RT ON VP.lcontacto_id = RT.lcontacto_id
        LEFT JOIN (
            SELECT IFNULL(SUM(dtotal),0) descuento, lcontacto_id FROM administraciondescuentociclo WHERE lciclo_id = @lCicloId GROUP BY lcontacto_id
        ) DS ON VP.lcontacto_id = DS.lcontacto_id
        ORDER BY vp.snombrecompleto
    ";
    #endregion
    public ReportesRepository(DapperContext context, ILogService log)
    {
        _context = context;
        _log = log;
    }
    public async Task<( ReporteComisionesDto Data ,  bool Success, string Mensaje)> GetReporteComision(string LogTransaccionId, int lCicloId, int lCotactoId)
    {
        string NombreMetodo = "GetReporteComision()";

        ReporteComisionesDto DataResponse = new ReporteComisionesDto();
        _log.Info(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, $"Inicio de metodo [script venta personal: {QUERY_VENTA_PERSONAL}]");
        _log.Info(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, $"Inicio de metodo [script venta grupo: {QUERY_VENTA_GRUPO}]");
        _log.Info(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, $"Inicio de metodo [script bono residual: {QUERY_BONO_RESIDUAL}]");
        _log.Info(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, $"Inicio de metodo [script bono liderazgo: {QUERY_BONO_LIDERAZGO}]");
        _log.Info(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, $"Inicio de metodo [script encabezado: {QUERY_ENCABEZADO}]");
        _log.Info(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, $"Inicio de metodo [script bon carrera: {QUERY_BONO_CARRERA}]");

        try
        {
            using var connection = _context.CreateConnection();

            DataResponse.VentasPersonales = await connection.QueryAsync<VentaItem>(QUERY_VENTA_PERSONAL, new { lCicloId, lCotactoId });
            DataResponse.VentasGrupo = await connection.QueryAsync<VentaGrupoItem>(QUERY_VENTA_GRUPO, new { lCicloId, lCotactoId });
            DataResponse.BonoRedisual = await connection.QueryAsync<BonoRedisualItem>(QUERY_BONO_RESIDUAL, new { lCicloId, lCotactoId });
            DataResponse.BonoLiderazgo = await connection.QueryAsync<BonoLiderazgoItem>(QUERY_BONO_LIDERAZGO, new { lCicloId, lCotactoId });
            DataResponse.Encabezado = await connection.QuerySingleOrDefaultAsync<Encabezado>(QUERY_ENCABEZADO, new { lCicloId, lCotactoId });
            DataResponse.BonoCarrera = await connection.QueryAsync<BonoCarrera>(QUERY_BONO_CARRERA, new { lCicloId, lCotactoId });


             
            return (DataResponse , true, "Okey");
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, "Fin de metodo", ex);
            return (new ReporteComisionesDto() , false, $"Error al obtener el reporte de comisiones: {ex.Message}");
        }
    }
    public async Task<(RptAplicaciones Data , bool Success, string Mensaje)> GetReporteAplicacines(string LogTransaccionId, int lCicloId, int lCotactoId)
    {
         const string NombreMetodo = "GetReporteAplicacines()";
        _log.Info(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, $"Inicio de metodo [script aplicaciones: {QUERY_APLICACIONES}]");
        RptAplicaciones DataResponse = new RptAplicaciones();


        try
        {
            using var connection = _context.CreateConnection();

            DataResponse.Aplicaciones = await connection.QueryAsync<AplicacionesItem>(
                QUERY_APLICACIONES,
                new { lCicloId, lCotactoId }
            );

            return (DataResponse, true, "listado de aplicaciones obtenidas correctamente.");
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, "Fin de metodo", ex);
            return (new RptAplicaciones(), false,  $"Error al obtener aplicaciones del ciclo: {ex.Message}");
        }

    }
     
}
