# Observaciones del proceso de Aplicaciones

Este documento registra consideraciones operativas del proceso de Aplicaciones para facilitar su mantenimiento.

## Empresas nuevas

Cuando se incorpore una empresa nueva en Guardian, se debe crear su mapeo en `BDQISHUR.dbo.AplicacionesEmpresaGuardianAsumeSion` antes de ejecutar el proceso de Aplicaciones.

El proceso usa esta tabla para relacionar el identificador de empresa de Guardian (`lempresa_id`) con el identificador de empresa de Conexión (`IDBD`). Sin ese registro, no podrá sincronizar la comisión por empresa en `BDQISHUR.dbo.AplicacionesComisionPorEmpresa` y el proceso finalizará con error.

## Conciliación de comisiones por empresa

`BDQISHUR.dbo.AplicacionesComisionPorEmpresa` es una instantánea por empresa. Se elimina y se carga desde `tbl_retencionempresa` y `tbl_retencionempresa_exterior` de Guardian; el campo `Neto` recibe el valor de `total_comision` de esas tablas.

No se debe comparar su cantidad de filas directamente con el listado de comisionados: la tabla tiene una fila por documento y empresa, mientras que el listado agrupa por contacto.

Para conciliar importes, las fuentes deben estar calculadas en el mismo momento. El procedimiento que genera `tbl_retencionempresa` incluye también `t_bono_liderazgo`, mientras que la consulta de comisionados usa ventas personales, grupales, residual, `t_ganadores_bonoliderazgo_empresa_pagar`, `bonopar` y `t_top_vendedores`, pero no `t_bono_liderazgo`. Por ello, la suma de `Neto` puede diferir de `TotalAplicar` incluso usando el mismo ciclo.

`t_bono_liderazgo` está excluido de la definición versionada del procedimiento `RetencionEmpresa` para que no se cargue en las retenciones ni en `Neto` durante futuras sincronizaciones.
