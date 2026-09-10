-- Reordena la etapa de pagos de COMISIONES.
-- Orden final: Bono Par -> Facturación -> Retención -> Aplicación -> Migración aplicaciones.
-- Ejecutar después de agregar_pasos_pagos_comisiones.sql.

START TRANSACTION;

UPDATE conf_pasos paso
INNER JOIN conf_procesos proceso ON proceso.id = paso.proceso_id
SET paso.orden = CASE paso.nombre
    WHEN 'FACTURACION' THEN 16
    WHEN 'RETENCION' THEN 17
    WHEN 'APLICACION' THEN 18
    WHEN 'MIGRACION APLICACIONES' THEN 19
    ELSE paso.orden
END
WHERE CONVERT(proceso.nombre USING utf8mb4) COLLATE utf8mb4_general_ci = _utf8mb4'COMISIONES' COLLATE utf8mb4_general_ci
  AND proceso.estado = 1
  AND CONVERT(paso.nombre USING utf8mb4) COLLATE utf8mb4_general_ci IN (
      _utf8mb4'FACTURACION' COLLATE utf8mb4_general_ci,
      _utf8mb4'RETENCION' COLLATE utf8mb4_general_ci,
      _utf8mb4'APLICACION' COLLATE utf8mb4_general_ci,
      _utf8mb4'MIGRACION APLICACIONES' COLLATE utf8mb4_general_ci
  );

-- Elimina únicamente las dependencias anteriores entre los cuatro pasos de pagos.
DELETE dependencia
FROM conf_paso_dependencias dependencia
INNER JOIN conf_pasos paso_actual ON paso_actual.id = dependencia.paso_id
INNER JOIN conf_pasos paso_requerido ON paso_requerido.id = dependencia.paso_requerido_id
INNER JOIN conf_procesos proceso ON proceso.id = paso_actual.proceso_id
WHERE proceso.id = paso_requerido.proceso_id
  AND CONVERT(proceso.nombre USING utf8mb4) COLLATE utf8mb4_general_ci = _utf8mb4'COMISIONES' COLLATE utf8mb4_general_ci
  AND CONVERT(paso_actual.nombre USING utf8mb4) COLLATE utf8mb4_general_ci IN (
      _utf8mb4'FACTURACION' COLLATE utf8mb4_general_ci,
      _utf8mb4'RETENCION' COLLATE utf8mb4_general_ci,
      _utf8mb4'APLICACION' COLLATE utf8mb4_general_ci,
      _utf8mb4'MIGRACION APLICACIONES' COLLATE utf8mb4_general_ci
  )
  AND CONVERT(paso_requerido.nombre USING utf8mb4) COLLATE utf8mb4_general_ci IN (
      _utf8mb4'FACTURACION' COLLATE utf8mb4_general_ci,
      _utf8mb4'RETENCION' COLLATE utf8mb4_general_ci,
      _utf8mb4'APLICACION' COLLATE utf8mb4_general_ci,
      _utf8mb4'MIGRACION APLICACIONES' COLLATE utf8mb4_general_ci
  );

INSERT INTO conf_paso_dependencias (paso_id, paso_requerido_id)
SELECT paso_actual.id, paso_requerido.id
FROM conf_procesos proceso
INNER JOIN (
    SELECT 'FACTURACION' paso_actual, 'BONO PAR' paso_requerido
    UNION ALL SELECT 'RETENCION', 'FACTURACION'
    UNION ALL SELECT 'APLICACION', 'RETENCION'
    UNION ALL SELECT 'MIGRACION APLICACIONES', 'APLICACION'
) configuracion ON 1 = 1
INNER JOIN conf_pasos paso_actual
    ON paso_actual.proceso_id = proceso.id
   AND CONVERT(paso_actual.nombre USING utf8mb4) COLLATE utf8mb4_general_ci = configuracion.paso_actual COLLATE utf8mb4_general_ci
INNER JOIN conf_pasos paso_requerido
    ON paso_requerido.proceso_id = proceso.id
   AND CONVERT(paso_requerido.nombre USING utf8mb4) COLLATE utf8mb4_general_ci = configuracion.paso_requerido COLLATE utf8mb4_general_ci
LEFT JOIN conf_paso_dependencias existente
    ON existente.paso_id = paso_actual.id
   AND existente.paso_requerido_id = paso_requerido.id
WHERE CONVERT(proceso.nombre USING utf8mb4) COLLATE utf8mb4_general_ci = _utf8mb4'COMISIONES' COLLATE utf8mb4_general_ci
  AND proceso.estado = 1
  AND existente.id IS NULL;

COMMIT;
