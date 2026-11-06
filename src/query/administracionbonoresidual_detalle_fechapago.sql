-- Fecha de pago de la cuota que originó cada fila del detalle del bono residual.
-- Ejecutar una sola vez. Si la columna ya existe, MySQL devolverá error y no hay que volver a correrlo.
ALTER TABLE administracionbonoresidual_detalle
    ADD COLUMN fechapago DATETIME NULL AFTER lciclo_id;
