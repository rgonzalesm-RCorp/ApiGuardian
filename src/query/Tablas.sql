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

alter table administracioncontacto add lbanco_id INT DEFAULT 0;

 CREATE TABLE administracionestadocontrato (
    lestadocontrato_id INT PRIMARY KEY,
    snombre VARCHAR(100) NOT NULL,
    fechaadd DATETIME,
    usuarioadd VARCHAR(50),
    fechamod DATETIME,
    usuariomod VARCHAR(50)
) ;


insert into administracionestadocontrato 
(lestadocontrato_id, snombre, fechaadd, usuarioadd, fechamod, usuariomod)
values 
(0, 'En Reserva', now(), 'admin', now(), 'admin'),
(1, 'Cuota Inicial Pagada', now(), 'admin', now(), 'admin'),
(2, 'Contrato Retenido por Etica', now(), 'admin', now(), 'admin'),
(3, 'En Plan de Pagos', now(), 'admin', now(), 'admin'),
(4, 'Contrato Firmado', now(), 'admin', now(), 'admin'),
(5, 'Comisión pagada', now(), 'admin', now(), 'admin'),
(6, 'Recuperación', now(), 'admin', now(), 'admin'),
(7, 'Suscripcion', now(), 'admin', now(), 'admin');

 CREATE TABLE administraciontipobaja (
    ltipobaja_id INT PRIMARY KEY,
    snombre VARCHAR(100) NOT NULL,
    fechaadd DATETIME,
    usuarioadd VARCHAR(50),
    fechamod DATETIME,
    usuariomod VARCHAR(50)
) ;


insert into administraciontipobaja
(ltipobaja_id, snombre, fechaadd, usuarioadd, fechamod, usuariomod)
values 
(0, 'Cesión de derecho', now(), 'admin', now(), 'admin'),
(1, 'Suspensión temporal', now(), 'admin', now(), 'admin'),
(2, 'Expulsión definitiva', now(), 'admin', now(), 'admin'),
(3, 'Otro caso', now(), 'admin', now(), 'admin');




alter table administraciontipocontrato add abr VARCHAR(150) DEFAULT '';
update administraciontipocontrato set abr = 'CR' where ltipocontrato_id = 1;
update administraciontipocontrato set abr = 'CT' where ltipocontrato_id = 2;
update administraciontipocontrato set abr = 'IT' where ltipocontrato_id = 3;
update administraciontipocontrato set abr = 'PB' where ltipocontrato_id = 4;
update administraciontipocontrato set abr = 'AM' where ltipocontrato_id = 5;