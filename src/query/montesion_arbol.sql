-- Árbol de patrocinio de Monte Sion por ciclo.
-- Foto del árbol construida desde administracioncontacto.lpatrocinante_id.
-- ruta contiene los ids desde la raíz hasta la persona, ej. '/1/5/23/'.
CREATE TABLE IF NOT EXISTS montesion_arbol (
    arbol_id           BIGINT NOT NULL AUTO_INCREMENT,
    lciclo_id          INT NOT NULL,
    lcontacto_id       INT NOT NULL,
    lpadre_contacto_id INT NULL,
    nivel              INT NOT NULL,
    ruta               VARCHAR(500) NOT NULL,
    snombrecompleto    VARCHAR(255) NOT NULL,
    usuario            VARCHAR(150) NOT NULL,
    fecharegistro      DATETIME NOT NULL,
    PRIMARY KEY (arbol_id),
    UNIQUE KEY uq_montesion_arbol_ciclo_contacto (lciclo_id, lcontacto_id),
    KEY ix_montesion_arbol_ciclo_padre (lciclo_id, lpadre_contacto_id),
    KEY ix_montesion_arbol_ciclo_ruta (lciclo_id, ruta)
);
