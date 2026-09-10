-- Agrega la etapa de pagos al proceso COMISIONES.
-- Es idempotente: puede ejecutarse más de una vez sin duplicar pasos ni dependencias.
-- Orden operativo: Bono Par -> Retención -> Aplicación -> Facturación -> Migración aplicaciones.

START TRANSACTION;

INSERT INTO conf_pasos (proceso_id, nombre, orden, es_obligatorio, estado)
SELECT proceso.id, pasos.nombre, pasos.orden, 1, 1
FROM conf_procesos proceso
INNER JOIN (
    SELECT 'RETENCION' nombre, 16 orden
    UNION ALL SELECT 'APLICACION', 17
    UNION ALL SELECT 'FACTURACION', 18
    UNION ALL SELECT 'MIGRACION APLICACIONES', 19
) pasos
    ON 1 = 1
LEFT JOIN conf_pasos existente
    ON existente.proceso_id = proceso.id
   AND CONVERT(existente.nombre USING utf8mb4) COLLATE utf8mb4_general_ci = pasos.nombre COLLATE utf8mb4_general_ci
WHERE CONVERT(proceso.nombre USING utf8mb4) COLLATE utf8mb4_general_ci = _utf8mb4'COMISIONES' COLLATE utf8mb4_general_ci
  AND proceso.estado = 1
  AND existente.id IS NULL;

INSERT INTO conf_paso_dependencias (paso_id, paso_requerido_id)
SELECT paso_actual.id, paso_requerido.id
FROM conf_procesos proceso
INNER JOIN (
    SELECT 'RETENCION' paso_actual, 'BONO PAR' paso_requerido
    UNION ALL SELECT 'APLICACION', 'RETENCION'
    UNION ALL SELECT 'FACTURACION', 'APLICACION'
    UNION ALL SELECT 'MIGRACION APLICACIONES', 'FACTURACION'
) dependencias
    ON 1 = 1
INNER JOIN conf_pasos paso_actual
    ON paso_actual.proceso_id = proceso.id
   AND CONVERT(paso_actual.nombre USING utf8mb4) COLLATE utf8mb4_general_ci = dependencias.paso_actual COLLATE utf8mb4_general_ci
INNER JOIN conf_pasos paso_requerido
    ON paso_requerido.proceso_id = proceso.id
   AND CONVERT(paso_requerido.nombre USING utf8mb4) COLLATE utf8mb4_general_ci = dependencias.paso_requerido COLLATE utf8mb4_general_ci
LEFT JOIN conf_paso_dependencias existente
    ON existente.paso_id = paso_actual.id
   AND existente.paso_requerido_id = paso_requerido.id
WHERE CONVERT(proceso.nombre USING utf8mb4) COLLATE utf8mb4_general_ci = _utf8mb4'COMISIONES' COLLATE utf8mb4_general_ci
  AND proceso.estado = 1
  AND existente.id IS NULL;

COMMIT;
