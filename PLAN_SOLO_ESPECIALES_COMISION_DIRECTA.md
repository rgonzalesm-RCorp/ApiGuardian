# Plan: quien solo tiene ventas especiales cobra solo comisión directa

Creado el 13/11/2026. Estado: **fases 0 a 4 completadas el 16/11/2026; fase 5 a medias. La regla está en el código (sin commit), pasó las pruebas de solo lectura y falta la corrida completa en un ambiente de pruebas**.

## 1. Regla

- **Venta especial:** contrato de tipo 6 a 9 (`UPGRADE`, `RECUPERACION`, `RECOMPRA`, `CASOSESPECIALES`). Son los tipos de `HabilitacionComisionHelper.TiposContratoEspeciales`.
- **Persona "solo especiales":** en el ciclo tiene ventas y todas son especiales. Cobra **solo comisión directa** (67 % de la cuota inicial, como hoy).
- **Persona mixta:** tiene al menos una venta que no es especial (tipos 1 a 5) además de la especial. Cobra todo, igual que hoy. Los tipos 3, 4 y 5 siguen como están (decisión 4).
- **Entrada al proceso:** entran **todas** las ventas especiales del ciclo, sin mínimo por vendedor (decisión 7).

### Garantías que debe cumplir el cambio (confirmadas el 16/11/2026)

| # | Garantía | Cómo se comprueba en la fase 5 |
|---|---|---|
| G1 | La persona "solo especiales" cobra la comisión directa de sus ventas especiales y ningún otro bono | Sin filas suyas en residual, grupo, venta residual ni Monte Sion del ciclo; sus cuotas de venta residual quedan con "recibe: no" |
| G2 | La persona "solo especiales" no entra al armado de la red comprimida | No figura como activa; sus niveles suben al siguiente activo |
| G3 | La persona mixta (una venta normal y otra especial) cobra todos los bonos que le correspondan | Mismos montos que el "antes" de la fase 1 en todos los bonos |
| G4 | La persona mixta entra al armado de la red comprimida | Figura como activa, igual que hoy |
| G5 | Todas las ventas especiales entran al proceso (decisión 7) | Ninguna especial del período queda fuera |
| G6 | Las ventas especiales no cuentan para el bono par ni generan cuotas de venta residual (decisiones 3 y 6) | Ningún contrato tipo 6 a 9 en el detalle del bono par ni en `t_productos_pagar_mensuales` |

- Punto por confirmar en G1: el bono par se paga al patrocinador por las ventas normales de sus directos, no por sus ventas propias. Con la decisión 3 las ventas especiales no cuentan, pero un patrocinador "solo especiales" todavía lo cobraría por las ventas normales de sus directos.
- "Si le corresponde" en G3 significa que la persona mixta se evalúa con las reglas de hoy de cada bono; la venta especial no le suma ni le quita.
- La lista de "solo especiales" de la fase 2 es la única fuente para G1 a G4: todos los cálculos la usan, para que una persona no quede fuera de un bono y dentro de otro.

## 2. Situación actual (leída del código el 13/11/2026)

| Cálculo | ¿Cuenta hoy la venta especial? | Dónde se ve |
|---|---|---|
| Comisión directa | Sí, para el vendedor: 67 % de la cuota inicial | `ProcesoComisionesService.cs:325` |
| Activo en la red comprimida | Sí: cualquier contrato del período lo deja como activo | `RedesRepository.cs:24` |
| Bono residual | Sí: el vendedor queda como activo del ciclo | `BonoResidualRepository.cs:604` |
| Comisión de grupo | No: solo contratos tipo 1 y 2 | `ProcesoComisionesRepository.cs:462` |
| Bono par | No: solo cuentan ventas tipo 1 y 2 de los directos del patrocinador | `GRD/Query.cs:48` |
| Comisión venta residual (cuotas nuevas) | No genera cuotas nuevas: excluye los tipos especiales | `ProcesoComisionesService.cs:776` |
| Comisión venta residual (pago mensual) | Sí: el vendedor con cualquier venta personal en el ciclo, incluida la directa de una especial, recibe la cuota del mes | `CuotasVentaResidualService.cs:272` |
| Monte Sion | No suma producción: solo tipo 1 y 2 | `MonteSionRepository.cs:441` |

Notas:

- El filtro que excluía a los tipos especiales de los activos del residual existe pero está comentado (`BonoResidualRepository.cs:610`). No sirve para esta regla: excluye por venta, no por persona.
- Las ventas especiales de un vendedor solo entran al proceso si tuvo al menos una venta no especial en el período o si tiene dos o más especiales (`CasosEspecialesRepository.cs:85-96`). Un vendedor con una sola especial y ninguna normal queda fuera. Este filtro se elimina (decisión 7).
- Ese filtro toma como "venta normal" cualquier contrato que no sea tipo 6 a 9, o sea que incluye los tipos 3, 4 y 5.
- El filtro solo se aplica al cargar la pantalla (`GET /CasosEspeciales/casos/especiales`). Al guardar (`save/vta/proceso` con `esEspecial`) el backend no vuelve a revisarlo.

## 3. Qué cambia para una persona "solo especiales"

| Cálculo | Hoy | Con la regla |
|---|---|---|
| Comisión directa | Cobra 67 % | Sin cambio |
| Bono residual | Cobra, porque queda como activa | Deja de cobrar |
| Comisión de grupo (como ganadora de ventas de su red) | Cobra, porque queda activa en la red comprimida | Deja de cobrar |
| Monte Sion | Entra a la evaluación de rango | Queda fuera |
| Comisión venta residual | Sigue cobrando las cuotas mensuales de ventas normales de ciclos anteriores | Deja de cobrar esas cuotas en el ciclo; la cuota se pierde |
| Bono par (como patrocinadora) | Cobra si sus directos tienen ventas normales | Sin cambio: las ventas especiales ya no cuentan (decisión 3) |

Efecto de la decisión 7: el vendedor con una sola venta especial y ninguna normal hoy no entra al proceso y no cobra nada por ella. Con el cambio entra, queda como "solo especiales" y cobra la comisión directa del 67 %.

## 4. Decisiones (fase 0)

| # | Decisión | Respuesta | Estado |
|---|---|---|---|
| 1 | Red comprimida | Se comprime: la persona "solo especiales" deja de contar como activa y sus niveles suben al siguiente activo | ✅ Confirmada (13/11/2026) |
| 2 | Habilitaciones manuales | La habilitación manda: si está habilitada para generar comisiones, cobra todo | ✅ Confirmada (13/11/2026) |
| 3 | Bono par | Las ventas especiales no entran al bono par. Ya es así en el código (solo tipos 1 y 2); no requiere cambio, solo comprobarlo | ✅ Confirmada (16/11/2026) |
| 4 | Intercambio, FTV y amortización (tipos 3, 4 y 5) | Siguen como están: no se tratan como especiales y la regla no los toca | ✅ Confirmada (16/11/2026) |
| 5 | Desde qué ciclo | Desde el ciclo en proceso; no se recalculan ciclos cerrados | ✅ Confirmada (13/11/2026) |
| 6 | Comisión venta residual | La persona "solo especiales" no entra al pago de cuotas del ciclo y la cuota se pierde; las ventas especiales tampoco generan cuotas | ✅ Confirmada (13/11/2026, ampliada el 16/11/2026) |
| 7 | Entrada de ventas especiales al proceso | Entran todas; se elimina el filtro de "venta normal o dos o más especiales" | ✅ Confirmada (16/11/2026) |

### Detalle de la decisión 3 (bono par)

Respuesta del 16/11/2026: las ventas especiales no entran para el bono par. La consulta `ScriptGrd.QueryBonoPar` ya filtra `ltipocontrato_id in (1,2)`, y la simulación usa la misma consulta. No se cambia código.

No se decidió nada sobre el patrocinador: el bono par no mira las ventas propias de quien lo cobra, así que un patrocinador "solo especiales" lo sigue cobrando por las ventas normales de sus directos. Las dos opciones planteadas el 13/11 (exigir una venta tipo 1 o 2 a todo patrocinador, o excluir solo a los "solo especiales") quedan sin aplicar.

### Detalle de la decisión 4 (otros tipos)

Datos de la base, consultados en solo lectura el 13/11/2026:

| Tipo | Nombre | Contratos en total | Desde julio 2026 | Última venta |
|---|---|---|---|---|
| 3 | Por Intercambio | 2.116 | 1 | 31/07/2026 |
| 4 | Publicidad FTV | 51 | 0 | 31/01/2017 |
| 5 | Amortización a Capital | 3.890 | 0 | 29/02/2020 |

Hoy el proceso solo puede crear el tipo 3 desde las ventas de Conexión.

Respuesta del 16/11/2026: que siga como está. Se entiende así: una venta tipo 3, 4 o 5 sigue dejando activo al vendedor como hoy, y la persona con una especial y una por intercambio no es "solo especiales". Esos tipos siguen sin contar para grupo, bono par ni Monte Sion, igual que hoy.

### Detalle de la decisión 6 (pago de cuotas de venta residual)

Análisis del 16/11/2026, que estaba pendiente:

- El pago mensual se arma en `CuotasVentaResidualService.ConstruirListadoAsync`.
- Recibe la cuota el asesor que tiene alguna fila en `administracionventapersonal` del ciclo y no está bloqueado, o el que tiene habilitación manual (`CuotasVentaResidualService.cs:272-277`).
- Como la directa de una venta especial crea esa fila, hoy la persona "solo especiales" recibe la cuota.
- Si no recibe, la cuota se guarda con `Habilitado = 0` y `ActivoMes = false`. Ese camino ya existe y es el que se usará.
- Cambio: quitar del conjunto de asesores a los de la lista "solo especiales". La habilitación manual sigue mandando (decisión 2).
- Comprobado en `CuotasVentaResidualRepository.SaveControlProductos`: la cuota se suma a `cuot_pagadas` aunque el asesor no reciba, y la fila de pago en `administracionventapersonal` solo se inserta si `Habilitado = 1`. La cuota no cobrada se pierde y no pasa a otro ciclo, como pide la decisión 6.

## 5. Fases

| Fase | Contenido | Estado |
|---|---|---|
| 0 | Confirmar las decisiones | ✅ Completada (16/11/2026): las 7 confirmadas |
| 1 | Solo lectura: listar personas "solo especiales" y mixtas del ciclo y cuánto cobran hoy por cada bono, incluida la venta residual; listar también las especiales que hoy quedan fuera | ✅ Completada (16/11/2026) |
| 2 | Consulta única que devuelve los contactos "solo especiales" del ciclo | ✅ Completada (16/11/2026) |
| 3 | Incluir todas las ventas especiales y aplicar la regla en red comprimida, residual, grupo, Monte Sion y pago de comisión venta residual | ✅ Completada (16/11/2026), sin probar con datos |
| 4 | Replicar lo mismo en la simulación | ✅ Completada (16/11/2026), sin probar con datos |
| 5 | Pruebas en ambiente controlado, nunca en producción | 🟡 En curso: pruebas de solo lectura pasadas (16/11/2026); falta la corrida completa en una base de pruebas |

### Fase 1: situación de partida (solo lectura)

Hecha el 16/11/2026 con consultas de solo lectura sobre Guardian (`grdsion`) y Conexión. No se escribió nada en ninguna base.

**Ciclos usados.** Solo agosto (147) y septiembre (148) tienen ventas especiales guardadas en Guardian. Octubre (149) es el ciclo en proceso: tiene completado "obtener ventas" y todavía no corrió el paso de ventas especiales.

**Resumen de lo que cobraron las personas "solo especiales"**

| Concepto | Agosto (147) | Septiembre (148) |
|---|---|---|
| Personas "solo especiales" | 12 | 10 |
| Ventas especiales de esas personas | 29 | 31 |
| Comisión directa (la conservan) | 2.221,57 | 3.757,56 |
| Comisión venta residual (la pierden) | 526,98 | 470,52 |
| Comisión de grupo (la pierden) | 7.157,17 | 4.715,31 |
| Bono residual (lo pierden) | 5.704,99 | 3.552,05 |
| **Total que dejarían de cobrar** | **13.389,14** | **8.737,88** |
| Personas que cobraron algo además de la directa | 11 de 12 | 9 de 10 |
| Bono par cobrado | 0,00 | 0,00 |
| Monte Sion, columna `Monto` de `reportesmontesion` | 2.300,00 | 1.500,00 |
| Con habilitación manual | 0 | 0 |

Personas mixtas (no cambian): 7 en agosto y 3 en septiembre.

**Hallazgos**

- Todas las personas "solo especiales" guardadas tienen 2 o más especiales, como exige el filtro actual. En septiembre, 7 de 10 tienen exactamente 2.
- Ninguna persona "solo especiales" cobró bono par en los dos ciclos. El punto abierto del patrocinador "solo especiales" no tuvo efecto en estos datos.
- Ninguna tenía habilitación manual, así que la decisión 2 no habría cambiado el resultado.
- Red comprimida: solo está guardada desde septiembre. En ese ciclo, 6 de las 10 personas "solo especiales" figuran como ganadoras en la red.
- La comisión de grupo que dejan de cobrar no desaparece: con la compresión (decisión 1) sube al siguiente activo. El ahorro real para la empresa hay que medirlo en la fase 5.
- Monte Sion: no se verificó qué representa `Monto` ni si la persona entró a la evaluación por la venta especial. Es un dato de referencia, no un monto que se pierda.
- En Conexión cada venta especial sale como una sola fila; el conteo de "2 o más" es por venta.

**Ventas especiales que el filtro actual deja fuera** (vendedor con una sola especial y ninguna venta normal)

| Mes | Especiales en Conexión | Pasan el filtro | Quedan fuera | Guardadas en Guardian |
|---|---|---|---|---|
| Agosto 2026 | 37 | 36 | 1 | 36 |
| Septiembre 2026 | 42 | 36 | 6 | 37 |
| Octubre 2026 | 39 | 37 | 2 | 0 (paso sin ejecutar) |
| Noviembre 2026, al 16/11 | 4 | 0 | 4 | 0 |

- Septiembre no cuadra con el filtro: 2 de las 6 ventas que quedarían fuera sí están en Guardian (vendedores Larico Blanco Flora Adela y Fernández Larico Adrián Brandon), y una que pasa el filtro no está. No se investigó la causa.
- No se calculó la comisión directa que generarían las ventas que quedan fuera: depende de la base por tipo (`CasosEspecialesService`) y de la moneda del ciclo. Queda para la fase 5.

**Octubre 2026 (ciclo en proceso), según Conexión y las ventas normales ya cargadas**

| Clase | Vendedores | Ventas especiales |
|---|---|---|
| Solo especiales | 13 | 30 |
| Mixta | 7 | 9 |

De los 13 "solo especiales", 2 tienen una sola especial y hoy no entrarían (Franco Tarqui Zoraya y Rojas Martínez Gladys). La clasificación puede cambiar si se adicionan ventas rezagadas.

**Detalle por persona** (montos como están guardados en Guardian)

**Agosto 2026 (ciclo 147) — Personas "solo especiales": 12**

| Persona | Esp. | Norm. | Directa esp. | Directa norm. | Venta residual | Grupo | Residual | Bono par | Monte Sion | Gana en red |
|---|---|---|---|---|---|---|---|---|---|---|
| Choque Serrudo Jhoana Jazmin (65475) | 2 | 0 | 172,86 | 0,00 | 49,33 | 797,97 | 1.297,95 | 0,00 | 300,00 | — |
| Galvis Jose Luis (45585) | 2 | 0 | 103,18 | 0,00 | 27,20 | 327,94 | 170,17 | 0,00 | 200,00 | — |
| Grupo Sion Inversion S.A. (110911) | 6 | 0 | 563,35 | 0,00 | 0,00 | 0,00 | 3,28 | 0,00 | 0,00 | — |
| Honor Rodriguez Fisher (1019) | 2 | 0 | 114,57 | 0,00 | 0,00 | 1.493,80 | 498,48 | 0,00 | 400,00 | — |
| Justiniano Romero Daniela (111219) | 2 | 0 | 151,69 | 0,00 | 0,00 | 0,00 | 0,00 | 0,00 | 0,00 | — |
| Loayza Flores Jose Nery (84) | 2 | 0 | 166,29 | 0,00 | 88,53 | 848,17 | 374,84 | 0,00 | 300,00 | — |
| Olivera Bernal Rosa (12461) | 2 | 0 | 172,73 | 0,00 | 94,86 | 95,73 | 212,00 | 0,00 | 0,00 | — |
| Ortega Duran Caleb Alejandro (47389) | 2 | 0 | 96,61 | 0,00 | 28,57 | 715,32 | 1.290,50 | 0,00 | 300,00 | — |
| Revollo Rodriguez Maribel (4288) | 2 | 0 | 135,34 | 0,00 | 67,78 | 0,00 | 31,56 | 0,00 | 0,00 | — |
| Rojas Flores Ana Nieves Ingrid / Salvatierra Rivero Hugo (831) | 3 | 0 | 254,65 | 0,00 | 36,18 | 2.863,98 | 1.653,79 | 0,00 | 800,00 | — |
| Verduguez Montoya Alfredo (601) | 2 | 0 | 110,74 | 0,00 | 19,20 | 14,26 | 127,54 | 0,00 | 0,00 | — |
| Villca Singuri Giovanna (106064) | 2 | 0 | 179,56 | 0,00 | 115,33 | 0,00 | 44,88 | 0,00 | 0,00 | — |
| **Total** | **29** | **0** | **2.221,57** | **0,00** | **526,98** | **7.157,17** | **5.704,99** | **0,00** | **2.300,00** | |

**Agosto 2026 (ciclo 147) — Personas mixtas: 7**

| Persona | Esp. | Norm. | Directa esp. | Directa norm. | Venta residual | Grupo | Residual | Bono par | Monte Sion | Gana en red |
|---|---|---|---|---|---|---|---|---|---|---|
| Altamirano Alcon Silvia Amparo (50452) | 1 | 1 | 57,62 | 222,30 | 0,00 | 197,68 | 69,50 | 0,00 | 0,00 | — |
| Garfias Cuevas Christian Javier (114772) | 1 | 1 | 89,78 | 452,10 | 0,00 | 0,00 | 0,00 | 0,00 | 0,00 | — |
| Huarahuara Botello Victor Hugo (79039) | 1 | 1 | 56,28 | 76,04 | 56,22 | 635,28 | 140,94 | 700,00 | 300,00 | — |
| Montoya García Angello David (110689) | 1 | 1 | 124,62 | 262,03 | 41,38 | 133,77 | 52,12 | 200,00 | 0,00 | — |
| Quimbert Montes Diego Enrique (17406) | 1 | 1 | 105,86 | 90,90 | 16,67 | 1.534,23 | 1.010,11 | 200,00 | 0,00 | — |
| Romero Cutily Agar Ninfa (31516) | 1 | 1 | 251,25 | 117,90 | 16,40 | 1.237,17 | 631,46 | 200,00 | 0,00 | — |
| Uriona Ayllon De Tapia Agrelin (18504) | 1 | 1 | 46,90 | 115,20 | 60,00 | 76,30 | 276,84 | 0,00 | 0,00 | — |
| **Total** | **7** | **7** | **732,31** | **1.336,47** | **190,67** | **3.814,43** | **2.180,97** | **1.300,00** | **300,00** | |

**Septiembre 2026 (ciclo 148) — Personas "solo especiales": 10**

| Persona | Esp. | Norm. | Directa esp. | Directa norm. | Venta residual | Grupo | Residual | Bono par | Monte Sion | Gana en red |
|---|---|---|---|---|---|---|---|---|---|---|
| Choque Serrudo Jhoana Jazmin (65475) | 2 | 0 | 182,24 | 0,00 | 35,50 | 212,40 | 576,72 | 0,00 | 0,00 | Sí |
| Grupo Sion Inversion S.A. (110911) | 10 | 0 | 1.668,34 | 0,00 | 0,00 | 0,00 | 0,00 | 0,00 | 0,00 | No |
| Honor Rodriguez Fisher (1019) | 2 | 0 | 145,27 | 0,00 | 0,00 | 1.373,86 | 608,25 | 0,00 | 400,00 | Sí |
| Huarahuara Botello Victor Hugo (79039) | 2 | 0 | 142,04 | 0,00 | 20,67 | 588,08 | 152,08 | 0,00 | 300,00 | Sí |
| Larico Blanco Flora Adela (113441) | 2 | 0 | 511,98 | 0,00 | 295,01 | 0,00 | 138,43 | 0,00 | 0,00 | No |
| Paniagua Llave Yerko  (92279) | 2 | 0 | 119,26 | 0,00 | 0,00 | 0,00 | 90,05 | 0,00 | 0,00 | No |
| Rodriguez Zambrana Grissel Adriana (96058) | 4 | 0 | 377,21 | 0,00 | 0,00 | 0,00 | 109,20 | 0,00 | 0,00 | No |
| Rojas Flores Ana Nieves Ingrid / Salvatierra Rivero Hugo (831) | 2 | 0 | 166,83 | 0,00 | 36,18 | 1.705,99 | 1.486,61 | 0,00 | 400,00 | Sí |
| Sea Villalba Jael Nathalie (110989) | 3 | 0 | 312,89 | 0,00 | 23,16 | 393,35 | 23,77 | 0,00 | 200,00 | Sí |
| Uriona Ayllon De Tapia Agrelin (18504) | 2 | 0 | 131,50 | 0,00 | 60,00 | 441,63 | 366,94 | 0,00 | 200,00 | Sí |
| **Total** | **31** | **0** | **3.757,56** | **0,00** | **470,52** | **4.715,31** | **3.552,05** | **0,00** | **1.500,00** | |

**Septiembre 2026 (ciclo 148) — Personas mixtas: 3**

| Persona | Esp. | Norm. | Directa esp. | Directa norm. | Venta residual | Grupo | Residual | Bono par | Monte Sion | Gana en red |
|---|---|---|---|---|---|---|---|---|---|---|
| Eguez Siles Carlos Andres (103757) | 1 | 1 | 47,49 | 160,69 | 59,96 | 45,19 | 46,64 | 0,00 | 0,00 | Sí |
| Jimenez Alvarado Fabiola Alejandra (1025) | 4 | 2 | 279,89 | 170,72 | 70,62 | 194,70 | 66,94 | 0,00 | 0,00 | Sí |
| Tavera Chavez Sara Cristina (112369) | 1 | 2 | 54,31 | 180,78 | 19,20 | 0,00 | 47,75 | 0,00 | 0,00 | No |
| **Total** | **6** | **5** | **381,69** | **512,19** | **149,78** | **239,89** | **161,33** | **0,00** | **0,00** | |

"Gana en red" indica si la persona figura como ganadora en `red_comprimida` del ciclo; agosto no está guardado.

### Fase 2: identificar a las personas "solo especiales"

Hecha el 16/11/2026. Solo agrega código; ningún cálculo lo llama todavía, así que el proceso se comporta igual que antes.

| Pieza | Dónde | Qué hace |
|---|---|---|
| `GetContactosSoloEspeciales(LogTransaccionId, inicio, fin)` | `IAdministracionContratoRepository` y `AdministracionContratoRepository` | Devuelve los asesores cuyos contratos del período son todos de tipo 6 a 9 |
| `HabilitacionComisionHelper.GetContactosSoloEspeciales(contactos, habilitaciones)` | `src/Domain/Entities/HabilitacionComisionHelper.cs` | Quita de esa lista a quien tiene habilitación manual que genera comisiones (decisión 2) |

- Quien tiene además una venta tipo 1 a 5 no sale en la lista (decisión 4).
- Una lista vacía es un resultado válido. `Success` solo es `false` si la consulta falla; en ese caso cada cálculo de la fase 3 debe cancelar su paso y no seguir como si no hubiera nadie.
- Uso previsto en la fase 3: pedir la lista con las fechas del ciclo, pasarla por el helper con las habilitaciones del ciclo y usar el conjunto resultante en cada cálculo.

Comprobación:

- La solución compila sin errores.
- La consulta, ejecutada en solo lectura, devuelve las mismas personas que la fase 1: 12 en agosto y 10 en septiembre, con los mismos identificadores. En octubre devuelve cero porque el paso de ventas especiales aún no corrió.
- La comprobación se hizo con los tipos escritos a mano (`IN (6,7,8,9)`). El método no se ejecutó desde la API; la expansión de la lista por Dapper es la misma que ya usa `CasosEspecialesRepository`.

### Fase 3: aplicar la regla

Hecha el 16/11/2026. Compila y la API arranca con el nuevo servicio; no se ejecutó ningún paso del proceso ni se probó con datos (eso es la fase 5).

**Fuente única.** `IContactosSoloEspecialesService` (`src/Api/Services/ContactosSoloEspecialesService.cs`) devuelve, para un ciclo, los contactos "solo especiales" ya sin los que tienen habilitación manual. Todos los cálculos lo usan. Si falla, el cálculo se detiene y, donde hay control de pasos, el paso se cancela.

| Cálculo | Cambio hecho | Archivo |
|---|---|---|
| Entrada de ventas especiales | Se quitó el filtro por vendedor y la consulta de ventas normales que lo alimentaba; devuelve todas las especiales del período (decisión 7) | `CasosEspecialesRepository.GetVentasCasosEspeciales` |
| Red comprimida | Los "solo especiales" se quitan de los activos por venta; sus niveles suben al siguiente activo (decisión 1) | `RedesService.ObtenerRedComprimidaAsync` |
| Comisión de grupo | Sin cambio de código: los ganadores salen de la red comprimida, así que al no estar en la red no ganan grupo | — |
| Bono residual | Se quitan de los activos del residual | `BonoResidualService.ConstruirListadoResidualAsync` |
| Monte Sion | Se quitan de los evaluables | `MonteSionService.CalcularRangosAsync` |
| Comisión venta residual (pago mensual) | Se quitan del conjunto de asesores que reciben la cuota; la cuota queda no habilitada y se pierde (decisión 6) | `CuotasVentaResidualService.ConstruirListadoAsync` |
| Comisión venta residual (cuotas nuevas) | Sin cambio: las especiales ya no generan cuotas | — |
| Bono par | Sin cambio: las especiales ya no cuentan (decisión 3) | — |

**Vigencia (decisión 5).** Nueva clave `SoloEspecialesDesdeCicloId` en `appsettings.json` y en el ejemplo, con valor 149 (Octubre 2026, el ciclo en proceso). Para ciclos anteriores el servicio devuelve una lista vacía y todo se calcula como antes. Hace falta porque Monte Sion y las vistas previas recalculan en línea cualquier ciclo que se consulte. Si la clave no existe, el código usa 149.

**Comportamientos que se conservaron y conviene conocer**

- Los contactos de `HabilidacionesParaNoComprimirRed` siguen entrando a la red comprimida y al residual aunque sean "solo especiales". Uno de ellos, Grupo Sion Inversión S.A. (110911), fue "solo especiales" en agosto y septiembre.
- Las habilitaciones del ciclo siguen agregando a la persona como activa en la red, igual que antes.
- La pantalla de casos especiales no cambia: ya enviaba todo lo que recibía.
- La simulación no usa ninguno de estos servicios; su parte se hizo en la fase 4.

**Punto abierto.** El patrocinador "solo especiales" sigue cobrando el bono par por las ventas normales de sus directos. No se cambió.

### Fase 4: simulación

Hecha el 16/11/2026. Compila; no se ejecutó ninguna corrida de simulación.

**Qué necesitaba cambio y qué no**

| Paso de la simulación | ¿Cambio? | Motivo |
|---|---|---|
| Obtener ventas | No | La simulación no carga ventas especiales (solo las normales del ciclo); es su diseño y no se tocó |
| Comisión directa | No | Sin ventas especiales en el ciclo simulado, no hay directa de especiales |
| Red comprimida | **Sí** | Sus activos salen de los contratos de los últimos 3 meses, que sí incluyen especiales de meses anteriores |
| Comisión de grupo | No | Sale de la red comprimida |
| Bono residual | No | Sus activos son los de venta personal del ciclo simulado, donde no hay especiales |
| Bono par | No | Las especiales ya no cuentan |
| Pago de venta residual y Monte Sion (rangos) | No | La simulación no ejecuta esos pasos |

**Cambio hecho.** `SimulacionService.RedComprimidaAsync` quita de los activos a quien solo tiene ventas especiales en la misma ventana de 3 meses, salvo habilitación manual. Usa el mismo servicio único, con el nuevo método `ObtenerPorPeriodoAsync`, y respeta la vigencia desde el ciclo 149.

**Diferencia con el proceso real.** En el proceso real la regla mira solo el ciclo; en la simulación mira la ventana de 3 meses, porque ahí es donde la simulación decide quién está activo. Una persona con una venta normal en cualquiera de los 3 meses sigue activa en la simulación.

**Efecto esperado en octubre** (consulta de solo lectura, ventana del 01/08 al 31/10): 9 de 325 vendedores con contratos quedan como "solo especiales". Uno de ellos, 110911, vuelve a entrar por `HabilidacionesParaNoComprimirRed`.

### Fase 5: pruebas (ambiente controlado, nunca producción)

#### Parte A: pruebas de solo lectura (hecha el 16/11/2026)

No hay todavía una base de pruebas indicada, así que se probó sin escribir nada: un programa de prueba ejecuta los servicios reales contra los datos de septiembre 2026 (ciclo 148), con la vigencia adelantada a ese ciclo solo dentro de la prueba. Los repositorios reales quedan envueltos: solo pasan los métodos de lectura y el guardado de la red se captura en memoria.

Resultado: **20 comprobaciones, todas pasaron.**

| Qué se probó | Cómo | Resultado |
|---|---|---|
| Lista única | Servicio real | 10 personas, las mismas de la fase 1; ninguna mixta ni normal. Con vigencia 149, el ciclo 148 devuelve lista vacía |
| Red comprimida sin la regla | `RedesService` real | Reproduce exactamente la red guardada del ciclo: 897 de 897 filas |
| Red comprimida con la regla (G2, G4) | `RedesService` real | 829 filas. Los 9 afectados no figuran ni como ganadores ni como vendedores activos; las 3 mixtas siguen igual; los demás vendedores no cambian; los niveles suben al siguiente activo en 54 vendedores |
| Comisión de grupo (G1, G3) | Fórmula de la consulta, en memoria, con cada red | Los afectados pasan de 4.715,22 a 0,00. 18 ganadores cobran más por la compresión, 4.441,63 en total. Nadie más cobra menos |
| Monte Sion (G1, G3) | `MonteSionService` real | Califican 127 sin la regla y 119 con ella; salen los 8 "solo especiales" que calificaban; rango y producción de los demás no cambian |
| Entrada de especiales (G5) | `CasosEspecialesRepository` real, lectura de Conexión | Septiembre: 42 ventas de 18 vendedores, 8 con una sola. Octubre: 39 ventas de 20 vendedores, 7 con una sola. Antes esas no entraban |
| Pago de venta residual (G1, G3, decisiones 2 y 6) | Método real con datos de prueba en memoria | La "solo especiales" no recibe la cuota; la mixta sí; la habilitada manualmente sí; la cuota no cobrada se contabiliza igual |

Lecturas de estos números:

- En grupo, casi todo lo que dejan de cobrar los "solo especiales" pasa a otros ganadores: de 4.715,22 la empresa deja de pagar 273,59.
- 1 de las 10 personas "solo especiales" (110911) no se ve afectada porque está en `HabilidacionesParaNoComprimirRed`.
- El total de grupo recalculado (32.392,20) es mayor que el guardado en el ciclo (28.139,28) porque la prueba no aplica los bloqueos por habilitación ni el filtro de guardado. Sirve para comparar con y sin la regla, no como monto a pagar.
- La prueba de Monte Sion usa la red guardada del ciclo, sin la regla. En una corrida real la red cambia y la producción de otros puede cambiar por la compresión.

Lo que esta parte no cubre:

- El bono residual con datos reales (sus tablas temporales tienen el ciclo en curso, no septiembre). El cambio es un filtro de una línea, igual al de la red.
- El pago de venta residual con datos reales.
- La simulación: no se ejecutó ninguna corrida.
- El guardado en la base, el control de pasos y la cancelación del paso cuando la lista falla.
- Un caso real de "solo especiales" con habilitación manual (no hubo ninguno en agosto ni septiembre).

El programa de prueba está en `/private/tmp/claude-503/-Users-rolandomedina-Desktop-ProyectoGuardian/dfe3426e-a85a-401c-9aa8-37dbb5506d02/scratchpad/prueba`. Es una carpeta temporal de la sesión; si se quiere conservar hay que copiarlo.

#### Parte B: corrida completa en una base de pruebas (pendiente)

Falta que se indique qué base usar. La configurada en `appsettings.json` tiene el ciclo de octubre en proceso y no se usa para esto.

- Una persona "solo especiales", una mixta y una normal.
- Una de ellas con cuotas de venta residual pendientes.
- Una persona "solo especiales" con habilitación manual.
- Un vendedor con una sola venta especial y ninguna normal: debe entrar al proceso y cobrar solo la directa.
- Una persona con una venta especial y otra por intercambio: no es "solo especiales" y queda como hoy.
- Un patrocinador cuyo directo solo tiene ventas especiales: esas ventas no suman al bono par.
- Comparar contra el "antes" de la fase 1.
- Revisar una por una las garantías G1 a G6 de la sección 1; la fase no se cierra si alguna falla.

## 6. Seguimiento

| Fecha | Avance |
|---|---|
| 13/11/2026 | Se definió la regla y se añadió la comisión venta residual. Decisiones 1, 2, 5 y 6 confirmadas; la 3 en espera; la 4 pendiente. Sin cambios en el código |
| 16/11/2026 | Se revisó el filtro de entrada de ventas especiales y se confirmó la decisión 7: entran todas. Añadida a las fases 1, 3 y 5. Se añadieron las garantías G1 a G5 (solo especiales: solo directa y fuera de la red comprimida; mixta: todos los bonos y dentro de la red). Sin cambios en el código |
| 16/11/2026 | Fase 0 completada. Decisión 3: las ventas especiales no entran al bono par (ya es así, sin cambio de código). Decisión 4: los tipos 3, 4 y 5 siguen como están. Decisión 6 ampliada y analizado el pago mensual de la venta residual (`CuotasVentaResidualService`). Añadida la garantía G6. Sin cambios en el código |
| 16/11/2026 | Fase 1 completada con consultas de solo lectura: "antes" de agosto y septiembre por persona, ventas que el filtro deja fuera y clasificación de octubre. Sin cambios en el código ni en las bases |
| 16/11/2026 | Fase 2 completada: agregados `GetContactosSoloEspeciales` en el repositorio de contratos y en `HabilitacionComisionHelper`. Compila; la consulta coincide con la fase 1 (12 y 10 personas). Ningún cálculo la usa aún |
| 16/11/2026 | Fase 3 completada: servicio único `IContactosSoloEspecialesService`, filtro de entrada eliminado y regla aplicada en red comprimida, residual, Monte Sion y pago de venta residual. Vigencia desde el ciclo 149 por la clave `SoloEspecialesDesdeCicloId`. Compila y arranca; sin probar con datos |
| 16/11/2026 | Fase 4 completada: la red comprimida de la simulación aplica la regla sobre su ventana de 3 meses, con el mismo servicio único (`ObtenerPorPeriodoAsync`). Compila; sin corrida de prueba |
| 16/11/2026 | Fase 5, parte A: 20 comprobaciones de solo lectura sobre septiembre con los servicios reales, todas pasaron. Pendiente la corrida completa en una base de pruebas |
