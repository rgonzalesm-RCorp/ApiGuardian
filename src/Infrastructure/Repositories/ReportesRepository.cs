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

    #region "SQL_SCRIPTS_REPORTE_COMISIONES"
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
    #region "SCRIPT_REPORTE_APLICACIONES"
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
        WHERE CASE WHEN @lContactoId <= 0 THEN vp.lcontacto_id > 0 ELSE vp.lcontacto_id = @lContactoId END 
        
        ORDER BY vp.snombrecompleto
        
    ";
    #endregion
    #region "SCRITP_DESCUENTO_EMPRESA"
    private const string QUERY_DESCUENTO_EMPRESA = @"SELECT
                    UPPER(concat(ciclo.snombre, '(', DATE_FORMAT( ciclo.dtFechaInicio, '%d/%m/%Y' ), ' - ',    DATE_FORMAT( ciclo.dtFechaFin, '%d/%m/%Y' ),    ')' )) as Ciclo
                    , contacto.snombrecompleto as Asesor
                    , UPPER(complejo.snombre) as Complejo
                    , detDesc.smanzano as Mz
                    , detDesc.slote as Lote
                    , detDesc.suv as Uv
                    , UPPER(tipoDesc.snombre) as Tipo
                    , detDesc.dmonto as Monto
                    , detDesc.sobservacion as Observacion
                    , complejo.lcomplejo_id
                    , UPPER(empresa.snombre) as Empresa
                    , empresa.lempresa_id
                FROM
                    administraciondescuentociclo cicloDesc
                    INNER JOIN administraciondescuentociclodetalle detDesc USING ( ldescuentociclo_id )
                    inner join administracionciclo ciclo using(lciclo_id)
                    inner join administracionsemanaciclo semana on semana.lciclo_id = ciclo.lciclo_id
                    inner join administraciondescuentociclotipo tipoDesc on tipoDesc.ldescuentociclotipo_id = detDesc.ldescuentociclotipo_id
                    inner join administracioncomplejo complejo on complejo.lcomplejo_id = detDesc.lcomplejo_id
                    inner join administracioncontacto contacto on contacto.lcontacto_id = cicloDesc.lcontacto_id
                    inner join tbl_empresacomplejo empresaComp on complejo.lcomplejo_id = empresaComp.lcomplejo_id
                    inner join administracionempresa empresa on empresa.lempresa_id = empresaComp.lempresa_id
                WHERE
                    cicloDesc.lciclo_id = @lCicloId
                    
                    and
                        case
                            when cicloDesc.lciclo_id >= 55 then
                                cicloDesc.lcontacto_id != (select lcontacto_id from administracioncontacto where scedulaidentidad = '4823437')
                            else
                                cicloDesc.lcontacto_id in
                                    (select lcontacto_id from administraciondescuentociclo where lciclo_id = cicloDesc.lciclo_id)
                        end
                    
                    AND empresa.lempresa_id = @Empresaid
                order by contacto.snombrecompleto";
    #endregion
    #region "SCRIPT_FACTURACION"
    private const string QUERY_FACTURACION = @"SELECT
                                                    dat.*,
                                                    ROUND((dat.TotalComisionVtaGrupoResidual * 6.96), 2) AS TotalComisionVtaGrupoResidualBs,
                                                    ROUND((dat.TotalComisionVtaPersonal * 6.96), 2) AS TotalComisionVtaPersonalBs,
                                                    empresa.snombre AS RazonSocial,
                                                    empresa.snit AS Nit,
                                                    UPPER(ciclo.snombre) AS NombreCiclo,
                                                    DATE(ciclo.dtfechainicio) AS FechaInicio,
                                                    DATE(ciclo.dtfechafin) AS FechaFin
                                                FROM
                                                (
                                                    SELECT
                                                        dat.lcontacto_id LContactoId,
                                                        contacto.scodigo SCodigo,
                                                        contacto.scedulaidentidad SCedulaIdentidad,
                                                        contacto.snombrecompleto SNombreCompleto,
                                                        dat.lempresa_id LEmpresaId,
                                                        dat.empresa Empresa,
                                                        SUM(dat.comision_vta_grupo_residual) AS TotalComisionVtaGrupoResidual,
                                                        SUM(dat.comision_vta_personal) AS TotalComisionVtaPersonal,
                                                        dat.lciclo_id LCicloId
                                                    FROM
                                                    (
                                                        SELECT
                                                            vtaGrupo.lcontacto_id,
                                                            contrato.lcomplejo_id,
                                                            em.empresa_id AS lempresa_id,
                                                            em.empresa_nombre AS empresa,
                                                            vtaGrupo.dcomision AS comision_vta_grupo_residual,
                                                            0 AS comision_vta_personal,
                                                            vtaGrupo.lciclo_id
                                                        FROM administracionventagrupo vtaGrupo
                                                            INNER JOIN administracioncontrato contrato USING (lcontrato_id)
                                                            INNER JOIN complejosempresa em ON em.lcomplejo_id = contrato.lcomplejo_id
                                                        WHERE vtaGrupo.lciclo_id = @LCicloId
                                                        AND vtaGrupo.lcontacto_id > 1
                                                        AND vtaGrupo.lcontacto_id = @LContactoId

                                                        UNION ALL

                                                        SELECT
                                                            residual.lcontacto_id,
                                                            residual.lcomplejo_id,
                                                            em.empresa_id AS lempresa_id,
                                                            em.empresa_nombre AS empresa,
                                                            residual.dmonto AS comision_vta_grupo_residual,
                                                            0 AS comision_vta_personal,
                                                            residual.lciclo_id
                                                        FROM administracionredempresacomplejo residual
                                                            INNER JOIN administracioncomplejo complejo USING (lcomplejo_id)
                                                            INNER JOIN complejosempresa em ON em.lcomplejo_id = residual.lcomplejo_id
                                                        WHERE residual.lciclo_id = @LCicloId
                                                        AND residual.lcontacto_id = @LContactoId

                                                        UNION ALL

                                                        SELECT
                                                            vtaPersonal.lcontacto_id,
                                                            contrato.lcomplejo_id,
                                                            em.empresa_id AS lempresa_id,
                                                            em.empresa_nombre AS empresa,
                                                            0 AS comision_vta_grupo_residual,
                                                            vtaPersonal.dcomision AS comision_vta_personal,
                                                            vtaPersonal.lciclo_id
                                                        FROM administracionventapersonal vtaPersonal
                                                            INNER JOIN administracioncontrato contrato USING (lcontrato_id)
                                                            INNER JOIN complejosempresa em ON em.lcomplejo_id = contrato.lcomplejo_id
                                                        WHERE vtaPersonal.lciclo_id = @LCicloId
                                                        AND vtaPersonal.lcontacto_id = @LContactoId

                                                        UNION ALL

                                                        SELECT
                                                            liderazgo.vendedores_Mes_id AS lcontacto_id,
                                                            liderazgo.lcomplejo_id,
                                                            em.empresa_id AS lempresa_id,
                                                            em.empresa_nombre AS empresa,
                                                            liderazgo.monto AS comision_vta_grupo_residual,
                                                            0 AS comision_vta_personal,
                                                            liderazgo.lciclo_id
                                                        FROM T_GANADORES_BONOLIDERAZGO_EMPRESA_PAGAR liderazgo
                                                            INNER JOIN administracioncomplejo complejo USING (lcomplejo_id)
                                                            INNER JOIN complejosempresa em ON em.lcomplejo_id = liderazgo.lcomplejo_id
                                                        WHERE liderazgo.lciclo_id = @LCicloId
                                                        AND liderazgo.vendedores_Mes_id = @LContactoId

                                                        UNION ALL

                                                        SELECT
                                                            liderazgo.vendedores_id AS lcontacto_id,
                                                            liderazgo.lcomplejo_id,
                                                            em.empresa_id AS lempresa_id,
                                                            em.empresa_nombre AS empresa,
                                                            liderazgo.pagar AS comision_vta_grupo_residual,
                                                            0 AS comision_vta_personal,
                                                            liderazgo.lciclo_id
                                                        FROM t_bono_liderazgo liderazgo
                                                            INNER JOIN administracioncomplejo complejo USING (lcomplejo_id)
                                                            INNER JOIN complejosempresa em ON em.lcomplejo_id = liderazgo.lcomplejo_id
                                                        WHERE liderazgo.lciclo_id = @LCicloId
                                                        AND liderazgo.vendedores_id = @LContactoId

                                                        UNION ALL

                                                        SELECT
                                                            top_vend.vendedor_lcontacto_id AS lcontacto_id,
                                                            top_vend.lcomplejo_id,
                                                            em.empresa_id AS lempresa_id,
                                                            em.empresa_nombre AS empresa,
                                                            top_vend.pagar AS comision_vta_grupo_residual,
                                                            0 AS comision_vta_personal,
                                                            top_vend.lciclo_id
                                                        FROM t_top_vendedores top_vend
                                                            INNER JOIN administracioncomplejo complejo USING (lcomplejo_id)
                                                            INNER JOIN complejosempresa em ON em.lcomplejo_id = top_vend.lcomplejo_id
                                                        WHERE top_vend.lciclo_id = @LCicloId
                                                        AND top_vend.vendedor_lcontacto_id = @LContactoId

                                                    ) dat
                                                        INNER JOIN administracioncontacto contacto 
                                                            ON dat.lcontacto_id = contacto.lcontacto_id
                                                    GROUP BY dat.lcontacto_id, dat.lempresa_id
                                                ) dat
                                                LEFT OUTER JOIN (
                                                    SELECT lcontacto_id
                                                    FROM administracionventapersonal
                                                    WHERE lciclo_id = @LCicloId
                                                    GROUP BY lcontacto_id
                                                ) datVtaPersonal ON dat.LContactoId = datVtaPersonal.lcontacto_id
                                                INNER JOIN administracionempresa empresa ON empresa.lempresa_id = dat.LEmpresaId
                                                INNER JOIN administracionciclo ciclo ON ciclo.lciclo_id = dat.LCicloId
                                                WHERE datVtaPersonal.lcontacto_id IS NOT NULL
                                                AND (
                                                        CASE 
                                                            WHEN ciclo.lciclo_id >= 55 THEN
                                                                dat.LContactoId != (
                                                                    SELECT lcontacto_id 
                                                                    FROM administracioncontacto 
                                                                    WHERE scedulaidentidad = '4823437'
                                                                )
                                                            ELSE
                                                                dat.LContactoId IN (
                                                                    SELECT lcontacto_id 
                                                                    FROM administracioncontacto 
                                                                    WHERE lcontacto_id > 3
                                                                )
                                                        END
                                                    )
                                                AND dat.LContactoId = @LContactoId;
                                                ";
    #endregion
    #region  "SCRIPT_PRORRATEO"
    private const string QUERY_PRORRATEO = @"select * from (
                                                select 
                                                    ACT.lcodigobanco LCodigoBanco
                                                    , ACT.lcuentabanco LCuentaBanco
                                                    , ACT.snombrecompleto SNombreCompleto
                                                    , ACT.scedulaidentidad SCedulaIdentidad
                                                    , CE.snombre SEmpresa
                                                    , IFNULL(VtaPersonal.ComisionPersonal, 0) + IFNULL(VtaGrupo.ComisionGrupo, 0) + IFNULL(BonoResidual.ComisionResidual, 0) + IFNULL(BonoLiderago.ComisionLiderazgo, 0) Importe
                                                    , IFNULL(Retencion.Retencion, 0) Retencion
                                                    , IFNULL(VtaPersonal.ComisionPersonal, 0) + IFNULL(VtaGrupo.ComisionGrupo, 0)+IFNULL(BonoResidual.ComisionResidual, 0) + IFNULL(BonoLiderago.ComisionLiderazgo, 0)  - IFNULL(Retencion.Retencion, 0) Liquido
                                                    , IFNULL(Descuento.Descuento, 0) Descuento
                                                    , ACPR.dmonto Prorrateo
                                                    , Retencion.empresa_id EmpresaId
                                                    , ACPR.lprorrateo_id LProrrateoId
                                                    , ACT.lcontacto_id LContactoId
                                                    , ACI.snombre Ciclo
                                                from administracioncomisionprorrateo ACPR
                                                inner join administracionempresa CE on CE.lempresa_id = CASE 
                                                                                                            WHEN ACPR.lempresa_id_temp = 20 THEN 21
                                                                                                            WHEN ACPR.lempresa_id_temp = 13 THEN 14
                                                                                                            ELSE ACPR.lempresa_id_temp  
                                                                                                        end
                                                inner join administracioncontacto ACT on ACT.lcontacto_id =  ACPR.lcontacto_id 
                                                INNER JOIN administracionciclo ACI ON ACI.lciclo_id = ACPR.lciclo_id
                                                LEFT JOIN (
                                                    select SUM(AVP.dcomision) ComisionPersonal, AVP.lcontacto_id, CE.empresa_id  from administracionventapersonal AVP 
                                                    inner join administracioncontrato AC on AC.lcontrato_id = AVP.lcontrato_id
                                                    inner join (
                                                        SELECT distinct EC.complejo_id lcomplejo_id, EC.empresa_id FROM administracionempresa E
                                                        INNER JOIN empresa_complejo EC ON EC.empresa_id = E.lempresa_id
                                                    ) CE on CE.lcomplejo_id = AC.lcomplejo_id 
                                                    where AVP.lciclo_id = @LCicloId and AVP.lcontacto_Id > 3  
                                                    GROUP BY AVP.lcontacto_id, CE.empresa_id
                                                )VtaPersonal on VtaPersonal.empresa_id = CE.lempresa_id and VtaPersonal.lcontacto_id = ACPR.lcontacto_id 
                                                left join (
                                                    select sum(AVG.dcomision) ComisionGrupo, AVG.lcontacto_id, CE.empresa_id  from administracionventagrupo AVG
                                                    inner join administracioncontrato AC on AC.lcontrato_id = AVG.lcontrato_id
                                                    inner join (
                                                        SELECT distinct EC.complejo_id lcomplejo_id, EC.empresa_id FROM administracionempresa E
                                                        INNER JOIN empresa_complejo EC ON EC.empresa_id = E.lempresa_id
                                                    ) CE on CE.lcomplejo_id = AC.lcomplejo_id
                                                    where AVG.lciclo_id = @LCicloId and AVG.lcontacto_Id > 3  
                                                    GROUP BY AVG.lcontacto_id, CE.empresa_id 
                                                )VtaGrupo on VtaGrupo.empresa_id = CE.lempresa_id and VtaGrupo.lcontacto_id = ACPR.lcontacto_id 
                                                left join (
                                                    select SUM(ABR.dmonto)ComisionResidual, ABR.lcontacto_id, CE.empresa_id from administracionredempresacomplejo ABR
                                                    inner join (
                                                        SELECT distinct EC.complejo_id lcomplejo_id, EC.empresa_id FROM administracionempresa E
                                                        INNER JOIN empresa_complejo EC ON EC.empresa_id = E.lempresa_id
                                                    ) CE on CE.lcomplejo_id = ABR.lcomplejo_id
                                                    where ABR.lciclo_id = @LCicloId and ABR.lcontacto_Id > 3  
                                                    GROUP BY ABR.lcontacto_id, CE.empresa_id 
                                                )BonoResidual on BonoResidual.empresa_id = CE.lempresa_id and BonoResidual.lcontacto_id = ACPR.lcontacto_id 
                                                left join (
                                                    SELECT SUM(ADE.dmonto)Descuento, AD.lcontacto_id, CE.empresa_id   FROM administraciondescuentociclo AD 
                                                    inner join administraciondescuentociclodetalle ADE on ADE.ldescuentociclo_id = AD.ldescuentociclo_id
                                                    inner join (
                                                        SELECT distinct EC.complejo_id lcomplejo_id, EC.empresa_id FROM administracionempresa E
                                                        INNER JOIN empresa_complejo EC ON EC.empresa_id = E.lempresa_id
                                                    ) CE on CE.lcomplejo_id = ADE.lcomplejo_id
                                                    WHERE AD.lciclo_id = @LCicloId and AD.lcontacto_Id > 3  
                                                    GROUP BY AD.lcontacto_id, CE.empresa_id 
                                                ) Descuento on Descuento.empresa_id = CE.lempresa_id and Descuento.lcontacto_id = ACPR.lcontacto_id
                                                left join (
                                                    SELECT sum(pagar)ComisionLiderazgo, BL.vendedores_id lcontacto_id, CE.empresa_id  FROM t_bono_liderazgo BL 
                                                    inner join (
                                                        SELECT distinct EC.complejo_id lcomplejo_id, EC.empresa_id FROM administracionempresa E
                                                        INNER JOIN empresa_complejo EC ON EC.empresa_id = E.lempresa_id
                                                    ) CE on CE.lcomplejo_id = BL.lcomplejo_id
                                                    WHERE BL.lciclo_id = @LCicloId and BL.lcontacto_Id > 3  
                                                    group by BL.vendedores_id, CE.empresa_id 
                                                )BonoLiderago on BonoLiderago.empresa_id = CE.lempresa_id and BonoLiderago.lcontacto_id = ACPR.lcontacto_id
                                                left join (
                                                    select SUM(montoretencion) Retencion, RE.lcontacto_id, CE.lempresa_id empresa_id from tbl_retencionempresa RE
                                                    inner join administracionempresa CE on CE.lempresa_id = RE.idempresa 
                                                    WHERE RE.lciclo_id = @LCicloId and RE.lcontacto_Id > 3 and RE.lcontacto_Id != 6474
                                                    group by RE.lcontacto_id, CE.lempresa_id 
                                                )Retencion on Retencion.empresa_id = CE.lempresa_id and Retencion.lcontacto_id = ACPR.lcontacto_id
                                                where ACPR.lciclo_id = @LCicloId
                                                ) dat where  dat.Prorrateo > 0 ";
    #endregion
    #region "SCRIPT_COMISION_SERVICIO"
    const string QUERY_COMISION_SERVICIO = @"SELECT 
                                                scodigo SCodigo
                                                , snombrecompleto SNombreCompleto
                                                , sum(comisionpersonal) Comision
                                                , sum(servicio) Servicio
                                                , max(PorcentajeRetencion) PorcentajeRetencion
                                                , sum(MontoRetencion) MontoRetencion
                                                , empresa_id EmpresaId
                                                , AE.snombre Empresa
                                                , AE.snit SNit
                                                , UPPER(ACI.snombre) Ciclo
                                                , ACI.dtfechainicio FechaInicio
                                                , ACI.dtfechafin FechaFin
                                            FROM (
                                                select 
                                                ACT.scodigo
                                                , ACT.snombrecompleto
                                                , SUM(AVP.dcomision) ComisionPersonal
                                                , 0 Servicio
                                                , 0 MontoRetencion
                                                , 0 PorcentajeRetencion
                                                , CE.empresa_id
                                                , ACT.lcontacto_id
                                                , AVP.lciclo_id
                                                from administracionventapersonal AVP 
                                                inner join administracioncontrato AC on AC.lcontrato_id = AVP.lcontrato_id
                                                inner join administracioncontacto ACT on ACT.lcontacto_id = AVP.lcontacto_Id  AND ACT.cbaja = 0 and ACT.lcontacto_id != 6474
                                                inner join (
                                                    SELECT distinct EC.complejo_id lcomplejo_id, EC.empresa_id FROM administracionempresa E
                                                    INNER JOIN empresa_complejo EC ON EC.empresa_id = E.lempresa_id
                                                ) CE on CE.lcomplejo_id = AC.lcomplejo_id and CE.empresa_id=@EmpresaId
                                                where AVP.lciclo_id = @LCicloId and AVP.lcontacto_Id > 3  
                                                GROUP BY AVP.lcontacto_id, CE.empresa_id

                                                union all

                                                select  
                                                ACT.scodigo
                                                , ACT.snombrecompleto
                                                , 0 ComisionPersonal
                                                , sum(AVG.dcomision) Servicio
                                                , 0
                                                , 0
                                                , CE.empresa_id
                                                , ACT.lcontacto_id
                                                , AVG.lciclo_id
                                                from administracionventagrupo AVG
                                                inner join administracioncontrato AC on AC.lcontrato_id = AVG.lcontrato_id
                                                inner join administracioncontacto ACT on ACT.lcontacto_id = AVG.lcontacto_Id  AND ACT.cbaja = 0 and ACT.lcontacto_id != 6474
                                                inner join (
                                                    SELECT distinct EC.complejo_id lcomplejo_id, EC.empresa_id FROM administracionempresa E
                                                    INNER JOIN empresa_complejo EC ON EC.empresa_id = E.lempresa_id
                                                ) CE on CE.lcomplejo_id = AC.lcomplejo_id and CE.empresa_id = @EmpresaId
                                                where AVG.lciclo_id = @LCicloId and AVG.lcontacto_id > 3  
                                                GROUP BY AVG.lcontacto_id, CE.empresa_id 

                                                union all

                                                select
                                                ACT.scodigo
                                                , ACT.snombrecompleto
                                                , 0 ComisionPersonal
                                                , sum(ABR.dmonto) Servicio
                                                , 0
                                                , 0
                                                , CE.empresa_id
                                                , ACT.lcontacto_id
                                                , ABR.lciclo_id
                                                from administracionredempresacomplejo ABR
                                                inner join administracioncontacto ACT on ACT.lcontacto_id = ABR.lcontacto_Id AND ACT.cbaja = 0 and ACT.lcontacto_id != 6474
                                                inner join (
                                                    SELECT distinct EC.complejo_id lcomplejo_id, EC.empresa_id FROM administracionempresa E
                                                    INNER JOIN empresa_complejo EC ON EC.empresa_id = E.lempresa_id
                                                ) CE on CE.lcomplejo_id = ABR.lcomplejo_id and CE.empresa_id = @EmpresaId
                                                where ABR.lciclo_id = @LCicloId and ABR.lcontacto_id > 3
                                                GROUP BY ABR.lcontacto_id, CE.empresa_id 

                                                union all

                                                SELECT 
                                                ACT.scodigo
                                                , ACT.snombrecompleto
                                                , 0 ComisionPersonal
                                                , sum(BL.pagar) Servicio
                                                , 0
                                                , 0
                                                , CE.empresa_id
                                                , ACT.lcontacto_id
                                                , BL.lciclo_id
                                                FROM t_bono_liderazgo BL 
                                                inner join administracioncontacto ACT on ACT.lcontacto_id = BL.vendedores_id  AND ACT.cbaja = 0 and ACT.lcontacto_id != 6474
                                                inner join (
                                                    SELECT distinct EC.complejo_id lcomplejo_id, EC.empresa_id FROM administracionempresa E
                                                    INNER JOIN empresa_complejo EC ON EC.empresa_id = E.lempresa_id
                                                ) CE on CE.lcomplejo_id = BL.lcomplejo_id
                                                WHERE BL.lciclo_id = @LCicloId and bl.lcontacto_id > 3 and CE.empresa_id = @EmpresaId
                                                group by BL.vendedores_id, CE.empresa_id 
                                                union all
                                                SELECT 
                                                ACT.scodigo
                                                , ACT.snombrecompleto
                                                , 0 ComisionPersonal
                                                , 0 Servicio
                                                , sum(RET.montoretencion) 
                                                , RET.porcentajeret
                                                , CE. lempresa_id
                                                , ACT.lcontacto_id
                                                , RET.lciclo_id
                                                FROM tbl_retencionempresa RET
                                                inner join administracioncontacto ACT on ACT.lcontacto_id = RET.lcontacto_id  AND ACT.cbaja = 0 and ACT.lcontacto_id != 6474
                                                inner join administracionempresa CE on CE.lempresa_id = RET.idempresa and RET.idempresa = @EmpresaId
                                                WHERE RET.lciclo_id = @LCicloId and RET.lcontacto_id > 3 and CE.lempresa_id = @EmpresaId
                                                group by RET.lcontacto_id, CE.lempresa_id 
                                            ) DAT
                                            inner join administracionempresa AE on AE.lempresa_id = DAT.empresa_id 
                                            inner join administracionciclo ACI on ACI.lciclo_id = DAT.lciclo_id
                                            where empresa_id = @EmpresaId
                                            group by scodigo 
                                            , snombrecompleto 
                                            , empresa_id 
                                            , AE.snombre 
 
                                            ";
    #endregion 
    #region "SCRIPT_PAGAR_COMISION"
    private const string QUERY_PAGAR_COMISION = @"
                                            SELECT
                                                CASE 
                                                    WHEN ACT.ctienecuenta = 1 THEN 'CTA BANCO $US'
                                                    WHEN ACT.ctienecuenta = 2 THEN 'CTA BANCO BS'
                                                    WHEN ACT.ctienecuenta = 3 THEN 'SION PAY'
                                                    WHEN ACT.ctienecuenta = 4 THEN 'PAGO TERCEROS'
                                                    ELSE ''
                                                END TipoCuenta
                                                , ACT.lcodigobanco CodigoBanco
                                                , ACT.lcuentabanco CuentaBanco
                                                , ACT.sciudad Ciudad
                                                , ACT.snombrecompleto NombreCompleto
                                                , COMISION.lcontacto_id LContactoId, SUM(COMISION.Personal)Personal, SUM(COMISION.Liderazgo) Liderazgo, SUM(COMISION.Residual) Residual
                                                , SUM(COMISION.Grupo) Grupo, SUM(COMISION.Descuento) Descuento, SUM(COMISION.Retencion) Retencion
                                                , (SELECT UPPER(ACL.snombre) FROM administracionciclo ACL WHERE ACL.Lciclo_id = @LCicloId LIMIT 1) Ciclo
                                                , ACT.scedulaidentidad CedulaIdentidad
                                            FROM (
                                                SELECT AVP.lcontacto_id, SUM(AVP.dcomision) Personal, 0 Liderazgo, 0 Residual, 0 Grupo, 0 Descuento, 0 Retencion
                                                FROM administracionventapersonal AVP
                                                WHERE AVP.lciclo_id = @LCicloId AND AVP.lcontacto_id > 3 AND AVP.lcontacto_id != 6474
                                                GROUP BY AVP.lcontacto_id
                                                UNION ALL
                                                SELECT BL.vendedores_id, 0 Personal, SUM(BL.pagar) Liderazgo, 0 Residual, 0 Grupo, 0 Descuento, 0 Retencion
                                                FROM t_bono_liderazgo BL
                                                WHERE BL.Lciclo_id = @LCicloId AND BL.vendedores_id > 3 AND BL.vendedores_id != 6474
                                                GROUP BY BL.vendedores_id
                                                UNION ALL
                                                SELECT ABR.lcontacto_id , 0 Personal, 0 Liderazgo, dtotalbono Residual, 0 Grupo, 0 Descuento, 0 Retencion
                                                FROM administracionbonoresidual ABR
                                                WHERE lciclo_id = @LCicloId AND ABR.lcontacto_id > 3 AND ABR.lcontacto_id != 6474
                                                GROUP BY ABR.lcontacto_id 
                                                UNION ALL
                                                SELECT AVG.lcontacto_id, 0 Personal, 0 Liderazgo, 0 Residual, SUM(AVG.dcomision) Grupo, 0 Descuento, 0 Retencion
                                                FROM AdministracionVentaGrupo AVG
                                                WHERE AVG.lciclo_id = @LCicloId AND AVG.lcontacto_id > 3 AND AVG.lcontacto_id != 6474
                                                GROUP BY AVG.lcontacto_id
                                                UNION ALL
                                                SELECT ADC.lcontacto_id, 0 Personal, 0 Liderazgo, 0 Residual, 0 Grupo, SUM(DTOTAL) Descuento, 0 Retencion
                                                FROM administraciondescuentociclo ADC
                                                WHERE ADC.lciclo_id = @LCicloId AND ADC.lcontacto_id > 3 AND ADC.lcontacto_id != 6474
                                                GROUP BY ADC.lcontacto_id
                                                UNION ALL
                                                SELECT RET.lcontacto_id, 0 Personal, 0 Liderazgo, 0 Residual, 0 Grupo, 0 Descuento, SUM(RET.MONTORETENCION) Retencion
                                                FROM tbl_retencionempresa RET
                                                WHERE RET.lciclo_id = @LCicloId AND RET.lcontacto_id > 3 AND RET.lcontacto_id != 6474
                                                GROUP BY RET.lcontacto_id
                                            ) COMISION 
                                            INNER JOIN administracioncontacto ACT ON ACT.lcontacto_id = COMISION.lcontacto_id
                                            WHERE ACT.cbaja = 0
                                            GROUP BY COMISION.lcontacto_id";
    #endregion
    public ReportesRepository(DapperContext context, ILogService log)
    {
        _context = context;
        _log = log;
    }
    public async Task<( ReporteComisionesDto Data ,  bool Success, string Mensaje)> GetReporteComision(string LogTransaccionId, int lCicloId, int lContactoId)
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

            DataResponse.VentasPersonales = await connection.QueryAsync<VentaItem>(QUERY_VENTA_PERSONAL, new { lCicloId, lContactoId });
            DataResponse.VentasGrupo = await connection.QueryAsync<VentaGrupoItem>(QUERY_VENTA_GRUPO, new { lCicloId, lContactoId });
            DataResponse.BonoRedisual = await connection.QueryAsync<BonoRedisualItem>(QUERY_BONO_RESIDUAL, new { lCicloId, lContactoId });
            DataResponse.BonoLiderazgo = await connection.QueryAsync<BonoLiderazgoItem>(QUERY_BONO_LIDERAZGO, new { lCicloId, lContactoId });
            DataResponse.Encabezado = await connection.QuerySingleOrDefaultAsync<Encabezado>(QUERY_ENCABEZADO, new { lCicloId, lContactoId });
            DataResponse.BonoCarrera = await connection.QueryAsync<BonoCarrera>(QUERY_BONO_CARRERA, new { lCicloId, lContactoId });


             
            return (DataResponse , true, "Okey");
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, "Fin de metodo", ex);
            return (new ReporteComisionesDto() , false, $"Error al obtener el reporte de comisiones: {ex.Message}");
        }
    }
    public async Task<(RptAplicaciones Data , bool Success, string Mensaje)> GetReporteAplicacines(string LogTransaccionId, int lCicloId, int lContactoId)
    {
         const string NombreMetodo = "GetReporteAplicacines()";
        _log.Info(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, $"Inicio de metodo [script aplicaciones: {QUERY_APLICACIONES}]");
        RptAplicaciones DataResponse = new RptAplicaciones();
        try
        {
            using var connection = _context.CreateConnection();

            DataResponse.Aplicaciones = await connection.QueryAsync<AplicacionesItem>(
                QUERY_APLICACIONES,
                new { lCicloId, lContactoId }
            );

            return (DataResponse, true, "listado de aplicaciones obtenidas correctamente.");
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, "Fin de metodo", ex);
            return (new RptAplicaciones(), false,  $"Error al obtener aplicaciones del ciclo: {ex.Message}");
        }

    }
    public async Task<(IEnumerable<RptDescuentoEmpresa> Data , bool Success, string Mensaje)> GetReporteDecuentoEmpresa(string LogTransaccionId, int lCicloId, int Empresaid)
    {
        const string NombreMetodo = "GetReporteDecuentoEmpresa()";
        _log.Info(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, $"Inicio de metodo [script descuento empresa: {QUERY_DESCUENTO_EMPRESA}]");
        try
        {
            using var connection = _context.CreateConnection();

            var Descuentos = await connection.QueryAsync<RptDescuentoEmpresa>(
                QUERY_DESCUENTO_EMPRESA,
                new { lCicloId, Empresaid }
            );

            return (Descuentos, true, "listado de descuento obtenidas correctamente.");
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, "Fin de metodo", ex);
            return (Enumerable.Empty<RptDescuentoEmpresa>(), false,  $"Error al obtener descuento por empresa del ciclo: {ex.Message}");
        }
        
    }
    public async  Task<(IEnumerable<RptFacturacion> Data , bool Success, string Mensaje)> GetReporteFacturacion(string LogTransaccionId, int LCicloId, int LContactoId)
    {
        const string NombreMetodo = "GetReporteDecuentoEmpresa()";
        _log.Info(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, $"Inicio de metodo [script facturacion: {QUERY_FACTURACION}]");
        try
        {
            using var connection = _context.CreateConnection();

            var Facturacion = await connection.QueryAsync<RptFacturacion>(
                QUERY_FACTURACION,
                new { LCicloId, LContactoId }
            );

            return (Facturacion, true, "listado de facturacion obtenidos correctamente.");
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, "Fin de metodo", ex);
            return (Enumerable.Empty<RptFacturacion>(), false,  $"Error al obtener la facturacion: {ex.Message}");
        }
    }
    public async Task<(IEnumerable<RptProrrateo> Data , bool Success, string Mensaje)> GetReporteProrrateo(string LogTransaccionId, int LCicloId)
    {
        const string NombreMetodo = "GetReporteProrrateo()";
        _log.Info(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, $"Inicio de metodo [script prorrateo: {QUERY_PRORRATEO}]");
        try
        {
            using var connection = _context.CreateConnection();

            var Prorrateo = await connection.QueryAsync<RptProrrateo>(
                QUERY_PRORRATEO,
                new { LCicloId }
            );

            return (Prorrateo, true, "listado de prorrateo obtenidos correctamente.");
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, "Fin de metodo", ex);
            return (Enumerable.Empty<RptProrrateo>(), false,  $"Error al obtener la prorrateo: {ex.Message}");
        }
    }
    public async Task<(IEnumerable<RptComisionServicio> Data , bool Success, string Mensaje)> GetReporteComisionServicio(string LogTransaccionId, int LCicloId, int EmpresaId)
    {
        const string NombreMetodo = "GetReporteProrrateo()";
        _log.Info(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, $"Inicio de metodo [script prorrateo: {QUERY_COMISION_SERVICIO}]");
        try
        {
            using var connection = _context.CreateConnection();

            var Prorrateo = await connection.QueryAsync<RptComisionServicio>(
                QUERY_COMISION_SERVICIO,
                new { LCicloId, EmpresaId }
            );

            return (Prorrateo, true, "listado de comision servicio obtenidos correctamente.");
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, "Fin de metodo", ex);
            return (Enumerable.Empty<RptComisionServicio>(), false,  $"Error al obtener la comision servicio: {ex.Message}");
        }
    }
    public async Task<(IEnumerable<RptPagarComision> Data , bool Success, string Mensaje)> GetReportePagarComision(string LogTransaccionId, int LCicloId)
    {
        const string NombreMetodo = "GetReporteProrrateo()";
        _log.Info(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, $"Inicio de metodo [script pagar comision: {QUERY_PAGAR_COMISION}]");
        try
        {
            using var connection = _context.CreateConnection();

            var pagarComision = await connection.QueryAsync<RptPagarComision>(
                QUERY_PAGAR_COMISION,
                new { LCicloId }
            );

            return (pagarComision, true, "listado de pagar comision obtenidos correctamente.");
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, "Fin de metodo", ex);
            return (Enumerable.Empty<RptPagarComision>(), false,  $"Error al obtener la pagar de comision: {ex.Message}");
        }
    }

}
