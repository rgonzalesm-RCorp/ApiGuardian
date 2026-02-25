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