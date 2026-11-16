-- =====================================================================================
-- Plan de carrera Monte Sion: actualizar los incentivos SUS en administracionnivelplancarrera
-- Plan: PLAN_PLAN_CARRERA_MONTE_SION_POR_MONEDA.md (Fase 1)
--
-- Fuente: appsettings.json -> MonteSion:Rangos (IncentivoUsd). La relación es por lnivel_id;
-- el nombre del rango se toma de administracionnivel.snombre.
--
-- Corrige la carga inicial SUS que solo tenía EMERALD (500) y DIAMOND (1.000).
-- Solo modifica las filas vigentes (estado = 1) de moneda SUS. Es re-ejecutable.
-- Ejecución MANUAL: primero en una base aislada.
-- =====================================================================================


-- -------------------------------------------------------------------------------------
-- 1. Antes: incentivos SUS vigentes vs. appsettings
-- -------------------------------------------------------------------------------------
SELECT n.lnivel_id, n.snombre, p.dincentivo AS incentivo_actual, i.incentivo AS incentivo_appsettings
FROM administracionnivelplancarrera p
INNER JOIN administracionnivel n ON n.lnivel_id = p.lnivel_id
LEFT JOIN (
              SELECT  6 AS lnivel_id,    500 AS incentivo
    UNION ALL SELECT  7,                1000
    UNION ALL SELECT  8,                2500
    UNION ALL SELECT  9,               10000
    UNION ALL SELECT 10,               20000
    UNION ALL SELECT 11,               40000
    UNION ALL SELECT 12,               60000
    UNION ALL SELECT 13,               80000
    UNION ALL SELECT 14,              100000
) i ON i.lnivel_id = p.lnivel_id
WHERE p.moneda = 'SUS' AND p.estado = 1
ORDER BY n.lnivel_id;


-- -------------------------------------------------------------------------------------
-- 2. Actualización
-- -------------------------------------------------------------------------------------
START TRANSACTION;

UPDATE administracionnivelplancarrera p
INNER JOIN (
              SELECT  6 AS lnivel_id,    500 AS incentivo   -- EMERALD                  (Esmeralda)
    UNION ALL SELECT  7,                1000                -- DIAMOND                  (Diamante)
    UNION ALL SELECT  8,                2500                -- REGIONAL AMBASSADOR      (Embajador Regional)
    UNION ALL SELECT  9,               10000                -- NATIONAL AMBASSADOR      (Embajador Nacional)
    UNION ALL SELECT 10,               20000                -- INTERNATIONAL AMBASSADOR (Embajador Internacional)
    UNION ALL SELECT 11,               40000                -- FUNDADOR
    UNION ALL SELECT 12,               60000                -- FUNDADOR ELITE
    UNION ALL SELECT 13,               80000                -- FUNDADOR IMPERIAL
    UNION ALL SELECT 14,              100000                -- FUNDADOR ROYAL
) i ON i.lnivel_id = p.lnivel_id
SET p.dincentivo  = i.incentivo,
    p.susuariomod = 'SCRIPT',
    p.dtfechamod  = NOW()
WHERE p.moneda = 'SUS'
  AND p.estado = 1
  AND p.dincentivo <> i.incentivo;

-- Filas esperadas en la primera ejecución: 7 (lnivel_id 8 a 14). En las siguientes: 0.
SELECT ROW_COUNT() AS filas_actualizadas;


-- -------------------------------------------------------------------------------------
-- 3. Verificación (debe devolver 0 filas): incentivos SUS distintos a appsettings
-- -------------------------------------------------------------------------------------
SELECT n.lnivel_id, n.snombre, p.dincentivo, COALESCE(i.incentivo, 0) AS esperado
FROM administracionnivelplancarrera p
INNER JOIN administracionnivel n ON n.lnivel_id = p.lnivel_id
LEFT JOIN (
              SELECT  6 AS lnivel_id,    500 AS incentivo
    UNION ALL SELECT  7,                1000
    UNION ALL SELECT  8,                2500
    UNION ALL SELECT  9,               10000
    UNION ALL SELECT 10,               20000
    UNION ALL SELECT 11,               40000
    UNION ALL SELECT 12,               60000
    UNION ALL SELECT 13,               80000
    UNION ALL SELECT 14,              100000
) i ON i.lnivel_id = p.lnivel_id
WHERE p.moneda = 'SUS' AND p.estado = 1
  AND p.dincentivo <> COALESCE(i.incentivo, 0);

-- Si la verificación está vacía: COMMIT;  si no: ROLLBACK;
-- COMMIT;
