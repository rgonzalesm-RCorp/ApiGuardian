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

## Moneda del ciclo (BS / SUS)

Desde el 12/11/2026 el proceso aplica la comisión **en la moneda del ciclo** (`administracionciclo.moneda`) y paga cada cuota **en la moneda de la cuota**. Detalle completo en `PLAN_APLICACIONES_POR_MONEDA_CICLO.md`.

- **Ciclo `SUS`:** misma lógica de siempre. Las cuotas en BOB (empresas de `PagosBolivianos.Empresas`) se convierten con `PagosBolivianos.TipoCambio` (7).
- **Ciclo `BS`:** las cuotas en USD se pagan en USD y descuentan su equivalente en Bs con `cambioDolar.tipoCambioHistorico` (6,97). Las cuotas en Bs se pagan sin conversión. Los descuentos de monto fijo y el saldo mínimo "a cuenta" están en USD y se convierten con `cambioDolar.tipoCambioOficial` (7,00).
- Si `administracionciclo.moneda` está vacía o no es `BS`/`SUS`, el proceso se detiene **antes** de borrar datos.
- La respuesta incluye `MonedaCiclo`, y cada operación incluye `MonedaComision` (moneda de lo descontado), `MonedaPago` (moneda de lo pagado en Sion) y `TipoCambio` (solo cuando hubo conversión).

### Evidencia de prueba (12/11/2026)

Prueba sin base de datos: un programa temporal invocó los métodos reales del repositorio compilado (`DecidirPago`, `MontoMinimoParaPagoACuenta`, `ConvertirMontoFijoUsdAMonedaCiclo`, `AgregarDetalleMoneda`) con la configuración de `src/Api/appsettings.json`.

| Caso | Resultado |
|---|---|
| `BS`, saldo 1.000 Bs, cuota 100 USD | Paga 100 USD, consume 697,00 Bs, completo ✅ |
| `BS`, saldo 348,50 Bs, cuota 100 USD | Paga 50 USD, consume 348,50 Bs, a cuenta ✅ |
| `BS`, cuota 500 Bs de la empresa 32 | Paga 500 BOB, consume 500 Bs, sin conversión ✅ |
| `BS`, saldo mínimo y descuento fijo de 50 USD | 140 Bs y 350 Bs ✅ |
| `SUS`, 20.000 casos al azar comparados con la fórmula anterior | 0 diferencias ✅ |

Observación generada en un pago con conversión (ciclo `BS`): `Pago USD 100,00; consume Bs 697,00; TC 6,97.`

Esta prueba cubre el cálculo. La ejecución completa (vista previa y aplicación de un ciclo `BS`) corresponde a la fase 4 del plan y debe hacerse en un ambiente controlado.

### Reportes y migración de descuentos (fase 3, 12/11/2026)

Los reportes de aplicaciones, prorrateo y pagar comisión ya mostraban la moneda del ciclo en sus encabezados y no cambiaron. Se corrigieron dos textos que asumían dólares:

- **Resumen de descuentos** (`administraciondescuentociclo.sdetalles`): escribe `Bs.` en ciclos `BS` y `$us.` en ciclos `SUS`.
- **Glosa del descuento migrado** (`MigracionAplicacionesProrrateo`): en ciclo `BS`, una cuota en USD agrega `Equivalente USD 100,00 (TC 6.97)` y una cuota en Bs no agrega equivalente. En ciclo `SUS` sigue igual (`Equivalente BOB 696 (TC 6.96)` para las empresas 32 y 33).

La migración se detiene sin insertar si el ciclo no tiene moneda `BS` o `SUS`.

| Prueba (sin escribir en la base) | Resultado |
|---|---|
| Separador del resumen: ciclo 150 (`BS`) y ciclo 149 (`SUS`) | ` Bs. - ` y ` $us. - ` ✅ |
| Resumen recalculado con la consulta nueva frente al guardado, ciclo 148 (`SUS`) | 102 de 102 iguales ✅ |
| Glosa `SUS`: empresa en USD y empresa 32 | Sin cambio respecto a la lógica anterior ✅ |
| Glosa `BS`: 697 Bs de una cuota en USD; 500 Bs de la empresa 33 | `Equivalente USD 100,00 (TC 6.97)`; sin equivalente ✅ |
| Glosa `BS`: 20.000 montos USD → Bs → USD | 0 diferencias ✅ |
