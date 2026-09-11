-- Ejecutar una sola vez antes de desplegar la funcionalidad.
CREATE TABLE IF NOT EXISTS administraciondescuentocicloprorrateo (
    ldescuentocicloprorrateo_id INT NOT NULL,
    ldescuentociclodetalle_id INT NOT NULL,
    lprorrateo_id INT NOT NULL,
    dmonto DECIMAL(18,2) NOT NULL,
    susuarioadd VARCHAR(100) NULL,
    dtfechaadd DATETIME NOT NULL,
    PRIMARY KEY (ldescuentocicloprorrateo_id),
    INDEX idx_descuento_prorrateo_detalle (ldescuentociclodetalle_id),
    INDEX idx_descuento_prorrateo_prorrateo (lprorrateo_id)
);
