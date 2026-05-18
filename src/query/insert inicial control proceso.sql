insert into administraciontipocontrato values ('admin', now(), 'admin', now(), 6, 'UPGRADE', 'UG');
insert into administraciontipocontrato values ('admin', now(), 'admin', now(), 7, 'RECUPERACION', 'RE');
insert into administraciontipocontrato values ('admin', now(), 'admin', now(), 8, 'RECOMPRA', 'RC');


TRUNCATE table conf_procesos;
TRUNCATE table conf_pasos;
TRUNCATE table conf_paso_dependencias;
TRUNCATE table conf_proceso_instancias;
TRUNCATE table conf_proceso_ciclos;
TRUNCATE table conf_proceso_pasos;

--SELECT * FROM conf_pasos



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
SELECT p.id, 'ADICIONAR VENTAS', 2, 1 FROM conf_procesos p WHERE p.nombre = 'COMISIONES' AND p.estado = 1
UNION ALL
SELECT p.id, 'VENTAS ESPECIALES', 3, 1 FROM conf_procesos p WHERE p.nombre = 'COMISIONES' AND p.estado = 1
UNION ALL
SELECT p.id, 'COMISION DIRECTA', 4, 1 FROM conf_procesos p WHERE p.nombre = 'COMISIONES' AND p.estado = 1
UNION ALL
SELECT p.id, 'RED COMPRIMIDA', 5, 1 FROM conf_procesos p WHERE p.nombre = 'COMISIONES' AND p.estado = 1
UNION ALL
SELECT p.id, 'RED COMPLETA', 6, 1 FROM conf_procesos p WHERE p.nombre = 'COMISIONES' AND p.estado = 1
UNION ALL
SELECT p.id, 'COMISION GRUPO', 7, 1 FROM conf_procesos p WHERE p.nombre = 'COMISIONES' AND p.estado = 1
UNION ALL
SELECT p.id, 'OBTENER CARTERA', 8, 1 FROM conf_procesos p WHERE p.nombre = 'COMISIONES' AND p.estado = 1
UNION ALL
SELECT p.id, 'OBTENER CUOTAS', 9, 1 FROM conf_procesos p WHERE p.nombre = 'COMISIONES' AND p.estado = 1
UNION ALL
SELECT p.id, 'OBTENER EXCEDENTE', 10, 1 FROM conf_procesos p WHERE p.nombre = 'COMISIONES' AND p.estado = 1
UNION ALL
SELECT p.id, 'COMISION RESIDUAL', 11, 1 FROM conf_procesos p WHERE p.nombre = 'COMISIONES' AND p.estado = 1
UNION ALL
SELECT p.id, 'COMISION VENTA RESIDUAL', 12, 1 FROM conf_procesos p WHERE p.nombre = 'COMISIONES' AND p.estado = 1
UNION ALL
SELECT p.id, 'BONO PAR', 13, 1 FROM conf_procesos p WHERE p.nombre = 'COMISIONES' AND p.estado = 1;

-- ============================================
-- 3. DEPENDENCIAS DINÁMICAS (por nombre)
-- ============================================
INSERT INTO conf_paso_dependencias (paso_id, paso_requerido_id)
SELECT p2.id, p1.id
FROM conf_pasos p1
JOIN conf_pasos p2 ON p2.proceso_id = p1.proceso_id
WHERE p1.nombre = 'OBTENER VENTAS' AND p2.nombre = 'ADICIONAR VENTAS'

UNION ALL
SELECT p2.id, p1.id
FROM conf_pasos p1
JOIN conf_pasos p2 ON p2.proceso_id = p1.proceso_id
WHERE p1.nombre = 'ADICIONAR VENTAS' AND p2.nombre = 'VENTAS ESPECIALES'

UNION ALL
SELECT p2.id, p1.id
FROM conf_pasos p1
JOIN conf_pasos p2 ON p2.proceso_id = p1.proceso_id
WHERE p1.nombre = 'VENTAS ESPECIALES' AND p2.nombre = 'COMISION DIRECTA'

UNION ALL
SELECT p2.id, p1.id
FROM conf_pasos p1
JOIN conf_pasos p2 ON p2.proceso_id = p1.proceso_id
WHERE p1.nombre = 'COMISION DIRECTA' AND p2.nombre = 'RED COMPRIMIDA'

UNION ALL
SELECT p2.id, p1.id
FROM conf_pasos p1
JOIN conf_pasos p2 ON p2.proceso_id = p1.proceso_id
WHERE p1.nombre = 'RED COMPRIMIDA' AND p2.nombre = 'RED COMPLETA'

UNION ALL
SELECT p2.id, p1.id
FROM conf_pasos p1
JOIN conf_pasos p2 ON p2.proceso_id = p1.proceso_id
WHERE p1.nombre = 'RED COMPLETA' AND p2.nombre = 'COMISION GRUPO'

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
WHERE p1.nombre = 'COMISION RESIDUAL' AND p2.nombre = 'COMISION VENTA RESIDUAL'

UNION ALL
SELECT p2.id, p1.id
FROM conf_pasos p1
JOIN conf_pasos p2 ON p2.proceso_id = p1.proceso_id
WHERE p1.nombre = 'COMISION VENTA RESIDUAL' AND p2.nombre = 'BONO PAR';
