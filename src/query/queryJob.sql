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