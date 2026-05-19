CREATE TABLE IF NOT EXISTS administracionhabilitacioncomision (
    lhabilitacion_id INT PRIMARY KEY,
    lcontacto_id INT NOT NULL,
    lciclo_id INT NOT NULL,
    monto_venta DECIMAL(18,2) NOT NULL,
    observacion VARCHAR(500) NULL,
    estado INT NOT NULL DEFAULT 1,
    usuario_creacion VARCHAR(100) NOT NULL,
    fecha_creacion DATETIME NOT NULL,
    usuario_modificacion VARCHAR(100) NULL,
    fecha_modificacion DATETIME NULL
);

UPDATE conf_pasos CP
INNER JOIN conf_procesos PR ON PR.id = CP.proceso_id
SET CP.orden = CP.orden + 1
WHERE PR.nombre = 'COMISIONES'
  AND PR.estado = 1
  AND CP.estado = 1
  AND CP.orden >= 5
  AND NOT EXISTS (
      SELECT 1
      FROM conf_pasos PX
      WHERE PX.proceso_id = CP.proceso_id
        AND PX.nombre = 'REGISTRO_HABILITACIONES'
        AND PX.estado = 1
  );

INSERT INTO conf_pasos (proceso_id, nombre, orden, es_obligatorio)
SELECT PR.id, 'REGISTRO_HABILITACIONES', 5, 1
FROM conf_procesos PR
WHERE PR.nombre = 'COMISIONES'
  AND PR.estado = 1
  AND NOT EXISTS (
      SELECT 1
      FROM conf_pasos CP
      WHERE CP.proceso_id = PR.id
        AND CP.nombre = 'REGISTRO_HABILITACIONES'
  );

DELETE CPD
FROM conf_paso_dependencias CPD
INNER JOIN conf_pasos PASO ON PASO.id = CPD.paso_id
INNER JOIN conf_pasos REQ ON REQ.id = CPD.paso_requerido_id
INNER JOIN conf_procesos PR ON PR.id = PASO.proceso_id AND PR.id = REQ.proceso_id
WHERE PR.nombre = 'COMISIONES'
  AND PASO.nombre = 'RED COMPRIMIDA'
  AND REQ.nombre = 'COMISION DIRECTA';

INSERT INTO conf_paso_dependencias (paso_id, paso_requerido_id)
SELECT PASO_ACTUAL.id, PASO_REQUERIDO.id
FROM conf_pasos PASO_REQUERIDO
INNER JOIN conf_pasos PASO_ACTUAL ON PASO_ACTUAL.proceso_id = PASO_REQUERIDO.proceso_id
INNER JOIN conf_procesos PR ON PR.id = PASO_REQUERIDO.proceso_id
WHERE PR.nombre = 'COMISIONES'
  AND PASO_REQUERIDO.nombre = 'COMISION DIRECTA'
  AND PASO_ACTUAL.nombre = 'REGISTRO_HABILITACIONES'
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
  AND PASO_REQUERIDO.nombre = 'REGISTRO_HABILITACIONES'
  AND PASO_ACTUAL.nombre = 'RED COMPRIMIDA'
  AND NOT EXISTS (
      SELECT 1
      FROM conf_paso_dependencias CPD
      WHERE CPD.paso_id = PASO_ACTUAL.id
        AND CPD.paso_requerido_id = PASO_REQUERIDO.id
  );
