# Plan: cálculo Monte Sion dentro de la simulación

## Objetivo

Agregar a la corrida de simulación un paso **MONTE SION**, que se ejecute **después de `ARMADO ARBOL`**. El paso:

- usa el **mismo cálculo y el mismo guardado** que el proceso principal (código compartido);
- escribe en las mismas tablas: `t_plan_montesion`, `reportesmontesion` y `t_nuevos_rangos_montesion_2025`;
- **no actualiza `administracioncontacto`** cuando se ejecuta desde la simulación; solo la lee.

Además, Monte Sion (simulación y proceso real) pasa a respetar la **moneda del ciclo**: en un ciclo `BS`, los umbrales, el bono y el incentivo se expresan en bolivianos.

## Cómo funciona hoy

| Parte | Archivo | Qué hace |
|---|---|---|
| Cálculo | `src/Api/Services/MonteSionService.cs` → `CalcularRangosAsync` | Solo lectura. Lee los datos del ciclo y los niveles (`administracionnivel`), arma los equipos, aplica el tope VME por equipo, evalúa los rangos de mayor a menor, aplica la regla de primera venta desde mayo de 2026 y calcula el beneficio (bono o incentivo). Devuelve solo los emprendedores que califican. |
| Datos de entrada | `MonteSionRepository.ObtenerDatosCicloAsync` | `administracioncontacto`, `administracionventapersonal` del ciclo, `red_comprimida` + `administracioncontrato` (producción niveles 1–7, solo crédito/contado), primera venta y ventas antes de mayo de 2026. |
| Umbrales | `administracionnivel` | `ddesde`/`dhasta` (producción del rango), `VME` (tope por equipo, **monto**), `dbono` (bono). Están en USD. |
| Incentivos | `appsettings.json` → `MonteSion:Rangos` | `IncentivoUsd` (monto) e `Incentivo` (texto, por ejemplo "Pago único de 40.000 $us"). |
| Guardado | `MonteSionRepository.GuardarRangosAsync` | Transacción: rechaza el ciclo si ya tiene datos; inserta en `t_plan_montesion` y `reportesmontesion`; si sube de nivel, **`UPDATE administracioncontacto.lnivel_id`** e insert en `t_nuevos_rangos_montesion_2025`. IDs con `MAX + 1`. Si no hay resultados, devuelve error. |

En la simulación, todas las tablas de entrada ya las llenan los pasos anteriores de la corrida (`administracionventapersonal`, `red_comprimida`, `administracionventagrupo`, `administracioncontrato`).

## Diseño

### 1. Moneda en el cálculo (compartido)
En `MonteSionService.CalcularRangosAsync`:
- Obtener la moneda del ciclo con `IAdministracionCicloRepository.ObtenerMonedaAsync` (helper de la Fase 2 del plan de tipo de cambio). Si es inválida, devolver error.
- **Ciclo `SUS`:** sin cambios.
- **Ciclo `BS`:** factor = `CambioDolarService.TipoCambioHistorico` (6,97). Al armar los rangos desde `administracionnivel`, multiplicar por el factor `ProduccionRequerida`, `ProduccionHasta`, `VmeMonto`, `BonoUsd` e `IncentivoUsd`, redondeando a 2 decimales.
- **Incentivo en Bs:** en ciclo `BS`, el texto se genera a partir del monto: `"Pago único de {IncentivoUsd × factor:N0} Bs"`. Opcional: si en `appsettings` se define `IncentivoBs` para un rango, se usa ese texto (para montos redondos).
- Agregar `Moneda` a `MonteSionResultadoRango`, para que el XLSX y el frontend muestren la moneda correcta.

### 2. Guardado compartido con modo simulación
Agregar el parámetro `bool esSimulacion = false` a `IMonteSionService.GuardarRangosAsync` y a `IMonteSionRepository.GuardarRangosAsync`:

| Comportamiento | Proceso real (`esSimulacion = false`) | Simulación (`esSimulacion = true`) |
|---|---|---|
| Validación "el ciclo ya tiene datos" | Igual que hoy | Igual (el RESET de la simulación ya borró el ciclo, así que pasa) |
| Inserts en `t_plan_montesion` y `reportesmontesion` | Sí | Sí |
| Insert en `t_nuevos_rangos_montesion_2025` si asciende | Sí | Sí |
| `UPDATE administracioncontacto.lnivel_id` | Sí | **No** |
| Ningún emprendedor califica | Error (como hoy) | **OK con 0 registros** |

El endpoint `POST api/MonteSion/rangos/guardar` no cambia (usa `false` por defecto).

### 3. Reset de la simulación
Agregar a `SqlResetTransaccional` (`SimulacionRepositorio.Reset.cs`):
- `DELETE FROM t_nuevos_rangos_montesion_2025 WHERE lciclo_id = @LCicloId;`
- `DELETE FROM reportesmontesion WHERE lciclo_id = @LCicloId;`
- `DELETE FROM t_plan_montesion WHERE lciclo_id = @LCicloId;`

Así, cada corrida reemplaza los datos Monte Sion del ciclo.

### 4. Paso en la corrida
En `SimulacionService.EjecutarCorridaAsync`, después de `ARMADO ARBOL`:
- `paso = "MONTE SION"` → `_monteSion.GuardarRangosAsync(ciclo.LCicloId, usuario, esSimulacion: true)`.
- Si falla, se detiene la corrida con `CerrarConErrorAsync`.
- El mensaje final pasa a "nueve pasos" e incluye rangos y ascensos.
- Inyectar `IMonteSionService` en `SimulacionService` (no hay dependencia circular).

### Consecuencias aceptadas
- Como `lnivel_id` no cambia en la simulación, cada corrida parte del rango real actual del contacto, y repetirla da el mismo resultado.
- Los ascensos simulados se registran en `t_nuevos_rangos_montesion_2025` aunque el contacto conserve su nivel.
- El RESET de la simulación puede reemplazar el Monte Sion de un ciclo. Es aceptado: el ciclo define si es de simulación o no.

## Resumen de estados

| Fase | Descripción | Estado |
|---|---|---|
| 0 | Confirmación de decisiones | ✅ Terminado |
| 1 | Moneda en el cálculo Monte Sion (compartido) | ✅ Cubierto por `PLAN_PLAN_CARRERA_MONTE_SION_POR_MONEDA.md` |
| 2 | Guardado compartido con modo simulación | ✅ Terminado |
| 3 | Reset y paso MONTE SION en la simulación | ✅ Terminado |
| 4 | Pruebas y cierre | 🔄 En curso (compilación OK; faltan pruebas con base) |

Leyenda: ⏳ Pendiente · 🔄 En curso · ✅ Terminado · ⛔ Bloqueado

---

## Fase 0 — Confirmación de decisiones

**Estado:** ✅ Terminado

- [x] **Datos reales del mismo ciclo → ACEPTADO:** el ciclo define si es simulación; el RESET puede reemplazar los datos Monte Sion del ciclo.
- [x] **Ascensos sin actualizar el contacto → SÍ:** se inserta en `t_nuevos_rangos_montesion_2025` aunque no se actualice `administracioncontacto`.
- [x] **Alcance → COMPARTIDO:** simulación y proceso real usan el mismo cálculo y guardado; solo en simulación no se actualiza el contacto.
- [x] **Incentivos → en Bs** si el ciclo es `BS`.
- [x] **Monto a pagar →** se guarda en la moneda del ciclo y se borra por ciclo en el RESET. Se mantiene el truncado a entero de `Monto_Pagar` en `t_plan_montesion`.
- [x] **Sin calificados → OK** con 0 registros en la simulación.
- [x] **Opción de moneda para VME y umbrales → resuelta por otro plan:** `PLAN_PLAN_CARRERA_MONTE_SION_POR_MONEDA.md` (cerrado 2026-11-11) lee umbrales, VME, bono e incentivo de `administracionnivelplancarrera` según la moneda del ciclo. Texto original: se propone la **A** (convertir umbrales, bono e incentivo × `tipoCambioHistorico` en ciclos `BS`, sin cambios en la base). Alternativas: B (valores por moneda en la base) o C (producción a USD solo para Monte Sion). **Falta confirmación.**

---

## Fase 1 — Moneda en el cálculo Monte Sion

**Estado:** ✅ Cubierto por `PLAN_PLAN_CARRERA_MONTE_SION_POR_MONEDA.md` (no se implementó la opción A; las tareas de abajo quedan sin efecto)

- [ ] Inyectar `IAdministracionCicloRepository` y `CambioDolarService` en `MonteSionService`.
- [ ] En `CalcularRangosAsync`, obtener y validar la moneda del ciclo.
- [ ] En ciclo `BS`, convertir `ProduccionRequerida`, `ProduccionHasta`, `VmeMonto`, `BonoUsd` e `IncentivoUsd` × factor.
- [ ] En ciclo `BS`, generar el texto del incentivo en Bs (o usar `IncentivoBs` si está configurado).
- [ ] Agregar `Moneda` a `MonteSionResultadoRango`.
- [ ] Ajustar los encabezados de `MonteSion.xlsx.cs` (bono/incentivo) a la moneda del ciclo.
- [ ] (Opcional) Etiquetas de moneda en la pantalla de Monte Sion del frontend.

## Fase 2 — Guardado compartido con modo simulación

**Estado:** ✅ Terminado

- [x] Agregar `bool esSimulacion = false` en `IMonteSionService.GuardarRangosAsync` / `MonteSionService`.
- [x] Agregar `bool esSimulacion = false` en `IMonteSionRepository.GuardarRangosAsync` / `MonteSionRepository`.
- [x] En el repositorio, no ejecutar `UPDATE administracioncontacto` cuando `esSimulacion = true`.
- [x] En el servicio, con `esSimulacion = true` y 0 calificados, devolver éxito con 0 registros.
- [x] Verificar (por código) que el endpoint `rangos/guardar` mantiene su comportamiento.

## Fase 3 — Reset y paso MONTE SION en la simulación

**Estado:** ✅ Terminado

- [x] Agregar los 3 `DELETE` por `lciclo_id` al RESET de la simulación.
- [x] Inyectar `IMonteSionService` en `SimulacionService`.
- [x] Agregar el paso `MONTE SION` después de `ARMADO ARBOL`, con el manejo de error de los demás pasos.
- [x] Actualizar el comentario de la corrida y el mensaje final ("todos los pasos terminaron", con rangos y ascensos).

## Fase 4 — Pruebas y cierre

**Estado:** 🔄 En curso

- [x] Compilación sin errores ni advertencias nuevas.
- [ ] Ciclo `SUS`: el cálculo da lo mismo que hoy (regresión).
- [ ] Ciclo `BS`: umbrales, VME, bono e incentivo en Bs; mismo rango para vendedores de proyectos USD que en un ciclo SUS equivalente.
- [ ] Simulación: `administracioncontacto.lnivel_id` **no cambia**.
- [ ] Proceso real (`rangos/guardar`): sigue actualizando `lnivel_id` en ascensos.
- [ ] Simulación repetida dos veces: sin duplicados en las 3 tablas.
- [ ] Revisar las pantallas y reportes que leen estas tablas (Monte Sion, plan de carrera, ascenso de rango).
- [ ] Aprobación del usuario.

---

## Bitácora

| Fecha | Fase | Cambio de estado / nota |
|---|---|---|
| 2026-11-11 | — | Plan creado. Todas las fases en Pendiente. |
| 2026-11-11 | 0 → 🔄 | Confirmado: reemplazo de datos del ciclo desde la simulación; registro de ascensos sin actualizar `administracioncontacto`. |
| 2026-11-11 | 0 | Confirmado: cálculo y guardado compartidos (en simulación no se actualiza el contacto); incentivos en Bs para ciclos BS; monto a pagar en la moneda del ciclo, borrado por ciclo; sin calificados = OK. Plan reestructurado en 5 fases. Pendiente: confirmar la opción A de moneda. |
| 2026-11-17 | 0–3 → ✅, 4 → 🔄 | Moneda resuelta por el plan de carrera por moneda. Implementado `esSimulacion` (sin UPDATE de `lnivel_id`, 0 calificados = OK), 3 DELETE en el RESET y paso MONTE SION tras ARMADO ARBOL. Compila sin advertencias nuevas. Faltan pruebas con base. |
