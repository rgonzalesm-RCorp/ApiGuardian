CREATE TABLE administracionbanco (
    lbanco_id INT AUTO_INCREMENT PRIMARY KEY,
    snombre VARCHAR(100) NOT NULL,
    scodigo VARCHAR(50) NOT NULL,
    lmoneda_id INT,
    estado TINYINT DEFAULT 1,
    fechaadd DATETIME,
    usuarioadd VARCHAR(50),
    fechamod DATETIME,
    usuariomod VARCHAR(50)
) ;

CREATE TABLE administracionmoneda(
    lmoneda_id INT AUTO_INCREMENT PRIMARY KEY,
    snombre VARCHAR(100) NOT NULL
);
insert into administracionmoneda (snombre) values 
('SUS'), ('BOB');

alter table administracioncontacto add lbanco_id INT DEFAULT 0