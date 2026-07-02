alter table administracioncontrato add porcentaje_inicial DECIMAL(10,4);



insert into administraciontipocontrato values ('admin', now(), 'admin', now(), 6, 'UPGRADE', 'UG');
insert into administraciontipocontrato values ('admin', now(), 'admin', now(), 7, 'RECUPERACION', 'RE');
insert into administraciontipocontrato values ('admin', now(), 'admin', now(), 8, 'RECOMPRA', 'RC');
insert into administraciontipocontrato values ('admin', now(), 'admin', now(), 9, 'CASOSESPECIALES', 'CE');

ALTER table tmp_residual_contacto add lpatrocinante_id int