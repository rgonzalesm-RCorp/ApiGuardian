SET @casos_observados_exists := (
    SELECT COUNT(*)
    FROM conf_pasos CP
    INNER JOIN conf_procesos PR ON PR.id = CP.proceso_id
    WHERE PR.nombre = 'COMISIONES'
      AND PR.estado = 1
      AND CP.nombre = 'CASOS OBSERVADOS'
);

UPDATE conf_pasos CP
INNER JOIN conf_procesos PR ON PR.id = CP.proceso_id
SET CP.orden = CP.orden + 1
WHERE PR.nombre = 'COMISIONES'
  AND PR.estado = 1
  AND CP.estado = 1
  AND CP.orden >= 2
  AND @casos_observados_exists = 0;

INSERT INTO conf_pasos (proceso_id, nombre, orden, es_obligatorio)
SELECT PR.id, 'CASOS OBSERVADOS', 2, 1
FROM conf_procesos PR
WHERE PR.nombre = 'COMISIONES'
  AND PR.estado = 1
  AND NOT EXISTS (
      SELECT 1
      FROM conf_pasos CP
      WHERE CP.proceso_id = PR.id
        AND CP.nombre = 'CASOS OBSERVADOS'
  );

UPDATE conf_pasos CP
INNER JOIN conf_procesos PR ON PR.id = CP.proceso_id
SET CP.orden = 2
WHERE PR.nombre = 'COMISIONES'
  AND PR.estado = 1
  AND CP.nombre = 'CASOS OBSERVADOS';

DELETE CPD
FROM conf_paso_dependencias CPD
INNER JOIN conf_pasos PASO ON PASO.id = CPD.paso_id
INNER JOIN conf_pasos REQ ON REQ.id = CPD.paso_requerido_id
INNER JOIN conf_procesos PR ON PR.id = PASO.proceso_id AND PR.id = REQ.proceso_id
WHERE PR.nombre = 'COMISIONES'
  AND PASO.nombre = 'ADICIONAR VENTAS'
  AND REQ.nombre = 'OBTENER VENTAS';

INSERT INTO conf_paso_dependencias (paso_id, paso_requerido_id)
SELECT PASO_ACTUAL.id, PASO_REQUERIDO.id
FROM conf_pasos PASO_REQUERIDO
INNER JOIN conf_pasos PASO_ACTUAL ON PASO_ACTUAL.proceso_id = PASO_REQUERIDO.proceso_id
INNER JOIN conf_procesos PR ON PR.id = PASO_REQUERIDO.proceso_id
WHERE PR.nombre = 'COMISIONES'
  AND PASO_REQUERIDO.nombre = 'OBTENER VENTAS'
  AND PASO_ACTUAL.nombre = 'CASOS OBSERVADOS'
  AND NOT EXISTS (
      SELECT 1
      FROM conf_paso_dependencias CPD
      WHERE CPD.paso_id = PASO_ACTUAL.id
        AND CPD.paso_requerido_id = PASO_REQUERIDO.id
  );

INSERT INTO conf_paso_dependencias (paso_id, paso_requerido_id)
SELECT PASO_ACTUAL.id, PASO_REQUERIDO.id
FROM conf_pasos PASO_REQUERIDO
INNER JOIN conf_pasos PASO_ACTUAL ON PASO_ACTUAL.proceso_id = PASO_REQUERIDO.proceso_id
INNER JOIN conf_procesos PR ON PR.id = PASO_REQUERIDO.proceso_id
WHERE PR.nombre = 'COMISIONES'
  AND PASO_REQUERIDO.nombre = 'CASOS OBSERVADOS'
  AND PASO_ACTUAL.nombre = 'ADICIONAR VENTAS'
  AND NOT EXISTS (
      SELECT 1
      FROM conf_paso_dependencias CPD
      WHERE CPD.paso_id = PASO_ACTUAL.id
        AND CPD.paso_requerido_id = PASO_REQUERIDO.id
  );
