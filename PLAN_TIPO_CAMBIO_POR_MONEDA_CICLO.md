# Plan: tipo de cambio según la moneda del ciclo

## Objetivo

Que la conversión de montos de ventas y cuotas dependa de la columna `moneda` de `administracionciclo`.

Configuración usada (`src/Api/appsettings.json` → `cambioDolar`):

| Parámetro | Valor |
|---|---|
| `idsComplejosProyectos` (proyectos BOB) | 81–91 |
| `fechaCorte` | 2026-06-30 |
| `tipoCambioHistorico` | 6,97 |
| `tipoCambioOficial` | 7,00 |

### Regla por moneda del ciclo

**Ciclo `SUS` (dólares): lógica actual, sin cambios**

| Proyecto | Ventas (venta personal, casos especiales, upgrades) | Cuotas (bono residual, venta residual) |
|---|---|---|
| BOB (81–91) | `monto / 7` | `monto / 7` |
| USD | sin cambio | fecha venta `< 2026-06-30`: `monto × 6,97 / 7`; desde el corte: sin cambio |

**Ciclo `BS` (bolivianos): lógica nueva**

| Proyecto | Ventas | Cuotas |
|---|---|---|
| BOB (81–91) | sin cambio (ya están en BOB) | sin cambio |
| USD | `monto × 6,97` (`tipoCambioHistorico`) | `monto × 6,97` (`tipoCambioHistorico`) |

En ciclo `BS` **no se usa** `fechaCorte` ni `tipoCambioOficial`: todo proyecto USD se multiplica por `tipoCambioHistorico`, sin importar la fecha de venta.

**Alcance:** la regla se aplica en todas las lecturas que traen montos desde Conexión:

| Lectura | Repositorio | Montos afectados |
|---|---|---|
| Ventas | `VentaCnxRepository.GetVentaCnx` | `DPrecio`, `PrecioInicial`, `SCuotaInicial`, `SCuotaInicialOriginal`, `ValorCi` |
| Cuotas | `BonoResidualRepository.GetCuota` | Bono, Amortización, Capital, Interés, Seguro, Expensa, Multa, Acuenta, Totalpago, Montodeuda, Pagosacuenta |
| Cuota/venta residual | `CuotasVentaResidualRepository.GetCuotasVentasResidual` | `ImporteTotal` (proyecto = `IdAlmacen`) |
| Casos especiales | `CasosEspecialesRepository.GetVentasCasosEspeciales` | mismos campos que Ventas (`ItemVentaCnx`) |
| Upgrades | `CasosEspecialesRepository.GetUpgradeSolicitudPorVentasCnx` | `MontoHold`, `PagadoHold`, `DeudaHold` (proyecto = `ProyectoHoldId`); `Monto`, `Deuda`, `Cuota` (proyecto = `ProyectoId`) |

Los demás pasos (comisión directa, grupo, bono residual, bono par) se calculan a partir de esos datos ya convertidos.

**Moneda vacía o inválida:** la corrida/proceso se detiene con error **antes** del RESET.

Redondeo: igual que hoy, una sola vez al resultado final, 2 decimales, `MidpointRounding.AwayFromZero`.

Ejemplos ciclo `BS`:
- Venta USD de 10.000 → `10.000 × 6,97 = 69.700,00 BOB`.
- Cuota BOB de 700 → `700,00 BOB` (sin cambio).

## Resumen de estados

| Fase | Descripción | Estado |
|---|---|---|
| 0 | Confirmación de decisiones | ✅ Terminado |
| 1 | Simulación | ✅ Terminado |
| 2 | Proceso de comisiones real | 🔄 En curso (código listo; faltan pruebas con base de datos) |
| 3 | Validación final y cierre | ⏳ Pendiente |

Leyenda: ⏳ Pendiente · 🔄 En curso · ✅ Terminado · ⛔ Bloqueado

---

## Fase 0 — Confirmación de decisiones

**Estado:** ✅ Terminado (2026-11-11)

- [x] **Ventas USD en ciclo BS:** se convierten a BOB con `tipoCambioHistorico`.
- [x] **Fecha de corte en ciclo BS:** todos los proyectos USD se multiplican por `tipoCambioHistorico`, sin importar la fecha de venta.
- [x] **Alcance:** lectura de ventas (`GetVentaCnx`), cuotas (`GetCuota`), cuota/venta residual (`GetCuotasVentasResidual`), casos especiales (`GetVentasCasosEspeciales`) y upgrades (`GetUpgradeSolicitudPorVentasCnx`); el resto del proceso se basa en esos datos.
- [x] **Cuota inicial:** se mantiene `SCuotaInicial = ValorCi` cuando difieren y luego se convierte `SCuotaInicial` × 6,97 en proyectos USD.
- [x] **Tabla `T_ACCIONESCUOTASGRL` compartida:** sin riesgo; solo hay un ciclo con una moneda a la vez.
- [x] Valores válidos de `moneda`: `BS` y `SUS` (los mismos que valida `AdministracionCicloService`).

---

## Fase 1 — Simulación

**Estado:** ✅ Terminado (2026-11-11) — validado por el usuario.

### 1.1 Leer la moneda del ciclo
- [x] Agregar `moneda AS Moneda` en `SqlUltimoCiclo` (`src/Infrastructure/Repositories/Simulacion/SimulacionRepositorio.Ciclo.cs`).
- [x] Agregar la propiedad `Moneda` en `CicloReferencia`.
- [x] En `SimulacionService.EjecutarCorridaAsync`, normalizar (`Trim().ToUpperInvariant()`) y validar `BS`/`SUS`; si es inválida, detener la corrida antes del RESET.

### 1.2 Lógica de conversión por moneda en `CambioDolarService`
Archivo: `src/Infrastructure/Service/CambioDolarService.cs`.

- [x] Definir la moneda del ciclo como valor tipado: `enum MonedaCiclo { SUS, BS }` + `MonedaCicloHelper.TryParse` en `src/Domain/Entities/MonedaCiclo.cs`.
- [x] Agregar sobrecargas que reciban la moneda del ciclo:
  - `Convertir(ItemVentaCnx venta, MonedaCiclo moneda)`
  - `Convertir(TCuota cuota, MonedaCiclo moneda)`
  - `Convertir(VentaResidual venta, MonedaCiclo moneda)` (se usa en Fase 2)
  - `Convertir(UpgradeSolicitudDto solicitud, MonedaCiclo moneda)` (se usa en Fase 2)
- [x] Con `SUS`: delegar a la lógica actual tal como está (sin tocar fórmulas).
- [x] Con `BS`: método privado `ConvertirABolivianos(monto, idProyecto)`:
  - proyecto en `idsComplejosProyectos` → `monto` (sin cambio);
  - resto → `Redondear(monto × tipoCambioHistorico)`.
- [x] `ItemVentaCnx` en `BS`: primero `SCuotaInicial = ValorCi` si difieren (igual que hoy); después convertir `DPrecio`, `PrecioInicial`, `SCuotaInicial`, `SCuotaInicialOriginal` y `ValorCi`. No aplicar el recálculo `DPrecio × PorcentajeCuotaInicial` que hoy existe solo para proyectos BOB en `SUS`.
- [x] `TCuota` en `BS`: convertir los mismos campos que hoy (Bono, Amortización, Capital, Interés, Seguro, Expensa, Multa, Acuenta, Totalpago, Montodeuda, Pagosacuenta).
- [x] `VentaResidual` en `BS`: convertir `ImporteTotal` usando `IdAlmacen` como proyecto.
- [x] `UpgradeSolicitudDto` en `BS`: convertir `MontoHold`, `PagadoHold`, `DeudaHold` con `ProyectoHoldId`; y `Monto`, `Deuda`, `Cuota` con `ProyectoId` (solo si tiene valor, igual que hoy).
- [x] Mantener los métodos actuales sin parámetro como están (equivalen a `SUS`) para no romper consumidores.

### 1.3 Pasar la moneda por las lecturas (sin romper otros consumidores)
- [x] `IVentasCnxRepository.GetVentaCnx(..., MonedaCiclo moneda = MonedaCiclo.SUS)` y su implementación en `src/Infrastructure/Repositories/cnx/VentaCnxRepository.cs` (línea ~46).
- [x] `IBonoResidualRepository.GetCuota(..., MonedaCiclo moneda = MonedaCiclo.SUS)` y su implementación en `src/Infrastructure/Repositories/BonoResidualRepository.cs` (línea ~345).
- [x] Verificar que los consumidores actuales compilan sin cambios (default `SUS` = comportamiento actual).

### 1.4 Usar la moneda en la simulación
- [x] Propagar `ciclo.Moneda` a `ObtenerVentasAsync` → `GetVentaCnx(..., moneda)` (`SimulacionService.cs:257`).
- [x] Propagar a `DatosResidualAsync` → `GetCuota(..., moneda)` (`SimulacionService.cs:458`).
- [x] Ajustar `ISimulacionService` si cambian las firmas públicas.
- [x] Registrar en log y en el mensaje de `TerminarCorridaAsync` la moneda del ciclo y el TC aplicado.

### 1.5 Pruebas
- [x] Prueba unitaria de fórmulas de `CambioDolarService` (19 casos: SUS y BS; ventas, cuotas, venta residual, upgrades; antes y después del corte; redondeo). Todas OK.
- [x] Compilación de la solución: 0 errores.
- [x] Ciclo `SUS`: resultados idénticos a una corrida anterior (regresión).
- [x] Ciclo `BS`, proyecto BOB: montos en `administracioncontrato` y `T_ACCIONESCUOTASGRL` iguales a los de Conexión.
- [x] Ciclo `BS`, proyecto USD: montos = origen × 6,97, redondeados a 2 decimales.
- [x] Ciclo con moneda vacía/inválida: la corrida se detiene sin ejecutar RESET.
- [x] Revisar pasos posteriores (comisión directa, grupo, bono residual, bono par) con ambos tipos de ciclo; verificar que topes, montos mínimos o umbrales configurados en USD no se comparen contra montos en BOB.

---

## Fase 2 — Proceso de comisiones real

**Estado:** 🔄 En curso — código implementado y compilando; pendientes las pruebas contra base de datos.

Pasar la moneda del ciclo procesado a cada consumidor que hoy convierte siempre:

**Ventas y cuotas** (lecturas ya modificadas en la Fase 1):
- [x] `src/Api/Services/ProcesoComisionesService.cs:203` → `GetVentaCnx`.
- [x] `src/Api/Services/BonoResidualService.cs:364` y `:687` → `GetCuota`.
- [x] `src/Api/Services/BonoResidualService.cs:431` y `:800` → `GetVentaCnx`.
- [x] `src/Api/Job/CronJob.cs:220` y `:300` → `GetVentaCnx`.

**Cuota/venta residual:**
- [x] `ICuotasVentaResidualRepository.GetCuotasVentasResidual(..., MonedaCiclo moneda = MonedaCiclo.SUS)` y su implementación (`CuotasVentaResidualRepository.cs:48`). Ya recibe `LCicloId`; se puede obtener la moneda desde el servicio.
- [x] `src/Api/Services/CuotasVentaResidualService.cs:40` y `:130` → pasar la moneda del ciclo.

**Casos especiales:**
- [x] `GetVentasCasosEspeciales(..., MonedaCiclo moneda = MonedaCiclo.SUS)` y su implementación (`CasosEspecialesRepository.cs:49`).
- [x] `src/Api/Services/CasosEspecialesService.cs:29` → pasar la moneda del ciclo.

**Upgrades:**
- [x] `GetUpgradeSolicitudPorVentasCnx(..., MonedaCiclo moneda = MonedaCiclo.SUS)` y su implementación (`CasosEspecialesRepository.cs:160`).
- [x] Ubicar en `CasosEspecialesService` el/los puntos que llaman a esta lectura y pasar la moneda del ciclo.

**General:**
- [x] Obtener la moneda del ciclo en cada flujo (reutilizar la lectura de `AdministracionCicloRepository`) y detener el proceso si es inválida.
  - Helper común: `src/Application/Interface/AdministracionCicloMonedaExtensions.cs` (`ValidarMoneda` y `ObtenerMonedaAsync`).
  - En todos los pasos con control de proceso la moneda se valida **antes** de `IniciarPaso`.
  - `CronJob`: `GetProceso` ahora trae `C.moneda` (`ItemProcesoJon.Moneda`); si es inválida, el job no procesa.
  - `CasosEspecialesService`: se inyecta `IAdministracionCicloRepository`.

### Pruebas
- [ ] Ciclo `SUS`: resultados idénticos a los actuales en cada proceso.
- [ ] Ciclo `BS`: proyectos BOB sin cambio y proyectos USD × 6,97 en ventas, cuotas, venta residual, casos especiales y upgrades.

---

## Fase 3 — Validación final y cierre

**Estado:** ⏳ Pendiente

- [ ] Comparar simulación vs. proceso real para un mismo ciclo `SUS` y un mismo ciclo `BS`.
- [ ] Revisar reportes (`ReportesService`) para que la moneda mostrada coincida con los montos.
- [ ] Documentar la regla final en el README del proyecto.
- [ ] Aprobación del usuario.

---

## Bitácora

| Fecha | Fase | Cambio de estado / nota |
|---|---|---|
| 2026-11-11 | — | Plan creado. Todas las fases en Pendiente. |
| 2026-11-11 | 0 | Lógica replanteada: en ciclo `BS`, proyectos USD se convierten a BOB con `tipoCambioHistorico` (6,97); proyectos BOB sin cambio. Ciclo `SUS` mantiene la lógica actual. Se cierra la duda de ventas USD en ciclo BS. |
| 2026-11-11 | 0 → ✅ | Decisiones confirmadas: en `BS` todo proyecto USD × `tipoCambioHistorico` sin fecha de corte; alcance solo en lectura de ventas y cuotas; cuota inicial se convierte; tabla de cuotas sin riesgo (un ciclo a la vez). Fase 1 lista para iniciar. |
| 2026-11-11 | 0 / 2 | Alcance ampliado: la regla por moneda del ciclo también aplica a `GetCuotasVentasResidual`, `GetVentasCasosEspeciales` y upgrades (`GetUpgradeSolicitudPorVentasCnx`). Se agregan a la Fase 2. |
| 2026-11-11 | 1 → 🔄 | Implementado: `MonedaCiclo`, sobrecargas en `CambioDolarService`, parámetro `moneda` en `GetVentaCnx`/`GetCuota` (default `SUS`), lectura y validación de `moneda` en la simulación (paso `MONEDA CICLO`, antes del RESET). Compila y pasan las pruebas de fórmulas. Pendiente: pruebas con ciclos reales. |
| 2026-11-11 | 1 | Corrección R-17: `Cartera` se trunca para el ciclo que se está simulando (`simulacion = 1`, `lestado = 0`) y no solo para el de mayor `lciclo_id`. Cambiado en `SimulacionRepositorio.Reset.cs` y `SimulacionRepositorio.DatosResidual.cs`. |
| 2026-11-11 | 1 | Se elimina la guardia R-17: `Cartera` se trunca en cada ejecución de la simulación (RESET y DATOS RESIDUAL), sin importar el ciclo. El proceso real (`BonoResidualRepository.GuardarCartera`) ya truncaba siempre. |
| 2026-11-11 | 1 | Nuevo paso `ARMADO ARBOL` después de `BONO PAR`: la simulación llama a `IMonteSionArbolService.ArmarAsync` (el mismo servicio del endpoint `POST api/MonteSionArbol/armar`) con el ciclo simulado. |
| 2026-11-11 | 1 → ✅ | Fase 1 (simulación) cerrada: validada por el usuario con ciclos reales. |
| 2026-11-11 | 2 → 🔄 | Implementado: moneda del ciclo en `ProcesoComisionesService` (obtener/guardar ventas y upgrades), `BonoResidualService` (cuotas y excedente), `CuotasVentaResidualService`, `CasosEspecialesService` y `CronJob`; parámetro `moneda` en `GetCuotasVentasResidual`, `GetVentasCasosEspeciales` y `GetUpgradeSolicitudPorVentasCnx`. Compila sin errores ni advertencias nuevas. Pendiente: pruebas con ciclos reales. |
