using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using Newtonsoft.Json;
using DocumentFormat.OpenXml.Office.CoverPageProps;
using DocumentFormat.OpenXml.Office.CustomUI;

namespace ApiGuardian.Infrastructure.Repositories;

public class ControlProcesoRepository : IControlProcesoRepository
{
    private readonly DapperContext _context;
    private readonly ILogService _log;
    private string NOMBREARCHIVO = "ControlProcesoRepository.CS";
    public ControlProcesoRepository(DapperContext context, ILogService log)
    {
        _context = context;
        _log = log;
    }
    private sealed class ControlProcesoPasoContext
    {
        public int ProcesoId { get; set; }
        public int ProcesoInstanciaId { get; set; }
        public int ProcesoCicloId { get; set; }
        public int PasoId { get; set; }
    }
    private sealed class ControlProcesoPasoEstadoRow
    {
        public int id { get; set; }
        public string estado { get; set; } = string.Empty;
    }

    private static ItemControlProcesoPrincipal BuildPasoResponse(bool status, string mensaje, bool next = true)
    {
        return new ItemControlProcesoPrincipal
        {
            status = status,
            mensaje = mensaje,
            mensajes = mensaje,
            next = next
        };
    }

    private async Task<(bool Success, string Mensaje, ControlProcesoPasoContext Data)> ResolvePasoContextAsync(
        System.Data.IDbConnection connection,
        string proceso,
        int LCicloId,
        string paso,
        bool crearInstanciaSiNoExiste,
        System.Data.IDbTransaction? transaction = null
    )
    {
        const string queryInstancia = @"
            SELECT id
            FROM conf_proceso_instancias
            WHERE proceso_id = @ProcesoId
            ORDER BY CASE WHEN estado = 'EN_PROCESO' THEN 0 ELSE 1 END, id DESC
            LIMIT 1;
        ";

        const string insertInstancia = @"
            INSERT INTO conf_proceso_instancias (proceso_id, estado, fecha_inicio)
            VALUES (@ProcesoId, 'EN_PROCESO', NOW());
            SELECT LAST_INSERT_ID();
        ";

        const string queryCicloExistente = @"
            SELECT
                CPI.id ProcesoInstanciaId,
                CPC.id ProcesoCicloId
            FROM conf_proceso_ciclos CPC
            INNER JOIN conf_proceso_instancias CPI ON CPI.id = CPC.proceso_instancia_id
            WHERE CPI.proceso_id = @ProcesoId
              AND CPC.numero_ciclo = @LCicloId
            ORDER BY CASE WHEN CPC.estado = 'EN_PROCESO' THEN 0 ELSE 1 END, CPC.id DESC
            LIMIT 1;
        ";

        const string insertCiclo = @"
            INSERT INTO conf_proceso_ciclos (proceso_instancia_id, numero_ciclo, estado, fecha_inicio)
            VALUES (@ProcesoInstanciaId, @LCicloId, 'EN_PROCESO', NOW());
            SELECT LAST_INSERT_ID();
        ";

        const string queryPaso = @"
            SELECT id
            FROM conf_pasos
            WHERE proceso_id = @ProcesoId
              AND nombre = @paso
              AND estado = 1
            ORDER BY id DESC
            LIMIT 1;
        ";

        var procesoConfigurado = await GetProcesoConfiguradoAsync(connection, proceso, LCicloId, transaction);
        if (procesoConfigurado == null || procesoConfigurado.ProcesoId <= 0)
        {
            return (false, "El proceso no existe.", new ControlProcesoPasoContext());
        }

        int procesoId = procesoConfigurado.ProcesoId;
        int procesoInstanciaId = 0;
        int procesoCicloId = 0;

        var cicloExistente = await connection.QueryFirstOrDefaultAsync<ControlProcesoPasoContext>(
            queryCicloExistente,
            new { ProcesoId = procesoId, LCicloId },
            transaction
        );

        if (cicloExistente != null && cicloExistente.ProcesoCicloId > 0)
        {
            procesoInstanciaId = cicloExistente.ProcesoInstanciaId;
            procesoCicloId = cicloExistente.ProcesoCicloId;
        }
        else
        {
            procesoInstanciaId = await connection.QueryFirstOrDefaultAsync<int>(
                queryInstancia,
                new { ProcesoId = procesoId },
                transaction
            );

            if (procesoInstanciaId <= 0 && crearInstanciaSiNoExiste)
            {
                procesoInstanciaId = await connection.ExecuteScalarAsync<int>(
                    insertInstancia,
                    new { ProcesoId = procesoId },
                    transaction
                );
            }

            if (procesoCicloId <= 0 && crearInstanciaSiNoExiste)
            {
                if (procesoInstanciaId <= 0)
                {
                    return (false, "No se pudo inicializar la instancia del proceso.", new ControlProcesoPasoContext());
                }

                procesoCicloId = await connection.ExecuteScalarAsync<int>(
                    insertCiclo,
                    new { ProcesoInstanciaId = procesoInstanciaId, LCicloId },
                    transaction
                );
            }
        }

        int pasoId = 0;
        if (!string.IsNullOrWhiteSpace(paso))
        {
            pasoId = await connection.QueryFirstOrDefaultAsync<int>(
                queryPaso,
                new { ProcesoId = procesoId, paso },
                transaction
            );

            if (pasoId <= 0)
            {
                return (false, "El paso no existe.", new ControlProcesoPasoContext());
            }
        }

        return (
            true,
            "Contexto obtenido correctamente.",
            new ControlProcesoPasoContext
            {
                ProcesoId = procesoId,
                ProcesoInstanciaId = procesoInstanciaId,
                ProcesoCicloId = procesoCicloId,
                PasoId = pasoId
            }
        );
    }
    private async Task<ControlProcesoConfiguracion?> GetProcesoConfiguradoAsync(
        System.Data.IDbConnection connection,
        string proceso,
        int LCicloId,
        System.Data.IDbTransaction? transaction = null
    )
    {
        const string query = @"
            SELECT
                CP.id ProcesoId,
                CP.nombre Nombre,
                IFNULL(CP.descripcion, '') Descripcion,
                CP.estado Estado,
                CP.fecha_creacion FechaCreacion
            FROM conf_procesos CP
            WHERE CP.nombre = @proceso
              AND CP.estado = 1
            ORDER BY CP.id DESC
            LIMIT 1;
        ";

        return await connection.QueryFirstOrDefaultAsync<ControlProcesoConfiguracion>(
            query,
            new { proceso, LCicloId },
            transaction
        );
    }
    public async Task<(bool Success, string Mensaje, IEnumerable<ControlProcesoConfiguracion> Data)> GetConfiguracionProcesos(string LogTransaccionId, string Usuario)
    {
        string nombreMetodo = "GetConfiguracionProcesos()";

        const string queryProcesos = @"
             SELECT
                id ProcesoId,
                nombre Nombre,
                IFNULL(descripcion, '') Descripcion,
                estado Estado,
                fecha_creacion FechaCreacion
            FROM conf_procesos
            WHERE estado = 1
            ORDER BY id DESC;
        ";

        const string queryPasos = @"
            SELECT
                id PasoId,
                proceso_id ProcesoId,
                CONCAT('step-', id) Referencia,
                nombre Nombre,
                orden Orden,
                es_obligatorio EsObligatorio,
                estado Estado
            FROM conf_pasos
            WHERE estado = 1
            ORDER BY proceso_id, orden, id;
        ";

        const string queryDependencias = @"
            SELECT
                PD.id DependenciaId,
                PD.paso_id PasoId,
                PD.paso_requerido_id PasoRequeridoId,
                PR.nombre PasoRequeridoNombre,
                CONCAT('step-', PD.paso_requerido_id) PasoRequeridoReferencia
            FROM conf_paso_dependencias PD
            INNER JOIN conf_pasos PR ON PR.id = PD.paso_requerido_id
            ORDER BY PD.id;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
            $"Inicio de metodo [queryProcesos: {queryProcesos}, queryPasos: {queryPasos}, queryDependencias: {queryDependencias}]");

        try
        {
            using var connection = _context.CreateConnection();

            var procesos = (await connection.QueryAsync<ControlProcesoConfiguracion>(queryProcesos)).ToList();
            var pasos = (await connection.QueryAsync<ControlProcesoPasoConfiguracion>(queryPasos)).ToList();
            var dependencias = (await connection.QueryAsync<ControlProcesoDependenciaConfiguracion>(queryDependencias)).ToList();

            foreach (var proceso in procesos)
            {
                var pasosProceso = pasos
                    .Where(x => x.ProcesoId == proceso.ProcesoId)
                    .OrderBy(x => x.Orden)
                    .ThenBy(x => x.PasoId)
                    .ToList();

                foreach (var paso in pasosProceso)
                {
                    paso.Nombre = PasosDiccionario.ObtenerNombreVisual(paso.Nombre);
                    paso.Dependencias = dependencias
                        .Where(x => x.PasoId == paso.PasoId)
                        .Select(x =>
                        {
                            x.PasoRequeridoNombre = PasosDiccionario.ObtenerNombreVisual(x.PasoRequeridoNombre);
                            return x;
                        })
                        .ToList();
                    paso.DependenciasReferencia = paso.Dependencias
                        .Select(x => x.PasoRequeridoReferencia)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }

                proceso.Pasos = pasosProceso;
            }

            const string mensaje = "Configuraciones de control de proceso obtenidas correctamente.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, totalProcesos:{procesos.Count}]");

            return (true, mensaje, procesos);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al obtener las configuraciones del control de proceso: {ex.Message}", Enumerable.Empty<ControlProcesoConfiguracion>());
        }
    }
    public async Task<(bool Success, string Mensaje, ControlProcesoConfiguracion Data)> GuardarConfiguracionProceso(string LogTransaccionId, string Usuario, ControlProcesoConfiguracion Data)
    {
        string nombreMetodo = "GuardarConfiguracionProceso()";

        const string queryProcesoActual = @"
            SELECT
                id ProcesoId,
                nombre Nombre,
                IFNULL(descripcion, '') Descripcion,
                estado Estado,
                fecha_creacion FechaCreacion
            FROM conf_procesos
            WHERE id = @ProcesoId
            LIMIT 1;
        ";

        const string insertProceso = @"
            INSERT INTO conf_procesos (nombre, descripcion, estado, fecha_creacion)
            VALUES (@Nombre, @Descripcion, 1, NOW());
        ";

        const string updateProceso = @"
            UPDATE conf_procesos
            SET nombre = @Nombre,
                descripcion = @Descripcion,
                estado = 1
            WHERE id = @ProcesoId;
        ";

        const string queryPasosExistentes = @"
            SELECT
                id PasoId,
                proceso_id ProcesoId,
                CONCAT('step-', id) Referencia,
                nombre Nombre,
                orden Orden,
                es_obligatorio EsObligatorio,
                estado Estado
            FROM conf_pasos
            WHERE proceso_id = @ProcesoId;
        ";

        const string queryProcesoDuplicado = @"
            SELECT
                id ProcesoId,
                nombre Nombre,
                IFNULL(descripcion, '') Descripcion,
                estado Estado,
                fecha_creacion FechaCreacion
            FROM conf_procesos
            WHERE UPPER(nombre) = @Nombre 
              AND id <> @ProcesoId
            LIMIT 1;
        ";

        const string insertPaso = @"
            INSERT INTO conf_pasos (proceso_id, nombre, orden, es_obligatorio, estado)
            VALUES (@ProcesoId, @Nombre, @Orden, @EsObligatorio, 1);
            SELECT LAST_INSERT_ID();
        ";

        const string updatePaso = @"
            UPDATE conf_pasos
            SET nombre = @Nombre,
                orden = @Orden,
                es_obligatorio = @EsObligatorio,
                estado = 1
            WHERE id = @PasoId
              AND proceso_id = @ProcesoId;
        ";

        const string deleteDependenciasProceso = @"
            DELETE PD
            FROM conf_paso_dependencias PD
            INNER JOIN conf_pasos P ON P.id = PD.paso_id
            WHERE P.proceso_id = @ProcesoId;
        ";

        const string deleteDependenciasPaso = @"
            DELETE FROM conf_paso_dependencias
            WHERE paso_id = @PasoId
               OR paso_requerido_id = @PasoId;
        ";

        const string insertDependencia = @"
            INSERT INTO conf_paso_dependencias (paso_id, paso_requerido_id)
            VALUES (@PasoId, @PasoRequeridoId);
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [usuario:{Usuario}]");

        try
        {
            Data.Nombre = (Data.Nombre ?? "").Trim().ToUpperInvariant();
            Data.Descripcion = (Data.Descripcion ?? "").Trim();
            Data.Pasos ??= new List<ControlProcesoPasoConfiguracion>();

            if (string.IsNullOrWhiteSpace(Data.Nombre))
            {
                return (false, "Debe ingresar el nombre del proceso.", new ControlProcesoConfiguracion());
            }

            if (!Data.Pasos.Any())
            {
                return (false, "Debe registrar al menos un paso.", new ControlProcesoConfiguracion());
            }

            var pasos = Data.Pasos
                .Select(x => new ControlProcesoPasoConfiguracion
                {
                    PasoId = x.PasoId,
                    ProcesoId = x.ProcesoId,
                    Referencia = string.IsNullOrWhiteSpace(x.Referencia)
                        ? (x.PasoId > 0 ? $"step-{x.PasoId}" : $"tmp-{Guid.NewGuid():N}")
                        : x.Referencia.Trim(),
                    Nombre = PasosDiccionario.ObtenerNombreVisual((x.Nombre ?? "").Trim().ToUpperInvariant()),
                    Orden = x.Orden,
                    EsObligatorio = x.EsObligatorio,
                    Estado = 1,
                    DependenciasReferencia = (x.DependenciasReferencia ?? new List<string>())
                        .Where(refItem => !string.IsNullOrWhiteSpace(refItem))
                        .Select(refItem => refItem.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList(),
                })
                .OrderBy(x => x.Orden)
                .ThenBy(x => x.Nombre)
                .ToList();

            if (pasos.Any(x => string.IsNullOrWhiteSpace(x.Nombre)))
            {
                return (false, "Todos los pasos deben tener nombre.", new ControlProcesoConfiguracion());
            }

            if (pasos.Any(x => x.Orden <= 0))
            {
                return (false, "Todos los pasos deben tener un orden mayor a cero.", new ControlProcesoConfiguracion());
            }

            if (pasos.GroupBy(x => x.Nombre, StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1))
            {
                return (false, "No se permiten pasos duplicados dentro del mismo proceso.", new ControlProcesoConfiguracion());
            }

            if (pasos.GroupBy(x => x.Orden).Any(x => x.Count() > 1))
            {
                return (false, "No se permiten órdenes repetidos dentro del mismo proceso.", new ControlProcesoConfiguracion());
            }

            if (pasos.Any(x => x.DependenciasReferencia.Contains(x.Referencia, StringComparer.OrdinalIgnoreCase)))
            {
                return (false, "Un paso no puede depender de sí mismo.", new ControlProcesoConfiguracion());
            }

            var pasosPorReferencia = pasos.ToDictionary(x => x.Referencia, StringComparer.OrdinalIgnoreCase);

            foreach (var paso in pasos)
            {
                foreach (var dependenciaReferencia in paso.DependenciasReferencia)
                {
                    if (!pasosPorReferencia.TryGetValue(dependenciaReferencia, out var pasoDependencia))
                    {
                        return (false, $"No se encontró la dependencia {dependenciaReferencia} para el paso {paso.Nombre}.", new ControlProcesoConfiguracion());
                    }

                    if (pasoDependencia.Orden >= paso.Orden)
                    {
                        return (false, $"La dependencia {pasoDependencia.Nombre} debe estar ubicada antes del paso {paso.Nombre}.", new ControlProcesoConfiguracion());
                    }
                }
            }

            var visitados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var enProceso = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            bool TieneDependenciaCircular(ControlProcesoPasoConfiguracion paso)
            {
                if (enProceso.Contains(paso.Referencia))
                {
                    return true;
                }

                if (visitados.Contains(paso.Referencia))
                {
                    return false;
                }

                visitados.Add(paso.Referencia);
                enProceso.Add(paso.Referencia);

                foreach (var dependenciaReferencia in paso.DependenciasReferencia)
                {
                    if (pasosPorReferencia.TryGetValue(dependenciaReferencia, out var pasoDependencia)
                        && TieneDependenciaCircular(pasoDependencia))
                    {
                        return true;
                    }
                }

                enProceso.Remove(paso.Referencia);
                return false;
            }

            if (pasos.Any(TieneDependenciaCircular))
            {
                return (false, "No se permiten dependencias circulares entre pasos.", new ControlProcesoConfiguracion());
            }

            using var connection = _context.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            var procesoDuplicado = await connection.QueryFirstOrDefaultAsync<ControlProcesoConfiguracion>(
                queryProcesoDuplicado,
                new
                {
                    Nombre = Data.Nombre.ToUpperInvariant(),
                    Data.ProcesoId
                },
                transaction
            );

            if (procesoDuplicado != null && procesoDuplicado.ProcesoId > 0)
            {
                transaction.Rollback();
                return (false, $"Ya existe un proceso con el nombre {Data.Nombre}.", new ControlProcesoConfiguracion());
            }

            if (Data.ProcesoId > 0)
            {
                var procesoActual = await connection.QueryFirstOrDefaultAsync<ControlProcesoConfiguracion>(
                    queryProcesoActual,
                    new { Data.ProcesoId },
                    transaction
                );

                if (procesoActual == null || procesoActual.ProcesoId <= 0)
                {
                    transaction.Rollback();
                    return (false, "El proceso a modificar no existe.", new ControlProcesoConfiguracion());
                }

                if (string.Equals(procesoActual.Nombre, ProcesosDiccionario.COMISIONES, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(procesoActual.Nombre, Data.Nombre, StringComparison.OrdinalIgnoreCase))
                {
                    transaction.Rollback();
                    return (false, "El proceso principal COMISIONES no puede cambiar de nombre.", new ControlProcesoConfiguracion());
                }

                await connection.ExecuteAsync(
                    updateProceso,
                    new
                    {
                        Data.ProcesoId,
                        Data.Nombre,
                        Data.Descripcion
                    },
                    transaction
                );
            }
            else
            {
                Data.ProcesoId = await connection.ExecuteScalarAsync<int>(
                    insertProceso,
                    new
                    {
                        Data.Nombre,
                        Data.Descripcion
                    },
                    transaction
                );
            }

            var pasosExistentes = (await connection.QueryAsync<ControlProcesoPasoConfiguracion>(
                queryPasosExistentes,
                new { Data.ProcesoId },
                transaction
            )).ToList();

            var referencias = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var pasosVigentes = new List<int>();

            foreach (var paso in pasos)
            {
                paso.ProcesoId = Data.ProcesoId;

                if (paso.PasoId > 0)
                {
                    await connection.ExecuteAsync(
                        updatePaso,
                        new
                        {
                            paso.PasoId,
                            ProcesoId = Data.ProcesoId,
                            paso.Nombre,
                            paso.Orden,
                            paso.EsObligatorio
                        },
                        transaction
                    );
                }
                else
                {
                    paso.PasoId = await connection.ExecuteScalarAsync<int>(
                        insertPaso,
                        new
                        {
                            ProcesoId = Data.ProcesoId,
                            paso.Nombre,
                            paso.Orden,
                            paso.EsObligatorio
                        },
                        transaction
                    );
                }

                referencias[paso.Referencia] = paso.PasoId;
                pasosVigentes.Add(paso.PasoId);
            }

            var pasosInactivos = pasosExistentes
                .Where(x => !pasosVigentes.Contains(x.PasoId))
                .Select(x => x.PasoId)
                .ToList();

            foreach (var pasoId in pasosInactivos)
            {
                await connection.ExecuteAsync(
                    "UPDATE conf_pasos SET estado = 0 WHERE id = @PasoId;",
                    new { PasoId = pasoId },
                    transaction
                );

                await connection.ExecuteAsync(deleteDependenciasPaso, new { PasoId = pasoId }, transaction);
            }

            await connection.ExecuteAsync(deleteDependenciasProceso, new { Data.ProcesoId }, transaction);

            foreach (var paso in pasos)
            {
                foreach (var dependenciaReferencia in paso.DependenciasReferencia)
                {
                    if (!referencias.TryGetValue(dependenciaReferencia, out var pasoRequeridoId))
                    {
                        transaction.Rollback();
                        return (false, $"No se encontró la dependencia '{dependenciaReferencia}' para el paso {paso.Nombre}.", new ControlProcesoConfiguracion());
                    }

                    if (pasoRequeridoId == paso.PasoId)
                    {
                        transaction.Rollback();
                        return (false, "Un paso no puede depender de sí mismo.", new ControlProcesoConfiguracion());
                    }

                    await connection.ExecuteAsync(
                        insertDependencia,
                        new
                        {
                            PasoId = paso.PasoId,
                            PasoRequeridoId = pasoRequeridoId
                        },
                        transaction
                    );
                }
            }

            transaction.Commit();

            var response = await GetConfiguracionProcesos(LogTransaccionId, Usuario);
            var procesoGuardado = response.Data.FirstOrDefault(x => x.ProcesoId == Data.ProcesoId) ?? new ControlProcesoConfiguracion();

            return (true, "Configuración del proceso guardada correctamente.", procesoGuardado);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al guardar la configuración del proceso: {ex.Message}", new ControlProcesoConfiguracion());
        }
    }
    public async Task<(bool Success, string Mensaje)> DeleteConfiguracionProceso(string LogTransaccionId, string Usuario, int ProcesoId)
    {
        string nombreMetodo = "DeleteConfiguracionProceso()";

        const string queryProceso = @"
            SELECT
                id ProcesoId,
                nombre Nombre,
                IFNULL(descripcion, '') Descripcion,
                estado Estado,
                fecha_creacion FechaCreacion
            FROM conf_procesos
            WHERE id = @ProcesoId
            LIMIT 1;
        ";

        const string deleteDependenciasPaso = @"
            DELETE PD
            FROM conf_paso_dependencias PD
            INNER JOIN conf_pasos P ON P.id = PD.paso_id
            WHERE P.proceso_id = @ProcesoId;
        ";

        const string deleteDependenciasRequeridas = @"
            DELETE PD
            FROM conf_paso_dependencias PD
            INNER JOIN conf_pasos P ON P.id = PD.paso_requerido_id
            WHERE P.proceso_id = @ProcesoId;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [ProcesoId:{ProcesoId}, Usuario:{Usuario}]");

        try
        {
            using var connection = _context.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            var proceso = await connection.QueryFirstOrDefaultAsync<ControlProcesoConfiguracion>(
                queryProceso,
                new { ProcesoId },
                transaction
            );

            if (proceso == null || proceso.ProcesoId <= 0)
            {
                transaction.Rollback();
                return (false, "No se encontró el proceso a eliminar.");
            }

            if (string.Equals(proceso.Nombre, ProcesosDiccionario.COMISIONES, StringComparison.OrdinalIgnoreCase))
            {
                transaction.Rollback();
                return (false, "El proceso principal COMISIONES no se puede desactivar.");
            }

            await connection.ExecuteAsync("UPDATE conf_procesos SET estado = 0 WHERE id = @ProcesoId;", new { ProcesoId }, transaction);
            await connection.ExecuteAsync("UPDATE conf_pasos SET estado = 0 WHERE proceso_id = @ProcesoId;", new { ProcesoId }, transaction);
            await connection.ExecuteAsync(deleteDependenciasPaso, new { ProcesoId }, transaction);
            await connection.ExecuteAsync(deleteDependenciasRequeridas, new { ProcesoId }, transaction);

            transaction.Commit();

            return (true, "Proceso desactivado correctamente.");
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al eliminar la configuración del proceso: {ex.Message}");
        }
    }
    public async Task<(ItemControlProceso Data, bool Success, string Mensaje)> GetControlProceso(string LogTransaccionId, string Usuario, string Paso, int LCicloId)
    {
         string nombreMetodo = "GetProceso()";

        string query = @"select * from ControlProceso where paso = @Paso and lciclo_id = @LCicloId order by ControlProcesoId desc limit 1";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var proceso = await connection.QueryFirstOrDefaultAsync<ItemControlProceso>(query, new {Paso, LCicloId});

            bool success = true;
            string mensaje = success ? "Tipos de descuento obtenidos correctamente." : "No se encontraron tipos de descuento.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, tiposDescuento:{JsonConvert.SerializeObject(proceso, Formatting.Indented)}]");

            return (proceso ?? new ItemControlProceso(), success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (new ItemControlProceso(), false, $"Error al obtener los tipos de descuento: {ex.Message}");
        }
    }
    public async Task<(bool Success, string Mensaje)> GuardarControlProceso(string LogTransaccionId, string Usuario, ItemControlProceso Data)
    {
        string metodo = "GuardarControlProceso()";

        const string insertQuery = @"INSERT INTO ControlProceso (
                                        lciclo_id,
                                        paso,
                                        inicio,
                                        fin,
                                        estado,
                                        fechaadd,
                                        usuarioadd,
                                        fechamod,
                                        usuariomod
                                    )
                                    VALUES (
                                        @lciclo_id,
                                        @paso,
                                        @inicio,
                                        @fin,
                                        @estado,
                                        @fechaadd,
                                        @usuarioadd,
                                        @fechamod,
                                        @usuariomod
                                    );";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo, $"Inicio inserción. Script: { insertQuery}");

        try
        {
            using var con = _context.CreateConnection();

            var rows = await con.ExecuteAsync(insertQuery, Data);

            bool success = rows > 0;
            string mensaje = success ? "Registro insertado correctamente." : "No se insertó el registro.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo, $"Fin inserción. Success={success}. Mensaje:{mensaje}");

            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, metodo, "Error insertando semana", ex);
            return (false, ex.Message);
        }
    }
    public async Task<(bool Success, string Mensaje)> UpdateControlProceso(string LogTransaccionId, string Usuario, string Paso, int LCicloId)
    {
        string nombreMetodo = "UpdateControlProceso()";

        const string query = @"
            UPDATE ControlProceso SET fechamod = NOW(), usuariomod = @Usuario, fin = NOW() WHERE paso = @Paso AND lciclo_id = @LCicloId    
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var rows = await connection.ExecuteAsync(query, new
            {
                Usuario, Paso, LCicloId
            });

            bool success = rows > 0;
            string mensaje = success ? "Registro actualizado correctamente." : "No se encontró el registro o no se realizaron cambios.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, rowsAffected:{rows}, Usuario:{Usuario}, Paso:{Paso}, LCicloId: {LCicloId} ]");

            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al actualizar el registro: {ex.Message}");
        }
    }



    public async Task<(bool Success, string Mensaje, ItemControlProcesoNext Data)> GetSiguientePaso(string LogTransaccionId, string Usuario, string proceso, int LCicloId)
    {
        string nombreMetodo = "GetSiguientePaso()";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [proceso:{proceso}, LCicloId:{LCicloId}]");

        try
        {
            using var connection = _context.CreateConnection();
            var contextResponse = await ResolvePasoContextAsync(
                connection,
                proceso,
                LCicloId,
                string.Empty,
                false
            );

            if (!contextResponse.Success)
            {
                return (false, contextResponse.Mensaje, new ItemControlProcesoNext());
            }

            var context = contextResponse.Data;

            const string queryPasoEnProceso = @"
                SELECT
                    TRUE status,
                    'Paso en proceso.' mensajes,
                    TRUE next,
                    CP.id,
                    CP.nombre,
                    CP.orden,
                    CP.es_obligatorio EsObligatoria
                FROM conf_proceso_pasos CPP
                INNER JOIN conf_pasos CP ON CP.id = CPP.paso_id
                WHERE CPP.proceso_ciclo_id = @ProcesoCicloId
                  AND CPP.estado = 'EN_PROCESO'
                ORDER BY CPP.fecha_inicio DESC, CPP.id DESC
                LIMIT 1;
            ";

            const string querySiguientePaso = @"
                SELECT
                    TRUE status,
                    'OK' mensajes,
                    TRUE next,
                    CP.id,
                    CP.nombre,
                    CP.orden,
                    CP.es_obligatorio EsObligatoria
                FROM conf_pasos CP
                WHERE CP.proceso_id = @ProcesoId
                  AND CP.estado = 1
                  AND NOT EXISTS (
                        SELECT 1
                        FROM conf_proceso_pasos CPP
                        WHERE CPP.proceso_ciclo_id = @ProcesoCicloId
                          AND CPP.paso_id = CP.id
                  )
                  AND NOT EXISTS (
                        SELECT 1
                        FROM conf_paso_dependencias PD
                        LEFT JOIN conf_proceso_pasos CPP
                            ON CPP.paso_id = PD.paso_requerido_id
                           AND CPP.proceso_ciclo_id = @ProcesoCicloId
                        WHERE PD.paso_id = CP.id
                          AND (CPP.estado IS NULL OR CPP.estado <> 'COMPLETADO')
                  )
                ORDER BY CP.orden
                LIMIT 1;
            ";

            ItemControlProcesoNext item = new ItemControlProcesoNext();
            if (context.ProcesoCicloId > 0)
            {
                item = await connection.QueryFirstOrDefaultAsync<ItemControlProcesoNext>(
                    queryPasoEnProceso,
                    new { context.ProcesoCicloId }
                ) ?? new ItemControlProcesoNext();
            }

            if (item.id <= 0)
            {
                item = await connection.QueryFirstOrDefaultAsync<ItemControlProcesoNext>(
                    querySiguientePaso,
                    new
                    {
                        context.ProcesoId,
                        ProcesoCicloId = context.ProcesoCicloId > 0 ? context.ProcesoCicloId : 0
                    }
                ) ?? new ItemControlProcesoNext();
            }

            bool success = true;
            string mensaje =  "Procedimiento ejecutado correctamente";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, Siguiente Paso:{JsonConvert.SerializeObject(item, Formatting.Indented)}]");

            return (success, mensaje, item ?? new ItemControlProcesoNext());
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al obtener el siguiente paso: {ex.Message}", new ItemControlProcesoNext());
        }
    }
    public async Task<(bool Success, string Mensaje, ItemControlProcesoPrincipal Data)> IniciarPaso(string LogTransaccionId, string Usuario, string proceso, int LCicloId, string paso)
    {
        string nombreMetodo = "IniciarPaso()";

        const string queryPasoExistente = @"
            SELECT id, estado
            FROM conf_proceso_pasos
            WHERE proceso_ciclo_id = @ProcesoCicloId
              AND paso_id = @PasoId
            ORDER BY id DESC
            LIMIT 1;
        ";

        const string queryDependenciasPendientes = @"
            SELECT COUNT(*)
            FROM conf_paso_dependencias PD
            LEFT JOIN conf_proceso_pasos CPP
                ON CPP.paso_id = PD.paso_requerido_id
               AND CPP.proceso_ciclo_id = @ProcesoCicloId
            WHERE PD.paso_id = @PasoId
              AND (CPP.estado IS NULL OR CPP.estado <> 'COMPLETADO');
        ";

        const string insertPaso = @"
            INSERT INTO conf_proceso_pasos (proceso_ciclo_id, paso_id, estado, fecha_inicio, fecha_fin)
            VALUES (@ProcesoCicloId, @PasoId, 'EN_PROCESO', NOW(), NULL);
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [proceso:{proceso}, LCicloId:{LCicloId}, paso:{paso}]");

        try
        {
            using var connection = _context.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            var contextResponse = await ResolvePasoContextAsync(
                connection,
                proceso,
                LCicloId,
                paso,
                true,
                transaction
            );

            if (!contextResponse.Success)
            {
                transaction.Rollback();
                return (false, contextResponse.Mensaje, BuildPasoResponse(false, contextResponse.Mensaje, false));
            }

            var context = contextResponse.Data;

            var pasoExistente = await connection.QueryFirstOrDefaultAsync<ControlProcesoPasoEstadoRow>(
                queryPasoExistente,
                new { context.ProcesoCicloId, context.PasoId },
                transaction
            ) ?? new ControlProcesoPasoEstadoRow();

            if (pasoExistente.id > 0)
            {
                transaction.Rollback();
                string mensajeExistente = string.Equals(pasoExistente.estado, "EN_PROCESO", StringComparison.OrdinalIgnoreCase)
                    ? "El paso ya se encuentra en proceso."
                    : "Paso ya ejecutado.";

                return (false, mensajeExistente, BuildPasoResponse(false, mensajeExistente, false));
            }

            int dependenciasPendientes = await connection.ExecuteScalarAsync<int>(
                queryDependenciasPendientes,
                new { context.ProcesoCicloId, context.PasoId },
                transaction
            );

            if (dependenciasPendientes > 0)
            {
                transaction.Rollback();
                const string mensajeDependencias = "Dependencias no cumplidas.";
                return (false, mensajeDependencias, BuildPasoResponse(false, mensajeDependencias, false));
            }

            await connection.ExecuteAsync(
                insertPaso,
                new { context.ProcesoCicloId, context.PasoId },
                transaction
            );

            transaction.Commit();

            const string mensaje = "Paso iniciado correctamente.";
            return (true, mensaje, BuildPasoResponse(true, mensaje));
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al iniciar el paso: {ex.Message}", BuildPasoResponse(false, $"Error al iniciar el paso: {ex.Message}", false));
        }
    }
    public async Task<(bool Success, string Mensaje, ItemControlProcesoPrincipal Data)> FinalizarPaso(string LogTransaccionId, string Usuario, string proceso, int LCicloId, string paso)
    {
        string nombreMetodo = "FinalizarPaso()";

        const string queryPasoExistente = @"
            SELECT id, estado
            FROM conf_proceso_pasos
            WHERE proceso_ciclo_id = @ProcesoCicloId
              AND paso_id = @PasoId
            ORDER BY id DESC
            LIMIT 1;
        ";

        const string updatePaso = @"
            UPDATE conf_proceso_pasos
            SET estado = 'COMPLETADO',
                fecha_fin = NOW()
            WHERE id = @PasoRegistroId
              AND estado = 'EN_PROCESO';
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [proceso:{proceso}, LCicloId:{LCicloId}, paso:{paso}]");

        try
        {
            using var connection = _context.CreateConnection();

            var contextResponse = await ResolvePasoContextAsync(
                connection,
                proceso,
                LCicloId,
                paso,
                false
            );

            if (!contextResponse.Success)
            {
                return (false, contextResponse.Mensaje, BuildPasoResponse(false, contextResponse.Mensaje, false));
            }

            var context = contextResponse.Data;
            var pasoExistente = await connection.QueryFirstOrDefaultAsync<ControlProcesoPasoEstadoRow>(
                queryPasoExistente,
                new { context.ProcesoCicloId, context.PasoId }
            ) ?? new ControlProcesoPasoEstadoRow();

            if (pasoExistente.id <= 0)
            {
                const string mensajeNoIniciado = "El paso no fue iniciado.";
                return (false, mensajeNoIniciado, BuildPasoResponse(false, mensajeNoIniciado, false));
            }

            if (string.Equals(pasoExistente.estado, "COMPLETADO", StringComparison.OrdinalIgnoreCase))
            {
                const string mensajeYaCompletado = "El paso ya estaba completado.";
                return (true, mensajeYaCompletado, BuildPasoResponse(true, mensajeYaCompletado));
            }

            var rows = await connection.ExecuteAsync(
                updatePaso,
                new { PasoRegistroId = pasoExistente.id }
            );

            bool success = rows > 0;
            string mensaje = success ? "Paso ejecutado correctamente." : "No se pudo completar el paso.";

            return (success, mensaje, BuildPasoResponse(success, mensaje, success));
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al finalizar el paso: {ex.Message}", BuildPasoResponse(false, $"Error al finalizar el paso: {ex.Message}", false));
        }
    }
    public async Task<(bool Success, string Mensaje, ItemControlProcesoPrincipal Data)> CancelarPaso(string LogTransaccionId, string Usuario, string proceso, int LCicloId, string paso)
    {
        string nombreMetodo = "CancelarPaso()";

        const string queryPasoExistente = @"
            SELECT id, estado
            FROM conf_proceso_pasos
            WHERE proceso_ciclo_id = @ProcesoCicloId
              AND paso_id = @PasoId
            ORDER BY id DESC
            LIMIT 1;
        ";

        const string deletePaso = @"
            DELETE FROM conf_proceso_pasos
            WHERE id = @PasoRegistroId
              AND estado = 'EN_PROCESO';
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [proceso:{proceso}, LCicloId:{LCicloId}, paso:{paso}]");

        try
        {
            using var connection = _context.CreateConnection();

            var contextResponse = await ResolvePasoContextAsync(
                connection,
                proceso,
                LCicloId,
                paso,
                false
            );

            if (!contextResponse.Success)
            {
                return (true, contextResponse.Mensaje, BuildPasoResponse(true, contextResponse.Mensaje));
            }

            var context = contextResponse.Data;
            var pasoExistente = await connection.QueryFirstOrDefaultAsync<ControlProcesoPasoEstadoRow>(
                queryPasoExistente,
                new { context.ProcesoCicloId, context.PasoId }
            ) ?? new ControlProcesoPasoEstadoRow();

            if (pasoExistente.id <= 0 || !string.Equals(pasoExistente.estado, "EN_PROCESO", StringComparison.OrdinalIgnoreCase))
            {
                const string mensajeSinPaso = "No existe un paso en proceso para cancelar.";
                return (true, mensajeSinPaso, BuildPasoResponse(true, mensajeSinPaso));
            }

            await connection.ExecuteAsync(deletePaso, new { PasoRegistroId = pasoExistente.id });

            const string mensaje = "Paso cancelado correctamente.";
            return (true, mensaje, BuildPasoResponse(true, mensaje));
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al cancelar el paso: {ex.Message}", BuildPasoResponse(false, $"Error al cancelar el paso: {ex.Message}", false));
        }
    }
    public async Task<(bool Success, string Mensaje, ItemControlProcesoPrincipal Data)> EjecutarPaso(string LogTransaccionId, string Usuario, string proceso, int LCicloId, string paso)
    {
        string nombreMetodo = "EjecutarPaso()";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [proceso:{proceso}, LCicloId:{LCicloId}, paso:{paso}]");

        try
        {
            var responseInicio = await IniciarPaso(LogTransaccionId, Usuario, proceso, LCicloId, paso);
            if (!responseInicio.Success || !(responseInicio.Data?.status ?? false))
            {
                return responseInicio;
            }

            var responseFin = await FinalizarPaso(LogTransaccionId, Usuario, proceso, LCicloId, paso);

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [Response EjecutarPaso:{JsonConvert.SerializeObject(responseFin.Data, Formatting.Indented)}]");

            return responseFin;
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al ejecutar el paso: {ex.Message}", new ItemControlProcesoPrincipal());
        }
    }
    public async Task<(bool Success, string Mensaje, ItemControlProcesoResumen Data)> GetResumenProcesoCiclo(string LogTransaccionId, string Usuario, string proceso, int LCicloId)
    {
        string nombreMetodo = "GetResumenProcesoCiclo()";
        const string queryHistorial = @"
            SELECT
                CPC.id ProcesoCicloId,
                CPC.numero_ciclo NumeroCiclo,
                CPC.estado Estado,
                CPC.fecha_inicio FechaInicio,
                CPC.fecha_fin FechaFin
            FROM conf_proceso_ciclos CPC
            INNER JOIN conf_proceso_instancias CPI ON CPI.id = CPC.proceso_instancia_id
            WHERE CPI.proceso_id = @ProcesoId
              AND CPC.numero_ciclo = @LCicloId
            ORDER BY CPC.id DESC;
        ";

        const string queryPasos = @"
            SELECT
                CP.id PasoId,
                CP.nombre NombreInterno,
                CP.orden Orden,
                CP.es_obligatorio EsObligatorio,
                IFNULL(CPP.estado, 'PENDIENTE') Estado,
                CPP.fecha_inicio FechaInicio,
                CPP.fecha_fin FechaFin
            FROM conf_pasos CP
            LEFT JOIN conf_proceso_pasos CPP
                ON CPP.paso_id = CP.id
               AND CPP.proceso_ciclo_id = @ProcesoCicloId
            WHERE CP.proceso_id = @ProcesoId
              AND CP.estado = 1
            ORDER BY CP.orden;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
            $"Inicio de metodo [queryHistorial: {queryHistorial}, queryPasos: {queryPasos}]");

        try
        {
            using var connection = _context.CreateConnection();
            var procesoConfigurado = await GetProcesoConfiguradoAsync(connection, proceso, LCicloId);
            if (procesoConfigurado == null || procesoConfigurado.ProcesoId <= 0)
            {
                return (false, "El proceso no existe.", new ItemControlProcesoResumen());
            }

            var resumen = new ItemControlProcesoResumen
            {
                ProcesoId = procesoConfigurado.ProcesoId,
                Proceso = procesoConfigurado.Nombre,
                Descripcion = procesoConfigurado.Descripcion
            };

            var historial = (await connection.QueryAsync<ItemControlProcesoHistorial>(queryHistorial, new
            {
                ProcesoId = procesoConfigurado.ProcesoId,
                LCicloId
            })).ToList();
            
            
            var cicloActual = historial.FirstOrDefault(x => string.Equals(x.Estado, "EN_PROCESO", StringComparison.OrdinalIgnoreCase))
                ?? historial.FirstOrDefault();

            var pasos = (await connection.QueryAsync<ItemControlProcesoPasoDetalle>(queryPasos, new
            {
                ProcesoId = resumen.ProcesoId,
                ProcesoCicloId = cicloActual?.ProcesoCicloId ?? 0
            })).ToList();

            ItemControlProcesoNext siguientePaso = new ItemControlProcesoNext();
            if (cicloActual == null || string.Equals(cicloActual.Estado, "EN_PROCESO", StringComparison.OrdinalIgnoreCase))
            {
                var responseSiguientePaso = await GetSiguientePaso(LogTransaccionId, Usuario, proceso, LCicloId);
                if (responseSiguientePaso.Success)
                {
                    siguientePaso = responseSiguientePaso.Data ?? new ItemControlProcesoNext();
                }
            }

            foreach (var paso in pasos)
            {
                paso.Nombre = PasosDiccionario.ObtenerNombreVisual(paso.NombreInterno);
                paso.Ejecutado = !string.Equals(paso.Estado, "PENDIENTE", StringComparison.OrdinalIgnoreCase);
                paso.EsSiguientePaso = !string.IsNullOrWhiteSpace(siguientePaso.nombre)
                    && string.Equals(paso.NombreInterno, siguientePaso.nombre, StringComparison.OrdinalIgnoreCase);
            }

            foreach (var item in historial)
            {
                item.EsActual = cicloActual != null && item.ProcesoCicloId == cicloActual.ProcesoCicloId;
            }

            resumen.LCicloId = LCicloId;
            resumen.ProcesoCicloId = cicloActual?.ProcesoCicloId ?? 0;
            resumen.ExisteCiclo = historial.Count > 0;
            resumen.PuedeResetear = cicloActual != null;
            resumen.EstadoCiclo = cicloActual?.Estado ?? "NO_INICIADO";
            resumen.FechaInicio = cicloActual?.FechaInicio;
            resumen.FechaFin = cicloActual?.FechaFin;
            resumen.SiguientePaso = PasosDiccionario.ObtenerNombreVisual(siguientePaso.nombre);
            resumen.SiguientePasoOrden = siguientePaso.orden;
            resumen.Pasos = pasos;
            resumen.Historial = historial;

            string mensaje = historial.Count > 0
                ? "Proceso del ciclo obtenido correctamente."
                : "El ciclo no tiene ejecuciones registradas.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, data:{JsonConvert.SerializeObject(resumen, Formatting.Indented)}]");

            return (true, mensaje, resumen);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al obtener el detalle del proceso: {ex.Message}", new ItemControlProcesoResumen());
        }
    }
    public async Task<(bool Success, string Mensaje, ItemControlProcesoPrincipal Data)> ReiniciarCiclo(string LogTransaccionId, string Usuario, string proceso, int LCicloId, string Inicio, string Fin)
    {
        string nombreMetodo = "ReiniciarCiclo()";

        const string query = @"CALL sp_reiniciar_ciclo(@proceso, @LCicloId);";

        const string deleteBonoParDetalle = @"
            DELETE BPD
            FROM bonopardetalle BPD
            INNER JOIN bonopar BP ON BP.id = BPD.bonopar_id
            WHERE BP.lciclo_id = @LCicloId;
        ";
        const string deleteBonoPar = @"DELETE FROM bonopar WHERE lciclo_id = @LCicloId;";
        const string deleteVentaPersonal = @"DELETE FROM administracionventapersonal WHERE lciclo_id = @LCicloId;";
        const string deleteVentaGrupo = @"DELETE FROM administracionventagrupo WHERE lciclo_id = @LCicloId;";
        const string deleteBonoResidual = @"DELETE FROM administracionbonoresidual WHERE lciclo_id = @LCicloId;";
        const string deleteRedEmpresaComplejo = @"DELETE FROM administracionredempresacomplejo WHERE lciclo_id = @LCicloId;";
        const string deleteBonoCompleto = @"DELETE FROM t_bonocompleto WHERE lciclo_id = @LCicloId;";
        const string deleteRedComprimida = @"DELETE FROM red_comprimida WHERE lciclo_id = @LCicloId;";
        const string deleteRedCompletaCuotas = @"DELETE FROM red_completa_cuotas WHERE lciclo_id = @LCicloId;";
        const string deleteHabilitaciones = @"DELETE FROM administracionhabilitacioncomision WHERE lciclo_id = @LCicloId;";
        const string deleteControlProceso = @"DELETE FROM ControlProceso WHERE lciclo_id = @LCicloId;";
        const string deleteContrato = @"delete from administracioncontrato where dtfecha BETWEEN @Inicio and @Fin;";
        const string deleteCuotas = @"DELETE FROM T_ACCIONESCUOTASGRL WHERE FECHA_PAGO BETWEEN @Inicio AND @Fin;"; 
        const string deleteCuotasVentasResidual = @"delete from t_cuotas_ventas_productos_pagar_mensual where FECHA_RECIBO BETWEEN @Inicio AND @Fin;"; 
        const string upadteProductosPagarMensual = @"UPDATE t_productos_pagar_mensuales m
                                                    INNER JOIN
                                                    (
                                                        SELECT 
                                                            fk_id_producto_pagar,
                                                            SUM(cant_cuotas) AS total_cuotas
                                                        FROM t_productos_detalle_cuotas
                                                        WHERE lciclo_id = @LCicloId
                                                        GROUP BY fk_id_producto_pagar
                                                    ) d
                                                    ON m.id_producto_pagar = d.fk_id_producto_pagar
                                                    SET m.cuot_pagadas = m.cuot_pagadas - d.total_cuotas;"; 
        const string deleteProductosDetalleCuotas = @"DELETE FROM t_productos_detalle_cuotas WHERE lciclo_id = @LCicloId;";
        const string updateVentaRezagadas = @"update VentaRezagadasCiclo set EstadoVentaRezagadasCicloId = 1, FechaProceso = null where lciclo_id = @LCicloId;";
        const string deleteVentaRezagadas = @"delete from VentaRezagadasCiclo where dfecha BETWEEN @Inicio AND @Fin;";
        const string deleteProductosPagarMensuales = @"DELETE from t_productos_pagar_mensuales where dtfecha BETWEEN @Inicio AND @Fin";
        const string deleteUpgradeSolicitud = @"delete from upgrade_solicitud where lciclo_id = @LCicloId or lciclo_id is null;";
        

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [proceso:{proceso}, LCicloId:{LCicloId}]");

        try
        {
            using var connection = _context.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            await connection.ExecuteAsync(deleteContrato, new { Inicio, Fin }, transaction);
            await connection.ExecuteAsync(deleteVentaPersonal, new { LCicloId }, transaction);
            await connection.ExecuteAsync(deleteHabilitaciones, new { LCicloId }, transaction);
            await connection.ExecuteAsync(deleteRedComprimida, new { LCicloId }, transaction);
            await connection.ExecuteAsync(deleteVentaGrupo, new { LCicloId }, transaction);
            await connection.ExecuteAsync(deleteCuotas, new { Inicio, Fin }, transaction);
            await connection.ExecuteAsync(deleteBonoResidual, new { LCicloId }, transaction);
            await connection.ExecuteAsync(deleteBonoCompleto, new { LCicloId }, transaction);
            await connection.ExecuteAsync(deleteRedEmpresaComplejo, new { LCicloId }, transaction);
            await connection.ExecuteAsync(deleteCuotasVentasResidual, new { Inicio, Fin }, transaction);
            await connection.ExecuteAsync(upadteProductosPagarMensual, new { LCicloId }, transaction);
            await connection.ExecuteAsync(deleteProductosDetalleCuotas, new { LCicloId }, transaction);
            await connection.ExecuteAsync(deleteBonoParDetalle, new { LCicloId }, transaction);
            await connection.ExecuteAsync(deleteBonoPar, new { LCicloId }, transaction);
            
            await connection.ExecuteAsync(deleteRedCompletaCuotas, new { LCicloId }, transaction);
            await connection.ExecuteAsync(deleteControlProceso, new { LCicloId }, transaction);
            await connection.ExecuteAsync(updateVentaRezagadas, new { LCicloId}, transaction);
            await connection.ExecuteAsync(deleteVentaRezagadas, new { Inicio, Fin}, transaction);
            await connection.ExecuteAsync(deleteProductosPagarMensuales, new { Inicio, Fin}, transaction);
            await connection.ExecuteAsync(deleteUpgradeSolicitud, new { LCicloId}, transaction);


            var item = await connection.QueryFirstOrDefaultAsync<ItemControlProcesoPrincipal>(query, new {proceso, LCicloId}, transaction);
            if (item != null && string.IsNullOrWhiteSpace(item.mensaje))
            {
                item.mensaje = item.mensajes;
            }

            

            if (item == null || !item.status)
            {
                transaction.Rollback();
                string mensajeError = item?.mensaje ?? "No se pudo reiniciar el ciclo.";
                return (false, mensajeError, item ?? new ItemControlProcesoPrincipal());
            }
          
            transaction.Commit();

            

            bool success = true;
            string mensaje = "Procedimiento ejecutado correctamente";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, Response ReiniciarCiclo:{JsonConvert.SerializeObject(item, Formatting.Indented)}]");

            return (success, mensaje, item ?? new ItemControlProcesoPrincipal());
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al reiniciar el ciclo: {ex.Message}", new ItemControlProcesoPrincipal());
        }
    }
    public async Task<(bool Success, string Mensaje, ItemControlProcesoPrincipal Data)> CerrarCiclo(string LogTransaccionId, string Usuario, string proceso, int LCicloId)
    {
        string nombreMetodo = "CerrarCiclo()";

        string query = $@"CALL sp_cerrar_ciclo(@proceso, @LCicloId);";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");
        try
        {
            using var connection = _context.CreateConnection();
            var item = await connection.QueryFirstOrDefaultAsync<ItemControlProcesoPrincipal>(query, new {proceso, LCicloId});
            if (item != null && string.IsNullOrWhiteSpace(item.mensaje))
            {
                item.mensaje = item.mensajes;
            }

  
            bool success = true;
            string mensaje =  "Procedimiento ejecutado correctamente";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, Response CerrarCiclo:{JsonConvert.SerializeObject(item, Formatting.Indented)}]");

            return (success, mensaje, item ?? new ItemControlProcesoPrincipal());
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al cerrar el ciclo: {ex.Message}", new ItemControlProcesoPrincipal());
        }
    }


}
