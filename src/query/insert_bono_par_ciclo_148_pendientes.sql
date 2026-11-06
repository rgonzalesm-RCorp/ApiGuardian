/*
  Bono Par pendiente — ciclo 148 (septiembre de 2026).
  Generado a partir de las ventas calificables del ciclo.
  Ejecutar cada bloque por separado y reemplazar @usuario por el usuario ejecutor.
*/
SET @usuario := 'REEMPLAZAR_POR_USUARIO_EJECUTOR';

/* 1. contactoGanadorId: 79039 — HUARAHUARA BOTELLO VICTOR HUGO */
START TRANSACTION;
INSERT INTO bonopar (
    l_contacto_ganador_id, s_nombre_ganador, s_cedula_identidad_ganador,
    persona_que_vendieron, bono, cantidad_venta, vendedores_id, l_contrato_id,
    s_nro_venta, monto_ventas, cuotas_iniciales, estado, usuario_creacion,
    fecha_creacion, usuario_modificacion, fecha_modificacion, lciclo_id
) VALUES (
    79039, 'HUARAHUARA BOTELLO VICTOR HUGO', '9912044',
    4, 700.00, 6, '106251,108104,110611,110816',
    '143750,143782,143787,143823,143843,143863',
    '203-KTBS-10-35,236-KTBS-10-65,241-KTBS-10-18,25308-TANO-M45-L18,25303-UACN3-M55-L37,30-ULU-M9-L14',
    41335.22, 2444.31, 1, @usuario, NOW(), @usuario, NOW(), 148
);
SET @bonopar_id := LAST_INSERT_ID();
INSERT INTO bonopardetalle (
    bonopar_id, l_contacto_ganador_id, l_contacto_vendedor_id, s_nombre_vendedor,
    s_cedula_identidad_vendedor, l_contacto_cliente_id, s_nombre_cliente, s_cedula_cliente,
    l_contrato_id, dt_fecha, s_nro_venta, d_precio, d_cuota_inicial, estado,
    usuario_creacion, fecha_creacion, usuario_modificacion, fecha_modificacion
) VALUES
    (@bonopar_id,79039,106251,'LLANOS GUTIERREZ MIGUEL AUGUSTO','6753801',114890,'GIRON CHURQUINA AYDA YULISA','12495316',143750,'2026-09-26','203-KTBS-10-35',4268.29,213.41,1,@usuario,NOW(),@usuario,NOW()),
    (@bonopar_id,79039,110816,'MUÑOZ LLANOS SOFIA ANETTE','12927489',114939,'GUTIERREZ CESPEDES JAQUELINE MARIA','2634213',143782,'2026-09-30','236-KTBS-10-65',5021.52,251.08,1,@usuario,NOW(),@usuario,NOW()),
    (@bonopar_id,79039,106251,'LLANOS GUTIERREZ MIGUEL AUGUSTO','6753801',114944,'PAUCARA CONDORI LUIS','13631889',143787,'2026-09-30','241-KTBS-10-18',4268.29,213.41,1,@usuario,NOW(),@usuario,NOW()),
    (@bonopar_id,79039,108104,'VELASCO VEGA PAOLA GIOVANNA','4811528',114842,'CARRANZA FLORES WILMER PEDRO','6036544',143823,'2026-09-18','25308-TANO-M45-L18',7526.00,753.00,1,@usuario,NOW(),@usuario,NOW()),
    (@bonopar_id,79039,108104,'VELASCO VEGA PAOLA GIOVANNA','4811528',114852,'MANRIQUE VILLAGOMEZ SAMUEL ANGELO','4745098',143843,'2026-09-12','25303-UACN3-M55-L37',6223.00,312.00,1,@usuario,NOW(),@usuario,NOW()),
    (@bonopar_id,79039,110611,'VALENCIA CONDE HELEEN SANDI','6050206',114904,'YAPUCHURA ZARATE ALEXANDER OSCAR','11544892',143863,'2026-09-16','30-ULU-M9-L14',14028.12,701.41,1,@usuario,NOW(),@usuario,NOW());
COMMIT;

/* 2. contactoGanadorId: 113134 — JALDIN CABRERA PEGGY JANE */
START TRANSACTION;
INSERT INTO bonopar (
    l_contacto_ganador_id, s_nombre_ganador, s_cedula_identidad_ganador,
    persona_que_vendieron, bono, cantidad_venta, vendedores_id, l_contrato_id,
    s_nro_venta, monto_ventas, cuotas_iniciales, estado, usuario_creacion,
    fecha_creacion, usuario_modificacion, fecha_modificacion, lciclo_id
) VALUES (
    113134, 'JALDIN CABRERA PEGGY JANE', '4836736', 2, 200.00, 2, '114073,114159',
    '143829,143877', '25325-TANO-M43-L14,44-ULU-M50-L10',
    29394.17, 1470.61, 1, @usuario, NOW(), @usuario, NOW(), 148
);
SET @bonopar_id := LAST_INSERT_ID();
INSERT INTO bonopardetalle (
    bonopar_id, l_contacto_ganador_id, l_contacto_vendedor_id, s_nombre_vendedor,
    s_cedula_identidad_vendedor, l_contacto_cliente_id, s_nombre_cliente, s_cedula_cliente,
    l_contrato_id, dt_fecha, s_nro_venta, d_precio, d_cuota_inicial, estado,
    usuario_creacion, fecha_creacion, usuario_modificacion, fecha_modificacion
) VALUES
    (@bonopar_id,113134,114073,'MORALES RIOS PATRICIA','3484071',114959,'LOPEZ STRAUSS HEIDY ELIZABETH','3448703',143829,'2026-09-30','25325-TANO-M43-L14',10022.00,502.00,1,@usuario,NOW(),@usuario,NOW()),
    (@bonopar_id,113134,114159,'BONSAK ARCE ALIONKA ROSA','4799962',114983,'CONDORI CALANI JOEL','4374989',143877,'2026-09-30','44-ULU-M50-L10',19372.17,968.61,1,@usuario,NOW(),@usuario,NOW());
COMMIT;

/* 3. contactoGanadorId: 99599 — MAMANI CUBA MARCO ANTONIO */
START TRANSACTION;
INSERT INTO bonopar (
    l_contacto_ganador_id, s_nombre_ganador, s_cedula_identidad_ganador,
    persona_que_vendieron, bono, cantidad_venta, vendedores_id, l_contrato_id,
    s_nro_venta, monto_ventas, cuotas_iniciales, estado, usuario_creacion,
    fecha_creacion, usuario_modificacion, fecha_modificacion, lciclo_id
) VALUES (
    99599, 'MAMANI CUBA MARCO ANTONIO', '10068461', 3, 200.00, 3, '102157,113097,114722',
    '143818,143862,143802', '25286-TANO-M8-L32,29-ULU-M34-L11,10019395-R-NRDE-M92-L6',
    36679.18, 3053.72, 1, @usuario, NOW(), @usuario, NOW(), 148
);
SET @bonopar_id := LAST_INSERT_ID();
INSERT INTO bonopardetalle (
    bonopar_id, l_contacto_ganador_id, l_contacto_vendedor_id, s_nombre_vendedor,
    s_cedula_identidad_vendedor, l_contacto_cliente_id, s_nombre_cliente, s_cedula_cliente,
    l_contrato_id, dt_fecha, s_nro_venta, d_precio, d_cuota_inicial, estado,
    usuario_creacion, fecha_creacion, usuario_modificacion, fecha_modificacion
) VALUES
    (@bonopar_id,99599,114722,'ARIAS ARROYO WILDER ANDRÉS','4963479',114829,'QUISPE RAMOS ROLANDO','4845515',143802,'2026-09-25','10019395-R-NRDE-M92-L6',9502.00,950.20,1,@usuario,NOW(),@usuario,NOW()),
    (@bonopar_id,99599,102157,'FERNÁNDEZ SOLIZ JAVIER','7923080',114837,'ROSALES RUIZ JUAN JOSE','13227776',143818,'2026-09-02','25286-TANO-M8-L32',12302.00,616.00,1,@usuario,NOW(),@usuario,NOW()),
    (@bonopar_id,99599,113097,'NINA VERA GLADYS','7450741',114903,'TOLA QUISPE GLADYS ROSMERY','6060817',143862,'2026-09-15','29-ULU-M34-L11',14875.18,1487.52,1,@usuario,NOW(),@usuario,NOW());
COMMIT;

/* 4. contactoGanadorId: 110989 — SEA VILLALBA JAEL NATHALIE */
START TRANSACTION;
INSERT INTO bonopar (
    l_contacto_ganador_id, s_nombre_ganador, s_cedula_identidad_ganador,
    persona_que_vendieron, bono, cantidad_venta, vendedores_id, l_contrato_id,
    s_nro_venta, monto_ventas, cuotas_iniciales, estado, usuario_creacion,
    fecha_creacion, usuario_modificacion, fecha_modificacion, lciclo_id
) VALUES (
    110989, 'SEA VILLALBA JAEL NATHALIE', '10003662', 2, 200.00, 2, '112587,114992',
    '143749,143860', '202-KTBS-10-21,27-ULU-M39-L18',
    23138.45, 1156.92, 1, @usuario, NOW(), @usuario, NOW(), 148
);
SET @bonopar_id := LAST_INSERT_ID();
INSERT INTO bonopardetalle (
    bonopar_id, l_contacto_ganador_id, l_contacto_vendedor_id, s_nombre_vendedor,
    s_cedula_identidad_vendedor, l_contacto_cliente_id, s_nombre_cliente, s_cedula_cliente,
    l_contrato_id, dt_fecha, s_nro_venta, d_precio, d_cuota_inicial, estado,
    usuario_creacion, fecha_creacion, usuario_modificacion, fecha_modificacion
) VALUES
    (@bonopar_id,110989,112587,'ESCOBAR PAZ ALBERTO','8320911',114986,'PILLCO NILDA SANTUSA','3488748',143749,'2026-09-24','202-KTBS-10-21',4268.29,213.41,1,@usuario,NOW(),@usuario,NOW()),
    (@bonopar_id,110989,114992,'UNTIVEROS ROBLES BELKY MERCEDES','5891121',114901,'ZERDA SANTIVAÑEZ BRAYAN ALVARO','7956285',143860,'2026-09-09','27-ULU-M39-L18',18870.16,943.51,1,@usuario,NOW(),@usuario,NOW());
COMMIT;

/* 5. contactoGanadorId: 18504 — URIONA AYLLON DE TAPIA AGRELIN */
START TRANSACTION;
INSERT INTO bonopar (
    l_contacto_ganador_id, s_nombre_ganador, s_cedula_identidad_ganador,
    persona_que_vendieron, bono, cantidad_venta, vendedores_id, l_contrato_id,
    s_nro_venta, monto_ventas, cuotas_iniciales, estado, usuario_creacion,
    fecha_creacion, usuario_modificacion, fecha_modificacion, lciclo_id
) VALUES (
    18504, 'URIONA AYLLON DE TAPIA AGRELIN', '1094419', 2, 200.00, 3, '113991,114040',
    '143812,143864,143875', '123602-MDE-M15-L16,31-ULU-M46-L2,42-ULU-M50-L6',
    38612.62, 3060.61, 1, @usuario, NOW(), @usuario, NOW(), 148
);
SET @bonopar_id := LAST_INSERT_ID();
INSERT INTO bonopardetalle (
    bonopar_id, l_contacto_ganador_id, l_contacto_vendedor_id, s_nombre_vendedor,
    s_cedula_identidad_vendedor, l_contacto_cliente_id, s_nombre_cliente, s_cedula_cliente,
    l_contrato_id, dt_fecha, s_nro_venta, d_precio, d_cuota_inicial, estado,
    usuario_creacion, fecha_creacion, usuario_modificacion, fecha_modificacion
) VALUES
    (@bonopar_id,18504,114040,'VIRACA BARCAYA DAVID VIRGILIO','5197813',114953,'VIRACA BARCAYA BRAULIO','6623939',143812,'2026-09-30','123602-MDE-M15-L16',7602.00,761.00,1,@usuario,NOW(),@usuario,NOW()),
    (@bonopar_id,18504,114040,'VIRACA BARCAYA DAVID VIRGILIO','5197813',114905,'LEÓN ESPADA PAMELA','10352784',143864,'2026-09-16','31-ULU-M46-L2',16032.14,801.61,1,@usuario,NOW(),@usuario,NOW()),
    (@bonopar_id,18504,113991,'MENDOZA LOPEZ LIDIA','4033302',114916,'MIRANDA FLORES ROLY','5996509',143875,'2026-09-25','42-ULU-M50-L6',14978.48,1498.00,1,@usuario,NOW(),@usuario,NOW());
COMMIT;
