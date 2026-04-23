
SELECT * FROM(
SELECT 
    CT.lcontacto_id
    , CT.SNombreCompleto
    , CT.scedulaidentidad
    , COUNT(*)PersonaQueVendieron
    , case 
        when  TRUNCATE(COUNT(*), 0) BETWEEN 2 AND 3 THEN 200
        when  TRUNCATE(COUNT(*), 0) BETWEEN 4 AND 5 THEN 700
        when  TRUNCATE(COUNT(*), 0) BETWEEN 6 AND 7 THEN 1100
        when  TRUNCATE(COUNT(*), 0) BETWEEN 6 AND 9 THEN 1400
        when  TRUNCATE(COUNT(*), 0) > 10 THEN 2000
        ELSE 0
    END bonoV1
    , sum(DAT.CantVta) CantVta
    , GROUP_CONCAT(DAT.VId)VendedoresId
    , GROUP_CONCAT(DAT.VentasId)VentasId
    , GROUP_CONCAT(DAT.Ventas)Ventas
    , sum(DAT.MontoVentas)MontoVentas
    , sum(DAT.CuotasIniciales)CuotasIniciales
FROM (
    SELECT
        V.lpatrocinante_id GId
        , V.lcontacto_id VId 
        , GROUP_CONCAT(ACT.snroventa) Ventas
        , GROUP_CONCAT(ACT.lcontrato_id) VentasId
        , GROUP_CONCAT(ACT.dtfecha) Fecha
        , COUNT(*) CantVta
        , sum(act.dprecio) MontoVentas
        , sum(act.dcuota_inicial) CuotasIniciales
    FROM administracioncontrato ACT
    INNER JOIN administracioncontacto V ON v.lcontacto_id = ACT.lasesor_id
    WHERE ACT.dtfecha BETWEEN '20260301' AND '20260331' and 
        CASE WHEN (ACT.dcuota_inicial * 100 / ACT.dprecio) BETWEEN 2.9999 AND 3.011 THEN 
            CASE WHEN  ACT.dprecio >= 10000 THEN 1 ELSE 0 END
        ELSE 1 END = 1
    GROUP BY V.lpatrocinante_id, V.lcontacto_id 
)DAT 
INNER JOIN administracioncontacto CT ON CT.lcontacto_id = DAT.GId
GROUP BY CT.lcontacto_id, CT.SNombreCompleto, CT.scedulaidentidad
) DATG where DATG.bonoV1 > 0


select   *, (dcuota_inicial * 100 / dprecio) dd  from administracioncontrato WHERE dtfecha BETWEEN '20260301' AND '20260331' and 
CASE WHEN (dcuota_inicial * 100 / dprecio) BETWEEN 2.9999 AND 3.011 THEN 
    CASE WHEN  dprecio >= 10000 THEN 1 ELSE 0 END
ELSE 1 END = 0



/*
CALL sp_obtener_siguientes_pasos('COMISIONES', 142)
CALL sp_conf_ejecutar_paso('COMISIONES', 142, 'OBTENER VENTAS')
CALL sp_reiniciar_ciclo ('COMISIONES', 142)
CALL sp_cerrar_ciclo ('COMISIONES', 142)



delete from administracioncontrato where dtfecha BETWEEN '20260301' and '20260331';
TRUNCATE table VentaRezagadasCiclo;
delete from administracionventapersonal WHERE lciclo_id = 142;
delete from administracionventagrupo WHERE lciclo_id = 142;
TRUNCATE table T_ACCIONESCUOTASGRL;
TRUNCATE table Cartera;
delete from administracionbonoresidual where lciclo_id = 142;
delete from administracionredempresacomplejo where lciclo_id = 142;
delete from t_bonocompleto where lciclo_id = 142;


select count(*) from T_ACCIONESCUOTASGRL
select count(*) from Cartera

select sum(dprecio) from administracioncontrato where dtfecha BETWEEN '20260301' and '20260331';
select * from administracioncontrato where dtfecha BETWEEN '20260301' and '20260331';

select count(*) from Cartera;
select count(*) from T_ACCIONESCUOTASGRL;

*/