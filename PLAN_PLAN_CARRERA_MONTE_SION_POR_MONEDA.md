# Plan: plan de carrera Monte Sion por moneda del ciclo

## Objetivo

Que el cálculo de rangos y bonos de Monte Sion (`MonteSionService`) use los **umbrales, VME, bonos e incentivos que corresponden a la moneda del ciclo** (`administracionciclo.moneda` = `SUS` o `BS`), leyéndolos **desde base de datos**, en lugar de:

- los montos en dólares fijos de `administracionnivel` (`ddesde`, `dhasta`, `dbono`, `VME`), y
- el texto de incentivos que hoy vive en `appsettings.json` → `MonteSion:Rangos`.

Resultado esperado:

| Fuente | Rol después del cambio |
|---|---|
| `administracionnivel` | **Catálogo de rangos.** De aquí salen el **nombre del rango** (`snombre`), el id, la sigla, el orden y el `% liderazgo` (`dbonomembresia`). Sus montos en USD quedan como referencia histórica y **no** los usa Monte Sion. |
| Nueva tabla `administracionnivelplancarrera` | **Fuente única**, por rango y por moneda, de: producción **desde** y **hasta**, **VME** (%), **bono por rango**, **incentivo (pago único)** y **producción con corte de VME** (tope por equipo). |
| Captura `plancarrera.png` (raíz del proyecto) | Documento de negocio. Sus valores se cargan en la tabla nueva como configuración `BS`. |
| `appsettings.json` → `MonteSion:Rangos` | Se elimina. Solo queda `MonteSion:ProfundidadMaxima`. |

---

## 1. Diagnóstico actual

### 1.1 Por qué hoy solo funciona en dólares

1. `VentaCnxRepository.GetVentaCnx` convierte los precios según la moneda del ciclo (ver `PLAN_TIPO_CAMBIO_POR_MONEDA_CICLO.md`):
   - Ciclo `SUS`: `administracioncontrato.dprecio` queda en **USD**.
   - Ciclo `BS`: `dprecio` queda en **Bs** (proyectos USD × 6,97).
2. `MonteSionRepository.ObtenerDatosCicloAsync` suma `dprecio` de la red comprimida **sin mirar la moneda**.
3. `MonteSionService.CalcularRangosAsync` compara esa producción con `administracionnivel.ddesde` y aplica `VME`, que están **en USD**.
4. En un ciclo `BS` la producción queda ~7 veces "inflada" frente a los umbrales. Todos califican a rangos mucho más altos de los que les corresponden y se les asignan bonos en USD.

### 1.2 Configuración actual en base de datos (`administracionnivel`, USD)

Valores tomados de los logs de `AdministracionNivelRepository.GetNivel()` (05/11/2026):

| Id | Rango | Desde (USD) | Hasta (USD) | Bono (USD) | VME (monto USD) | VME implícito | % Liderazgo |
|---:|---|---:|---:|---:|---:|---:|---:|
| 0 | ASESOR COMERCIAL | 0 | 0 | 0 | 0 | — | 0,00 |
| 1 | EMPRENDEDOR | 0 | 0 | 0 | 0 | — | 0,00 |
| 2 | LEADER | 5.000 | 9.999 | 0 | 0 | — | 0,00 |
| 3 | SENIOR | 10.000 | 24.999 | 0 | 0 | — | 0,10 |
| 4 | SAPPHIRE | 25.000 | 49.999 | 100 | 0 | — | 0,15 |
| 5 | RUBY | 50.000 | 99.999 | 200 | 0 | — | 0,25 |
| 6 | EMERALD | 100.000 | 199.999 | 300 | 80.000 | 80 % | 0,30 |
| 7 | DIAMOND | 200.000 | 399.999 | 400 | 140.000 | 70 % | 0,35 |
| 8 | REGIONAL AMBASSADOR | 400.000 | 799.999 | 600 | 220.000 | 55 % | 0,40 |
| 9 | NATIONAL AMBASSADOR | 800.000 | 1.499.999 | 800 | 400.000 | 50 % | 0,45 |
| 10 | INTERNATIONAL AMBASSADOR | 1.500.000 | 2.499.999 | 1.000 | 600.000 | 40 % | 0,50 |
| 11 | FUNDADOR | 2.500.000 | 3.999.999 | 2.000 | 625.000 | 25 % | 0,55 |
| 12 | FUNDADOR ELITE | 4.000.000 | 4.999.999 | 3.000 | 800.000 | 20 % | 0,56 |
| 13 | FUNDADOR IMPERIAL | 5.000.000 | 24.999.999 | 4.000 | 750.000 | 15 % | 0,58 |
| 14 | FUNDADOR ROYAL | 25.000.000 | 100.000.000 | 5.000 | 2.500.000 | 10 % | 0,60 |

> Antes de migrar hay que volver a leer la tabla directamente en la base. Estos valores salen de los logs, no de una consulta en vivo.

En `appsettings.json` → `MonteSion:Rangos`, el cálculo solo usa el incentivo (`Incentivo`) de cada rango; los campos `ProduccionRequerida`, `BonoUsd` y `VmePorcentaje` **no se usan**. Los incentivos en USD configurados son: Esmeralda 500, Diamante 1.000, Embajador Regional 2.500, Embajador Nacional 10.000, Embajador Internacional 20.000, Fundador 40.000, Fundador Elite 60.000, Fundador Imperial 80.000 y Fundador Royal 100.000. Equivalen exactamente a los de la captura en Bs ÷ 7. Ojo: el `appsettings.json.example` solo trae Esmeralda y Diamante, así que está desactualizado.

### 1.3 Plan de carrera en bolivianos (captura `plancarrera.png`)

| Rango | Producción mensual (Bs) | VME | Bono por rango (Bs) | Incentivo pago único (Bs) |
|---|---:|---:|---:|---:|
| Líder | 35.000 | — | — | — |
| Senior | 70.000 | — | — | — |
| Safiro | 175.000 | — | 700 | — |
| Rubí | 350.000 | — | 1.400 | — |
| Esmeralda | 700.000 | 80 % | 2.100 | 3.500 |
| Diamante | 1.400.000 | 70 % | 2.800 | 7.000 |
| Embajador Regional | 2.800.000 | 55 % | 4.200 | 17.500 |
| Embajador Nacional | 5.600.000 | 50 % | 5.600 | 70.000 |
| Embajador Internacional | 10.500.000 | 40 % | 7.000 | 140.000 |
| Fundador | 17.500.000 | 25 % | 14.000 | 280.000 |
| Fundador Elite | 28.000.000 | 20 % | 21.000 | 420.000 |
| Fundador Imperial | 35.000.000 | 15 % | 28.000 | 560.000 |
| Fundador Royal | 175.000.000 | 10 % | 35.000 | 700.000 |

### 1.4 Comparación

- **Producción y bono:** la tabla en Bs es exactamente la de USD **× 7** en todos los rangos.
- **VME:** la captura lo expresa como **porcentaje**. En la base está como **monto**, pero en todos los casos el monto es igual a `ddesde × %` de la captura. Por eso conviene guardar el porcentaje y calcular el monto.
- **Incentivos:** la captura define incentivos **desde Esmeralda hasta Fundador Royal**. En USD están todos configurados en `appsettings.json` (ver 1.2); en Bs no hay ninguno configurado.
- **Tipo de cambio:** no interviene. Cada moneda tiene su propia tabla de valores y no se convierte de una a otra.
- **"Hasta":** la captura no tiene columna "hasta". Se toma como el siguiente "desde" menos 1, y el último rango queda abierto. Hoy "hasta" solo se usa para mostrar el "rango potencial".

### 1.5 Correspondencia de nombres (captura → `administracionnivel`)

La relación se hace por `lnivel_id`, no por nombre. Los nombres de la base no se cambian.

| Captura | `administracionnivel.snombre` | `lnivel_id` |
|---|---|---:|
| Líder | LEADER | 2 |
| Senior | SENIOR | 3 |
| Safiro | SAPPHIRE | 4 |
| Rubí | RUBY | 5 |
| Esmeralda | EMERALD | 6 |
| Diamante | DIAMOND | 7 |
| Embajador Regional | REGIONAL AMBASSADOR | 8 |
| Embajador Nacional | NATIONAL AMBASSADOR | 9 |
| Embajador Internacional | INTERNATIONAL AMBASSADOR | 10 |
| Fundador | FUNDADOR | 11 |
| Fundador Elite | FUNDADOR ELITE | 12 |
| Fundador Imperial | FUNDADOR IMPERIAL | 13 |
| Fundador Royal | FUNDADOR ROYAL | 14 |

---

## 2. Diseño propuesto

### 2.1 Nueva tabla `administracionnivelplancarrera` (MySQL)

```sql
CREATE TABLE administracionnivelplancarrera (
    lnivelplancarrera_id  INT            NOT NULL AUTO_INCREMENT,
    lnivel_id             INT            NOT NULL,          -- FK administracionnivel.lnivel_id
    moneda                ENUM('BS','SUS') NOT NULL,        -- mismo dominio que administracionciclo.moneda
    dproducciondesde      DECIMAL(18,2)  NOT NULL DEFAULT 0,
    dproduccionhasta      DECIMAL(18,2)  NOT NULL DEFAULT 0, -- 0 = sin tope
    dvmeporcentaje        DECIMAL(5,2)   NOT NULL DEFAULT 0, -- VME en %; 0 = sin VME
    dproduccioncortevme   DECIMAL(18,2)  NOT NULL DEFAULT 0, -- producción con corte de VME (tope por equipo); 0 = sin corte
    dbono                 DECIMAL(18,2)  NOT NULL DEFAULT 0, -- bono por rango
    dincentivo            DECIMAL(18,2)  NOT NULL DEFAULT 0, -- incentivo (pago único); 0 = sin incentivo
    estado                TINYINT        NOT NULL DEFAULT 1,
    activo                TINYINT        NULL     DEFAULT 1, -- 1 vigente / NULL histórico
    susuarioadd           VARCHAR(50)    NOT NULL,
    dtfechaadd            DATETIME       NOT NULL,
    susuariomod           VARCHAR(50)    NOT NULL,
    dtfechamod            DATETIME       NOT NULL,
    PRIMARY KEY (lnivelplancarrera_id),
    UNIQUE KEY uq_nivelplancarrera_activo (lnivel_id, moneda, activo),
    KEY ix_nivelplancarrera_moneda (moneda, estado)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
```

Notas:
- El charset se declara explícitamente. Si la tabla toma el default de la base (`latin1_swedish_ci`), al comparar `moneda` con los literales `'SUS'`/`'BS'` de la conexión (`utf8mb4`) MySQL devuelve el error 1267 "Illegal mix of collations".
- La tabla **no guarda el nombre del rango**: siempre se obtiene de `administracionnivel.snombre` con un `JOIN` por `lnivel_id`.
- Columnas que corresponden a la captura: Producción mensual → `dproducciondesde` (y `dproduccionhasta`), VME → `dvmeporcentaje`, Bono por rango → `dbono`, Incentivo (pago único) → `dincentivo`. A eso se suma `dproduccioncortevme`, que es el monto que se usa en el cálculo.
- **`dproduccioncortevme` es el valor que aplica el cálculo.** `dvmeporcentaje` es informativo: sirve para mostrar el % y para validar al registrar. Al guardar desde el CRUD, si el corte llega vacío se propone `dproducciondesde × dvmeporcentaje / 100`, pero se puede editar.
- El id es `AUTO_INCREMENT`, igual que `montesion_arbol` (la tabla más reciente del módulo).
- Para modificar una configuración se inactiva la fila anterior (`estado = 0`) y se inserta una nueva. La columna `activo` vale 1 en la fila vigente y `NULL` en las históricas. Como MySQL permite varios `NULL` en una clave única, la clave garantiza **una sola fila vigente por rango y moneda** y deja guardar varias filas históricas. `activo` es una columna normal, no generada, porque el servidor no soporta `GENERATED ALWAYS` (error 1064). Por eso, al inactivar una fila hay que actualizar las dos columnas: `SET estado = 0, activo = NULL`. La verificación 3.4 del script lo controla.
- Opcional (decisión D4): agregar `dtvigenciadesde` para que un cambio de plan aplique desde un ciclo determinado y los ciclos anteriores se puedan recalcular con su plan original.

### 2.2 Datos iniciales (script en `src/query/plan_carrera_monte_sion.sql`, **sin ejecución automática**)

- **`SUS`:** copiar `ddesde` → `dproducciondesde`, `dhasta` → `dproduccionhasta`, `dbono` → `dbono` y `VME` → `dproduccioncortevme` desde `administracionnivel`. `dvmeporcentaje = VME * 100 / ddesde` (redondeado: 80, 70, 55, 50, 40, 25, 20, 15, 10). Incentivos tomados de `appsettings.json` → `MonteSion:Rangos` (`IncentivoUsd`), asociados por `lnivel_id`: 6 → 500, 7 → 1.000, 8 → 2.500, 9 → 10.000, 10 → 20.000, 11 → 40.000, 12 → 60.000, 13 → 80.000, 14 → 100.000. Si la tabla ya se cargó con la versión anterior del script (solo 6 y 7), se corrige con `src/query/plan_carrera_monte_sion_actualizar_incentivos_sus.sql`.
- **`BS`:** valores de la tabla 1.3, asociados por `lnivel_id` según la tabla 1.5. `dproduccionhasta` = siguiente desde − 1. Fundador Royal = 0 (sin tope). `dproduccioncortevme = desde × VME %`:

| `lnivel_id` | Rango (de `administracionnivel`) | Desde (Bs) | Hasta (Bs) | VME % | Producción con corte VME (Bs) | Bono (Bs) | Incentivo (Bs) |
|---:|---|---:|---:|---:|---:|---:|---:|
| 2 | LEADER | 35.000 | 69.999 | 0 | 0 | 0 | 0 |
| 3 | SENIOR | 70.000 | 174.999 | 0 | 0 | 0 | 0 |
| 4 | SAPPHIRE | 175.000 | 349.999 | 0 | 0 | 700 | 0 |
| 5 | RUBY | 350.000 | 699.999 | 0 | 0 | 1.400 | 0 |
| 6 | EMERALD | 700.000 | 1.399.999 | 80 | 560.000 | 2.100 | 3.500 |
| 7 | DIAMOND | 1.400.000 | 2.799.999 | 70 | 980.000 | 2.800 | 7.000 |
| 8 | REGIONAL AMBASSADOR | 2.800.000 | 5.599.999 | 55 | 1.540.000 | 4.200 | 17.500 |
| 9 | NATIONAL AMBASSADOR | 5.600.000 | 10.499.999 | 50 | 2.800.000 | 5.600 | 70.000 |
| 10 | INTERNATIONAL AMBASSADOR | 10.500.000 | 17.499.999 | 40 | 4.200.000 | 7.000 | 140.000 |
| 11 | FUNDADOR | 17.500.000 | 27.999.999 | 25 | 4.375.000 | 14.000 | 280.000 |
| 12 | FUNDADOR ELITE | 28.000.000 | 34.999.999 | 20 | 5.600.000 | 21.000 | 420.000 |
| 13 | FUNDADOR IMPERIAL | 35.000.000 | 174.999.999 | 15 | 5.250.000 | 28.000 | 560.000 |
| 14 | FUNDADOR ROYAL | 175.000.000 | 0 (sin tope) | 10 | 17.500.000 | 35.000 | 700.000 |

  Los cortes en Bs equivalen exactamente al `VME` en USD × 7 (por ejemplo, 80.000 × 7 = 560.000), así que son coherentes con la configuración actual.
- **Emprendedor (1) y Asesor Comercial (0):** se cargan en ambas monedas con todo en 0. El cálculo requiere que exista el rango Emprendedor.

### 2.3 Backend

| Archivo | Cambio |
|---|---|
| `Domain/DTO/MonteSionRangoDto.cs` | `MonteSionRangoConfiguracion`: `BonoUsd` → `Bono`, `IncentivoUsd` → `Incentivo` (decimal), `VmeMonto` → `ProduccionCorteVme`, y se agregan `VmePorcentaje` y `Moneda`. `Nombre` sigue viniendo de `administracionnivel.snombre`. `MonteSionOpciones`: se quita `Rangos`. `MonteSionBeneficioRango`: `BonoConfigurado`, `BonoAPagar`, `IncentivoAPagar` (monto) y `Moneda`. |
| `Application/Interface/IMonteSionRepository.cs` | `ObtenerNivelesAsync(logId, MonedaCiclo moneda)` |
| `Infrastructure/Repositories/MonteSionRepository.cs` | `ObtenerNivelesAsync` hace un `JOIN` de `administracionnivel` (nombre, % liderazgo) con `administracionnivelplancarrera` filtrando `moneda = @Moneda AND estado = 1`. Si un nivel con `lnivel_id > 0` no tiene configuración para esa moneda, devuelve error. `ObtenerDatosCicloAsync` también devuelve la moneda del ciclo. |
| `Api/Services/MonteSionService.cs` | 1) Leer el ciclo y validar la moneda con `AdministracionCicloMonedaExtensions.ValidarMoneda`; si no es válida, responder con error. 2) Cargar los niveles de esa moneda. 3) `EvaluarRango`: `limite = dproduccioncortevme > 0 ? dproduccioncortevme : null`. Es la misma lógica de hoy, pero con el tope leído de la tabla nueva y en la moneda del ciclo. 4) `CalcularBeneficio`: usar el monto `dincentivo > 0` en lugar del texto. El texto se arma como `"Pago único de {monto} {Bs/$us}"`. 5) Eliminar `incentivosPorRango` y la lectura de `_opciones.Rangos`. |
| `Infrastructure/Service/xls/MonteSion.xlsx.cs` | Encabezados "Bono (Bs/$us)" e "Incentivo (Bs/$us)" según la moneda. La columna de incentivo pasa a ser numérica. |
| `MonteSionRepository.GuardarRangosAsync` | Sin cambios de estructura. `Monto_Pagar` guarda el bono en la moneda del ciclo. La moneda se deduce del ciclo, así que no hace falta una columna nueva. El registro del incentivo pagado queda fuera de este alcance. |
| `appsettings.json` / `.example` | Quitar `MonteSion:Rangos` y dejar `ProfundidadMaxima`. |
| Nuevo CRUD (opcional, fase 4) | `AdministracionNivelPlanCarreraController/Service/Repository`: `GET /` (por moneda), `POST /insert`, `PUT /update` (inactiva y crea), `DELETE /delete` (inactiva). |

Comportamiento que se mantiene: el orden de evaluación del rango más alto al más bajo, la regla de Emprendedor de mayo de 2026, los equipos, la profundidad de 7 niveles y el control de que un ciclo no se guarde dos veces.

### 2.4 Frontend (`FrontGuardian/src/views/MonteSion/MonteSion.js`)

- Reemplazar `formatUsd` por `formatMoneda(valor, moneda)` (`Bs` / `$us`), tomando la moneda de la respuesta.
- Usar los campos nuevos `bonoAPagar`, `bonoConfigurado` e `incentivoAPagar`.
- Mostrar el VME como `%` y como límite calculado.
- Opcional (fase 4): pantalla en `views/parametros` para mantener el plan de carrera por moneda.

---

## 3. Fases

### Resumen

| Fase | Contenido | Depende de | Estado |
|---|---|---|---|
| 0 | Confirmación de decisiones | — | ✅ Cerrada (D4 descartada) |
| 1 | Base de datos: tabla nueva y carga inicial | 0 | ✅ Cerrada |
| 2 | Backend: cálculo Monte Sion por moneda | 1 | ✅ Cerrada |
| 3 | Frontend: vista Monte Sion por moneda | 2 | ✅ Cerrada |
| 4 (opcional) | Mantenimiento del plan de carrera (CRUD de API y pantalla) | 1 | ⛔ Cerrada sin implementar (fuera de alcance) |
| 5 | Pruebas | 2, 3 | ✅ Cerrada (pruebas parciales, ver cierre) |

### Cierre del plan (11/11/2026)

El usuario cerró todas las fases el 11/11/2026.

**Entregado:**
- Tabla `administracionnivelplancarrera` creada y cargada en `SUS` y `BS`.
- Monte Sion y el reporte de plan de carrera leen umbrales, corte VME, bono e incentivo de esa tabla, según la moneda del ciclo. Se eliminó `MonteSion:Rangos` del `appsettings`.
- La red comprimida se borra y se guarda por ciclo (sin `TRUNCATE`).
- El corte VME se aplica por equipo, armando los equipos con toda la red de nivel 1 del ciclo.
- La vista Monte Sion y el Excel muestran la moneda del ciclo, el VME % y el corte VME.

**Pruebas realizadas:** `GET /api/MonteSion/rangos` contra `grdsion` para el ciclo 149 (SUS), el ciclo 150 (BS) y un ciclo inexistente. Compilación de la API, `eslint` y `vite build` del frontend.

**Quedó sin verificar o sin hacer al cierre:**
- Que `plan_carrera_monte_sion_actualizar_incentivos_sus.sql` esté aplicado con `COMMIT`. En la última prueba, REGIONAL AMBASSADOR (SUS) tenía incentivo 0.
- El caso de "195 millones" en el ambiente de prueba (no se reprodujo en `grdsion`).
- La revisión visual de la vista Monte Sion.
- La revisión de negocio de los 8 cambios de rango del ciclo 149 que produjo la corrección de equipos.
- Las pruebas de `rangos/guardar`, de configuración incompleta y la revisión manual de casos en Bs.
- La fase 4 (pantalla y API para editar el plan de carrera): no se implementó. Los cambios se hacen por SQL inactivando la fila vigente (`estado = 0, activo = NULL`) e insertando una nueva.

### Fase 0 — Confirmación de decisiones ✅

- [x] Nombres de rangos desde `administracionnivel.snombre` (D1, D2).
- [x] Columnas de la tabla nueva: desde, hasta, VME %, producción con corte de VME, bono e incentivo.
- [x] Sin tipo de cambio entre monedas (D3).
- [x] Registro del incentivo pagado y despliegue: fuera de alcance.
- [ ] (Opcional) D4: vigencia por ciclo.

### Fase 1 — Base de datos ✅

**Estado:** terminado (11/11/2026). Se ejecutaron `src/query/plan_carrera_monte_sion.sql` y `src/query/plan_carrera_monte_sion_actualizar_incentivos_sus.sql`, y el resultado fue confirmado como correcto.

Correcciones hechas durante la ejecución:
- **Error 1064:** el servidor no soporta `GENERATED ALWAYS`. Se reemplazó por la columna normal `activo`, que vale 1 en la fila vigente y `NULL` en las históricas.
- **Error 1267:** había mezcla de collations `latin1`/`utf8mb4`. La tabla se crea con `utf8mb4_general_ci` de forma explícita.
- **Incentivos `SUS`:** se tomaron de `appsettings.json` → `MonteSion:Rangos`, de Esmeralda a Fundador Royal.

**Entregable:** `src/query/plan_carrera_monte_sion.sql`. Se ejecuta a mano: primero en una base aislada y después en la de destino.

1. Leer en vivo `administracionnivel` y confirmar los valores de la tabla 1.2. Si hay diferencias, la carga `SUS` toma lo que está en la base.
2. `CREATE TABLE administracionnivelplancarrera` (sección 2.1).
3. Carga `SUS`: `INSERT ... SELECT` desde `administracionnivel` (desde, hasta, bono, `VME` → corte, % calculado) y los incentivos de `appsettings.json` → `MonteSion:Rangos`. Si ya se cargó con la versión anterior, se corrige con `plan_carrera_monte_sion_actualizar_incentivos_sus.sql`.
4. Carga `BS`: `INSERT` con la tabla de la sección 2.2.
5. Filas en 0 para Asesor Comercial (0) y Emprendedor (1), en ambas monedas.
6. Consultas de verificación incluidas en el script:
   - cada `lnivel_id > 0` de `administracionnivel` tiene exactamente una fila activa por moneda;
   - `dproduccioncortevme` coincide con `dproducciondesde × dvmeporcentaje / 100`;
   - los rangos desde/hasta no se superponen dentro de una moneda.

**Criterio de terminado:** el script corre sin errores en la base aislada y las cinco verificaciones devuelven 0 filas.

### Fase 2 — Backend 🔄

**Hecho (compila con `dotnet build`, 0 errores):**
- `MonteSionRepository.ObtenerNivelesAsync(logId, moneda)`: `administracionnivel` (nombre, % liderazgo) `LEFT JOIN administracionnivelplancarrera` (moneda del ciclo, `estado = 1`). Si algún nivel `> 0` no tiene configuración, devuelve error con los nombres de los rangos que faltan.
- `MonteSionService`:
  - obtiene la moneda del ciclo (`ObtenerMonedaAsync`);
  - el tope VME sale de `ProduccionCorteVme`;
  - el bono y el incentivo salen de la tabla;
  - el texto del incentivo se arma con la moneda (`Pago único de 3.500 Bs` / `Pago único de 500 $us`);
  - ya no lee `MonteSion:Rangos`.
- DTOs:
  - `BonoUsd` → `Bono`, `VmeMonto` → `ProduccionCorteVme` + `VmePorcentaje`;
  - `Beneficio.BonoAPagarUsd` → `BonoAPagar`, `BonoConfiguradoUsd` → `BonoConfigurado`;
  - campos nuevos `IncentivoAPagar`, `IncentivoConfiguradoMonto` y `Moneda`;
  - `MonteSionOpciones` solo conserva `ProfundidadMaxima`.
- `ReportesService.ReportePlanCarreraAsync`: los incentivos del reporte de plan de carrera también salen de la tabla, en la moneda del ciclo.
- Excel: "Bono ({Bs/$us})" e "Incentivo ({Bs/$us})" como números; la columna "VME %" ahora muestra el porcentaje (antes mostraba el monto).
- `appsettings.json` y `.example`: se quitó `MonteSion:Rangos`.

**Prueba contra la base (11/11/2026, API local apuntando a `grdsion` en 10.2.10.40, solo `GET /api/MonteSion/rangos`):**

| Ciclo | Moneda | Resultado |
|---|---|---|
| 149 (Octubre 2026) | SUS | ✅ 133 calificados, 4 ascensos y 4 equipos recortados por VME (cortes de 80.000 y 140.000). Los bonos coinciden con la tabla. |
| 150 (Noviembre) | BS | ✅ Leyó el plan en BS sin errores. 15 calificados (14 Emprendedor y 1 Senior). El ciclo está en curso. |
| 99999 | — | ✅ `status=false`: "No se encontró el ciclo 99999." |

⚠️ **Hallazgo:** en SUS, REGIONAL AMBASSADOR (id 8) devuelve un incentivo configurado de 0, cuando debería ser 2.500. Eso indica que en la base no quedó aplicado (o no se confirmó) `plan_carrera_monte_sion_actualizar_incentivos_sus.sql`. En el ciclo 149 no cambia el resultado, porque esos dos casos son recalificaciones y cobran el bono. Pero un ascenso a los rangos 8 a 14 cobraría el bono en lugar del incentivo.

**Correcciones adicionales (11/11/2026):**
- **Red comprimida por ciclo:** `RedesRepository.GuardarRedComprimida` cambió `TRUNCATE TABLE red_comprimida` por `DELETE ... WHERE lciclo_id = @CicloId`. Cada ciclo conserva su red y el borrado se revierte si falla la inserción.
- **Agrupación por equipo para el corte VME:** el mapa vendedor → upline directo se armaba solo con las filas de la red que tenían ventas. Si un intermedio no vendía, la cadena se cortaba y la venta quedaba como un equipo suelto, sin corte VME (629 de 1.253 ventas en el ciclo 149). Ahora `ObtenerDatosCicloAsync` devuelve `RedNivel1` (todas las filas de nivel 1 de `red_comprimida` del ciclo) y el servicio arma el mapa con eso. Resultado en el ciclo 149: cambia el rango de 8 personas y la suma de producción válida baja de 6,71 a 5,74 millones. En el ciclo 150 no hay cambios.

**Pendiente:**
- volver a ejecutar el script de incentivos SUS y probar de nuevo;
- aclarar el caso de "195 millones" que se reportó en el ambiente de prueba (no se reprodujo en `grdsion`);
- la regresión exacta contra el resultado anterior al cambio (fase 5).


1. **DTOs** (`Domain/DTO/MonteSionRangoDto.cs`): los cambios de la sección 2.3, más `MonteSionNivel` con `ProduccionCorteVme`, `VmePorcentaje`, `Incentivo` y `Moneda`.
2. **Repositorio** (`MonteSionRepository` y `IMonteSionRepository`):
   - `ObtenerNivelesAsync(logId, MonedaCiclo moneda)`: `JOIN` de `administracionnivel` (nombre, % liderazgo) con `administracionnivelplancarrera` (`moneda = @Moneda AND estado = 1`).
   - Devolver error si falta la configuración de algún nivel para esa moneda.
3. **Servicio** (`MonteSionService`):
   - Leer el ciclo y validar la moneda (`ValidarMoneda`); si no es válida, responder `status=false`.
   - Cargar los niveles de la moneda del ciclo.
   - `EvaluarRango`: tope = `ProduccionCorteVme` (0 → sin tope).
   - `CalcularBeneficio`: incentivo según el monto (`> 0`). Se arma el texto con la moneda.
   - Quitar `incentivosPorRango` y `_opciones.Rangos`.
   - Incluir `Moneda` en la respuesta.
4. **Excel** (`MonteSion.xlsx.cs`): encabezados con la moneda y el incentivo como número.
5. **Configuración:** quitar `MonteSion:Rangos` de `appsettings.json` y `appsettings.json.example`.
6. `dotnet build` sin errores.

**Criterio de terminado:** compila, `GET /api/MonteSion/rangos` responde para ciclos `SUS` y `BS`, y un ciclo con moneda inválida devuelve error.

### Fase 3 — Frontend ✅

**Hecho (11/11/2026)** en `FrontGuardian/src/views/MonteSion/MonteSion.js`. `eslint` sin errores y `vite build` correcto:
- `formatMoneda(valor, moneda)` reemplaza a `formatUsd`. Las tarjetas de producción, el bono a pagar y el bono configurado se muestran en Bs o $us según el ciclo.
- El encabezado de resultados muestra la moneda del ciclo.
- El detalle del emprendedor incluye un bloque con el rango evaluado y si califica, la producción requerida, el VME %, la producción con corte VME (tope por equipo) y la producción válida (puntos).
- La columna "Límite VME" pasa a llamarse "Corte VME" ("Sin corte" cuando no aplica).


1. `MonteSion.js`: reemplazar `formatUsd` por `formatMoneda(valor, moneda)` (`Bs` / `$us`).
2. Usar los campos nuevos (`bonoAPagar`, `bonoConfigurado`, `incentivoAPagar`, `moneda`).
3. En el detalle por equipo, mostrar el VME % y la producción con corte de VME.
4. Revisar que la descarga del Excel siga funcionando.

**Criterio de terminado:** la vista muestra los montos con la moneda correcta en un ciclo `SUS` y en uno `BS`.

### Fase 4 (opcional) — Mantenimiento del plan de carrera

1. API `AdministracionNivelPlanCarrera`:
   - `GET /` filtrado por moneda (con el nombre del rango tomado de `administracionnivel`);
   - `POST /insert`;
   - `PUT /update`, que inactiva la fila vigente y crea una nueva;
   - `DELETE /delete`, que inactiva.
2. Validaciones:
   - moneda `SUS`/`BS`;
   - sin superposición de desde/hasta;
   - si el corte VME viene vacío, se propone `desde × % / 100`.
3. Pantalla en `FrontGuardian/src/views/parametros` con selector de moneda y una tabla editable con las columnas de la captura.
4. En la pantalla actual de niveles, marcar `Desde`, `Hasta`, `Bono` y `VME` como "solo referencia".

**Criterio de terminado:** un cambio hecho desde la pantalla se refleja en el siguiente cálculo Monte Sion sin tocar código.

### Fase 5 — Pruebas

Ejecutar los casos de la sección 4 en un ambiente controlado.

**Criterio de terminado:**
- la regresión con un ciclo `SUS` da resultados idénticos;
- los casos `BS` revisados a mano coinciden;
- los errores por moneda inválida y por configuración incompleta responden como se espera.

## 4. Pruebas (no hay proyecto de tests; en ambiente controlado, nunca en producción)

1. **Regresión con un ciclo `SUS` ya calculado:** `GET /api/MonteSion/rangos` antes y después del cambio debe dar **exactamente los mismos** rangos, producción válida, bonos e incentivos.
2. **Ciclo `BS`:** verificar a mano 3 casos: uno en el límite de Líder (35.000 Bs), uno de Esmeralda con un equipo que pasa el corte VME (`dproduccioncortevme` = 560.000 Bs) y uno de ascenso con incentivo.
3. **Ciclo con moneda vacía o inválida:** debe responder `status=false` sin calcular nada.
4. **Configuración incompleta** (falta una fila `BS` de algún nivel): debe responder un error claro.
5. **Guardado:** probar `rangos/guardar` en un ciclo `BS` de prueba y revisar `t_plan_montesion`, `reportesmontesion`, `t_nuevos_rangos_montesion_2025` y `administracioncontacto.lnivel_id`.
6. Compilar con `dotnet build` y probar la vista Monte Sion y la exportación a Excel.

## 5. Decisiones

| # | Tema | Decisión |
|---|---|---|
| D1 | Nombres e incentivos desde Embajador Regional | Los rangos de la captura se asocian por `lnivel_id` con los nombres en inglés de la base (Embajador Regional = REGIONAL AMBASSADOR, id 8; ver 1.5). En `BS` se cargan los incentivos de la captura. En `SUS` se cargan los incentivos de `appsettings.json` → `MonteSion:Rangos` (Esmeralda a Fundador Royal). |
| D2 | Nombres en `administracionnivel` | No se modifican. |
| D3 | Tipo de cambio | No se toma en cuenta. Cada moneda usa su propia tabla, sin conversiones. |
| D4 | Vigencia por ciclo (opcional) | Pendiente. Agregar `dtvigenciadesde` solo si negocio prevé cambios de plan. |
| — | Registro del incentivo pagado | Fuera de alcance. |
| — | Despliegue | Fuera de alcance de este plan. |

## 6. Riesgos

- **Tabla incompleta para una moneda:** el cálculo debe fallar en lugar de asumir 0, porque si no todos calificarían a todos los rangos.
- **`administracionnivel` editable desde el CRUD actual:** quien cambie ahí `ddesde`, `dbono` o `VME` ya no verá efecto en Monte Sion. Hay que avisarlo en la pantalla o marcar esos campos como "solo referencia".
- **Otros consumidores de `administracionnivel`:** `ReportesRepository` (plan de carrera / ascenso de rango) y `AdministracionContactoRepository` también leen la tabla. Hay que revisar si esos reportes muestran montos USD que deban pasar a la tabla nueva.
- **Ciclos ya guardados:** no se recalculan. El cambio solo aplica a los ciclos que se guarden de ahora en adelante.
