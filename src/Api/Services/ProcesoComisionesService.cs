using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace CleanDapperApi.Api.Services;

public sealed class ProcesoComisionesService : IProcesoComisionesService
{
    private readonly ILogService _log;
    private readonly IProcesoComisionesRepository _procesoComisionesRepository;
    private readonly IAdministracionCicloRepository _administracionCicloRepository;
    private readonly IControlProcesoRepository _controlProcesoRepository;
    private readonly ICasosEspecialesRepository _casosEspecialesRepository;
    private readonly IVentasCnxRepository _ventasCnxRepository;
    private readonly IAdministracionContratoRepository _contratoRepository;
    private readonly IAdministracionVentaPersonalRepository _ventaPersonalRepository;
    private readonly IAdministracionVentaGrupoRepository _ventaGrupoRepository;
    private readonly IAdministracionHabilitacionComisionRepository _habilitacionRepository;
    private readonly IAdministracionSemanaCicloRepository _semanaCicloRepository;
    private readonly ICuotasVentaResidualRepository _cuotasRepository;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly HashSet<int> _contactosConComisionGrupoCero;

    private const string NombreArchivo = "ProcesoComisionesService.cs";

    public ProcesoComisionesService(
        ILogService log,
        IProcesoComisionesRepository procesoComisionesRepository,
        IAdministracionCicloRepository administracionCicloRepository,
        IControlProcesoRepository controlProcesoRepository,
        ICasosEspecialesRepository casosEspecialesRepository,
        IServiceScopeFactory serviceScopeFactory,
        IVentasCnxRepository ventasCnxRepository,
        IAdministracionContratoRepository contratoRepository,
        IAdministracionVentaPersonalRepository ventaPersonalRepository,
        IAdministracionVentaGrupoRepository ventaGrupoRepository,
        IAdministracionHabilitacionComisionRepository habilitacionRepository,
        IAdministracionSemanaCicloRepository semanaCicloRepository,
        ICuotasVentaResidualRepository cuotasRepository,
        IConfiguration configuration
    )
    {
        _log = log;
        _procesoComisionesRepository = procesoComisionesRepository;
        _administracionCicloRepository = administracionCicloRepository;
        _controlProcesoRepository = controlProcesoRepository;
        _casosEspecialesRepository = casosEspecialesRepository;
        _serviceScopeFactory = serviceScopeFactory;
        _ventasCnxRepository = ventasCnxRepository;
        _contratoRepository = contratoRepository;
        _ventaPersonalRepository = ventaPersonalRepository;
        _ventaGrupoRepository = ventaGrupoRepository;
        _habilitacionRepository = habilitacionRepository;
        _semanaCicloRepository = semanaCicloRepository;
        _cuotasRepository = cuotasRepository;
        _contactosConComisionGrupoCero = configuration
            .GetSection("HabilidacionesParaNoComprimirRed")
            .Get<int[]>()
            ?.Where(contactoId => contactoId > 0)
            .ToHashSet() ?? new HashSet<int>();
    }

    public async Task<(bool Success, string Mensaje)> GuardarVentaAsync(
        RequestGuardarVentaGRD request,
        string logTransaccionId
    )
    {
        try
        {
            var ciclo = await _administracionCicloRepository.GetCiclo(
                logTransaccionId,
                request.LCicloId
            );
            if (!ciclo.Success || ciclo.Data.LCicloId <= 0)
                return (false, $"No se encontró el ciclo {request.LCicloId}.");

            var inicio = ciclo.Data.DtFechaInicio ?? string.Empty;
            var fin = ciclo.Data.DtFechaFin ?? string.Empty;
            if (string.IsNullOrWhiteSpace(inicio) || string.IsNullOrWhiteSpace(fin))
                return (false, $"El ciclo {request.LCicloId} no tiene fechas configuradas.");

            var siguientePaso = await _controlProcesoRepository.GetSiguientePaso(
                logTransaccionId,
                request.Usuario,
                ProcesosDiccionario.COMISIONES,
                request.LCicloId
            );

            var pasoEsperado = (request.Rezagada, request.EsEspecial) switch
            {
                (true, _) => PasosDiccionario.ADICIONAR_VENTAS,
                (false, true) => PasosDiccionario.VENTAS_ESPECIALES,
                _ => PasosDiccionario.OBTENER_VENTAS,
            };

            if (pasoEsperado != siguientePaso.Data.nombre)
                return (
                    false,
                    "Este paso ya se encuentra ejecutado para este ciclo, si quieres volver a procesar debes reiniciar el proceso para el ciclo"
                );

            var inicioPaso = await _controlProcesoRepository.IniciarPaso(
                logTransaccionId,
                request.Usuario,
                ProcesosDiccionario.COMISIONES,
                request.LCicloId,
                pasoEsperado
            );

            if (!inicioPaso.Success || !(inicioPaso.Data?.status ?? false))
                return (false, inicioPaso.Data?.mensaje ?? inicioPaso.Mensaje);

            if (request.Rezagada)
                await _procesoComisionesRepository.GuardarVtaRezagadas(
                    logTransaccionId,
                    request.NoListaSeleccionado,
                    ""
                );

            if (request.EsEspecial)
            {
                var upgrade = await _casosEspecialesRepository.GetUpgradeSolicitudPorVentasCnx(
                    logTransaccionId,
                    request.Usuario,
                    string.Join(
                        ",",
                        request
                            .ListaSeleccionado.Where(x =>
                                x.TipoComisionable
                                == TiposContratosDiccionario.TiposContratosDiccionarioCnx.UPGRADE
                            )
                            .Select(x => x.IdVenta)
                    )
                );

                await _casosEspecialesRepository.SaveUpgradeSolicitud(
                    logTransaccionId,
                    request.Usuario,
                    request.LCicloId,
                    upgrade.Lista.ToList()
                );
            }

            var proceso = new RequestProcesoPrincipal
            {
                Tipo = "API",
                Rezagada = request.Rezagada,
                Usuario = request.Usuario,
                LCicloId = request.LCicloId,
                Paso = pasoEsperado,
            };

            var ventasSeleccionadas =
                request.ListaSeleccionado?.ToList() ?? new List<ItemVentaCnx>();
            if (request.Rezagada)
            {
                foreach (var venta in ventasSeleccionadas)
                    venta.DFecha = venta.DFecha.AddMonths(1);
            }

            // Se conserva deliberadamente el procesamiento en segundo plano y la respuesta inmediata.
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var cronJob = scope.ServiceProvider.GetRequiredService<MiCronJob>();
                    await cronJob.ProcesoPrincipal(logTransaccionId, proceso, ventasSeleccionadas);
                }
                catch (Exception ex)
                {
                    _log.Error(
                        logTransaccionId,
                        NombreArchivo,
                        "GuardarVentaAsync()",
                        "Error en procesamiento en segundo plano",
                        ex
                    );
                }
            });

            return (
                true,
                "Se esta registrando las ventas en segundo plano, por favor espere que termine para realizar el calculo de comisiones"
            );
        }
        catch
        {
            return (
                false,
                "Hubo un problema con el registro de ventas, por favor contactese con el administracion del sistema."
            );
        }
    }

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerVentaCnxAsync(int cicloId)
    {
        try
        {
            var fechas = await FechasAsync(cicloId);
            if (!fechas.Success)
                return (false, fechas.Mensaje, "");
            var ventas = await _ventasCnxRepository.GetVentaCnx(Id(), fechas.Inicio, fechas.Fin);
            var contratos = await _contratoRepository.GetContratoFecha(
                Id(),
                fechas.Inicio,
                fechas.Fin
            );
            var paso = await _controlProcesoRepository.GetSiguientePaso(
                Id(),
                "system",
                ProcesosDiccionario.COMISIONES,
                cicloId
            );
            foreach (var item in ventas.Data)
            {
                var limite = Math.Ceiling(item.DPrecio * .10m);
                if (item.SCuotaInicial > limite)
                    item.SCuotaInicial = limite;
            }

            return (
                ventas.Success,
                ventas.Mensaje,
                new
                {
                    VtaCnx = ventas.Data,
                    VtaGrd = contratos.Data,
                    controlPasos = new
                    {
                        ejecutado = PasosDiccionario.OBTENER_VENTAS != paso.Data.nombre,
                        data = paso.Data,
                    },
                }
            );
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerVtaRezagadasAsync(
        string usuario,
        int cicloId
    )
    {
        try
        {
            var r = await _procesoComisionesRepository.GetVtaRezada(Id(), usuario);
            var paso = await _controlProcesoRepository.GetSiguientePaso(
                Id(),
                usuario,
                ProcesosDiccionario.COMISIONES,
                cicloId
            );
            return (
                r.Success,
                r.Mensaje,
                new
                {
                    r.Data,
                    controlPasos = new
                    {
                        ejecutado = PasosDiccionario.ADICIONAR_VENTAS != paso.Data.nombre,
                        data = paso.Data,
                    },
                }
            );
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerVentaPersonalAsync(
        string usuario,
        int cicloId
    )
    {
        try
        {
            var fechas = await FechasAsync(cicloId);
            if (!fechas.Success)
                return (false, fechas.Mensaje, "");
            var ventas = await _procesoComisionesRepository.GetCalculoVentaPersonal(
                Id(),
                usuario,
                fechas.Inicio,
                fechas.Fin,
                cicloId
            );
            var habilitaciones = await _habilitacionRepository.GetHabilitaciones(
                Id(),
                usuario,
                cicloId
            );
            if (!habilitaciones.Success)
                return (false, habilitaciones.Mensaje, "");
            var paso = await _controlProcesoRepository.GetSiguientePaso(
                Id(),
                usuario,
                ProcesosDiccionario.COMISIONES,
                cicloId
            );
            var bloqueados = HabilitacionComisionHelper.GetContactosBloqueadosParaComision(
                habilitaciones.Data
            );
            var lista = ventas
                .Data.Where(x => !bloqueados.Contains(Convert.ToInt32(x.lcontacta_id)))
                .ToList();
            var calculada = ventas
                .ListaVtaPersonal.Where(x => !bloqueados.Contains(Convert.ToInt32(x.lcontacta_id)))
                .ToList();
            foreach (var x in lista)
            {
                if (
                    x.TipoContratoId
                        == TiposContratosDiccionario.TiposContratosDiccionarioGrd.UPGRADE
                    || x.TipoContratoId
                        == TiposContratosDiccionario.TiposContratosDiccionarioGrd.RECOMPRA
                    || x.TipoContratoId
                        == TiposContratosDiccionario.TiposContratosDiccionarioGrd.RECUPERACION
                    || x.TipoContratoId
                        == TiposContratosDiccionario.TiposContratosDiccionarioGrd.CASOSESPECIALES
                )
                {
                    x.dcomision = x.inicial * 67 / 100;
                    x.PorcentajeInicial = x.inicial * 100 / x.dprecio;
                    x.dporcentajecomision = 67;
                }
            }
            var xls = await new ComisionVentadirectaXls().GetComicionVentaPersonalXls(lista);
            return (
                ventas.Success,
                ventas.Mensaje,
                new
                {
                    ventaPersonal = lista,
                    ventaPersonalCalculado = calculada,
                    base64Xls = xls.base64,
                    controlPasos = new
                    {
                        ejecutado = PasosDiccionario.COMISION_DIRECTA != paso.Data.nombre,
                        data = paso.Data,
                    },
                }
            );
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje)> GuardarVentaPersonalAsync(
        RequestSaveVtaPersonal request
    )
    {
        var id = Id();
        bool iniciado = false;
        try
        {
            var fechas = await FechasAsync(request.LCicloId);
            if (!fechas.Success)
                return (false, fechas.Mensaje);

            var paso = await _controlProcesoRepository.GetSiguientePaso(
                id,
                request.Usuario,
                ProcesosDiccionario.COMISIONES,
                request.LCicloId
            );
            if (paso.Data.nombre != PasosDiccionario.COMISION_DIRECTA)
                return (
                    false,
                    "Esta paso ya se encuentra ejecutado para este ciclo, si quieres volver a a procesar debes reinicar el proceso para el ciclo"
                );

            var inicio = await _controlProcesoRepository.IniciarPaso(
                id,
                request.Usuario,
                ProcesosDiccionario.COMISIONES,
                request.LCicloId,
                PasosDiccionario.COMISION_DIRECTA
            );
            if (!inicio.Success || !(inicio.Data?.status ?? false))
                return (false, inicio.Data?.mensaje ?? inicio.Mensaje);
            iniciado = true;

            var ventas = await _procesoComisionesRepository.GetCalculoVentaPersonal(
                id,
                request.Usuario,
                fechas.Inicio,
                fechas.Fin,
                request.LCicloId
            );
            var hab = await _habilitacionRepository.GetHabilitaciones(
                id,
                request.Usuario,
                request.LCicloId
            );
            if (!hab.Success)
                return await CancelarAsync(
                    id,
                    request.Usuario,
                    request.LCicloId,
                    PasosDiccionario.COMISION_DIRECTA,
                    hab.Mensaje
                );

            var bloqueados = HabilitacionComisionHelper.GetContactosBloqueadosParaComision(
                hab.Data
            );
            var lista = ventas
                .Data.Where(x => !bloqueados.Contains(Convert.ToInt32(x.lcontacta_id)))
                .ToList();
            if (lista.Count != request.ListaComision.Count)
                return await CancelarAsync(
                    id,
                    request.Usuario,
                    request.LCicloId,
                    PasosDiccionario.COMISION_DIRECTA,
                    "La cantidad de registro enviada no coincide con la cantidad obtenida de DB"
                );
            foreach (var x in lista)
            {
                if (
                    x.TipoContratoId
                        == TiposContratosDiccionario.TiposContratosDiccionarioGrd.UPGRADE
                    || x.TipoContratoId
                        == TiposContratosDiccionario.TiposContratosDiccionarioGrd.RECOMPRA
                    || x.TipoContratoId
                        == TiposContratosDiccionario.TiposContratosDiccionarioGrd.RECUPERACION
                    || x.TipoContratoId
                        == TiposContratosDiccionario.TiposContratosDiccionarioGrd.CASOSESPECIALES
                )
                {
                    x.dcomision = x.inicial * 67 / 100;
                    x.dporcentajecomision = 67;
                }
            }

            var rows = lista
                .Select(x => new AdministracionVentaPersonal
                {
                    susuarioadd = request.Usuario,
                    susuariomod = request.Usuario,
                    lciclo_id = request.LCicloId,
                    lcontacto_id = x.lcontacta_id,
                    dpreciolote = x.inicial,
                    dporcentajecomision = x.dporcentajecomision,
                    dcomision = x.dcomision,
                    lcontrato_id = x.lcontrato_id,
                    lnrosemana = 1,
                    lsemana_id = 124,
                })
                .ToList();
            (bool Success, string Mensaje) guardado =
                rows.Count > 0
                    ? await _ventaPersonalRepository.InsertVentaPersonal(id, rows)
                    : (true, "No existen ventas personales que generen comisión para guardar.");
            if (
                !guardado.Success
                || !await CalculoVentaResidualAsync(
                    request.LCicloId,
                    fechas.Inicio,
                    fechas.Fin,
                    request.Usuario
                )
            )
            {
                return await CancelarAsync(
                    id,
                    request.Usuario,
                    request.LCicloId,
                    PasosDiccionario.COMISION_DIRECTA,
                    guardado.Success
                        ? "No se pudo calcular la venta residual asociada al paso."
                        : guardado.Mensaje
                );
            }

            var fin = await _controlProcesoRepository.FinalizarPaso(
                id,
                request.Usuario,
                ProcesosDiccionario.COMISIONES,
                request.LCicloId,
                PasosDiccionario.COMISION_DIRECTA
            );
            if (!fin.Success || !(fin.Data?.status ?? false))
                return (false, fin.Data?.mensaje ?? fin.Mensaje);
            iniciado = false;
            return guardado;
        }
        catch (Exception ex)
        {
            if (iniciado)
            {
                await _controlProcesoRepository.CancelarPaso(
                    id,
                    request.Usuario,
                    ProcesosDiccionario.COMISIONES,
                    request.LCicloId,
                    PasosDiccionario.COMISION_DIRECTA
                );
            }
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerVentaGrupoAsync(
        string usuario,
        int cicloId
    )
    {
        try
        {
            var fechas = await FechasAsync(cicloId);
            if (!fechas.Success)
                return (false, fechas.Mensaje, "");

            var ventas = await _procesoComisionesRepository.GetCalculoVentaGrupo(
                Id(),
                usuario,
                fechas.Inicio,
                fechas.Fin,
                cicloId
            );
            var hab = await _habilitacionRepository.GetHabilitaciones(Id(), usuario, cicloId);
            if (!hab.Success)
                return (false, hab.Mensaje, "");

            var paso = await _controlProcesoRepository.GetSiguientePaso(
                Id(),
                usuario,
                ProcesosDiccionario.COMISIONES,
                cicloId
            );
            var personas = hab.Data.ToList();
            var bloqueados = HabilitacionComisionHelper.GetContactosBloqueadosParaComision(
                personas
            );
            var habilitados = HabilitacionComisionHelper.GetContactosHabilitadosQueGeneranComision(
                personas
            );
            // Esta excepción se limita a venta de grupo: los contactos configurados se
            // muestran y se guardan, aunque estén bloqueados, pero nunca cobran comisión.
            var lista = ventas
                .Data.Where(x =>
                    !bloqueados.Contains(x.LGanadorId)
                    || _contactosConComisionGrupoCero.Contains(x.LGanadorId)
                )
                .ToList();
            AplicarComisionGrupoCeroConfigurada(lista);
            foreach (var x in lista)
                x.EsHabilitado = habilitados.Contains(x.LGanadorId);

            var xls = await new ComisionVentaGrupoXls().GetComicionVentaGrupoXls(lista);
            return (
                ventas.Success,
                ventas.Mensaje,
                new
                {
                    listado = lista,
                    personasHabilitadas = personas,
                    base64Xls = xls.base64,
                    controlPasos = new
                    {
                        ejecutado = PasosDiccionario.COMISION_GRUPO != paso.Data.nombre,
                        data = paso.Data,
                    },
                }
            );
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje)> GuardarVentaGrupoAsync(
        RequestGuardarVentaGrupo request
    )
    {
        var id = Id();
        bool iniciado = false;
        try
        {
            var fechas = await FechasAsync(request.LCicloId);
            if (!fechas.Success)
                return (false, fechas.Mensaje);
            var paso = await _controlProcesoRepository.GetSiguientePaso(
                id,
                request.Usuario,
                ProcesosDiccionario.COMISIONES,
                request.LCicloId
            );
            if (paso.Data.nombre != PasosDiccionario.COMISION_GRUPO)
                return (
                    false,
                    "Esta paso ya se encuentra ejecutado para este ciclo, si quieres volver a a procesar debes reinicar el proceso para el ciclo"
                );
            var ini = await _controlProcesoRepository.IniciarPaso(
                id,
                request.Usuario,
                ProcesosDiccionario.COMISIONES,
                request.LCicloId,
                PasosDiccionario.COMISION_GRUPO
            );
            if (!ini.Success || !(ini.Data?.status ?? false))
                return (false, ini.Data?.mensaje ?? ini.Mensaje);
            iniciado = true;

            var ventas = await _procesoComisionesRepository.GetCalculoVentaGrupo(
                id,
                request.Usuario,
                fechas.Inicio,
                fechas.Fin,
                request.LCicloId
            );
            var hab = await _habilitacionRepository.GetHabilitaciones(
                id,
                request.Usuario,
                request.LCicloId
            );
            if (!hab.Success)
                return await CancelarAsync(
                    id,
                    request.Usuario,
                    request.LCicloId,
                    PasosDiccionario.COMISION_GRUPO,
                    hab.Mensaje
                );

            var bloqueados = HabilitacionComisionHelper.GetContactosBloqueadosParaComision(
                hab.Data
            );
            var lista = ventas
                .Data.Where(x =>
                    !bloqueados.Contains(x.LGanadorId)
                    || _contactosConComisionGrupoCero.Contains(x.LGanadorId)
                )
                .ToList();
            AplicarComisionGrupoCeroConfigurada(lista);
            if (lista.Count != request.ListaComision.Count)
                return await CancelarAsync(
                    id,
                    request.Usuario,
                    request.LCicloId,
                    PasosDiccionario.COMISION_GRUPO,
                    "La cantidad de registro enviada no coincide con la cantidad obtenida de DB"
                );

            var semanas = await _semanaCicloRepository.GetSemanaCicloId(id, request.LCicloId);
            var semana = semanas.Semanas.First();
            var rows = lista
                .Select(x => new ItemVentaGrupo
                {
                    usuario = request.Usuario,
                    lciclo_id = request.LCicloId,
                    lcontacto_id = x.LGanadorId,
                    lgeneracion = x.Nivel,
                    lasesor_id = x.LVendedorId,
                    dporcentajecomision = x.Porcentaje,
                    dcomision = x.Comision,
                    dventapersonal = x.DCuotaInicial,
                    dventapersonalinicial = x.DCuotaInicial,
                    lcontrato_id = x.LContratoId,
                    lnrosemana = semana.LNroSemana,
                    lsemana_id = semana.LSemanaId,
                })
                .ToList();

            (bool Success, string Mensaje) guardado =
                rows.Count > 0
                    ? await _ventaGrupoRepository.InsertAdministracionVentaGrupo(id, rows)
                    : (true, "No existen comisiones de grupo habilitadas para guardar.");
            if (!guardado.Success)
                return await CancelarAsync(
                    id,
                    request.Usuario,
                    request.LCicloId,
                    PasosDiccionario.COMISION_GRUPO,
                    guardado.Mensaje
                );

            var fin = await _controlProcesoRepository.FinalizarPaso(
                id,
                request.Usuario,
                ProcesosDiccionario.COMISIONES,
                request.LCicloId,
                PasosDiccionario.COMISION_GRUPO
            );
            if (!fin.Success || !(fin.Data?.status ?? false))
                return (false, fin.Data?.mensaje ?? fin.Mensaje);
            iniciado = false;
            return guardado;
        }
        catch (Exception ex)
        {
            if (iniciado)
            {
                await _controlProcesoRepository.CancelarPaso(
                    id,
                    request.Usuario,
                    ProcesosDiccionario.COMISIONES,
                    request.LCicloId,
                    PasosDiccionario.COMISION_GRUPO
                );
            }
            return (false, ex.Message);
        }
    }

    private void AplicarComisionGrupoCeroConfigurada(
        IEnumerable<ItemComisionVentaGrupoDto> comisiones
    )
    {
        foreach (var comision in comisiones)
        {
            if (!_contactosConComisionGrupoCero.Contains(comision.LGanadorId))
                continue;

            comision.Comision = 0;
            comision.EsCero = true;
            comision.EsComisionGrupoCeroConfigurada = true;
        }
    }

    private async Task<(bool Success, string Mensaje, string Inicio, string Fin)> FechasAsync(
        int cicloId
    )
    {
        var r = await _administracionCicloRepository.GetCiclo(Id(), cicloId);
        if (!r.Success || r.Data.LCicloId <= 0)
            return (false, $"No se encontró el ciclo {cicloId}.", "", "");
        if (
            string.IsNullOrWhiteSpace(r.Data.DtFechaInicio)
            || string.IsNullOrWhiteSpace(r.Data.DtFechaFin)
        )
            return (false, $"El ciclo {cicloId} no tiene fechas configuradas.", "", "");
        return (true, r.Mensaje, r.Data.DtFechaInicio, r.Data.DtFechaFin);
    }

    private async Task<bool> CalculoVentaResidualAsync(
        int cicloId,
        string inicio,
        string fin,
        string usuario
    )
    {
        var contratos = await _contratoRepository.GetAdministracionContratoFechaVentaResidual(
            Id(),
            inicio,
            fin
        );
        var hab = await _habilitacionRepository.GetHabilitaciones(Id(), usuario, cicloId);
        if (!hab.Success)
            return false;

        var bloqueados = HabilitacionComisionHelper.GetContactosBloqueadosParaComision(hab.Data);
        var membresia = new HashSet<int> { 85, 29, 58, 95, 98, 101, 102 };
        var lista = contratos
            .Data.Where(x =>
                !HabilitacionComisionHelper.TiposContratoEspeciales.Contains(x.LTipoContratoId)
                && !bloqueados.Contains(x.LAsesorId)
            )
            .Select(x =>
            {
                var esMembresia = membresia.Contains(x.LComplejoId);
                var inicial10 = x.Precio * 10 / 100;
                var directa = x.CuotaInicial * (esMembresia ? 40 : 30) / 100;
                var nueva = esMembresia ? inicial10 : inicial10 * 30 / 100;
                var cuotas = esMembresia ? 12 : 6;
                return new ProductosPagarMensuales
                {
                    LcontratoId = x.LcontratoId,
                    LcomplejoId = x.LComplejoId,
                    Snroventa = x.NroVenta ?? "",
                    LcontactoId = x.LcontratoId,
                    LasesorId = x.LAsesorId,
                    Dtfecha = x.Fecha,
                    Precio = x.Precio,
                    CuotaInicial = x.CuotaInicial,
                    Porcentaje = x.PorcentajeInicial,
                    Comision = directa,
                    CuotAccPen = cuotas,
                    Inicial10 = inicial10,
                    MontPagar = nueva - directa,
                    MensPagar = (nueva - directa) / cuotas,
                };
            })
            .Where(x => x.MontPagar > 0 && x.Porcentaje < 100)
            .ToList();
        return (
            await _cuotasRepository.InsertProductosPagarMensuales(Id(), usuario, lista)
        ).Success;
    }

    private async Task<(bool Success, string Mensaje)> CancelarAsync(
        string id,
        string usuario,
        int cicloId,
        string paso,
        string mensaje
    )
    {
        await _controlProcesoRepository.CancelarPaso(
            id,
            usuario,
            ProcesosDiccionario.COMISIONES,
            cicloId,
            paso
        );
        return (false, mensaje);
    }

    private static string Id() => DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
}
