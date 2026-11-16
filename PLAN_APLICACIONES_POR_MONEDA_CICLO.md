# Plan: Aplicaciones según la moneda del ciclo

## Objetivo

Que el proceso de Aplicaciones (`/api/aplicaciones`) aplique la comisión del ciclo **en la moneda del ciclo** (`administracionciclo.moneda` = `SUS` o `BS`) y pague cada cuota **en la moneda de la cuota**, convirtiendo solo cuando las dos monedas son distintas.

- **Hoy:** la comisión siempre se trata como **USD**. Si la cuota es de una empresa en bolivianos, se convierte el saldo USD a Bs para pagarla.
- **Lo que se necesita:** en un ciclo `BS` la comisión está en **Bs**. Las cuotas en **USD** se pagan convirtiendo Bs → USD, y las cuotas en Bs se pagan sin conversión.

---

## 1. Situación actual

### 1.1 Configuración (`appsettings.json` → `Aplicaciones`)

| Clave | Valor actual | Uso |
|---|---|---|
| `PagosBolivianos.Habilitado` | `true` | Activa la conversión para empresas con cuotas en BOB |
| `PagosBolivianos.TipoCambio` | `7` | Tipo de cambio de la conversión |
| `PagosBolivianos.Empresas` | `32, 33, 34, 35, 36` | Empresas cuyas cuotas están en **BOB**. Todas las demás están en **USD** |
| `MontoMinimoParaPagoACuenta` | `20` | Saldo máximo que se aplica "a cuenta" de la siguiente cuota (implícitamente USD) |

### 1.2 Lógica actual (`AplicacionesRepositorio.cs`)

`DecidirPago(cuota, montoDisponible, ahora, esPagoEnBolivianos)`:

| Cuota | Saldo consumido (USD) | Monto que se paga en Sion |
|---|---|---|
| USD | `min(cuota, saldo)` | lo mismo, en USD |
| BOB (empresas 32 a 36) | `min(cuota / TC, saldo)` | completo: la cuota en BOB tal cual; a cuenta: `saldo × TC` en BOB |

Otros puntos que suponen USD:
- `ConvertirMontoCuotaAUsd(...)` lo usan `DecidirPago` y `DebeOmitirCartaYaRegistrada` (cartas).
- `AgregarDetalleMoneda`: la observación dice "Pago BOB x; consume USD y; TC z".
- `MonedaPago` de la operación vale "BOB" o "USD".
- `MontoMinimoParaPagoACuenta` = 20 se compara contra el saldo, como si fuera USD.
- Los descuentos de monto fijo (`DBITSIS.dbo.aplicaciones_descuentos_freelancer.monto_porcentaje`) se restan del saldo sin conversión.
- **Ninguna parte del proceso lee `administracionciclo.moneda`.**

### 1.3 Fuentes de la comisión

- `TotalAplicar` (en `AplicacionesComisionado`) y `Neto` (en `AplicacionesComisionPorEmpresa`, usado en el prorrateo) salen de `tbl_retencionempresa` y `tbl_retencionempresa_exterior`.
- Esas tablas se generan a partir de los cálculos de comisión del ciclo, que ya están en la **moneda del ciclo** (ver `PLAN_TIPO_CAMBIO_POR_MONEDA_CICLO.md`).
- Por eso, en un ciclo `BS` el saldo **ya llega en Bs**. Hay que verificarlo con datos en la fase 1.

---

## 2. Regla nueva

**Regla confirmada por el usuario:**
- **Ciclo `BS`:**
  - si la cuota está en **USD**, se paga en USD y de la comisión se descuenta su **equivalente en Bs** (× TC histórico 6,97);
  - si la cuota está en **Bs**, se paga en Bs y se descuentan **esos mismos Bs** de la comisión.
- **Ciclo `SUS`:** se aplica **la lógica actual** sin cambios.


**Moneda de la comisión** = moneda del ciclo. **Moneda de la cuota** = BOB si la empresa está en `PagosBolivianos.Empresas`; USD si no.

`factor` = cuánto de la moneda de la comisión equivale a 1 unidad de la moneda de la cuota:

| Ciclo (comisión) | Cuota | `factor` | Saldo consumido (moneda del ciclo) | Monto que se paga en Sion (moneda de la cuota) |
|---|---|---|---|---|
| `SUS` | USD | 1 | `min(cuota, saldo)` | igual |
| `SUS` | BOB | `1 / TC` | `min(cuota / TC, saldo)` | completo: cuota BOB; a cuenta: `saldo × TC` (**igual que hoy**) |
| `BS` | BOB | 1 | `min(cuota, saldo)` | igual (**sin conversión**) |
| `BS` | USD | `TC` | `min(cuota × TC, saldo)` | completo: cuota USD; a cuenta: `saldo / TC` en USD |

Cálculo general (reemplaza el actual):

```
cuotaEnMonedaComision = Redondear(cuota.MontoPago × factor, 2)
montoPagar            = min(cuotaEnMonedaComision, saldo)          // lo que se descuenta de la comisión
pagoCompleto          = saldo >= cuotaEnMonedaComision
montoEjecutar         = pagoCompleto ? cuota.MontoPago               // la cuota exacta, sin diferencias de redondeo
                                     : Redondear(montoPagar / factor, 2)
```

Redondeo: 2 decimales, `MidpointRounding.AwayFromZero`, igual que hoy.

### Ejemplos (ciclo `BS`, TC histórico = 6,97)

| Saldo comisión | Cuota | Resultado |
|---|---|---|
| 1.000,00 Bs | 100,00 USD | Completo: paga 100,00 USD y consume 697,00 Bs. Saldo: 303,00 Bs |
| 348,50 Bs | 100,00 USD | A cuenta: paga 50,00 USD y consume 348,50 Bs. Saldo: 0 |
| 1.000,00 Bs | 500,00 Bs (empresa 32) | Completo: paga 500,00 Bs y consume 500,00 Bs. Saldo: 500,00 Bs |

En un ciclo `SUS` el resultado tiene que ser **idéntico** al actual.

---

## 3. Cambios

### 3.1 Backend (`src/Infrastructure/Repositories/Aplicaciones/`)

| # | Archivo / método | Cambio |
|---|---|---|
| 1 | `EjecutarProcesoAsync` | Al inicio, **antes de limpiar datos**, leer `administracionciclo.moneda` (Guardian) y validarla con `MonedaCicloHelper.TryParse`. Si no es válida, detener con error grave. Guardar la moneda en `EstadoProcesoAplicaciones.MonedaCiclo` |
| 2 | `ReprocesarGrupoSion` | Leer y validar la moneda del ciclo de la misma forma |
| 3 | `DecidirPago` | Usar la regla de la sección 2: el parámetro `bool esPagoEnBolivianos` se reemplaza por `monedaComision` y `monedaCuota`, y se calcula `factor` |
| 4 | `ConvertirMontoCuotaAUsd` | Reemplazar por `ConvertirCuotaAMonedaComision(empresaId, monto, monedaCiclo)`. Usarlo en `DecidirPago` y en `DebeOmitirCartaYaRegistrada` (cartas) |
| 5 | `EsPagoEnBolivianos` | Pasa a ser `MonedaCuota(empresaId)` → `BOB`/`USD`. Si `PagosBolivianos.Habilitado = false`, se toma USD como hoy |
| 6 | `AgregarDetalleMoneda` | Agregar el detalle solo cuando hay conversión, con las dos monedas: "Pago USD 100,00; consume Bs 697,00; TC 6,97." o "Pago BOB …; consume USD …; TC …" |
| 7 | `DecisionPagoAplicaciones` / `OperacionAplicacion` | Agregar `MonedaComision`. `MonedaPago` sigue indicando la moneda de la cuota. `TipoCambio` se informa siempre que hay conversión |
| 8 | Saldo mínimo "a cuenta" | `MontoMinimoParaPagoACuenta` (20) está en USD. En un ciclo `BS` se compara contra `20 × tipoCambioOficial` = 140,00 Bs |
| 9 | Descuentos de monto fijo | En ciclos `BS`: `monto = Redondear(montoUsd × tipoCambioOficial, 2)`. Si el saldo no alcanza, se descuenta lo que queda y se marca "A cuenta", como hoy. En ciclos `SUS`, sin cambios. Los descuentos por porcentaje no cambian (se calculan sobre `TotalAplicar`, que ya está en la moneda del ciclo) |
| 10 | Prorrateo | Sin cambios de cálculo: `Neto` y los montos aplicados están en la misma moneda (la del ciclo). Se valida en las pruebas |
| 11 | `RespuestaVistaPreviaAplicaciones` / `RespuestaEjecucionAplicaciones` | Agregar `MonedaCiclo` para que el consumidor sepa en qué moneda están `TotalAplicar`, los saldos y los montos |

### 3.2 Configuración

| Clave | Cambio |
|---|---|
| `cambioDolar.tipoCambioHistorico` (6,97) | Tipo de cambio para convertir las cuotas USD en ciclos `BS` |
| `cambioDolar.tipoCambioOficial` (7,00) | Tipo de cambio para convertir los **descuentos de monto fijo** y el **saldo mínimo "a cuenta"** en ciclos `BS` |
| `Aplicaciones.PagosBolivianos.TipoCambio` (7) | Sigue usándose en ciclos `SUS` para cuotas BOB (según D1-b) |
| `Aplicaciones.PagosBolivianos.Empresas` | Sin cambios: identifica las empresas con cuotas en BOB |

### 3.3 Base de datos

- **No se crean tablas ni columnas.** `AplicacionesPagos.Monto` y `AplicacionesProrrateo.Monto` guardan lo consumido de la comisión, que ahora queda en la moneda del ciclo. La moneda se deduce del ciclo.
- Si se necesita auditar el monto pagado en la moneda de la cuota, sigue quedando en la observación (punto 6).

### 3.4 Reportes

- `/api/Reportes/aplicaciones` y `/api/Reportes/prorrateo`: confirmar que muestran los montos con la moneda del ciclo (`ReportesService` ya obtiene la moneda en los demás reportes). Ajustar los encabezados si dicen USD fijo.

---

## 4. Fases

| Fase | Contenido | Estado |
|---|---|---|
| 0 | Confirmar decisiones | ✅ Terminado |
| 1 | Verificar con datos que `tbl_retencionempresa` y `AplicacionesComisionPorEmpresa.Neto` de un ciclo `BS` estén en Bs (solo lectura) | ✅ Terminado: confirmado por el usuario (11/11/2026) |
| 2 | Backend: moneda del ciclo, `DecidirPago` generalizado, cartas, descuentos, saldo mínimo, respuestas | ✅ Terminado (12/11/2026): `dotnet build` sin errores; `DecidirPago` y conversiones probados sin base de datos (ejemplos del plan + 20.000 casos `SUS` idénticos a la lógica anterior) |
| 3 | Reportes de aplicaciones y prorrateo con la moneda del ciclo | ✅ Terminado (12/11/2026): los reportes ya usaban la moneda del ciclo; se corrigieron el resumen de descuentos (`$us.` fijo) y la glosa de la migración de descuentos; `dotnet build` sin errores y pruebas sin escribir en la base |
| 4 | Pruebas | ⏳ Pendiente |

### Fase 1: verificación de las fuentes (solo lectura)
- ✅ El usuario confirmó que en ciclos `BS` la comisión ya llega en **Bs** a `tbl_retencionempresa`. Como `AplicacionesComisionPorEmpresa.Neto` se carga desde esa tabla, el saldo y el prorrateo quedan en Bs.
- Comparar, para el ciclo 150 (`BS`), el `total_comision` de `tbl_retencionempresa` con la suma de venta personal, grupo, residual y bono par del ciclo en Guardian, que ya está en Bs.
- **No usar la vista previa para esto:** borra `AplicacionesPagos`, `AplicacionesProrrateo` y `AplicacionesComisionado` del ciclo en BDQISHUR.

### Fase 2: backend
- Implementar los puntos 1 a 11 de la sección 3.1.
- `dotnet build` sin errores.

### Fase 3: reportes
- Revisar `ReportesRepository` y `ReportesService` en los reportes de aplicaciones y prorrateo.

Resultado (12/11/2026):
- `/api/Reportes/aplicaciones`, `/api/Reportes/prorrateo` y "pagar comisión" ya leían la moneda del ciclo (`ReportesService.ObtenerMonedaCicloAsync`) y la ponen en los encabezados de PDF y Excel. No necesitaron cambios.
- Sus importes salen de tablas que ya están en la moneda del ciclo: comisiones, `tbl_retencionempresa`, y los descuentos y prorrateos que la migración copia de `AplicacionesPagos.Monto` y `AplicacionesProrrateo.Monto` (lo descontado de la comisión).
- Se corrigió `administraciondescuentociclo.sdetalles`, que escribía siempre `$us.`: ahora usa `Bs.` en ciclos `BS` (`SqlMonedaCiclo.SeparadorMontoDescuento`, usado en `AdministracionDescuentoComisionRepository` y `MigracionAplicacionesProrrateoRepository`).
- Se corrigió la glosa de la migración de descuentos (`MigracionAplicacionesProrrateoRepository.AgregarEquivalenteGlosa`): en ciclo `BS` ya no agrega "Equivalente BOB" (multiplicaba Bs por 6,96); para cuotas en USD agrega "Equivalente USD" con `cambioDolar.tipoCambioHistorico`. En ciclo `SUS` el texto no cambia.
- La migración ahora valida la moneda del ciclo antes de insertar.

### Fase 4: pruebas (ambiente controlado, nunca producción)
1. **Pruebas de `DecidirPago`** con la matriz de la sección 2 y los ejemplos: completo, a cuenta, fecha valor, con TC histórico = 6,97 en ciclos `BS` y 7 en ciclos `SUS`.
2. **Regresión en un ciclo `SUS`:** la vista previa tiene que dar **los mismos** pagos, saldos y prorrateos que antes del cambio, en un ciclo sin aplicaciones registradas.
3. **Vista previa de un ciclo `BS`:** revisar a mano un comisionado con una cuota USD, uno con una cuota BOB, uno con una carta y uno con un descuento fijo.
4. **Moneda del ciclo vacía o inválida:** el proceso tiene que detenerse **antes** de borrar datos.
5. **Aplicar en un ciclo `BS` de prueba:** revisar el recibo en Sion (monto en la moneda de la cuota), `AplicacionesPagos` (monto en Bs) y el prorrateo.

---

## 5. Decisiones

| # | Tema | Decisión |
|---|---|---|
| D1 | Tipo de cambio para ciclos `BS` (cuotas USD) | ✅ **Histórico**: `cambioDolar.tipoCambioHistorico` = 6,97. Es el mismo que se usa para pasar a Bs las ventas y cuotas de proyectos USD en ciclos `BS` |
| D1-b | Ciclos `SUS` | ✅ **Se mantiene la lógica actual sin cambios**, incluida la conversión de cuotas BOB con `PagosBolivianos.TipoCambio` = 7 |
| D2 | Saldo mínimo "a cuenta" en ciclos `BS` | ✅ `MontoMinimoParaPagoACuenta` × **TC oficial** = 20 × 7,00 = **140,00 Bs**. Se calcula y no se agrega una clave nueva |
| D3 | Descuentos de monto fijo | ✅ Los montos fijos están en **USD**. En ciclos `BS` se convierten con el **tipo de cambio oficial** (`cambioDolar.tipoCambioOficial` = 7,00) y se descuenta ese monto en Bs de la comisión. En ciclos `SUS`, igual que hoy. Los descuentos por porcentaje no cambian |
| D4 | Cómo se identifica una cuota en BOB | ✅ Sigue la lista `PagosBolivianos.Empresas` (32 a 36) |

Con D1, la tabla de la sección 2 queda así para ciclos `BS`: `factor` = 6,97 (cuota USD) y el ejemplo de 100 USD consume **697,00 Bs**.

### D3: descuentos de monto fijo

> **Datos reales (consulta de solo lectura en `DBITSIS`, 11/11/2026):** existe un solo tipo de descuento, `id 1 "Aporte RPFC"`, de **porcentaje** (`es_porcentaje = 1`), empresa 21 y **inactivo** (`activo = 0`). **No hay descuentos de monto fijo** ni descuentos activos. La regla D3 queda como protección para cuando se carguen descuentos de monto fijo.

Los descuentos salen de `DBITSIS.dbo.aplicaciones_descuentos` (tipo: Royal, Tarjeta, etc.) y `aplicaciones_descuentos_freelancer` (comisionado y valor). Se aplican **después** de las cuotas propias y de las cartas, en este orden: Royal → Tarjeta → otros.
- **Porcentaje** (`es_porcentaje = 1`): `monto = TotalAplicar × % / 100`. Como `TotalAplicar` ya está en la moneda del ciclo, no necesita cambios.
- **Monto fijo** (`es_porcentaje = 0`): el valor `monto_porcentaje` se resta **tal cual** del saldo. No hay columna de moneda, así que el valor significa una cosa distinta según el ciclo.

Ejemplo: descuento "Tarjeta" de monto fijo **50** para un comisionado.

| Ciclo | Saldo antes | Código actual (resta 50) | Si los 50 son USD (×6,97) | Si los 50 son Bs |
|---|---|---|---|---|
| `SUS` | 1.000,00 USD | 950,00 USD ✅ | 950,00 USD | 1.000 − 7,17 = 992,83 USD |
| `BS` | 6.970,00 Bs | 6.920,00 Bs ❌ (descuenta el equivalente de 7,14 USD) | 6.970 − 350,00 = **6.620,00 Bs** (× 7,00, TC oficial) | 6.920,00 Bs |

Opciones:
- **A. (elegida)** Los montos fijos están en USD: en ciclos `BS` se multiplican por el TC oficial (7,00). No hay que tocar la base.
- **B.** Agregar una columna `moneda` en `aplicaciones_descuentos_freelancer` y convertir solo cuando no coincide con la moneda del ciclo. Es más flexible, pero cambia la base de DBITSIS.

## 6. Riesgos

- **La vista previa borra datos aplicados.** `VistaPrevia` borra `AplicacionesPagos`, `AplicacionesProrrateo` y `AplicacionesComisionado` del ciclo. Si se ejecuta sobre un ciclo ya aplicado y después se vuelve a aplicar, puede **pagar dos veces**. Las pruebas de este plan tienen que hacerse en ciclos sin aplicaciones registradas. Conviene corregirlo aparte.
- **Redondeo en pagos a cuenta en USD:** `saldo / TC` puede dejar diferencias de centavos entre lo pagado en Sion y lo consumido en Bs. El monto consumido es la fuente de verdad del saldo, y el pagado queda en la observación.
- **Si la fuente no está en Bs:** si la fase 1 muestra que `tbl_retencionempresa` no queda en Bs para ciclos `BS`, primero hay que corregir el procedimiento `RetencionEmpresa`. Si no, Aplicaciones trataría USD como Bs.
- **`reprocesar-grupo-sion`** tiene fijo el ciclo 147 (`SUS`). Tiene que leer la moneda del ciclo igual que `aplicar`.
- **Empresas en BOB:** si se agrega una empresa con cuotas en BOB y no se incluye en `PagosBolivianos.Empresas`, sus cuotas se tratan como USD.
