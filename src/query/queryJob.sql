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
    , inicial_desde decimal(18,2)
    , inicial_hasta decimal(18,2)
    , comision decimal(18,2)
    , estado INT
    , fechaadd DATETIME
    , usuarioadd VARCHAR(50)
    , fechamod DATETIME
    , usuariomod VARCHAR(50)
);



CREATE TABLE VentaRezagadasCiclo (
    VentaRezagadasCicloId INT AUTO_INCREMENT PRIMARY KEY,

    empresaId INT NOT NULL,
    lContratoId INT NOT NULL,

    dFecha DATETIME NOT NULL,

    sManzano VARCHAR(200),
    sLote VARCHAR(200),

    dPrecio DECIMAL(18,2) NOT NULL,

    lComplejoId INT NOT NULL,

    idVenta BIGINT NOT NULL,
    lote VARCHAR(500) NOT NULL,

    suv VARCHAR(50),

    precioInicial DECIMAL(18,2),
    sCuotaInicial DECIMAL(18,2),

    idCliente BIGINT NOT NULL,
    telefonoFijo VARCHAR(300),
    telefonoMovil VARCHAR(300),
    correo VARCHAR(1500),
    fechaNacimiento DATETIME,
    direccion VARCHAR(1500),
    idPaisResidencia INT,
    sCedulaIdentidad VARCHAR(300),
    sCiudad VARCHAR(500),
    fechaRegistro DATETIME,
    sNombreCompleto VARCHAR(1500),
    sTelefonoOficina VARCHAR(300),
    sContrasena VARCHAR(500),

    vendedorId BIGINT,
    telefonoFijoVendedor VARCHAR(300),
    telefonoMovilVendedor VARCHAR(300),
    correoVendedor VARCHAR(1500),
    fechaNacimientoVendedor DATETIME,
    direccionVendedor VARCHAR(2000),
    idPaisResidenciaVendedor INT,
    sCedulaIdentidadVendedor VARCHAR(300),
    fechaRegistroVendedor DATETIME,
    sNombreCompletoVendedor VARCHAR(1500),
    sTelefonoOficinaVendedor VARCHAR(300),
    sContrasenaVendedor VARCHAR(500),
    sCiudadVendedor VARCHAR(500),

    complejo VARCHAR(1500),
    tipoVenta INT,
    porcentajeCuotaInicial DECIMAL(5,2),

    EstadoVentaRezagadasCicloId INT,
    
    FechaRegistroGrd DATETIME,
    FechaProceso DATETIME
    
);

create TABLE EstadoVentaRezagadasCiclo(
    EstadoVentaRezagadasCicloId INT AUTO_INCREMENT PRIMARY KEY,
    Descripcion VARCHAR(1500),
    Estado int DEFAULT 1
);

INSERT INTO EstadoVentaRezagadasCiclo (EstadoVentaRezagadasCicloId, Descripcion, Estado)
VALUES
(0, 'PENDIENTE', 1);
INSERT INTO EstadoVentaRezagadasCiclo (EstadoVentaRezagadasCicloId, Descripcion, Estado)
VALUES
(0, 'PROCESADO', 1);
INSERT INTO EstadoVentaRezagadasCiclo (EstadoVentaRezagadasCicloId, Descripcion, Estado)
VALUES
(0, 'ANULADO', 1);



CREATE TABLE ControlProceso(
    controlProcesoId INT AUTO_INCREMENT PRIMARY KEY
    , lciclo_id INT
    , paso varchar(150)
    , inicio datetime
    , fin datetime
    , estado INT
    , fechaadd DATETIME
    , usuarioadd VARCHAR(50)
    , fechamod DATETIME
    , usuariomod VARCHAR(50)
);





CREATE TABLE T_ACCIONESCUOTASGRL(
ID INT AUTO_INCREMENT PRIMARY KEY,
IDPRODUCTO nvarchar(50) NULL,
IDPROYECTO int NULL,
PROYECTO nvarchar(60) NULL,
idrecibo int NULL,
IDVENTA int NULL,
IDTIPOPAGO int NULL,
DESCRIPCION nvarchar(60) NULL,
IDCLIENTE int NULL,
CLIENTE nvarchar(80) NULL,
DOCIDCLI nvarchar(50) NULL,
IDVENDEDOR int NULL,
VENDEDOR nvarchar(80) NULL,
DOCIDVEN nvarchar(50) NULL,
BONO decimal(10, 2) NULL,
AMORTIZACION decimal(10, 2) NULL,
CAPITAL decimal(10, 2) NULL,
INTERES decimal(10, 2) NULL,
SEGURO decimal(10, 2) NULL,
EXPENSA decimal(10, 2) NULL,
MULTA decimal(10, 2) NULL,
FECHA_VENTA datetime NULL,
FECHA_PAGO datetime NULL,
ACUENTA decimal(10, 2) NULL,
TOTALPAGO decimal(10, 2) NULL,
MONTODEUDA decimal(10, 2) NULL,
PAGOSACUENTA decimal(10, 2) NULL,
NROCUOTA int NULL,
FECHAINS datetime NULL);


CREATE TABLE Cartera (
ID int AUTO_INCREMENT PRIMARY KEY,
EMPRESA varchar(255) NULL,
LOTE varchar(255) NULL,
DOCID varchar(255) NULL,
CLIENTE varchar(255) NULL,
DOCID_VENDEDOR varchar(255) NULL,
NOMBRE varchar(255) NULL,
IDTIPOVENTA float NULL,
IDPROYECTO float NULL,
IDVENTA float NULL,
CUOTAINICIAL numeric(18, 2) NULL,
TOTALVENTA float NULL,
TOTALDEUDA float NULL,
FECHA datetime NULL,
PROYECTO varchar(255) NULL,
CUOTAS_LOTES_VENCIDAS float NULL,
ULTIMO_PAGO datetime NULL,
ESTADO varchar(255) NULL,
TRANS float NULL,
NIT varchar(255) NULL,
TEL_CEL varchar(255) NULL,
TELEFONO varchar(255) NULL,
DIRECCION varchar(255) NULL,
EMAIL varchar(255) NULL,
UV varchar(255) NULL,
MZNO varchar(255) NULL,
NRO_LOTE varchar(255) NULL,
PRECIO_LISTA float NULL,
CIUDAD_RESIDENCIA varchar(255) NULL,
MONTO_CAPITAL_VENC float NULL,
MONTO_INTERES_VENC float NULL,
MONTO_MULTA float NULL,
MONTO_EXPENSA float NULL,
F_VENC_MAS_ANT datetime NULL,
F_ULTIMO_VENC datetime NULL);




create table br_tipoproducto(
    brtipoproducto_id int AUTO_INCREMENT PRIMARY KEY
    , descripcion varchar(500)
    , estado INT
    , fechaadd DATETIME
    , usuarioadd VARCHAR(50)
    , fechamod DATETIME
    , usuariomod VARCHAR(50)
);

create table br_niveles(
    brniveles_id int AUTO_INCREMENT PRIMARY KEY
    , nivel INT
    , descripcion varchar(500)
    , estado INT
    , fechaadd DATETIME
    , usuarioadd VARCHAR(50)
    , fechamod DATETIME
    , usuariomod VARCHAR(50)
);

 CREATE TABLE br_configuracion(
    brconfiguracion_id int AUTO_INCREMENT PRIMARY KEY
    , lciclo_id int
    , brtipoproducto_id INT
    , estado INT
    , fechaadd DATETIME
    , usuarioadd VARCHAR(50)
    , fechamod DATETIME
    , usuariomod VARCHAR(50)
);

CREATE TABLE br_configuracionDetalle(
    brconfiguraciondetalle_id int AUTO_INCREMENT PRIMARY KEY
    , brconfiguracion_id int
    , brniveles_id int
    , porcentaje decimal(18,2)
    , estado INT
    , fechaadd DATETIME
    , usuarioadd VARCHAR(50)
    , fechamod DATETIME
    , usuariomod VARCHAR(50)
);

insert into br_tipoproducto values (0, 'TERRENO', 1, NOW(), '', NOW(), '');
insert into br_tipoproducto values (0, 'MEMBRESIA', 1, NOW(), '', NOW(), '');

insert into br_niveles values (0, 1, 'NIVEL 1', 1, NOW(), '', NOW(), '');
insert into br_niveles values (0, 2, 'NIVEL 2', 1, NOW(), '', NOW(), '');
insert into br_niveles values (0, 3, 'NIVEL 3', 1, NOW(), '', NOW(), '');
insert into br_niveles values (0, 4, 'NIVEL 4', 1, NOW(), '', NOW(), '');
insert into br_niveles values (0, 5, 'NIVEL 5', 1, NOW(), '', NOW(), '');
insert into br_niveles values (0, 6, 'NIVEL 6', 1, NOW(), '', NOW(), '');
insert into br_niveles values (0, 7, 'NIVEL 7', 1, NOW(), '', NOW(), '');