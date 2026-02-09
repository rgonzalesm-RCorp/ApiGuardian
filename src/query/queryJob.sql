   CREATE TABLE administracionjob(
    ladministracionjobId INT AUTO_INCREMENT PRIMARY KEY,
    proceso VARCHAR(1500),
    lciclo_id INT,
    estado INT,
    fechaadd DATETIME,
    usuarioadd VARCHAR(500),
    fechamod DATETIME,
    usuariomod VARCHAR(500)
)

ALTER TABLE ADMINISTRACIONCONTRATO
ADD COLUMN porcentaje_inicial DECIMAL(18,2) NULL DEFAULT 0;


CREATE TABLE PC_CONFIGVTAPERSONAL(
    PC_ConfigVtaPersonalId INT AUTO_INCREMENT PRIMARY KEY
    , lciclo_id INT
    , estado INT
    , fechaadd DATETIME
    , usuarioadd VARCHAR(50)
    , fechamod DATETIME
    , usuariomod VARCHAR(50)
);
CREATE TABLE PC_CONFIGVTAPERSONALCOMPLEJO(
    PC_ConfigVtaPersonalComplejoId INT AUTO_INCREMENT PRIMARY KEY
    , PC_ConfigVtaPersonalId INT
    , lcomplejo_id INT
    , estado INT
    , fechaadd DATETIME
    , usuarioadd VARCHAR(50)
    , fechamod DATETIME
    , usuariomod VARCHAR(50)
);
CREATE TABLE PC_CONFIGVTAPERSONALINICIAL(
    PC_ConfigVtaPersonalInicialId INT AUTO_INCREMENT PRIMARY KEY
    , PC_ConfigVtaPersonalId INT
    , inicial decimal(18,2)
    , comision decimal(18,2)
    , estado INT
    , fechaadd DATETIME
    , usuarioadd VARCHAR(50)
    , fechamod DATETIME
    , usuariomod VARCHAR(50)
);