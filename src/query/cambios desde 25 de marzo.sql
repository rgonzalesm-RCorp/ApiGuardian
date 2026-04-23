
CREATE INDEX idx_lpatrocinador1g ON tmp_residual_red (lpatrocinador1g);
CREATE INDEX idx_lpatrocinador2g ON tmp_residual_red (lpatrocinador2g);
CREATE INDEX idx_lpatrocinador3g ON tmp_residual_red (lpatrocinador3g);
CREATE INDEX idx_lpatrocinador4g ON tmp_residual_red (lpatrocinador4g);
CREATE INDEX idx_lpatrocinador5g ON tmp_residual_red (lpatrocinador5g);
CREATE INDEX idx_lpatrocinador6g ON tmp_residual_red (lpatrocinador6g);
CREATE INDEX idx_lpatrocinador7g ON tmp_residual_red (lpatrocinador7g);

CREATE INDEX idx_lcontacto_id ON administracioncontacto (lcontacto_id);
CREATE INDEX idx_lcontacto_id ON administracionventapersonal (lcontacto_id);




alter table T_ACCIONESCUOTASGRL add empresa varchar(200);



CREATE TABLE red_comprimida (
    RedComprimidaId INT PRIMARY KEY AUTO_INCREMENT,
    lcontrato_id INT,
    lciclo_id INT,
    lcontacto_id INT, 
    lasesor_id INT, 
    Nivel int,
    usuario varchar(500),
    fecharegistro DATETIME
) ;
CREATE TABLE red_completa_cuotas (
    RedComprimidaId INT PRIMARY KEY AUTO_INCREMENT,
    lcontrato_id INT,
    lciclo_id INT,
    lcontacto_id INT, 
    lasesor_id INT, 
    Nivel int,
    usuario varchar(500),
    fecharegistro DATETIME
) ;

CREATE INDEX idx_DOCID ON cartera (DOCID);
CREATE INDEX idx_CLIENTE ON cartera (CLIENTE);

ALTER table tmp_residual_contacto add lpatrocinante_id int