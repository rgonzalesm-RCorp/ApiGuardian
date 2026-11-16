-- =====================================================================
-- CORRECCIÓN de nivel_consolidado_mes (nivel de partida) en Monte Sion
-- Base: grdsion (MySQL 5.5 -> sin CTE ni funciones de ventana)
--
-- Regla: Monte Sion nunca baja el rango. El nivel de partida correcto de un ciclo es
-- el MAX(nivel_ciclo) alcanzado en ciclos anteriores (0 si no tiene historial).
-- Solo se corrigen filas cuya partida guardada es MENOR a la esperada.
-- Las filas con partida MAYOR (rangos previos al historial, ciclos 146/147) NO se tocan.
--
-- Qué corrige:
--   reportesmontesion            nivel_consolidado_mes, subieron_nivel, niveles_escalados, nroascensos
--   t_plan_montesion             Nivel
--   t_nuevos_rangos_montesion_2025  nivel, RANGO, niveles_escalados (y borra los que ya no son ascenso)
--   administracioncontacto       lnivel_id = MAX(nivel_ciclo) histórico cuando está por debajo
--
-- Qué NO corrige: Monto / bono / incentivo ni nivel_ciclo (dependen del cálculo completo
-- con la partida correcta). El paso 6 lista las filas cuyo monto debe revisarse a mano.
--
-- Uso: ejecutar los pasos 1 y 2 (crean tablas, hacen commit implícito), luego el paso 3
-- dentro de la transacción, revisar el paso 5 y recién entonces COMMIT o ROLLBACK.
-- =====================================================================
SET @CicloDesde = 147;
SET @CicloHasta = 149;
SET @Usuario    = 'correccion_partida';
SET SQL_SAFE_UPDATES = 0;

-- ---------------------------------------------------------------------
-- 1) Tabla de correcciones (queda como auditoría)
-- ---------------------------------------------------------------------
DROP TABLE IF EXISTS corr_partida_montesion_20261112;
CREATE TABLE corr_partida_montesion_20261112 AS
SELECT r.reportemontesion_id,
       r.lciclo_id,
       r.lcontacto_id,
       r.nivel_consolidado_mes AS partida_guardada,
       COALESCE((SELECT MAX(h.nivel_ciclo)
                 FROM reportesmontesion h
                 WHERE h.lcontacto_id = r.lcontacto_id
                   AND h.lciclo_id < r.lciclo_id), 0) AS partida_esperada,
       r.nivel_ciclo,
       r.subieron_nivel     AS subieron_guardado,
       r.niveles_escalados  AS escalados_guardado,
       r.nroascensos        AS nroascensos_guardado,
       r.Monto,
       r.Observacion
FROM reportesmontesion r
WHERE r.lciclo_id BETWEEN @CicloDesde AND @CicloHasta;

DELETE FROM corr_partida_montesion_20261112 WHERE partida_guardada >= partida_esperada;

ALTER TABLE corr_partida_montesion_20261112
    ADD COLUMN subieron_correcto  INT NULL,
    ADD COLUMN escalados_correcto INT NULL,
    ADD PRIMARY KEY (reportemontesion_id);

UPDATE corr_partida_montesion_20261112
SET subieron_correcto  = IF(nivel_ciclo > partida_esperada, 1, 0),
    escalados_correcto = IF(nivel_ciclo > partida_esperada, nivel_ciclo - partida_esperada, 0);

-- Revisión previa (en grdsion al 12/11/2026 deben salir 7 / 23 / 14 filas para 147 / 148 / 149)
SELECT lciclo_id, COUNT(*) AS filas,
       SUM(subieron_guardado = 1 AND subieron_correcto = 0) AS dejan_de_ser_ascenso
FROM corr_partida_montesion_20261112
GROUP BY lciclo_id;

-- ---------------------------------------------------------------------
-- 2) Respaldos
-- ---------------------------------------------------------------------
DROP TABLE IF EXISTS bk_reportesmontesion_20261112;
CREATE TABLE bk_reportesmontesion_20261112 AS
SELECT * FROM reportesmontesion WHERE lciclo_id BETWEEN @CicloDesde AND @CicloHasta;

DROP TABLE IF EXISTS bk_t_plan_montesion_20261112;
CREATE TABLE bk_t_plan_montesion_20261112 AS
SELECT * FROM t_plan_montesion WHERE lciclo_id BETWEEN @CicloDesde AND @CicloHasta;

DROP TABLE IF EXISTS bk_t_nuevos_rangos_montesion_20261112;
CREATE TABLE bk_t_nuevos_rangos_montesion_20261112 AS
SELECT * FROM t_nuevos_rangos_montesion_2025 WHERE lciclo_id BETWEEN @CicloDesde AND @CicloHasta;

DROP TABLE IF EXISTS bk_contacto_lnivel_20261112;
CREATE TABLE bk_contacto_lnivel_20261112 AS
SELECT c.lcontacto_id, c.lnivel_id, c.susuariomod, c.dtfechamod, h.max_nivel
FROM administracioncontacto c
INNER JOIN (SELECT lcontacto_id, MAX(nivel_ciclo) AS max_nivel
            FROM reportesmontesion GROUP BY lcontacto_id) h ON h.lcontacto_id = c.lcontacto_id
WHERE COALESCE(c.lnivel_id, 0) < h.max_nivel;

-- ---------------------------------------------------------------------
-- 3) Corrección (transaccional)
-- ---------------------------------------------------------------------
START TRANSACTION;

-- 3.1 reportesmontesion
UPDATE reportesmontesion r
INNER JOIN corr_partida_montesion_20261112 k ON k.reportemontesion_id = r.reportemontesion_id
SET r.nivel_consolidado_mes = k.partida_esperada,
    r.subieron_nivel        = k.subieron_correcto,
    r.niveles_escalados     = k.escalados_correcto,
    r.nroascensos           = IF(k.subieron_correcto = 1, r.nroascensos, 0),
    r.susuariomod           = @Usuario,
    r.dtfechamod            = NOW();

-- 3.2 t_plan_montesion (Nivel = nivel de partida)
UPDATE t_plan_montesion p
INNER JOIN corr_partida_montesion_20261112 k
        ON k.lcontacto_id = p.lcontacto_id AND k.lciclo_id = p.lciclo_id
SET p.Nivel       = k.partida_esperada,
    p.susuariomod = @Usuario,
    p.dtfechamod  = NOW();

-- 3.3 t_nuevos_rangos_montesion_2025: borrar los que ya no son ascenso
DELETE n
FROM t_nuevos_rangos_montesion_2025 n
INNER JOIN corr_partida_montesion_20261112 k
        ON k.lcontacto_id = n.lcontacto_id AND k.lciclo_id = n.lciclo_id
WHERE k.subieron_correcto = 0;

-- 3.4 t_nuevos_rangos_montesion_2025: corregir partida y escalados de los que siguen siendo ascenso
UPDATE t_nuevos_rangos_montesion_2025 n
INNER JOIN corr_partida_montesion_20261112 k
        ON k.lcontacto_id = n.lcontacto_id AND k.lciclo_id = n.lciclo_id
LEFT JOIN administracionnivel nv ON nv.lnivel_id = k.partida_esperada
SET n.nivel             = k.partida_esperada,
    n.RANGO             = COALESCE(nv.snombre, 'Asesor Comercial'),
    n.niveles_escalados = k.escalados_correcto
WHERE k.subieron_correcto = 1;

-- 3.5 Renumerar nroascensos por ciclo (quedan huecos al sacar ascensos)
SET @n = 0;
UPDATE reportesmontesion SET nroascensos = (@n := @n + 1)
WHERE lciclo_id = 147 AND subieron_nivel = 1 ORDER BY nroascensos, id_nro;
SET @n = 0;
UPDATE reportesmontesion SET nroascensos = (@n := @n + 1)
WHERE lciclo_id = 148 AND subieron_nivel = 1 ORDER BY nroascensos, id_nro;
SET @n = 0;
UPDATE reportesmontesion SET nroascensos = (@n := @n + 1)
WHERE lciclo_id = 149 AND subieron_nivel = 1 ORDER BY nroascensos, id_nro;

-- 3.6 administracioncontacto: lnivel_id nunca por debajo del máximo rango alcanzado
--     (deja listo el nivel de partida para el próximo ciclo, 150)
UPDATE administracioncontacto c
INNER JOIN bk_contacto_lnivel_20261112 b ON b.lcontacto_id = c.lcontacto_id
SET c.lnivel_id   = b.max_nivel,
    c.susuariomod = @Usuario,
    c.dtfechamod  = NOW()
WHERE COALESCE(c.lnivel_id, 0) < b.max_nivel;

-- ---------------------------------------------------------------------
-- 4) Verificación dentro de la transacción (todo debe dar 0)
-- ---------------------------------------------------------------------
SELECT 'reportes partida menor' AS chequeo, COUNT(*) AS pendientes
FROM reportesmontesion r
WHERE r.lciclo_id BETWEEN @CicloDesde AND @CicloHasta
  AND r.nivel_consolidado_mes < COALESCE((SELECT MAX(h.nivel_ciclo) FROM reportesmontesion h
                                          WHERE h.lcontacto_id = r.lcontacto_id AND h.lciclo_id < r.lciclo_id), 0)
UNION ALL
SELECT 'reportes escalados != ciclo - partida', COUNT(*)
FROM reportesmontesion r
INNER JOIN corr_partida_montesion_20261112 k ON k.reportemontesion_id = r.reportemontesion_id
WHERE r.niveles_escalados <> IF(r.nivel_ciclo > r.nivel_consolidado_mes, r.nivel_ciclo - r.nivel_consolidado_mes, 0)
UNION ALL
SELECT 'nuevos_rangos sin ascenso', COUNT(*)
FROM t_nuevos_rangos_montesion_2025 n
INNER JOIN reportesmontesion r ON r.lcontacto_id = n.lcontacto_id AND r.lciclo_id = n.lciclo_id
WHERE n.lciclo_id BETWEEN @CicloDesde AND @CicloHasta AND r.subieron_nivel = 0
UNION ALL
SELECT 'contactos lnivel_id < max historico', COUNT(*)
FROM administracioncontacto c
INNER JOIN (SELECT lcontacto_id, MAX(nivel_ciclo) AS m FROM reportesmontesion GROUP BY lcontacto_id) h
        ON h.lcontacto_id = c.lcontacto_id
WHERE COALESCE(c.lnivel_id, 0) < h.m;

-- ---------------------------------------------------------------------
-- 5) Confirmar: si el paso 4 da todo 0, ejecutar COMMIT; si no, ROLLBACK.
-- ---------------------------------------------------------------------
-- COMMIT;
-- ROLLBACK;

-- ---------------------------------------------------------------------
-- 6) Montos a revisar a mano (bono vs incentivo se decidió con la partida errónea)
--    - antes era ascenso y ahora no: debió ser recalificación/bono, no incentivo
--    - cambió la cantidad de escalados: revisar incentivos acumulados
-- ---------------------------------------------------------------------
SELECT k.lciclo_id, c.scedulaidentidad AS CI, c.snombrecompleto AS Nombre,
       k.partida_guardada, k.partida_esperada, k.nivel_ciclo,
       k.subieron_guardado, k.subieron_correcto,
       k.escalados_guardado, k.escalados_correcto,
       k.Monto, k.Observacion
FROM corr_partida_montesion_20261112 k
INNER JOIN administracioncontacto c ON c.lcontacto_id = k.lcontacto_id
ORDER BY k.lciclo_id, (k.subieron_guardado <> k.subieron_correcto) DESC, c.snombrecompleto;

-- ---------------------------------------------------------------------
-- 7) Reversión total (solo si hiciste COMMIT y necesitas volver atrás)
-- ---------------------------------------------------------------------
-- START TRANSACTION;
-- UPDATE reportesmontesion r INNER JOIN bk_reportesmontesion_20261112 b ON b.reportemontesion_id = r.reportemontesion_id
--    SET r.nivel_consolidado_mes = b.nivel_consolidado_mes, r.subieron_nivel = b.subieron_nivel,
--        r.niveles_escalados = b.niveles_escalados, r.nroascensos = b.nroascensos,
--        r.susuariomod = b.susuariomod, r.dtfechamod = b.dtfechamod;
-- UPDATE t_plan_montesion p INNER JOIN bk_t_plan_montesion_20261112 b ON b.id_plan_montesion = p.id_plan_montesion
--    SET p.Nivel = b.Nivel, p.susuariomod = b.susuariomod, p.dtfechamod = b.dtfechamod;
-- DELETE FROM t_nuevos_rangos_montesion_2025 WHERE lciclo_id BETWEEN @CicloDesde AND @CicloHasta;
-- INSERT INTO t_nuevos_rangos_montesion_2025 SELECT * FROM bk_t_nuevos_rangos_montesion_20261112;
-- UPDATE administracioncontacto c INNER JOIN bk_contacto_lnivel_20261112 b ON b.lcontacto_id = c.lcontacto_id
--    SET c.lnivel_id = b.lnivel_id, c.susuariomod = b.susuariomod, c.dtfechamod = b.dtfechamod;
-- COMMIT;
