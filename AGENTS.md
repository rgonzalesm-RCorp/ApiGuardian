# Guía del proyecto ApiGuardian

## Propósito y alcance

`ApiGuardian` es la API del sistema Guardian/Sentinel para administrar asesores, contratos y parámetros, ejecutar por ciclo el proceso de cálculo de comisiones y producir reportes. También integra datos operativos de bases SQL Server con la base propia MySQL y contiene un proceso de “aplicaciones” que registra pagos/prorrateos y puede invocar una pasarela SOAP de facturación.

Este documento describe el código que existe en este directorio. No presupone que el diseño sea ideal ni que todo archivo registrado esté activo en ejecución.

## Plataforma y arranque

- Solución .NET en `ApiGuardian.sln`, fijada al SDK `9.0.317` por `global.json`.
- Los cuatro proyectos usan `net9.0` y nullable reference types:
  - `src/Api/Api.csproj`: ASP.NET Core Web API y composición de la aplicación.
  - `src/Application/Application.csproj`: contratos de repositorio y logging.
  - `src/Domain/Domain.csproj`: entidades y DTOs sin dependencias de infraestructura.
  - `src/Infrastructure/Infrastructure.csproj`: Dapper, conexiones, repositorios, reportes y servicios externos.
- Punto de entrada: `src/Api/Program.cs`.
- URLs de desarrollo: `http://localhost:5237` y `https://localhost:7194`, según `src/Api/Properties/launchSettings.json`.
- En entorno `Production`, `Program.cs` configura `http://0.0.0.0:5000`.
- `src/Api/start-app.sh` muestra el arranque desplegado mediante `dotnet .../out/Api.dll`; no construye ni publica el proyecto.

Dependencias principales comprobadas:

- Dapper `2.1.66` para todo el acceso a datos; no se usa Entity Framework ni hay migraciones .NET.
- MySQL (`MySql.Data`/`MySqlConnector`) y SQL Server (`Microsoft.Data.SqlClient`).
- QuestPDF `2025.7.4` para PDF y ClosedXML `0.105.0` para XLSX.
- Swashbuckle `9.0.6` para Swagger.
- Newtonsoft.Json `13.0.4` en logging y serializaciones explícitas.
- Quartz `3.15.1` está referenciado, pero su scheduler y hosted service están comentados en `Program.cs`.

## Arquitectura real

La solución sigue una separación por capas, aunque los controladores llaman directamente a repositorios y concentran parte importante de la orquestación y reglas de negocio:

`HTTP -> Controller (Api) -> interfaz (Application) -> repositorio/servicio (Infrastructure) -> MySQL/SQL Server/HTTP externo -> respuesta del Controller`

Las referencias de proyecto son:

- `Api -> Application + Infrastructure`
- `Infrastructure -> Application + Domain`
- `Application -> Domain`
- `Domain` no referencia otros proyectos de la solución.

No existe una capa general de servicios de aplicación entre controladores y repositorios. Las excepciones son servicios concretos en infraestructura, por ejemplo `CambioDolarService`, `LogService`, generadores PDF/XLSX y la clase `MiCronJob` usada para migrar/procesar ventas.

## Estructura importante

- `src/Api/Program.cs`: CORS, Swagger, DI, contextos de datos y pipeline HTTP.
- `src/Api/Controllers/`: endpoints. `Controllers/Aplicaciones/AplicacionesController.cs` usa una ruta base explícita diferente.
- `src/Api/Job/CronJob.cs`: `MiCronJob`; contiene creación recursiva de contactos, homologación de complejos, alta de contratos y procesamiento principal de ventas. Aunque implementa `IJob`, el flujo HTTP también la resuelve desde un scope y llama `ProcesoPrincipal` en segundo plano.
- `src/Application/Interface/`: interfaces que los controladores consumen. Devuelven con frecuencia tuplas nombradas como `(Data, Success, Mensaje)` en vez de un tipo de resultado común.
- `src/Domain/Entities/` y `src/Domain/DTO/`: modelos para binding, resultados Dapper, reportes y solicitudes de proceso. No son entidades ORM.
- `src/Domain/Entities/ControlProceso.cs`: estados, configuración y constantes del proceso; es la fuente de verdad de nombres de pasos y mapeo de tipos de contrato CNX/GRD.
- `src/Infrastructure/DapperContext*.cs`: fábricas de conexiones.
- `src/Infrastructure/Repositories/`: SQL en cadenas dentro de cada repositorio. `cnx/` contiene consultas a la fuente SQL Server y `Aplicaciones/` divide un repositorio grande en lógica, datos y consultas.
- `src/Infrastructure/Service/Report/`: documentos PDF QuestPDF.
- `src/Infrastructure/Service/xls/`: libros XLSX ClosedXML, pese a la extensión compuesta `*.xlsx.cs`.
- `src/query/`: scripts SQL auxiliares existentes; no se ejecutan automáticamente desde `Program.cs`.

## Configuración y conexiones

ASP.NET Core carga `src/Api/appsettings.json`, sus variantes de entorno y las fuentes estándar del host. El único entorno definido explícitamente en el repositorio es `ASPNETCORE_ENVIRONMENT=Development` en `launchSettings.json`. No hay lectura directa de variables con `Environment.GetEnvironmentVariable`.

Claves consumidas por el código:

- `ConnectionStrings:DefaultConnection`: MySQL, creado por `DapperContext`; es la base principal de Guardian/GRD.
- `ConnectionStrings:DefaultConnectionSqlServer`: SQL Server, creado por `DapperContextSqlServer`; se usa para CNX/BDComisiones y también por Aplicaciones.
- `ConnectionStrings:DefaultConnectionSqlServer64`: segundo SQL Server, creado por `DapperContextSqlServer64`; lo usa `BonoResidualRepository`.
- `EmpresaCalculoComisiones`: lista de empresas, base de datos asociada y exclusiones de proyectos/productos; la consumen las consultas CNX y cálculos de cuotas.
- `HabilidacionesParaNoComprimirRed`: IDs que `RedesController` exceptúa al comprimir la red.
- `cambioDolar.idsComplejosProyectos` y `cambioDolar.tipoCambio`: controlan conversiones en `CambioDolarService` para ventas, cuotas y upgrades.
- `ControlProceso:PasoValidar`: está configurado; su uso debe volver a comprobarse antes de cambiarlo porque no aparece como lectura directa fuera de configuración.
- `Aplicaciones`: mínimos, límites de errores, timeouts, validación de conteos y configuración de facturación SOAP. La clase enlazada está en `src/Application/Interface/Aplicaciones/ConfiguracionAplicaciones.cs`.

No copies valores sensibles de `appsettings.json` a documentación, logs o commits. El archivo real contiene cadenas de conexión y credenciales. Usa los `*.example` como esquema, pero verifica diferencias con la configuración del ambiente. Hay una diferencia observada entre la propiedad C# `Contrasena` y una clave de configuración llamada `Password`; su efecto en el ambiente debe investigarse antes de tocar la integración.

## Acceso a datos e integraciones

### Base principal Guardian/GRD (MySQL)

La mayoría de repositorios abre una conexión por método mediante `DapperContext.CreateConnection()` y ejecuta SQL parametrizado con `QueryAsync`, `QueryFirstOrDefaultAsync` o `ExecuteAsync`. Los alias SQL están hechos para coincidir con propiedades C# y deben conservarse al cambiar consultas.

Tablas centrales comprobadas incluyen `administracioncontacto`, `administracioncontrato`, `administracionciclo`, `administracionventapersonal`, `administracionventagrupo`, `administracionhabilitacioncomision`, `red_comprimida`, `red_completa_cuotas`, `t_productos_pagar_mensuales`, `t_productos_detalle_cuotas`, `administracionbonoresidual`, `bonopar`, `bonopardetalle` y las tablas `conf_proceso*`. No es una lista exhaustiva: cada repositorio contiene la consulta vigente.

Varios CRUD calculan IDs con `MAX(id)+1`; no asumas autoincremento. Las operaciones compuestas de proceso/reset usan transacciones Dapper explícitas en algunos repositorios. Preserva el alcance de esas transacciones.

### SQL Server/CNX

- `src/Infrastructure/Repositories/cnx/VentaCnxRepository.cs` obtiene ventas/clientes desde SQL Server usando consultas construidas en `cnx/Query.cs` y la configuración por empresa.
- `CasosEspecialesRepository`, `CuotasVentaResidualRepository`, `BonoResidualRepository` y `AplicacionesRepositorio` combinan datos SQL Server con datos Guardian.
- Existen referencias verificadas a `BDComisiones`, `BDQISHUR`, `DBITSIS` y `BDBPMSION`, además de nombres de base dinámicos por empresa. Antes de editar una consulta identifica qué contexto abre el método; una consulta sintácticamente válida puede estar apuntando al servidor equivocado.

### Aplicaciones y facturación

`src/Infrastructure/Repositories/Aplicaciones/AplicacionesRepositorio.cs` es `partial` junto con:

- `AplicacionesRepositorio.Datos.cs`: operaciones Dapper, comandos SQL Server y llamada SOAP.
- `AplicacionesRepositorio.Consultas.cs`: constantes SQL.

La vista previa valida conexiones y simula buena parte del proceso, pero el propio código declara que ejecuta la carga de retenciones y puede sincronizar comisiones por empresa. `Aplicar` limpia por ciclo datos derivados, carga retenciones/prioridades/comisionados, procesa saldos en el orden Grupo Sion -> cartas -> descuentos -> prorrateo, registra pagos/recibos y solicita facturas. El pago decide entre completo y “a cuenta”, y puede usar fecha valor si el vencimiento cae desde el primer día del mes anterior hasta hoy. Al alcanzar el límite configurado de errores de facturación el proceso falla como fatal.

La facturación crea un `HttpRequestMessage` SOAP hacia `Aplicaciones:Facturacion:PuntoFinal`, agrega `SOAPAction` y usa el timeout configurado. No hay otro cliente HTTP externo en el backend.

## Pipeline HTTP, seguridad y errores

`Program.cs` configura, en este orden efectivo, CORS, Swagger/UI, redirección HTTPS y mapeo de controladores.

- La política `AllowReactApp` acepta cualquier origen, cabecera y método.
- Swagger está habilitado en todos los entornos; la condición de desarrollo está comentada.
- No hay middleware personalizado, `UseExceptionHandler` ni Problem Details.
- No hay `AddAuthentication`, `AddAuthorization`, JWT, cookies, atributos `[Authorize]` ni validación de claims.
- Cabeceras como `Usuario`, `usuario` o `LogTransaccionId` son parámetros de operación/auditoría, no autenticación comprobada por la API.

Los controladores suelen capturar excepciones, registrar y responder `Ok(...)` con un sobre JSON. El formato predominante es:

```json
{ "status": true, "mensaje": "...", "data": {} }
```

Los endpoints de `AplicacionesController` son la excepción comprobada y usan `estado`, `mensaje`, `datos`. Las acciones del código retornan explícitamente `Ok`; por ello un error funcional o una excepción capturada suele seguir siendo HTTP 200 y el consumidor debe mirar `status`/`estado`. El model binding de `[ApiController]` todavía puede producir respuestas automáticas antes de ejecutar la acción.

`src/Infrastructure/Service/LogService.cs` escribe simultáneamente a consola y a `Logs/yyyyMMdd.log` bajo `AppDomain.CurrentDomain.BaseDirectory`. Usa un lock estático y cada llamada recibe un ID de transacción, archivo y método. Muchos controladores generan ese ID desde Unix time en segundos o milisegundos. El servicio está registrado singleton.

## Catálogo de endpoints

Salvo Aplicaciones, la ruta base es `/api/{NombreControllerSinController}`. La siguiente lista refleja todos los atributos HTTP actuales; los parámetros están repartidos entre query string, headers y JSON body según la firma de cada acción.

| Ruta base | Métodos y subrutas |
|---|---|
| `/api/AdministracionBanco` | `GET /`, `GET /moneda`, `PUT /update`, `POST /insert`, `DELETE /delete` |
| `/api/AdministracionBuscarAsesor` | `GET /` |
| `/api/AdministracionCiclo` | `GET /`, `GET /paginacion`, `POST /insert`, `PUT /update`, `DELETE /delete` |
| `/api/AdministracionCicloFactura` | `GET /`, `POST /register`, `DELETE /delete` |
| `/api/AdministracionComplejo` | `GET /`, `GET /paginacion`, `POST /insert`, `PUT /update`, `DELETE /delete` |
| `/api/AdministracionContacto` | `GET /`, `POST /insert`, `PUT /update`, `DELETE /baja`, `GET /verificar/estado` |
| `/api/AdministracionContrato` | `GET /`, `POST /insert`, `PUT /update`, `DELETE /delete` |
| `/api/AdministracionCuentaBanco` | `GET /id`, `GET /`, `PUT /update` |
| `/api/AdministracionDescuentoCicloTipo` | `GET /paginacion`, `POST /insert`, `PUT /update`, `DELETE /delete` |
| `/api/AdministracionDescuentoComision` | `GET /`, `DELETE /delete`, `POST /insert` |
| `/api/AdministracionDetalleFactura` | `GET /paginacion`, `POST /insert`, `PUT /update`, `DELETE /delete`, `GET /tipo/comision` |
| `/api/AdministracionEmpresa` | `GET /`, `GET /paginacion`, `POST /insert`, `PUT /update`, `DELETE /delete` |
| `/api/AdministracionHabilitacionComision` | `GET /GetHabilitaciones`, `POST /SaveHabilitaciones`, `PUT /UpdateHabilitacion`, `DELETE /DeleteHabilitacion` |
| `/api/AdministracionNivel` | `GET /`, `GET /paginacion`, `POST /insert`, `PUT /update`, `DELETE /delete` |
| `/api/AdministracionObservacionComision` | `GET /`, `POST /register`, `PUT /update`, `DELETE /delete` |
| `/api/AdministracionSemana` | `GET /`, `GET /paginacion`, `POST /insert`, `PUT /update`, `DELETE /delete` |
| `/api/AdministracionSemanaCiclo` | `GET /paginacion`, `POST /insert`, `PUT /update`, `DELETE /delete` |
| `/api/AdministracionTipoContacto` | `GET /`, `GET /paginacion`, `POST /insert`, `PUT /update`, `DELETE /delete` |
| `/api/AdministracionTipoContrato` | `GET /`, `GET /paginacion`, `POST /insert`, `PUT /update`, `DELETE /delete` |
| `/api/BrConfiguracion` | `GET /get/datos`, `GET /get/configuracion`, `POST /save/configuracion`, `DELETE /delete/configuracion` |
| `/api/ConfiguracionProcesoComisiones` | `POST /vta/cnx`, `GET /get/vta/cnx`, `DELETE /delete/vta/cnx` |
| `/api/ControlProceso` | `GET /configuracion`, `POST /configuracion`, `DELETE /configuracion`, `GET /ciclo`, `POST /reset/ciclo`, `POST /cerrar/ciclo` |
| `/api/ProcesoComisiones` | `GET /vta/cnx`, `GET /vta/rezagadas`, `GET /venta/personal`, `POST /save/vta/proceso`, `POST /save/vta/personal`, `GET /venta/grupo`, `POST /save/vta/grupo`, `POST /ejemplo` |
| `/api/CasosObservados` | `GET /casos/observados`, `POST /procesar` |
| `/api/CasosEspeciales` | `GET /casos/especiales` |
| `/api/Redes` | `GET /armar/red/comprimida/mes`, `GET /armar/red/cuotas` |
| `/api/BonoResidual` | pares `GET /get/...` y `POST /save/...` para `cartera`, `cuota`, `excedente`, `calculo/residual` y `bono/par` |
| `/api/CuotasVentaResidual` | `GET /cuotas/venta/residual`, `POST /cuotas/venta/residual` |
| `/api/Reportes` | `GET /comisiones`, `/aplicaciones`, `/descuento/empresa`, `/facturacion`, `/prorrateo`, `/comision/servicio`, `/pagar/comision`, `/plan/carrera`, `/ascenso/rango` |
| `/api/Utils` | `GET /administracion/semana/ciclo`, `/administracion/departamento`, `/administracion/tipo/contrato`, `/administracion/estado/contrato`, `/administracion/tipo/baja`, `/administracion/pais`, `/tipo/descuento` |
| `/api/aplicaciones` | `GET /vista-previa`, `POST /aplicar` |

Para conocer el contrato exacto de una ruta, leer primero su acción y luego la interfaz inyectada. Muchos `GET` y `DELETE` requieren headers como `lCicloId`, `LCicloId`, `Usuario`, IDs, `page`, `pageSize` y `search`; la capitalización varía en el código, aunque HTTP trata los nombres de cabecera sin distinguir mayúsculas.

## Reglas de negocio y flujo de comisiones

### Control de proceso

`ControlProcesoRepository` administra la configuración, instancias por ciclo, dependencias, siguiente paso, inicio/finalización/cancelación, reset y cierre. Los controladores de cálculo comprueban el siguiente paso antes de guardar y, normalmente, siguen esta secuencia:

1. validar ciclo/fechas y siguiente paso;
2. `IniciarPaso`;
3. leer/calcular/guardar;
4. `FinalizarPaso` si todo sale bien;
5. `CancelarPaso` en fallos posteriores al inicio.

Los nombres canónicos están en `PasosDiccionario`: obtener ventas, casos observados, adicionar ventas, ventas especiales, comisión directa, registro de habilitaciones, red comprimida/completa, comisión grupo, cartera/cuotas/excedente, comisión residual, comisión venta residual y bono par. `COMISION LIDERAZGO` se trata como alias de bono par. No cambies literales sin revisar la configuración almacenada y `FrontGuardian/src/views/Comisiones/config/procesoComisiones.json`.

`reset/ciclo` elimina resultados derivados del ciclo y revierte ventas rezagadas dentro de la lógica del repositorio. Es una operación destructiva de negocio, aunque el endpoint sea `POST`.

### Ventas y comisión directa/grupo

- `GET ProcesoComisiones/vta/cnx` obtiene las fechas desde `AdministracionCicloRepository`, consulta ventas CNX y contratos GRD y limita la cuota inicial mostrada a `ceil(precio * 10%)` si llega mayor.
- `POST save/vta/proceso` selecciona el paso por `Rezagada`/`EsEspecial`, lo inicia y lanza `MiCronJob.ProcesoPrincipal` con `Task.Run` y un scope nuevo. Responde antes de finalizar el trabajo. Las ventas rezagadas seleccionadas desplazan su fecha un mes; las especiales cargan solicitudes upgrade.
- Contactos y contratos se crean/homologan en la lógica de `MiCronJob`; el patrocinio puede resolverse recursivamente desde CNX. La ausencia de homologación de complejo hace que la venta se omita.
- Habilitaciones con `GeneraComisiones=false` bloquean al contacto en cálculos. Tipos especiales (upgrade, recuperación, recompra y casos especiales) también se excluyen de ciertas bases residuales mediante `HabilitacionComisionHelper`.
- Si existe al menos un upgrade en comisión directa, esos cuatro tipos especiales se recalculan al 67% de la inicial. Antes de guardar, el backend exige que la cantidad calculada coincida con la enviada por el frontend.
- El cálculo base residual asociado a comisión directa usa reglas distintas: para complejos de membresía codificados en el controlador, 40% de la inicial y 12 meses; para los demás, 30% y 6 meses. Solo persiste diferencias positivas con porcentaje inicial menor a 100.
- Comisión de grupo filtra bloqueados, marca habilitados y guarda solo resultados admisibles para el paso.

### Red, residual y bono par

- `RedesController` construye primero red comprimida a partir de contactos con venta del mes y excepciones configuradas; después construye red completa para cuotas. Ambos endpoints ejecutan el paso, no son lecturas puras pese a usar `GET`.
- `BonoResidualController` presenta y persiste secuencialmente cartera, cuotas, excedente, cálculo residual y bono par. Cada par `get/save` comparte el paso correspondiente.
- `CuotasVentaResidualController` cruza cuotas SQL Server, productos mensuales y venta personal/habilitaciones; calcula topes/cuotas comisionables y genera XLSX. Su `POST` persiste el paso de comisión de venta residual.
- Los cálculos aplican `CambioDolarService` únicamente para IDs configurados. No reemplazarlo por una conversión global.

### Casos observados, especiales y habilitaciones

- Casos observados compara ventas del periodo con históricos de vendedores/clientes y expone resumen; `procesar` completa el paso.
- Casos especiales consulta upgrades/recuperaciones/recompras/casos especiales en SQL Server y los incorpora mediante `save/vta/proceso`.
- El registro de habilitaciones está situado después de comisión directa en la secuencia observada. Guardar puede aceptar una lista vacía para avanzar; actualizar/eliminar registros individuales no equivale por sí solo a completar el paso.

### Reportes

`ReportesController` consulta `ReportesRepository`, construye documentos QuestPDF y, según el reporte, también XLSX. Devuelve archivos como Base64 dentro de `data` con nombres como `fileBase64`, `fileName`, `base64Xls` y `fileNameXls`. No devuelve un stream binario. Reportes disponibles: comisiones por asesor, aplicaciones, descuentos por empresa, facturación, prorrateo, comisión de servicio, consolidado a pagar, plan de carrera y ascenso de rango.

## Relación con FrontGuardian

El frontend construye la URL como `config.path + url`; la configuración observada apunta al prefijo `/api` de esta aplicación. No existe intercambio de JWT/cookie entre ambos proyectos. `FrontGuardian` suele enviar `user.usuarioDominio` en `Usuario` o dentro del body, además de IDs de ciclo/contacto en headers.

Flujos comprobados:

- `Usuario -> FrontGuardian /proceso/comisiones -> sendRequest -> ControlProceso/ciclo -> ControlProcesoRepository -> MySQL -> estado de pestañas`.
- `Usuario -> VentasCnx -> ProcesoComisiones/vta/cnx -> SQL Server CNX + MySQL Guardian -> selección -> save/vta/proceso -> MiCronJob en background -> contactos/contratos + control de paso`.
- `Usuario -> pestañas de cálculo -> endpoint GET de previsualización -> repositorios -> XLSX Base64 -> endpoint POST save -> tablas Guardian + finalización del paso`.
- `Usuario -> vista Reportes -> Reportes/* -> Dapper -> QuestPDF/ClosedXML -> Base64 -> PdfViewer/descarga del navegador`.
- `Usuario -> CRUD administrativo -> sendRequest -> controlador -> repositorio Dapper -> MySQL -> sobre status/mensaje/data`.

Los endpoints `/api/aplicaciones/vista-previa` y `/api/aplicaciones/aplicar` no tienen una llamada comprobada desde el código actual de `FrontGuardian`; no confundirlos con `/api/Reportes/aplicaciones`, que sí consume la vista de reporte.

## Convenciones y comportamiento a preservar

- Mantener nombres de propiedades/IDs heredados (`lCicloId`, `LCicloId`, `sNombre`, etc.) y alias SQL: frontend, model binding y Dapper dependen de ellos.
- Mantener la diferencia entre parámetros en header, query y body; el frontend usa los tres patrones.
- Mantener los sobres actuales y la excepción `estado/datos` de Aplicaciones salvo una migración coordinada.
- Tratar `status=false` en HTTP 200 como error funcional esperado por el frontend.
- No introducir autenticación implícita: si se agrega, debe coordinarse con el servicio externo de permisos y `FrontGuardian`.
- No ejecutar `src/query/*.sql`, endpoints de `save`, reset/cierre, ni procesos de aplicaciones para “probar” sin una base aislada y autorización explícita.
- No asumir que un `GET` es inocuo: los endpoints de red y la vista previa de Aplicaciones realizan escrituras/cargas comprobadas.
- Preservar cancelación/finalización de pasos alrededor de errores y el trabajo asíncrono de `save/vta/proceso` hasta diseñar conscientemente otra semántica.
- Revisar ambos repositorios antes de cambiar un DTO compartido conceptualmente; el frontend no genera clientes tipados.
- No hay proyectos de pruebas automatizadas encontrados. Verificar cambios al menos por compilación y, para lógica de datos, con un ambiente controlado; no usar producción.

## Puntos no determinados a partir del código analizado

- El esquema completo, constraints, procedimientos almacenados y permisos efectivos de las bases no pueden determinarse solo desde este repositorio.
- No se puede confirmar qué valores/configuración se inyectan en producción ni si sobreescriben `appsettings.json`.
- No se puede confirmar qué sistema externo inicia la autenticación del frontend ni la política de expiración del token; esa lógica pertenece al servicio `PermisosWebServices`.
- Aunque `MiCronJob` implementa Quartz, no hay scheduler activo en `Program.cs`; no determinado si un host externo lo agenda de otra forma.
- No determinado a partir del código analizado si todos los endpoints del catálogo siguen siendo usados por consumidores distintos de `FrontGuardian`.
