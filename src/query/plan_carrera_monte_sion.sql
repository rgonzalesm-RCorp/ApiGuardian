-- =====================================================================================
-- Plan de carrera Monte Sion por moneda del ciclo (Fase 1)
-- Plan: PLAN_PLAN_CARRERA_MONTE_SION_POR_MONEDA.md
--
-- Crea administracionnivelplancarrera y carga la configuración inicial:
--   * SUS: copiada de administracionnivel (valores vigentes en la base al momento de ejecutar).
--   * BS : tabla de la captura plancarrera.png.
--
-- El nombre del rango NO se guarda aquí: siempre se toma de administracionnivel.snombre.
-- dproduccioncortevme es el tope por equipo que usa el cálculo; dvmeporcentaje es informativo.
--
-- Ejecución MANUAL: primero en una base aislada y después en la de destino.
-- Re-ejecutable: no duplica filas activas (rango + moneda).
-- Compatible con versiones de MySQL sin columnas generadas: "activo" es una columna normal.
-- La tabla se crea en utf8mb4 / utf8mb4_general_ci (el charset de la conexión), no con el default latin1 de la base.
-- Si ya existe una versión anterior en latin1 (vacía o sin COMMIT): ROLLBACK; DROP TABLE administracionnivelplancarrera;
-- Al inactivar una fila se debe hacer: SET estado = 0, activo = NULL.
-- =====================================================================================


-- -------------------------------------------------------------------------------------
-- 0. Revisión previa (solo lectura): valores vigentes que se copiarán como SUS.
-- -------------------------------------------------------------------------------------
SELECT lnivel_id, snombre, ddesde, dhasta, dbono, dbonomembresia, VME
FROM administracionnivel
ORDER BY lnivel_id;


-- -------------------------------------------------------------------------------------
-- 1. Tabla
-- -------------------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS administracionnivelplancarrera (
    lnivelplancarrera_id  INT             NOT NULL AUTO_INCREMENT,
    lnivel_id             INT             NOT NULL,              -- administracionnivel.lnivel_id
    moneda                ENUM('BS','SUS') NOT NULL,             -- mismo dominio que administracionciclo.moneda
    dproducciondesde      DECIMAL(18,2)   NOT NULL DEFAULT 0,    -- producción mensual desde
    dproduccionhasta      DECIMAL(18,2)   NOT NULL DEFAULT 0,    -- producción mensual hasta; 0 = sin tope
    dvmeporcentaje        DECIMAL(5,2)    NOT NULL DEFAULT 0,    -- VME en %; 0 = sin VME (informativo)
    dproduccioncortevme   DECIMAL(18,2)   NOT NULL DEFAULT 0,    -- producción con corte de VME (tope por equipo); 0 = sin corte
    dbono                 DECIMAL(18,2)   NOT NULL DEFAULT 0,    -- bono por rango
    dincentivo            DECIMAL(18,2)   NOT NULL DEFAULT 0,    -- incentivo (pago único); 0 = sin incentivo
    estado                TINYINT         NOT NULL DEFAULT 1,    -- 1 = vigente, 0 = histórico
    activo                TINYINT         NULL     DEFAULT 1,    -- 1 si estado = 1; NULL si estado = 0 (se actualiza junto con estado)
    susuarioadd           VARCHAR(50)     NOT NULL,
    dtfechaadd            DATETIME        NOT NULL,
    susuariomod           VARCHAR(50)     NOT NULL,
    dtfechamod            DATETIME        NOT NULL,
    PRIMARY KEY (lnivelplancarrera_id),
    -- Una sola fila vigente por rango y moneda; las históricas (estado = 0) quedan con activo = NULL y no chocan.
    UNIQUE KEY uq_nivelplancarrera_activo (lnivel_id, moneda, activo),
    KEY ix_nivelplancarrera_moneda (moneda, estado)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
-- Charset explícito: si se toma el default de la base (latin1), comparar "moneda" con los literales
-- 'SUS'/'BS' de la conexión (utf8mb4) da el error 1267 "Illegal mix of collations".


-- -------------------------------------------------------------------------------------
-- 2. Carga inicial
-- -------------------------------------------------------------------------------------
START TRANSACTION;

-- 2.1 SUS: desde administracionnivel.
--     VME (monto) -> dproduccioncortevme; % = VME * 100 / ddesde.
--     Incentivos: los de appsettings.json -> MonteSion:Rangos (IncentivoUsd), asociados por lnivel_id.
INSERT INTO administracionnivelplancarrera
    (lnivel_id, moneda, dproducciondesde, dproduccionhasta, dvmeporcentaje, dproduccioncortevme,
     dbono, dincentivo, estado, susuarioadd, dtfechaadd, susuariomod, dtfechamod)
SELECT
    n.lnivel_id,
    'SUS',
    COALESCE(n.ddesde, 0),
    COALESCE(n.dhasta, 0),
    CASE WHEN COALESCE(n.VME, 0) > 0 AND COALESCE(n.ddesde, 0) > 0
         THEN ROUND(n.VME * 100 / n.ddesde, 2) ELSE 0 END,
    COALESCE(n.VME, 0),
    COALESCE(n.dbono, 0),
    CASE n.lnivel_id
        WHEN  6 THEN    500   -- EMERALD                  (Esmeralda)
        WHEN  7 THEN   1000   -- DIAMOND                  (Diamante)
        WHEN  8 THEN   2500   -- REGIONAL AMBASSADOR      (Embajador Regional)
        WHEN  9 THEN  10000   -- NATIONAL AMBASSADOR      (Embajador Nacional)
        WHEN 10 THEN  20000   -- INTERNATIONAL AMBASSADOR (Embajador Internacional)
        WHEN 11 THEN  40000   -- FUNDADOR
        WHEN 12 THEN  60000   -- FUNDADOR ELITE
        WHEN 13 THEN  80000   -- FUNDADOR IMPERIAL
        WHEN 14 THEN 100000   -- FUNDADOR ROYAL
        ELSE 0
    END,
    1, 'SCRIPT', NOW(), 'SCRIPT', NOW()
FROM administracionnivel n
WHERE n.lnivel_id >= 0
  AND NOT EXISTS (
        SELECT 1 FROM administracionnivelplancarrera p
        WHERE p.lnivel_id = n.lnivel_id AND p.moneda = 'SUS' AND p.estado = 1
  );

-- 2.2 BS: captura plancarrera.png. Hasta = siguiente desde - 1; FUNDADOR ROYAL sin tope.
--     Corte VME = desde * VME % / 100.
INSERT INTO administracionnivelplancarrera
    (lnivel_id, moneda, dproducciondesde, dproduccionhasta, dvmeporcentaje, dproduccioncortevme,
     dbono, dincentivo, estado, susuarioadd, dtfechaadd, susuariomod, dtfechamod)
SELECT
    b.lnivel_id, 'BS', b.desde, b.hasta, b.vme, b.corte, b.bono, b.incentivo,
    1, 'SCRIPT', NOW(), 'SCRIPT', NOW()
FROM (
              SELECT  0 AS lnivel_id,         0 AS desde,         0 AS hasta,  0 AS vme,        0 AS corte,     0 AS bono,      0 AS incentivo -- ASESOR COMERCIAL
    UNION ALL SELECT  1,                      0,                  0,           0,               0,              0,              0               -- EMPRENDEDOR
    UNION ALL SELECT  2,                  35000,              69999,           0,               0,              0,              0               -- LEADER
    UNION ALL SELECT  3,                  70000,             174999,           0,               0,              0,              0               -- SENIOR
    UNION ALL SELECT  4,                 175000,             349999,           0,               0,            700,              0               -- SAPPHIRE
    UNION ALL SELECT  5,                 350000,             699999,           0,               0,           1400,              0               -- RUBY
    UNION ALL SELECT  6,                 700000,            1399999,          80,          560000,           2100,           3500               -- EMERALD
    UNION ALL SELECT  7,                1400000,            2799999,          70,          980000,           2800,           7000               -- DIAMOND
    UNION ALL SELECT  8,                2800000,            5599999,          55,         1540000,           4200,          17500               -- REGIONAL AMBASSADOR
    UNION ALL SELECT  9,                5600000,           10499999,          50,         2800000,           5600,          70000               -- NATIONAL AMBASSADOR
    UNION ALL SELECT 10,               10500000,           17499999,          40,         4200000,           7000,         140000               -- INTERNATIONAL AMBASSADOR
    UNION ALL SELECT 11,               17500000,           27999999,          25,         4375000,          14000,         280000               -- FUNDADOR
    UNION ALL SELECT 12,               28000000,           34999999,          20,         5600000,          21000,         420000               -- FUNDADOR ELITE
    UNION ALL SELECT 13,               35000000,          174999999,          15,         5250000,          28000,         560000               -- FUNDADOR IMPERIAL
    UNION ALL SELECT 14,              175000000,                  0,          10,        17500000,          35000,         700000               -- FUNDADOR ROYAL
) b
INNER JOIN administracionnivel n ON n.lnivel_id = b.lnivel_id   -- solo niveles que existen en el catálogo
WHERE NOT EXISTS (
        SELECT 1 FROM administracionnivelplancarrera p
        WHERE p.lnivel_id = b.lnivel_id AND p.moneda = 'BS' AND p.estado = 1
  );

-- Revisar las verificaciones (sección 3) ANTES de confirmar.
-- Si alguna devuelve filas: ROLLBACK;  si todas están vacías: COMMIT;
-- COMMIT;


-- -------------------------------------------------------------------------------------
-- 3. Verificaciones (todas deben devolver 0 filas)
-- -------------------------------------------------------------------------------------

-- 3.1 Niveles del catálogo sin configuración vigente en alguna moneda.
SELECT n.lnivel_id, n.snombre, m.moneda
FROM administracionnivel n
CROSS JOIN (SELECT 'SUS' AS moneda UNION ALL SELECT 'BS') m
LEFT JOIN administracionnivelplancarrera p
       ON p.lnivel_id = n.lnivel_id AND p.moneda = m.moneda AND p.estado = 1
WHERE n.lnivel_id >= 0
  AND p.lnivel_id IS NULL;

-- 3.2 Configuración huérfana (nivel que no existe en administracionnivel).
SELECT p.*
FROM administracionnivelplancarrera p
LEFT JOIN administracionnivel n ON n.lnivel_id = p.lnivel_id
WHERE n.lnivel_id IS NULL;

-- 3.3 Corte VME distinto de desde * % / 100 (tolerancia 1 por redondeo del %).
SELECT n.snombre, p.moneda, p.dproducciondesde, p.dvmeporcentaje, p.dproduccioncortevme,
       ROUND(p.dproducciondesde * p.dvmeporcentaje / 100, 2) AS corte_esperado
FROM administracionnivelplancarrera p
INNER JOIN administracionnivel n ON n.lnivel_id = p.lnivel_id
WHERE p.estado = 1
  AND ABS(p.dproduccioncortevme - p.dproducciondesde * p.dvmeporcentaje / 100) > 1;

-- 3.4 activo desincronizado con estado (activo debe ser 1 cuando estado = 1 y NULL cuando estado = 0).
SELECT p.*
FROM administracionnivelplancarrera p
WHERE (p.estado = 1 AND (p.activo IS NULL OR p.activo <> 1))
   OR (p.estado <> 1 AND p.activo IS NOT NULL);

-- 3.5 Rangos de producción superpuestos dentro de la misma moneda.
SELECT p1.moneda,
       n1.snombre AS rango, p1.dproducciondesde AS desde, p1.dproduccionhasta AS hasta,
       n2.snombre AS rango_superpuesto, p2.dproducciondesde AS desde2
FROM administracionnivelplancarrera p1
INNER JOIN administracionnivelplancarrera p2
        ON p2.moneda = p1.moneda AND p2.estado = 1 AND p2.lnivel_id > p1.lnivel_id
INNER JOIN administracionnivel n1 ON n1.lnivel_id = p1.lnivel_id
INNER JOIN administracionnivel n2 ON n2.lnivel_id = p2.lnivel_id
WHERE p1.estado = 1
  AND p1.dproducciondesde > 0
  AND p2.dproducciondesde > 0
  AND (p1.dproduccionhasta = 0 OR p2.dproducciondesde <= p1.dproduccionhasta);


-- -------------------------------------------------------------------------------------
-- 4. Consulta de revisión: plan de carrera vigente en ambas monedas
-- -------------------------------------------------------------------------------------
SELECT n.lnivel_id,
       n.snombre                AS rango,
       p.moneda,
       p.dproducciondesde       AS desde,
       p.dproduccionhasta       AS hasta,
       p.dvmeporcentaje         AS vme_porcentaje,
       p.dproduccioncortevme    AS produccion_corte_vme,
       p.dbono                  AS bono,
       p.dincentivo             AS incentivo
FROM administracionnivelplancarrera p
INNER JOIN administracionnivel n ON n.lnivel_id = p.lnivel_id
WHERE p.estado = 1
ORDER BY p.moneda DESC, n.lnivel_id;


-- -------------------------------------------------------------------------------------
-- 5. Reversión (solo si hay que deshacer la fase 1 completa)
-- -------------------------------------------------------------------------------------
-- DROP TABLE administracionnivelplancarrera;
