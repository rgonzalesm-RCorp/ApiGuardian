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




----REPORTES, se debe agregar los nuevo complejos a esta tabla con su empresa, esto es para el REPORTE DE FACTURACION, ya que la actual consulta realiza muchos case
---- y eso hace que la consulta demore mucho tiempo en arrojar resutado
    DROP TABLE IF EXISTS complejosempresa;

    CREATE TABLE complejosempresa (
        lcomplejo_id   INT ,
        empresa_id     INT,
        empresa_nombre VARCHAR(80)
    );

    INSERT INTO complejosempresa (lcomplejo_id, empresa_id, empresa_nombre)
VALUES
(1, 1, 'SION'),
(2, 1, 'SION'),
(5, 1, 'SION'),

(3, 2, 'KINTAS'),
(4, 2, 'KINTAS'),
(6, 2, 'KINTAS'),
(11, 2, 'KINTAS'),
(66, 2, 'KINTAS'),
(67, 2, 'KINTAS'),
(68, 2, 'KINTAS'),
(69, 2, 'KINTAS'),
(70, 2, 'KINTAS'),
(71, 2, 'KINTAS'),
(72, 2, 'KINTAS'),
(73, 2, 'KINTAS'),
(74, 2, 'KINTAS'),
(75, 2, 'KINTAS'),
(96, 2, 'KINTAS'),
(97, 2, 'KINTAS'),

(7, 3, 'ZURIEL'),
(8, 3, 'ZURIEL'),
(9, 3, 'ZURIEL'),
(10, 3, 'ZURIEL'),
(52, 3, 'ZURIEL'),
(53, 3, 'ZURIEL'),
(54, 3, 'ZURIEL'),
(57, 3, 'ZURIEL'),
(60, 3, 'ZURIEL'),
(86, 3, 'ZURIEL'),
(92, 3, 'ZURIEL'),
(99, 3, 'ZURIEL'),

(13, 4, 'NICAPOLIS'),
(37, 4, 'NICAPOLIS'),
(40, 4, 'NICAPOLIS'),
(41, 4, 'NICAPOLIS'),
(42, 4, 'NICAPOLIS'),
(43, 4, 'NICAPOLIS'),
(47, 4, 'NICAPOLIS'),
(50, 4, 'NICAPOLIS'),
(61, 4, 'NICAPOLIS'),
(64, 4, 'NICAPOLIS'),

(16, 5, 'ASHER'),
(19, 5, 'ASHER'),
(21, 5, 'ASHER'),
(26, 5, 'ASHER'),
(38, 5, 'ASHER'),
(51, 5, 'ASHER'),

(17, 6, 'SHOFAR'),
(20, 6, 'SHOFAR'),
(25, 6, 'SHOFAR'),

(14, 7, 'CEMENTERIO'),
(15, 7, 'CEMENTERIO'),

(18, 8, 'MEXICO'),

(22, 10, 'SEDE LAS PRADERAS/ROYAL PARI'),
(58, 10, 'SEDE LAS PRADERAS/ROYAL PARI'),
(59, 10, 'SEDE LAS PRADERAS/ROYAL PARI'),
(62, 10, 'SEDE LAS PRADERAS/ROYAL PARI'),

(27, 11, 'MURANO'),

(29, 12, 'KALOMAI'),

(28, 13, 'VALLE ANGOSTURA/ELIAN'),

(30, 14, 'JAYIL'),
(32, 14, 'JAYIL'),
(35, 14, 'JAYIL'),
(36, 14, 'JAYIL'),
(39, 14, 'JAYIL'),
(33, 14, 'JAYIL'),
(45, 14, 'JAYIL'),
(44, 14, 'JAYIL'),
(48, 14, 'JAYIL'),
(55, 14, 'JAYIL'),
(65, 14, 'JAYIL'),
(76, 14, 'JAYIL'),
(77, 14, 'JAYIL'),
(78, 14, 'JAYIL'),
(79, 14, 'JAYIL'),
(80, 14, 'JAYIL'),
(82, 14, 'JAYIL'),
(83, 14, 'JAYIL'),
(84, 14, 'JAYIL'),
(87, 14, 'JAYIL'),
(88, 14, 'JAYIL'),
(89, 14, 'JAYIL'),
(90, 14, 'JAYIL'),
(91, 14, 'JAYIL'),
(93, 14, 'JAYIL'),
(94, 14, 'JAYIL'),

(31, 15, 'NEIZAN JAYIL'),
(81, 15, 'NEIZAN JAYIL'),

(23, 16, 'NEIZAN ASHER'),

(46, 17, 'CLUB DEPORTIVO ROYAL PARI / JAIM'),
(49, 17, 'CLUB DEPORTIVO ROYAL PARI / JAIM'),

(34, 18, 'MENORAH'),

(85, 21, 'AVDEL'),
(95, 21, 'AVDEL'),
(100, 21, 'AVDEL'),
(101, 21, 'AVDEL'),
(102, 21, 'AVDEL');

ALTER TABLE complejosempresa ADD identificadorVtaPersonal INT;

UPDATE complejosempresa SET identificadorVtaPersonal = 1 WHERE lcomplejo_id IN (1, 2, 5);
UPDATE complejosempresa SET identificadorVtaPersonal = 4 WHERE lcomplejo_id IN (3, 4, 6, 11, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 96, 97);
UPDATE complejosempresa SET identificadorVtaPersonal = 7 WHERE lcomplejo_id IN (7, 8, 9, 10, 52, 53, 54, 57, 60, 86, 92, 99);
UPDATE complejosempresa SET identificadorVtaPersonal = 10 WHERE lcomplejo_id IN (13, 37, 40, 41, 42, 43, 47, 50, 61, 64);
UPDATE complejosempresa SET identificadorVtaPersonal = 13 WHERE lcomplejo_id IN (16, 19, 21, 26, 38, 51);
UPDATE complejosempresa SET identificadorVtaPersonal = 16 WHERE lcomplejo_id IN (17, 20, 25);
UPDATE complejosempresa SET identificadorVtaPersonal = 19 WHERE lcomplejo_id IN (14, 15);
UPDATE complejosempresa SET identificadorVtaPersonal = 22 WHERE lcomplejo_id IN (18);
UPDATE complejosempresa SET identificadorVtaPersonal = 25 WHERE lcomplejo_id IN (23, 31);
UPDATE complejosempresa SET identificadorVtaPersonal = 28 WHERE lcomplejo_id IN (22, 58, 59, 62);
UPDATE complejosempresa SET identificadorVtaPersonal = 31 WHERE lcomplejo_id IN (27);
UPDATE complejosempresa SET identificadorVtaPersonal = 34 WHERE lcomplejo_id IN (29);
UPDATE complejosempresa SET identificadorVtaPersonal = 37 WHERE lcomplejo_id IN (28);
UPDATE complejosempresa SET identificadorVtaPersonal = 40 WHERE lcomplejo_id IN (30, 32, 35, 36, 39, 33, 45, 44, 48, 55, 65, 76, 77, 78, 79, 80, 82, 83, 84, 87, 88, 89, 90, 91, 93, 94);
UPDATE complejosempresa SET identificadorVtaPersonal = 43 WHERE lcomplejo_id IN (31, 81);
UPDATE complejosempresa SET identificadorVtaPersonal = 46 WHERE lcomplejo_id IN (23);
UPDATE complejosempresa SET identificadorVtaPersonal = 49 WHERE lcomplejo_id IN (46);
UPDATE complejosempresa SET identificadorVtaPersonal = 52 WHERE lcomplejo_id IN (34);
UPDATE complejosempresa SET identificadorVtaPersonal = 96 WHERE lcomplejo_id IN (85, 95, 100, 101, 102);

ALTER TABLE complejosempresa ADD identificadorVtaGrupo INT;

UPDATE complejosempresa SET identificadorVtaGrupo = 2 WHERE lcomplejo_id IN (1, 2, 5);
UPDATE complejosempresa SET identificadorVtaGrupo = 5 WHERE lcomplejo_id IN (3, 4, 6, 11, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 96, 97);
UPDATE complejosempresa SET identificadorVtaGrupo = 8 WHERE lcomplejo_id IN (7, 8, 9, 10, 52, 53, 54, 57, 60, 86, 92, 99);
UPDATE complejosempresa SET identificadorVtaGrupo = 11 WHERE lcomplejo_id IN (13, 37, 40, 41, 42, 43, 47, 50, 61, 64);
UPDATE complejosempresa SET identificadorVtaGrupo = 14 WHERE lcomplejo_id IN (16, 19, 21, 26, 38, 51);
UPDATE complejosempresa SET identificadorVtaGrupo = 17 WHERE lcomplejo_id IN (17, 20, 25);
UPDATE complejosempresa SET identificadorVtaGrupo = 20 WHERE lcomplejo_id IN (14, 15);
UPDATE complejosempresa SET identificadorVtaGrupo = 23 WHERE lcomplejo_id IN (18);
UPDATE complejosempresa SET identificadorVtaGrupo = 26 WHERE lcomplejo_id IN (23, 31);
UPDATE complejosempresa SET identificadorVtaGrupo = 29 WHERE lcomplejo_id IN (22, 58, 59, 62);
UPDATE complejosempresa SET identificadorVtaGrupo = 32 WHERE lcomplejo_id IN (27);
UPDATE complejosempresa SET identificadorVtaGrupo = 35 WHERE lcomplejo_id IN (29);
UPDATE complejosempresa SET identificadorVtaGrupo = 38 WHERE lcomplejo_id IN (28);
UPDATE complejosempresa SET identificadorVtaGrupo = 41 WHERE lcomplejo_id IN (30, 32, 35, 36, 39, 33, 45, 44, 48, 55, 65, 76, 77, 78, 79, 80, 82, 83, 84, 87, 88, 89, 90, 91, 93, 94);
UPDATE complejosempresa SET identificadorVtaGrupo = 44 WHERE lcomplejo_id IN (31, 81);
UPDATE complejosempresa SET identificadorVtaGrupo = 47 WHERE lcomplejo_id IN (23);
UPDATE complejosempresa SET identificadorVtaGrupo = 50 WHERE lcomplejo_id IN (46);
UPDATE complejosempresa SET identificadorVtaGrupo = 53 WHERE lcomplejo_id IN (34);
UPDATE complejosempresa SET identificadorVtaGrupo = 97 WHERE lcomplejo_id IN (85, 95, 100, 101, 102);

ALTER TABLE complejosempresa ADD identificadorResidual INT;

UPDATE complejosempresa SET identificadorResidual = 3 WHERE lcomplejo_id IN (1, 2, 5);
UPDATE complejosempresa SET identificadorResidual = 6 WHERE lcomplejo_id IN (3, 4, 6, 11, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 96, 97);
UPDATE complejosempresa SET identificadorResidual = 9 WHERE lcomplejo_id IN (7, 8, 9, 10, 52, 53, 54, 57, 60, 86, 92, 99);
UPDATE complejosempresa SET identificadorResidual = 12 WHERE lcomplejo_id IN (13, 37, 40, 41, 42, 43, 47, 50, 61, 64);
UPDATE complejosempresa SET identificadorResidual = 15 WHERE lcomplejo_id IN (16, 19, 21, 26, 38, 51);
UPDATE complejosempresa SET identificadorResidual = 18 WHERE lcomplejo_id IN (17, 20, 25);
UPDATE complejosempresa SET identificadorResidual = 21 WHERE lcomplejo_id IN (14, 15);
UPDATE complejosempresa SET identificadorResidual = 24 WHERE lcomplejo_id IN (18);
UPDATE complejosempresa SET identificadorResidual = 27 WHERE lcomplejo_id IN (23, 31);
UPDATE complejosempresa SET identificadorResidual = 30 WHERE lcomplejo_id IN (22, 58, 59, 62);
UPDATE complejosempresa SET identificadorResidual = 33 WHERE lcomplejo_id IN (27);
UPDATE complejosempresa SET identificadorResidual = 36 WHERE lcomplejo_id IN (29);
UPDATE complejosempresa SET identificadorResidual = 39 WHERE lcomplejo_id IN (28);
UPDATE complejosempresa SET identificadorResidual = 42 WHERE lcomplejo_id IN (30, 32, 35, 36, 39, 33, 45, 44, 48, 55, 65, 76, 77, 78, 79, 80, 82, 83, 84, 87, 88, 89, 90, 91, 93, 94);
UPDATE complejosempresa SET identificadorResidual = 45 WHERE lcomplejo_id IN (31, 81);
UPDATE complejosempresa SET identificadorResidual = 48 WHERE lcomplejo_id IN (23);
UPDATE complejosempresa SET identificadorResidual = 51 WHERE lcomplejo_id IN (46);
UPDATE complejosempresa SET identificadorResidual = 54 WHERE lcomplejo_id IN (34);
UPDATE complejosempresa SET identificadorResidual = 98 WHERE lcomplejo_id IN (85, 95, 100, 101, 102);

ALTER TABLE complejosempresa ADD identificadorLiderazgo INT;

UPDATE complejosempresa SET identificadorLiderazgo = 200 WHERE lcomplejo_id IN (1, 2, 5);
UPDATE complejosempresa SET identificadorLiderazgo = 201 WHERE lcomplejo_id IN (3, 4, 6, 11, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 96, 97);
UPDATE complejosempresa SET identificadorLiderazgo = 202 WHERE lcomplejo_id IN (7, 8, 9, 10, 52, 53, 54, 57, 60, 86, 92, 99);
UPDATE complejosempresa SET identificadorLiderazgo = 203 WHERE lcomplejo_id IN (13, 37, 40, 41, 42, 43, 47, 50, 61, 64);
UPDATE complejosempresa SET identificadorLiderazgo = 204 WHERE lcomplejo_id IN (16, 19, 21, 26, 38, 51);
UPDATE complejosempresa SET identificadorLiderazgo = 205 WHERE lcomplejo_id IN (17, 20, 25);
UPDATE complejosempresa SET identificadorLiderazgo = 206 WHERE lcomplejo_id IN (14, 15);
UPDATE complejosempresa SET identificadorLiderazgo = 207 WHERE lcomplejo_id IN (18);
UPDATE complejosempresa SET identificadorLiderazgo = 208 WHERE lcomplejo_id IN (23, 31);
UPDATE complejosempresa SET identificadorLiderazgo = 209 WHERE lcomplejo_id IN (22, 58, 59, 62);
UPDATE complejosempresa SET identificadorLiderazgo = 210 WHERE lcomplejo_id IN (27);
UPDATE complejosempresa SET identificadorLiderazgo = 211 WHERE lcomplejo_id IN (29);
UPDATE complejosempresa SET identificadorLiderazgo = 212 WHERE lcomplejo_id IN (28);
UPDATE complejosempresa SET identificadorLiderazgo = 213 WHERE lcomplejo_id IN (30, 32, 35, 36, 39, 33, 5, 44, 48, 55, 65, 76, 77, 78, 79, 80, 82, 83, 84, 87, 88, 89, 90, 91, 93, 94);
UPDATE complejosempresa SET identificadorLiderazgo = 214 WHERE lcomplejo_id IN (31, 81);
UPDATE complejosempresa SET identificadorLiderazgo = 215 WHERE lcomplejo_id IN (23);
UPDATE complejosempresa SET identificadorLiderazgo = 216 WHERE lcomplejo_id IN (46);
UPDATE complejosempresa SET identificadorLiderazgo = 217 WHERE lcomplejo_id IN (34);
UPDATE complejosempresa SET identificadorLiderazgo = 218 WHERE lcomplejo_id IN (85, 95, 100, 101, 102);

ALTER TABLE complejosempresa ADD identificadorTopVendedores INT;

UPDATE complejosempresa SET identificadorTopVendedores = 220 WHERE lcomplejo_id IN (1, 2, 5);
UPDATE complejosempresa SET identificadorTopVendedores = 221 WHERE lcomplejo_id IN (3, 4, 6, 11, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 96, 97);
UPDATE complejosempresa SET identificadorTopVendedores = 222 WHERE lcomplejo_id IN (7, 8, 9, 10, 52, 53, 54, 57, 60, 86, 92, 99);
UPDATE complejosempresa SET identificadorTopVendedores = 223 WHERE lcomplejo_id IN (13, 37, 40, 41, 42, 43, 47, 50, 61, 64);
UPDATE complejosempresa SET identificadorTopVendedores = 224 WHERE lcomplejo_id IN (16, 19, 21, 26, 38, 51);
UPDATE complejosempresa SET identificadorTopVendedores = 225 WHERE lcomplejo_id IN (17, 20, 25);
UPDATE complejosempresa SET identificadorTopVendedores = 226 WHERE lcomplejo_id IN (14, 15);
UPDATE complejosempresa SET identificadorTopVendedores = 227 WHERE lcomplejo_id IN (18);
UPDATE complejosempresa SET identificadorTopVendedores = 228 WHERE lcomplejo_id IN (23, 31);
UPDATE complejosempresa SET identificadorTopVendedores = 229 WHERE lcomplejo_id IN (22, 58, 59, 62);
UPDATE complejosempresa SET identificadorTopVendedores = 230 WHERE lcomplejo_id IN (27);
UPDATE complejosempresa SET identificadorTopVendedores = 231 WHERE lcomplejo_id IN (29);
UPDATE complejosempresa SET identificadorTopVendedores = 232 WHERE lcomplejo_id IN (28);
UPDATE complejosempresa SET identificadorTopVendedores = 233 WHERE lcomplejo_id IN (30, 32, 35, 36, 39, 33, 45, 44, 48, 55, 65, 76, 77, 78, 79, 80, 82, 83, 84, 87, 88, 89, 90, 91, 93, 94);
UPDATE complejosempresa SET identificadorTopVendedores = 234 WHERE lcomplejo_id IN (31, 81);
UPDATE complejosempresa SET identificadorTopVendedores = 235 WHERE lcomplejo_id IN (23);
UPDATE complejosempresa SET identificadorTopVendedores = 236 WHERE lcomplejo_id IN (46);
UPDATE complejosempresa SET identificadorTopVendedores = 237 WHERE lcomplejo_id IN (34);
UPDATE complejosempresa SET identificadorTopVendedores = 238 WHERE lcomplejo_id IN (85, 95, 100, 101, 102);

ALTER TABLE complejosempresa ADD identificadorBonoLiderazgoEmpresaPagar INT;

UPDATE complejosempresa SET identificadorBonoLiderazgoEmpresaPagar = 150 WHERE lcomplejo_id IN (1, 2, 5);
UPDATE complejosempresa SET identificadorBonoLiderazgoEmpresaPagar = 151 WHERE lcomplejo_id IN (3, 4, 6, 11, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 96, 97);
UPDATE complejosempresa SET identificadorBonoLiderazgoEmpresaPagar = 152 WHERE lcomplejo_id IN (7, 8, 9, 10, 52, 53, 54, 57, 60, 86, 92, 99);
UPDATE complejosempresa SET identificadorBonoLiderazgoEmpresaPagar = 153 WHERE lcomplejo_id IN (13, 37, 40, 41, 42, 43, 47, 50, 61, 64);
UPDATE complejosempresa SET identificadorBonoLiderazgoEmpresaPagar = 154 WHERE lcomplejo_id IN (16, 19, 21, 26, 38, 51);
UPDATE complejosempresa SET identificadorBonoLiderazgoEmpresaPagar = 155 WHERE lcomplejo_id IN (17, 20, 25);
UPDATE complejosempresa SET identificadorBonoLiderazgoEmpresaPagar = 156 WHERE lcomplejo_id IN (14, 15);
UPDATE complejosempresa SET identificadorBonoLiderazgoEmpresaPagar = 157 WHERE lcomplejo_id IN (18);
UPDATE complejosempresa SET identificadorBonoLiderazgoEmpresaPagar = 158 WHERE lcomplejo_id IN (23, 31);
UPDATE complejosempresa SET identificadorBonoLiderazgoEmpresaPagar = 159 WHERE lcomplejo_id IN (22, 58, 59, 62);
UPDATE complejosempresa SET identificadorBonoLiderazgoEmpresaPagar = 160 WHERE lcomplejo_id IN (27);
UPDATE complejosempresa SET identificadorBonoLiderazgoEmpresaPagar = 161 WHERE lcomplejo_id IN (29);
UPDATE complejosempresa SET identificadorBonoLiderazgoEmpresaPagar = 162 WHERE lcomplejo_id IN (28);
UPDATE complejosempresa SET identificadorBonoLiderazgoEmpresaPagar = 163 WHERE lcomplejo_id IN (30, 32, 35, 36, 39, 33, 45, 44, 48, 55, 65, 76, 77, 78, 79, 80, 82, 83, 84, 87, 88, 89, 90, 91, 93, 94);
UPDATE complejosempresa SET identificadorBonoLiderazgoEmpresaPagar = 164 WHERE lcomplejo_id IN (31, 81);
UPDATE complejosempresa SET identificadorBonoLiderazgoEmpresaPagar = 165 WHERE lcomplejo_id IN (23);
UPDATE complejosempresa SET identificadorBonoLiderazgoEmpresaPagar = 166 WHERE lcomplejo_id IN (46);
UPDATE complejosempresa SET identificadorBonoLiderazgoEmpresaPagar = 167 WHERE lcomplejo_id IN (34);
UPDATE complejosempresa SET identificadorBonoLiderazgoEmpresaPagar = 168 WHERE lcomplejo_id IN (85, 95, 100, 101, 102);


ALTER TABLE complejosempresa ADD identificadorDescuentoCiclo INT;

UPDATE complejosempresa SET identificadorDescuentoCiclo = 58  WHERE lcomplejo_id IN (1,2,5);
UPDATE complejosempresa SET identificadorDescuentoCiclo = 59  WHERE lcomplejo_id IN (3,4,6,11,66,67,68,69,70,71,72,73,74,75,96,97);
UPDATE complejosempresa SET identificadorDescuentoCiclo = 60  WHERE lcomplejo_id IN (7,8,9,10,52,53,54,57,60,86,92,99);
UPDATE complejosempresa SET identificadorDescuentoCiclo = 61  WHERE lcomplejo_id IN (13,37,40,41,42,43,47,50,61,64);
UPDATE complejosempresa SET identificadorDescuentoCiclo = 62  WHERE lcomplejo_id IN (16,19,21,26,38,51);
UPDATE complejosempresa SET identificadorDescuentoCiclo = 63  WHERE lcomplejo_id IN (17,20,25);
UPDATE complejosempresa SET identificadorDescuentoCiclo = 64  WHERE lcomplejo_id IN (14,15);
UPDATE complejosempresa SET identificadorDescuentoCiclo = 65  WHERE lcomplejo_id IN (18);
UPDATE complejosempresa SET identificadorDescuentoCiclo = 66  WHERE lcomplejo_id IN (23,31);
UPDATE complejosempresa SET identificadorDescuentoCiclo = 67  WHERE lcomplejo_id IN (22,58,59,62);
UPDATE complejosempresa SET identificadorDescuentoCiclo = 68  WHERE lcomplejo_id IN (27);
UPDATE complejosempresa SET identificadorDescuentoCiclo = 69  WHERE lcomplejo_id IN (29);
UPDATE complejosempresa SET identificadorDescuentoCiclo = 70  WHERE lcomplejo_id IN (28);
UPDATE complejosempresa SET identificadorDescuentoCiclo = 71  WHERE lcomplejo_id IN (30,32,35,36,39,33,45,44,48,55,65,76,77,78,79,80,82,83,84,87,88,89,90,91,93,94);
UPDATE complejosempresa SET identificadorDescuentoCiclo = 72  WHERE lcomplejo_id IN (31,81);
UPDATE complejosempresa SET identificadorDescuentoCiclo = 73  WHERE lcomplejo_id IN (23);
UPDATE complejosempresa SET identificadorDescuentoCiclo = 74  WHERE lcomplejo_id IN (46,49);
UPDATE complejosempresa SET identificadorDescuentoCiclo = 75  WHERE lcomplejo_id IN (34);
UPDATE complejosempresa SET identificadorDescuentoCiclo = 76  WHERE lcomplejo_id IN (24);
UPDATE complejosempresa SET identificadorDescuentoCiclo = 99  WHERE lcomplejo_id IN (85,95,100,101,102);


ALTER TABLE complejosempresa ADD identificadorProrrateoEmpresa INT;

UPDATE complejosempresa SET identificadorProrrateoEmpresa = 77  WHERE empresa_id = 1;
UPDATE complejosempresa SET identificadorProrrateoEmpresa = 78  WHERE empresa_id = 2;
UPDATE complejosempresa SET identificadorProrrateoEmpresa = 79  WHERE empresa_id = 3;
UPDATE complejosempresa SET identificadorProrrateoEmpresa = 80  WHERE empresa_id = 4;
UPDATE complejosempresa SET identificadorProrrateoEmpresa = 81  WHERE empresa_id = 5;
UPDATE complejosempresa SET identificadorProrrateoEmpresa = 82  WHERE empresa_id = 6;
UPDATE complejosempresa SET identificadorProrrateoEmpresa = 83  WHERE empresa_id = 7;
UPDATE complejosempresa SET identificadorProrrateoEmpresa = 84  WHERE empresa_id = 8;
UPDATE complejosempresa SET identificadorProrrateoEmpresa = 85  WHERE empresa_id = 9;
UPDATE complejosempresa SET identificadorProrrateoEmpresa = 86  WHERE empresa_id = 10;
UPDATE complejosempresa SET identificadorProrrateoEmpresa = 87  WHERE empresa_id = 11;
UPDATE complejosempresa SET identificadorProrrateoEmpresa = 88  WHERE empresa_id = 12;
UPDATE complejosempresa SET identificadorProrrateoEmpresa = 89  WHERE empresa_id = 13;
UPDATE complejosempresa SET identificadorProrrateoEmpresa = 90  WHERE empresa_id = 14;
UPDATE complejosempresa SET identificadorProrrateoEmpresa = 91  WHERE empresa_id = 15;
UPDATE complejosempresa SET identificadorProrrateoEmpresa = 92  WHERE empresa_id = 16;
UPDATE complejosempresa SET identificadorProrrateoEmpresa = 93  WHERE empresa_id = 17;
UPDATE complejosempresa SET identificadorProrrateoEmpresa = 100 WHERE empresa_id = 20;
UPDATE complejosempresa SET identificadorProrrateoEmpresa = 95  WHERE empresa_id = 63;

 CREATE TABLE administraciontipocomision (
    ltipocomision_id INT PRIMARY KEY,
    snombre VARCHAR(100) NOT NULL,
    fechaadd DATETIME,
    usuarioadd VARCHAR(50),
    fechamod DATETIME,
    usuariomod VARCHAR(50)
) ;
insert into administraciontipocomision VALUES
(1, 'COMISION', NOW(), 'ADMIN', NOW(), 'ADMIN'),
(2, 'SERVICIO', NOW(), 'ADMIN', NOW(), 'ADMIN');


 CREATE TABLE administraciondetallefactura (
    ldetallefactura_id INT PRIMARY KEY,
    ltipocomision_id INT,
    sdetalle VARCHAR(8000) NOT NULL,
    estado int , -- 0: inactivo: 1:activo
    fechaadd DATETIME,
    usuarioadd VARCHAR(50),
    fechamod DATETIME,
    usuariomod VARCHAR(50)
) ;