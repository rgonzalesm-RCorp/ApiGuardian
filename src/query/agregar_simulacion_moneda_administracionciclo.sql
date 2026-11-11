-- Agrega configuración de simulación y moneda a los ciclos.
-- Ejecutar primero en la copia de la base de datos y verificar los valores antes de producción.
-- Valor inicial seguro: ningún ciclo queda habilitado para simulación.
-- La moneda inicial de los registros existentes queda en BS; ajustar los ciclos que correspondan desde Parámetros > Ciclos.

ALTER TABLE administracionciclo
    ADD COLUMN simulacion TINYINT(1) NOT NULL DEFAULT 0
        COMMENT '1 habilita el ciclo para el job de simulación; 0 lo excluye',
    ADD COLUMN moneda ENUM('BS', 'SUS') NOT NULL DEFAULT 'BS'
        COMMENT 'Moneda configurada para el ciclo';

-- Verificación posterior a la migración.
SELECT lciclo_id, snombre, simulacion, moneda
FROM administracionciclo
ORDER BY lciclo_id DESC;
