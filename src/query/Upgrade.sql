
CREATE TABLE upgrade_solicitud (
    upgrade_solicitud_id BIGINT NOT NULL AUTO_INCREMENT,

    solicitud_id INT NOT NULL,
    doc_id VARCHAR(200) NOT NULL,
    doc_id_vendedor VARCHAR(50) NULL,

    empresa_hold_id INT NOT NULL,
    proyecto_hold_id INT NOT NULL,
    venta_hold_id INT NOT NULL,
    producto_hold_id VARCHAR(200) NOT NULL,

    monto_hold DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    pagado_hold DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    deuda_hold DECIMAL(18,2) NOT NULL DEFAULT 0.00,

    empresa_id INT NULL,
    proyecto_id INT NULL,
    venta_id INT NULL,
    producto_id VARCHAR(200) NOT NULL,

    monto DECIMAL(18,2) NULL,
    deuda DECIMAL(18,2) NULL,
    cuota INT NULL,

    estado INT NOT NULL DEFAULT 1,
    usuario_creacion VARCHAR(100) NOT NULL,
    fecha_creacion DATETIME NOT NULL,
    usuario_modificacion VARCHAR(100) NULL,
    fecha_modificacion DATETIME NULL,
    lciclo_id INT NULL,


    PRIMARY KEY (upgrade_solicitud_id)
);