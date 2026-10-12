-- Estructura aislada para /api/RedSer. No modifica red_comprimida ni el flujo de comisiones.
CREATE TABLE IF NOT EXISTS red_ser (
    redser_id BIGINT NOT NULL AUTO_INCREMENT,
    lcontacto_id INT NOT NULL,
    lpadre_contacto_id INT NULL,
    snombrecompleto VARCHAR(255) NOT NULL,
    scedulaidentidad VARCHAR(50) NOT NULL DEFAULT '',
    nivel INT NOT NULL,
    cestructural TINYINT(1) NOT NULL DEFAULT 0,
    dt_inicio DATE NOT NULL,
    dt_fin DATE NOT NULL,
    usuario VARCHAR(150) NOT NULL,
    fecharegistro DATETIME NOT NULL,
    PRIMARY KEY (redser_id),
    UNIQUE KEY uq_red_ser_periodo_contacto (dt_inicio, dt_fin, lcontacto_id),
    KEY ix_red_ser_periodo_padre (dt_inicio, dt_fin, lpadre_contacto_id)
);

-- Ejecutar también en instalaciones donde red_ser ya fue creada previamente.
ALTER TABLE red_ser
    ADD COLUMN IF NOT EXISTS scedulaidentidad VARCHAR(50) NOT NULL DEFAULT '' AFTER snombrecompleto;
