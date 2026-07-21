
-- ============================================
-- 1. PROCESOS
-- ============================================
CREATE TABLE conf_procesos (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nombre VARCHAR(150) NOT NULL,
    descripcion TEXT,
    estado TINYINT DEFAULT 1,
    fecha_creacion DATETIME 
);

-- ============================================
-- 2. PASOS (DEFINICIÓN DEL FLUJO)
-- ============================================
CREATE TABLE conf_pasos (
    id INT AUTO_INCREMENT PRIMARY KEY,
    proceso_id INT NOT NULL,
    nombre VARCHAR(150) NOT NULL,
    orden INT NOT NULL,
    es_obligatorio TINYINT DEFAULT 1, -- 1 = obligatorio, 0 = opcional
    estado TINYINT DEFAULT 1
);

-- ============================================
-- 3. DEPENDENCIAS ENTRE PASOS
-- ============================================
CREATE TABLE conf_paso_dependencias (
    id INT AUTO_INCREMENT PRIMARY KEY,
    paso_id INT NOT NULL,              -- paso actual
    paso_requerido_id INT NOT NULL  -- paso que debe completarse antes
);

-- ============================================
-- 4. INSTANCIAS DEL PROCESO
-- ============================================
CREATE TABLE conf_proceso_instancias (
    id INT AUTO_INCREMENT PRIMARY KEY,
    proceso_id INT NOT NULL,
    estado VARCHAR(50) DEFAULT 'EN_PROCESO',
    fecha_inicio DATETIME ,
    fecha_fin DATETIME
);

-- ============================================
-- 5. CICLOS DEL PROCESO (ITERACIONES)
-- ============================================
CREATE TABLE conf_proceso_ciclos (
    id INT AUTO_INCREMENT PRIMARY KEY,
    proceso_instancia_id INT NOT NULL,
    numero_ciclo INT NOT NULL,
    estado VARCHAR(50) DEFAULT 'EN_PROCESO',
    fecha_inicio DATETIME ,
    fecha_fin DATETIME
);

-- ============================================
-- 6. EJECUCIÓN DE PASOS POR CICLO
-- ============================================
CREATE TABLE conf_proceso_pasos (
    id INT AUTO_INCREMENT PRIMARY KEY,
    proceso_ciclo_id INT NOT NULL,
    paso_id INT NOT NULL,
    estado VARCHAR(50) DEFAULT 'PENDIENTE', -- PENDIENTE, COMPLETADO, OMITIDO
    fecha_inicio DATETIME,
    fecha_fin DATETIME
);

-- ============================================
-- ÍNDICES RECOMENDADOS (MEJOR PERFORMANCE)
-- ============================================
CREATE INDEX idx_pasos_proceso ON conf_pasos(proceso_id);
CREATE INDEX idx_procesos_nombre_estado ON conf_procesos(nombre, estado);
CREATE INDEX idx_dependencias_paso ON conf_paso_dependencias(paso_id);
CREATE INDEX idx_dependencias_requerido ON conf_paso_dependencias(paso_requerido_id);
CREATE INDEX idx_proceso_instancia ON conf_proceso_instancias(proceso_id);
CREATE INDEX idx_ciclos_instancia ON conf_proceso_ciclos(proceso_instancia_id);
CREATE INDEX idx_pasos_ciclo ON conf_proceso_pasos(proceso_ciclo_id);







-- ============================================
-- 1. INSERT PROCESO
-- ============================================
INSERT INTO conf_procesos (nombre, descripcion)
VALUES ('COMISIONES', 'Proceso de cálculo de comisiones');

-- ============================================
-- 2. INSERT PASOS (usando el proceso recién creado)
-- ============================================
INSERT INTO conf_pasos (proceso_id, nombre, orden, es_obligatorio)
SELECT p.id, 'OBTENER VENTAS', 1, 1 FROM conf_procesos p WHERE p.nombre = 'COMISIONES' AND p.estado = 1
UNION ALL
SELECT p.id, 'CASOS OBSERVADOS', 2, 1 FROM conf_procesos p WHERE p.nombre = 'COMISIONES' AND p.estado = 1
UNION ALL
SELECT p.id, 'ADICIONAR VENTAS', 3, 1 FROM conf_procesos p WHERE p.nombre = 'COMISIONES' AND p.estado = 1
UNION ALL
SELECT p.id, 'COMISION DIRECTA', 4, 1 FROM conf_procesos p WHERE p.nombre = 'COMISIONES' AND p.estado = 1
UNION ALL
SELECT p.id, 'COMISION GRUPO', 5, 1 FROM conf_procesos p WHERE p.nombre = 'COMISIONES' AND p.estado = 1
UNION ALL
SELECT p.id, 'OBTENER CARTERA', 6, 1 FROM conf_procesos p WHERE p.nombre = 'COMISIONES' AND p.estado = 1
UNION ALL
SELECT p.id, 'OBTENER CUOTAS', 7, 1 FROM conf_procesos p WHERE p.nombre = 'COMISIONES' AND p.estado = 1
UNION ALL
SELECT p.id, 'OBTENER EXCEDENTE', 8, 1 FROM conf_procesos p WHERE p.nombre = 'COMISIONES' AND p.estado = 1
UNION ALL
SELECT p.id, 'COMISION RESIDUAL', 9, 1 FROM conf_procesos p WHERE p.nombre = 'COMISIONES' AND p.estado = 1
UNION ALL
SELECT p.id, 'BONO PAR', 10, 1 FROM conf_procesos p WHERE p.nombre = 'COMISIONES' AND p.estado = 1;

-- ============================================
-- 3. DEPENDENCIAS DINÁMICAS (por nombre)
-- ============================================
INSERT INTO conf_paso_dependencias (paso_id, paso_requerido_id)
SELECT p2.id, p1.id
FROM conf_pasos p1
JOIN conf_pasos p2 ON p2.proceso_id = p1.proceso_id
WHERE p1.nombre = 'OBTENER VENTAS' AND p2.nombre = 'CASOS OBSERVADOS'

UNION ALL
SELECT p2.id, p1.id
FROM conf_pasos p1
JOIN conf_pasos p2 ON p2.proceso_id = p1.proceso_id
WHERE p1.nombre = 'CASOS OBSERVADOS' AND p2.nombre = 'ADICIONAR VENTAS'

UNION ALL
SELECT p2.id, p1.id
FROM conf_pasos p1
JOIN conf_pasos p2 ON p2.proceso_id = p1.proceso_id
WHERE p1.nombre = 'ADICIONAR VENTAS' AND p2.nombre = 'COMISION DIRECTA'

UNION ALL
SELECT p2.id, p1.id
FROM conf_pasos p1
JOIN conf_pasos p2 ON p2.proceso_id = p1.proceso_id
WHERE p1.nombre = 'COMISION DIRECTA' AND p2.nombre = 'COMISION GRUPO'

UNION ALL
SELECT p2.id, p1.id
FROM conf_pasos p1
JOIN conf_pasos p2 ON p2.proceso_id = p1.proceso_id
WHERE p1.nombre = 'COMISION GRUPO' AND p2.nombre = 'OBTENER CARTERA'

UNION ALL
SELECT p2.id, p1.id
FROM conf_pasos p1
JOIN conf_pasos p2 ON p2.proceso_id = p1.proceso_id
WHERE p1.nombre = 'OBTENER CARTERA' AND p2.nombre = 'OBTENER CUOTAS'

UNION ALL
SELECT p2.id, p1.id
FROM conf_pasos p1
JOIN conf_pasos p2 ON p2.proceso_id = p1.proceso_id
WHERE p1.nombre = 'OBTENER CUOTAS' AND p2.nombre = 'OBTENER EXCEDENTE'

UNION ALL
SELECT p2.id, p1.id
FROM conf_pasos p1
JOIN conf_pasos p2 ON p2.proceso_id = p1.proceso_id
WHERE p1.nombre = 'OBTENER EXCEDENTE' AND p2.nombre = 'COMISION RESIDUAL'

UNION ALL
SELECT p2.id, p1.id
FROM conf_pasos p1
JOIN conf_pasos p2 ON p2.proceso_id = p1.proceso_id
WHERE p1.nombre = 'COMISION RESIDUAL' AND p2.nombre = 'BONO PAR';



 

CREATE PROCEDURE sp_conf_ejecutar_paso (
    IN p_proceso_nombre VARCHAR(150),
    IN p_numero_ciclo INT,
    IN p_paso_nombre VARCHAR(150)
)
BEGIN
    DECLARE v_proceso_id INT;
    DECLARE v_instancia_id INT;
    DECLARE v_ciclo_id INT;
    DECLARE v_paso_id INT;
    DECLARE v_existe INT;
    DECLARE v_pendientes INT;

    -- ============================================
    -- 1. OBTENER PROCESO
    -- ============================================
    SELECT id INTO v_proceso_id
    FROM procesos
    WHERE nombre = p_proceso_nombre
    LIMIT 1;

    IF v_proceso_id IS NULL THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'El proceso no existe';
    END IF;

    -- ============================================
    -- 2. OBTENER O CREAR INSTANCIA
    -- ============================================
    SELECT id INTO v_instancia_id
    FROM proceso_instancias
    WHERE proceso_id = v_proceso_id
    ORDER BY id DESC
    LIMIT 1;

    IF v_instancia_id IS NULL THEN
        INSERT INTO proceso_instancias (proceso_id, estado)
        VALUES (v_proceso_id, 'EN_PROCESO');

        SET v_instancia_id = LAST_INSERT_ID();
    END IF;

    -- ============================================
    -- 3. OBTENER O CREAR CICLO
    -- ============================================
    SELECT id INTO v_ciclo_id
    FROM proceso_ciclos
    WHERE proceso_instancia_id = v_instancia_id
      AND numero_ciclo = p_numero_ciclo
    LIMIT 1;

    IF v_ciclo_id IS NULL THEN
        INSERT INTO proceso_ciclos (
            proceso_instancia_id,
            numero_ciclo,
            estado
        )
        VALUES (
            v_instancia_id,
            p_numero_ciclo,
            'EN_PROCESO'
        );

        SET v_ciclo_id = LAST_INSERT_ID();
    END IF;

    -- ============================================
    -- 4. OBTENER PASO (VALIDANDO PROCESO)
    -- ============================================
    SELECT id INTO v_paso_id
    FROM pasos
    WHERE nombre = p_paso_nombre
      AND proceso_id = v_proceso_id
    LIMIT 1;

    IF v_paso_id IS NULL THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'El paso no existe en el proceso';
    END IF;

    -- ============================================
    -- 5. VALIDAR QUE NO ESTÉ YA EJECUTADO
    -- ============================================
    SELECT COUNT(*) INTO v_existe
    FROM proceso_pasos
    WHERE proceso_ciclo_id = v_ciclo_id
      AND paso_id = v_paso_id;

    IF v_existe > 0 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'El paso ya fue ejecutado en este ciclo';
    END IF;

    -- ============================================
    -- 6. VALIDAR DEPENDENCIAS
    -- ============================================
    SELECT COUNT(*) INTO v_pendientes
    FROM paso_dependencias pd
    LEFT JOIN proceso_pasos pp
        ON pp.paso_id = pd.paso_requerido_id
        AND pp.proceso_ciclo_id = v_ciclo_id
    WHERE pd.paso_id = v_paso_id
      AND (pp.estado IS NULL OR pp.estado <> 'COMPLETADO');

    IF v_pendientes > 0 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'No se cumplen las dependencias del paso';
    END IF;

    -- ============================================
    -- 7. INSERTAR PASO COMO COMPLETADO
    -- ============================================
    INSERT INTO proceso_pasos (
        proceso_ciclo_id,
        paso_id,
        estado,
        fecha_inicio,
        fecha_fin
    )
    VALUES (
        v_ciclo_id,
        v_paso_id,
        'COMPLETADO',
        NOW(),
        NOW()
    );

END
 
