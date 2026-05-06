CREATE TABLE bonopar (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,

    l_contacto_ganador_id INT NOT NULL,
    s_nombre_ganador VARCHAR(250) NOT NULL DEFAULT '',
    s_cedula_identidad_ganador VARCHAR(50) NOT NULL DEFAULT '',

    persona_que_vendieron INT NOT NULL DEFAULT 0,
    bono DECIMAL(18,2) NOT NULL DEFAULT 0,
    cantidad_venta INT NOT NULL DEFAULT 0,

    vendedores_id TEXT NOT NULL,
    l_contrato_id TEXT NOT NULL,
    s_nro_venta TEXT NOT NULL,

    monto_ventas DECIMAL(18,2) NOT NULL DEFAULT 0,
    cuotas_iniciales DECIMAL(18,2) NOT NULL DEFAULT 0,

    estado TINYINT NOT NULL DEFAULT 1,
    usuario_creacion VARCHAR(100) NULL,

    fecha_creacion TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    usuario_modificacion VARCHAR(100) NULL,
    fecha_modificacion DATETIME NULL
);

CREATE TABLE bonopardetalle (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,

    bonopar_id BIGINT NOT NULL,

    l_contacto_ganador_id INT NOT NULL,
    l_contacto_vendedor_id INT NOT NULL,
    s_nombre_vendedor VARCHAR(250) NOT NULL DEFAULT '',
    s_cedula_identidad_vendedor VARCHAR(50) NOT NULL DEFAULT '',

    l_contacto_cliente_id INT NOT NULL,
    s_nombre_cliente VARCHAR(250) NOT NULL DEFAULT '',
    s_cedula_cliente VARCHAR(50) NOT NULL DEFAULT '',

    l_contrato_id INT NOT NULL,
    dt_fecha DATETIME NOT NULL,

    s_nro_venta VARCHAR(100) NOT NULL DEFAULT '',
    d_precio DECIMAL(18,2) NOT NULL DEFAULT 0,
    d_cuota_inicial DECIMAL(18,2) NOT NULL DEFAULT 0,

    estado TINYINT NOT NULL DEFAULT 1,
    usuario_creacion VARCHAR(100) NULL,

    fecha_creacion TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    usuario_modificacion VARCHAR(100) NULL,
    fecha_modificacion DATETIME NULL
);