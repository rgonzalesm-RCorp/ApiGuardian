using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Services;

namespace CleanDapperApi.Api.Services;

public sealed class BonoResidualService : IBonoResidualService
{
    private readonly ILogService _log;
    private readonly IBonoResidualRepository _bonoResidualRepository;
    private readonly IControlProcesoRepository _controlProcesoRepository;
    private readonly IVentasCnxRepository _ventasCnxRepository;
    private readonly IAdministracionCicloRepository _administracionCicloRepository;
    private readonly IAdministracionBonoResidualRepository _adminBonoResidualRepository;
    private readonly IBonoParRepository _bonoParRepository;
    private readonly IAdministracionContratoRepository _administracionContratoRepository;
    private readonly IBrConfiguracionRepository _brConfiguracionRepository;
    private readonly IAdministracionHabilitacionComisionRepository _habilitacionRepository;
    private readonly HashSet<int> _contactosConComisionResidualCero;

    private const string NombreArchivo = "BonoResidualService.cs";

    public BonoResidualService(
        ILogService log,
        IBonoResidualRepository bonoResidualRepository,
        IControlProcesoRepository controlProcesoRepository,
        IVentasCnxRepository ventasCnxRepository,
        IAdministracionCicloRepository administracionCicloRepository,
        IAdministracionBonoResidualRepository adminBonoResidualRepository,
        IBonoParRepository bonoParRepository,
        IAdministracionHabilitacionComisionRepository habilitacionRepository,
        IAdministracionContratoRepository administracionContratoRepository,
        IBrConfiguracionRepository brConfiguracionRepository,
        IConfiguration configuration
    )
    {
        _log = log;
        _bonoResidualRepository = bonoResidualRepository;
        _controlProcesoRepository = controlProcesoRepository;
        _ventasCnxRepository = ventasCnxRepository;
        _administracionCicloRepository = administracionCicloRepository;
        _adminBonoResidualRepository = adminBonoResidualRepository;
        _bonoParRepository = bonoParRepository;
        _administracionContratoRepository = administracionContratoRepository;
        _brConfiguracionRepository = brConfiguracionRepository;
        _habilitacionRepository = habilitacionRepository;
        _contactosConComisionResidualCero = configuration
            .GetSection("HabilidacionesParaNoComprimirRed")
            .Get<int[]>()
            ?.Where(contactoId => contactoId > 0)
            .ToHashSet() ?? new HashSet<int>();
    }

    public async Task<BonoResidualCalculoResult> ConstruirListadoResidualAsync(
        string usuario,
        int cicloId,
        string logTransaccionId
    )
    {
        var datos = await _bonoResidualRepository.GetDataCalculoBonoResidual(
            logTransaccionId,
            usuario,
            cicloId
        );
        var habilitaciones = await _habilitacionRepository.GetHabilitaciones(
            logTransaccionId,
            usuario,
            cicloId
        );
        var configuracion = await _brConfiguracionRepository.GetConfiguracion(
            logTransaccionId,
            usuario
        );
        var cartera = await _bonoResidualRepository.GetCarteraGRD(logTransaccionId, usuario);
        if (!datos.Success || !habilitaciones.Success || !configuracion.Success || !cartera.Success)
        {
            var mensaje =
                !datos.Success ? datos.Mensaje
                : !habilitaciones.Success ? habilitaciones.Mensaje
                : !configuracion.Success ? configuracion.Mensaje
                : cartera.Mensaje;
            return new BonoResidualCalculoResult { Mensaje = mensaje };
        }

        var contactos = datos.ListaContacto.ToDictionary(x => x.LContactoId, x => x);
        var personas = habilitaciones.Data.ToList();
        var bloqueados = HabilitacionComisionHelper.GetContactosBloqueadosParaComision(personas);
        var habilitados = HabilitacionComisionHelper.GetContactosHabilitadosQueGeneranComision(
            personas
        );
        var activos = datos
            .ListaContactosActivos.Select(x => x.LContactoId)
            .Where(x => !bloqueados.Contains(x))
            .Concat(habilitados)
            // Solo en bono residual, los contactos configurados se incluyen aunque
            // estén bloqueados; su comisión se fuerza a cero más adelante.
            .Concat(_contactosConComisionResidualCero)
            .ToHashSet();
        var configs = configuracion.Data.Where(x => x.LCicloId == cicloId).ToList();
        var terreno = configs.Where(x => x.TipoProductoId == 1).ToDictionary(x => x.Nivel, x => x);
        var membresia = configs
            .Where(x => x.TipoProductoId == 2)
            .ToDictionary(x => x.Nivel, x => x);
        var vencidos = cartera
            .ListaCartera.Where(x =>
                string.Equals(x.Estado?.Trim(), "VENCIDO", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(x.DocId)
            )
            .Select(x => x.DocId.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var listado = new List<BrCalculoItem>();

        foreach (var item in datos.ListaCuotaRed)
        {
            var tipo = item.GetType();
            for (var nivel = 1; nivel <= 7; nivel++)
            {
                var propiedad = tipo.GetProperty($"LPatrocinado{nivel}");
                var patrocinado = propiedad?.GetValue(item) is { } valor
                    ? Convert.ToInt32(valor)
                    : 0;
                contactos.TryGetValue(patrocinado, out var contacto);
                terreno.TryGetValue(nivel, out var configTerreno);
                membresia.TryGetValue(nivel, out var configMembresia);
                var porcentaje = string.Equals(
                    item.Empresa?.Trim(),
                    "ADVEL",
                    StringComparison.OrdinalIgnoreCase
                )
                    ? configMembresia?.PorcentajeComision ?? 0
                    : configTerreno?.PorcentajeComision ?? 0;
                var documento = contacto?.SCedulaIdentidad?.Trim();
                listado.Add(
                    new BrCalculoItem
                    {
                        LContactoId = patrocinado,
                        NombreCompleto = contacto?.SNombreCompleto ?? "",
                        Documento = contacto?.SCedulaIdentidad ?? "",
                        LContactoIdHijo = item.LContactoId,
                        NombreCompletoHijo = item.Cliente,
                        DocumentoHijo = item.DocumentoCliente,
                        Nivel = nivel,
                        Bono = item.Bono,
                        BonoResidual = item.Bono * porcentaje / 100,
                        ActivoMes = activos.Contains(patrocinado),
                        PorcentajeComision = porcentaje,
                        LComplejoId = item.ProyectoId,
                        Complejo = item.Proyecto,
                        Empresa = item.Empresa,
                        ProductoId = item.ProductoId,
                        EstaAlDia =
                            string.IsNullOrWhiteSpace(documento) || !vencidos.Contains(documento),
                    }
                );
            }
        }

        listado = listado.Where(x => x.ActivoMes && x.EstaAlDia).ToList();
        AplicarComisionResidualCeroConfigurada(listado);
        return new BonoResidualCalculoResult
        {
            Success = true,
            Mensaje = datos.Mensaje,
            ListadoResidual = listado,
            PersonasHabilitadas = personas,
            TotalCuotas = datos.ListaCuotaRed.Count(),
            TotalContactos = datos.ListaContacto.Count(),
        };
    }

    private void AplicarComisionResidualCeroConfigurada(
        IEnumerable<BrCalculoItem> calculos
    )
    {
        foreach (var calculo in calculos)
        {
            if (_contactosConComisionResidualCero.Contains(calculo.LContactoId))
                calculo.BonoResidual = 0;
        }
    }

    public async Task<BonoResidualGuardadoResult> ObtenerBonoResidualAsync(
        string usuario,
        int cicloId,
        string logTransaccionId
    )
    {
        try
        {
            var calculo = await ConstruirListadoResidualAsync(usuario, cicloId, logTransaccionId);
            if (!calculo.Success)
                return new BonoResidualGuardadoResult { Mensaje = calculo.Mensaje };

            var resumen = calculo
                .ListadoResidual.GroupBy(x => new { x.Empresa })
                .Select(g => new
                {
                    g.Key.Empresa,
                    TotalPago = g.Sum(x => x.Bono),
                    TotalResidual = g.Sum(x => x.BonoResidual),
                    listadoProyecto = calculo
                        .ListadoResidual.Where(d => d.Empresa == g.Key.Empresa)
                        .GroupBy(x => new { x.Complejo })
                        .Select(p => new
                        {
                            p.Key.Complejo,
                            TotalPago = p.Sum(x => x.Bono),
                            TotalResidual = p.Sum(x => x.BonoResidual),
                        })
                        .ToList(),
                })
                .ToList();

            var xls = await new ComisionResidualXls().GetComisionResidualXls(
                calculo.ListadoResidual
            );
            var siguiente = await _controlProcesoRepository.GetSiguientePaso(
                logTransaccionId,
                usuario,
                ProcesosDiccionario.COMISIONES,
                cicloId
            );
            return new BonoResidualGuardadoResult
            {
                Success = true,
                Mensaje = calculo.Mensaje,
                Data = new
                {
                    ListadoResumenPorEmpresa = resumen,
                    counter = calculo.ListadoResidual.Count,
                    personasHabilitadas = calculo.PersonasHabilitadas,
                    base64 = xls.base64,
                    controlPasos = new
                    {
                        ejecutado = PasosDiccionario.COMISION_RESIDUAL == siguiente.Data.nombre
                            ? false
                            : true,
                        data = siguiente.Data,
                    },
                },
            };
        }
        catch (Exception ex)
        {
            _log.Error(
                logTransaccionId,
                NombreArchivo,
                "ObtenerBonoResidualAsync()",
                "Fin de metodo",
                ex
            );
            return new BonoResidualGuardadoResult { Mensaje = ex.Message };
        }
    }

    public async Task<BonoResidualGuardadoResult> ObtenerBonoParAsync(
        string usuario,
        int cicloId,
        string logTransaccionId
    )
    {
        try
        {
            var ciclo = await _administracionCicloRepository.GetCiclo(logTransaccionId, cicloId);
            if (!ciclo.Success || ciclo.Data.LCicloId <= 0)
                return new BonoResidualGuardadoResult
                {
                    Mensaje = $"No se encontró el ciclo {cicloId}.",
                };
            var inicio = ciclo.Data.DtFechaInicio ?? string.Empty;
            var fin = ciclo.Data.DtFechaFin ?? string.Empty;
            var bonos = await _bonoParRepository.GetBonoPar(logTransaccionId, usuario, inicio, fin);
            var habilitaciones = await _habilitacionRepository.GetHabilitaciones(
                logTransaccionId,
                usuario,
                cicloId
            );
            var contratos =
                await _administracionContratoRepository.GetAdministracionContratoFechaVentaResidual(
                    logTransaccionId,
                    inicio,
                    fin
                );
            if (!bonos.Success || !habilitaciones.Success)
                return new BonoResidualGuardadoResult
                {
                    Mensaje = !bonos.Success ? bonos.Mensaje : habilitaciones.Mensaje,
                };
            var bloqueados = HabilitacionComisionHelper.GetContactosBloqueadosParaComision(
                habilitaciones.Data
            );
            var habilitados = HabilitacionComisionHelper.GetContactosHabilitadosQueGeneranComision(
                habilitaciones.Data
            );
            var normales = contratos
                .Data.Where(x =>
                    !HabilitacionComisionHelper.TiposContratoEspeciales.Contains(x.LTipoContratoId)
                )
                .Select(x => x.LAsesorId)
                .ToHashSet();
            var listado = bonos
                .Data.Where(x =>
                    !bloqueados.Contains(x.LContctoGanadorId)
                    && (
                        normales.Contains(x.LContctoGanadorId)
                        || habilitados.Contains(x.LContctoGanadorId)
                    )
                )
                .ToList();
            var xls = await new BonoParXls().GetBonoParXls(listado);
            var siguiente = await _controlProcesoRepository.GetSiguientePaso(
                logTransaccionId,
                usuario,
                ProcesosDiccionario.COMISIONES,
                cicloId
            );
            return new BonoResidualGuardadoResult
            {
                Success = bonos.Success,
                Mensaje = bonos.Mensaje,
                Data = new
                {
                    ListaBonoPar = listado,
                    xls = xls.base64,
                    controlPasos = new
                    {
                        ejecutado = PasosDiccionario.EsBonoPar(siguiente.Data.nombre)
                            ? false
                            : true,
                        data = siguiente.Data,
                    },
                },
            };
        }
        catch (Exception ex)
        {
            _log.Error(
                logTransaccionId,
                NombreArchivo,
                "ObtenerBonoParAsync()",
                "Fin de metodo",
                ex
            );
            return new BonoResidualGuardadoResult { Mensaje = ex.Message };
        }
    }

    public async Task<BonoResidualGuardadoResult> ObtenerCuotaAsync(
        string usuario,
        int cicloId,
        string logTransaccionId
    )
    {
        try
        {
            var ciclo = await _administracionCicloRepository.GetCiclo(logTransaccionId, cicloId);
            if (!ciclo.Success || ciclo.Data.LCicloId <= 0)
                return new BonoResidualGuardadoResult
                {
                    Mensaje = $"No se encontró el ciclo {cicloId}.",
                };
            var inicio = ciclo.Data.DtFechaInicio ?? string.Empty;
            var fin = ciclo.Data.DtFechaFin ?? string.Empty;
            if (string.IsNullOrWhiteSpace(inicio) || string.IsNullOrWhiteSpace(fin))
                return new BonoResidualGuardadoResult
                {
                    Mensaje = $"El ciclo {cicloId} no tiene fechas configuradas.",
                };
            var cuotas = await _bonoResidualRepository.GetCuota(
                logTransaccionId,
                usuario,
                inicio,
                fin
            );
            if (!cuotas.Success)
                return new BonoResidualGuardadoResult { Mensaje = cuotas.Mensaje };
            var resumen = cuotas
                .ListaCuota.GroupBy(x => new { x.Idtipopago, x.Descripcion })
                .Select(g => new
                {
                    g.Key.Idtipopago,
                    g.Key.Descripcion,
                    TotalPago = g.Sum(x => x.Totalpago),
                    Cantidad = g.Count(),
                })
                .ToList();
            var xls = await new CuotaXls().GetCuotaXlS(cuotas.ListaCuota.ToList());
            var siguiente = await _controlProcesoRepository.GetSiguientePaso(
                logTransaccionId,
                usuario,
                ProcesosDiccionario.COMISIONES,
                cicloId
            );
            return new BonoResidualGuardadoResult
            {
                Success = true,
                Mensaje = cuotas.Mensaje,
                Data = new
                {
                    resumen,
                    xlsCuota = xls.base64,
                    fileNameXls = $"Reporte de cuotas del {inicio} al {fin}",
                    controlPasos = new
                    {
                        ejecutado = PasosDiccionario.OBTENER_CUOTAS == siguiente.Data.nombre
                            ? false
                            : true,
                        data = siguiente.Data,
                    },
                },
            };
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId, NombreArchivo, "ObtenerCuotaAsync()", "Fin de metodo", ex);
            return new BonoResidualGuardadoResult { Mensaje = ex.Message };
        }
    }

    public async Task<BonoResidualGuardadoResult> ObtenerExcedenteAsync(
        string usuario,
        int cicloId,
        string logTransaccionId
    )
    {
        try
        {
            var ciclo = await _administracionCicloRepository.GetCiclo(logTransaccionId, cicloId);
            if (!ciclo.Success || ciclo.Data.LCicloId <= 0)
                return new BonoResidualGuardadoResult
                {
                    Mensaje = $"No se encontró el ciclo {cicloId}.",
                };
            var inicio = ciclo.Data.DtFechaInicio ?? string.Empty;
            var fin = ciclo.Data.DtFechaFin ?? string.Empty;
            var ventas = await _ventasCnxRepository.GetVentaCnx(logTransaccionId, inicio, fin);
            if (!ventas.Success)
                return new BonoResidualGuardadoResult { Mensaje = ventas.Mensaje };
            var lista = ventas
                .Data.Where(x =>
                    (x.SCuotaInicialOriginal - x.ValorCi) > 0.05m && !x.Glosa.Contains("UPGRADE")
                )
                .ToList();
            var siguiente = await _controlProcesoRepository.GetSiguientePaso(
                logTransaccionId,
                usuario,
                ProcesosDiccionario.COMISIONES,
                cicloId
            );
            return new BonoResidualGuardadoResult
            {
                Success = true,
                Mensaje = "Se estan guardando las cuotas en segundo plano.",
                Data = new
                {
                    listaExcedente = lista,
                    controlPasos = new
                    {
                        ejecutado = PasosDiccionario.OBTENER_EXCEDENTE == siguiente.Data.nombre
                            ? false
                            : true,
                        data = siguiente.Data,
                    },
                },
            };
        }
        catch (Exception ex)
        {
            _log.Error(
                logTransaccionId,
                NombreArchivo,
                "ObtenerExcedenteAsync()",
                "Fin de metodo",
                ex
            );
            return new BonoResidualGuardadoResult { Mensaje = ex.Message };
        }
    }

    public async Task<BonoResidualGuardadoResult> ObtenerCarteraAsync(
        string usuario,
        int cicloId,
        string logTransaccionId
    )
    {
        try
        {
            var cartera = await _bonoResidualRepository.GetCarteraAll(logTransaccionId, usuario);
            var resumen = cartera
                .ListaCartera.GroupBy(x => new { x.Estado })
                .Select(g => new { g.Key.Estado, Cantidad = g.Count() })
                .ToList();
            var xls = await new CarteraXls().GetCarteraXlS(cartera.ListaCartera.ToList());
            var siguiente = await _controlProcesoRepository.GetSiguientePaso(
                logTransaccionId,
                usuario,
                ProcesosDiccionario.COMISIONES,
                cicloId
            );
            return new BonoResidualGuardadoResult
            {
                Success = cartera.Success,
                Mensaje = cartera.Mensaje,
                Data = new
                {
                    resumenCartera = resumen,
                    base64 = xls.base64,
                    fileNameXls = "Reporte de la cartera de clientes",
                    controlPasos = new
                    {
                        ejecutado = PasosDiccionario.OBTENER_CARTERA == siguiente.Data.nombre
                            ? false
                            : true,
                        data = siguiente.Data,
                    },
                },
            };
        }
        catch (Exception ex)
        {
            _log.Error(
                logTransaccionId,
                NombreArchivo,
                "ObtenerCarteraAsync()",
                "Fin de metodo",
                ex
            );
            return new BonoResidualGuardadoResult { Mensaje = ex.Message };
        }
    }

    public async Task<(
        bool Success,
        string Mensaje,
        DateTime? Inicio,
        DateTime? Fin
    )> GuardarCarteraAsync(string usuario, int cicloId, string logTransaccionId)
    {
        var pasoIniciado = false;
        var paso = PasosDiccionario.OBTENER_CARTERA;

        try
        {
            var inicio = DateTime.Now;
            var siguientePaso = await _controlProcesoRepository.GetSiguientePaso(
                logTransaccionId,
                usuario,
                ProcesosDiccionario.COMISIONES,
                cicloId
            );

            if (paso != siguientePaso.Data.nombre)
                return (
                    false,
                    "Esta paso ya se encuentra ejecutado para este ciclo, si quieres volver a a procesar debes reinicar el proceso para el ciclo",
                    null,
                    null
                );

            var inicioPaso = await _controlProcesoRepository.IniciarPaso(
                logTransaccionId,
                usuario,
                ProcesosDiccionario.COMISIONES,
                cicloId,
                paso
            );

            if (!inicioPaso.Success || !(inicioPaso.Data?.status ?? false))
                return (false, inicioPaso.Data?.mensaje ?? inicioPaso.Mensaje, null, null);

            pasoIniciado = true;
            _log.Info(logTransaccionId, NombreArchivo, "GuardarCarteraAsync()", "Inicio de metodo");

            var cartera = await _bonoResidualRepository.GetCarteraAll(logTransaccionId, usuario);
            if (!cartera.Success)
            {
                await _controlProcesoRepository.CancelarPaso(
                    logTransaccionId,
                    usuario,
                    ProcesosDiccionario.COMISIONES,
                    cicloId,
                    paso
                );
                return (false, cartera.Mensaje, null, null);
            }

            var guardado = await _bonoResidualRepository.GuardarCartera(
                logTransaccionId,
                usuario,
                cartera.ListaCartera.ToList()
            );

            if (!guardado.Success)
            {
                await _controlProcesoRepository.CancelarPaso(
                    logTransaccionId,
                    usuario,
                    ProcesosDiccionario.COMISIONES,
                    cicloId,
                    paso
                );
                return (false, guardado.Mensaje, null, null);
            }

            var finPaso = await _controlProcesoRepository.FinalizarPaso(
                logTransaccionId,
                usuario,
                ProcesosDiccionario.COMISIONES,
                cicloId,
                paso
            );

            if (!finPaso.Success || !(finPaso.Data?.status ?? false))
                return (false, finPaso.Data?.mensaje ?? finPaso.Mensaje, null, null);

            pasoIniciado = false;
            var fin = DateTime.Now;
            return (true, "Se esta guardando la cartera en segundo plano.", inicio, fin);
        }
        catch (Exception ex)
        {
            if (pasoIniciado)
                await _controlProcesoRepository.CancelarPaso(
                    logTransaccionId,
                    usuario,
                    ProcesosDiccionario.COMISIONES,
                    cicloId,
                    paso
                );

            _log.Error(
                logTransaccionId,
                NombreArchivo,
                "GuardarCarteraAsync()",
                "Fin de metodo",
                ex
            );
            return (false, ex.Message, null, null);
        }
    }

    public async Task<BonoResidualGuardadoResult> GuardarCuotaAsync(
        string usuario,
        int cicloId,
        string logTransaccionId
    )
    {
        const string paso = PasosDiccionario.OBTENER_CUOTAS;
        var pasoIniciado = false;
        try
        {
            var ciclo = await _administracionCicloRepository.GetCiclo(logTransaccionId, cicloId);
            if (!ciclo.Success || ciclo.Data.LCicloId <= 0)
                return new BonoResidualGuardadoResult
                {
                    Mensaje = $"No se encontró el ciclo {cicloId}.",
                };
            var inicio = ciclo.Data.DtFechaInicio ?? string.Empty;
            var fin = ciclo.Data.DtFechaFin ?? string.Empty;
            if (string.IsNullOrWhiteSpace(inicio) || string.IsNullOrWhiteSpace(fin))
                return new BonoResidualGuardadoResult
                {
                    Mensaje = $"El ciclo {cicloId} no tiene fechas configuradas.",
                };

            var siguiente = await _controlProcesoRepository.GetSiguientePaso(
                logTransaccionId,
                usuario,
                ProcesosDiccionario.COMISIONES,
                cicloId
            );
            if (siguiente.Data.nombre != paso)
                return new BonoResidualGuardadoResult
                {
                    Mensaje =
                        "Esta paso ya se encuentra ejecutado para este ciclo, si quieres volver a a procesar debes reinicar el proceso para el ciclo",
                };
            var inicioPaso = await _controlProcesoRepository.IniciarPaso(
                logTransaccionId,
                usuario,
                ProcesosDiccionario.COMISIONES,
                cicloId,
                paso
            );
            if (!inicioPaso.Success || !(inicioPaso.Data?.status ?? false))
                return new BonoResidualGuardadoResult
                {
                    Mensaje = inicioPaso.Data?.mensaje ?? inicioPaso.Mensaje,
                };
            pasoIniciado = true;

            var cuotas = await _bonoResidualRepository.GetCuota(
                logTransaccionId,
                usuario,
                inicio,
                fin
            );
            if (!cuotas.Success)
            {
                await _controlProcesoRepository.CancelarPaso(
                    logTransaccionId,
                    usuario,
                    ProcesosDiccionario.COMISIONES,
                    cicloId,
                    paso
                );
                return new BonoResidualGuardadoResult { Mensaje = cuotas.Mensaje };
            }
            var guardado = await _bonoResidualRepository.GuardarCuota(
                logTransaccionId,
                usuario,
                cuotas.ListaCuota.ToList()
            );
            if (!guardado.Success)
            {
                await _controlProcesoRepository.CancelarPaso(
                    logTransaccionId,
                    usuario,
                    ProcesosDiccionario.COMISIONES,
                    cicloId,
                    paso
                );
                return new BonoResidualGuardadoResult { Mensaje = guardado.Mensaje };
            }
            var finPaso = await _controlProcesoRepository.FinalizarPaso(
                logTransaccionId,
                usuario,
                ProcesosDiccionario.COMISIONES,
                cicloId,
                paso
            );
            if (!finPaso.Success || !(finPaso.Data?.status ?? false))
                return new BonoResidualGuardadoResult
                {
                    Mensaje = finPaso.Data?.mensaje ?? finPaso.Mensaje,
                };
            pasoIniciado = false;
            return new BonoResidualGuardadoResult
            {
                Success = true,
                Mensaje = "Se estan guardando las cuotas en segundo plano.",
                Data = new { ini = DateTime.Now, fins = DateTime.Now },
            };
        }
        catch (Exception ex)
        {
            if (pasoIniciado)
                await _controlProcesoRepository.CancelarPaso(
                    logTransaccionId,
                    usuario,
                    ProcesosDiccionario.COMISIONES,
                    cicloId,
                    paso
                );
            _log.Error(logTransaccionId, NombreArchivo, "GuardarCuotaAsync()", "Fin de metodo", ex);
            return new BonoResidualGuardadoResult { Mensaje = ex.Message };
        }
    }

    public async Task<(
        bool Success,
        string Mensaje,
        List<ItemVentaCnx> ListaExcedente
    )> GuardarExcedenteAsync(string usuario, int cicloId, string logTransaccionId)
    {
        const string paso = PasosDiccionario.OBTENER_EXCEDENTE;
        var listaFiltrada = new List<ItemVentaCnx>();
        var pasoIniciado = false;

        try
        {
            var ciclo = await _administracionCicloRepository.GetCiclo(logTransaccionId, cicloId);
            if (!ciclo.Success || ciclo.Data.LCicloId <= 0)
                return (false, $"No se encontró el ciclo {cicloId}.", listaFiltrada);

            var inicio = ciclo.Data.DtFechaInicio ?? string.Empty;
            var fin = ciclo.Data.DtFechaFin ?? string.Empty;
            if (string.IsNullOrWhiteSpace(inicio) || string.IsNullOrWhiteSpace(fin))
                return (false, $"El ciclo {cicloId} no tiene fechas configuradas.", listaFiltrada);

            var siguientePaso = await _controlProcesoRepository.GetSiguientePaso(
                logTransaccionId,
                usuario,
                ProcesosDiccionario.COMISIONES,
                cicloId
            );
            if (paso != siguientePaso.Data.nombre)
                return (
                    false,
                    "Esta paso ya se encuentra ejecutado para este ciclo, si quieres volver a a procesar debes reinicar el proceso para el ciclo",
                    listaFiltrada
                );

            var inicioPaso = await _controlProcesoRepository.IniciarPaso(
                logTransaccionId,
                usuario,
                ProcesosDiccionario.COMISIONES,
                cicloId,
                paso
            );
            if (!inicioPaso.Success || !(inicioPaso.Data?.status ?? false))
                return (false, inicioPaso.Data?.mensaje ?? inicioPaso.Mensaje, listaFiltrada);

            pasoIniciado = true;
            var ventas = await _ventasCnxRepository.GetVentaCnx(logTransaccionId, inicio, fin);
            if (!ventas.Success)
            {
                await _controlProcesoRepository.CancelarPaso(
                    logTransaccionId,
                    usuario,
                    ProcesosDiccionario.COMISIONES,
                    cicloId,
                    paso
                );
                return (false, ventas.Mensaje, listaFiltrada);
            }

            listaFiltrada = ventas
                .Data.Where(x =>
                    (x.SCuotaInicialOriginal - x.ValorCi) > 0.05m && !x.Glosa.Contains("UPGRADE")
                )
                .ToList();
            var excedentes = listaFiltrada
                .Select(item => new TCuota
                {
                    Idproducto = item.Lote,
                    Idproyecto = item.LComplejoId,
                    LComplejoId = item.LComplejoId,
                    Proyecto = item.Complejo ?? "",
                    Idrecibo = 0,
                    Idventa = item.IdVenta,
                    Idtipopago = 0,
                    Descripcion = "",
                    Idcliente = item.IdCliente,
                    Cliente = item.SNombreCompleto,
                    Docidcli = item.SCedulaIdentidad ?? "",
                    Idvendedor = item.VendedorId,
                    Vendedor = item.SNombreCompletoVendedor,
                    Docidven = item.SCedulaIdentidadVendedor ?? "",
                    Bono = item.SCuotaInicialOriginal - item.ValorCi,
                    Fecha_Venta = item.DFecha,
                    Fecha_Pago = DateTime.Now,
                    Empresa = item.Empresa ?? "",
                })
                .ToList();

            var guardado = await _bonoResidualRepository.GuardarCuota(
                logTransaccionId,
                usuario,
                excedentes,
                true
            );
            if (!guardado.Success)
            {
                await _controlProcesoRepository.CancelarPaso(
                    logTransaccionId,
                    usuario,
                    ProcesosDiccionario.COMISIONES,
                    cicloId,
                    paso
                );
                return (false, guardado.Mensaje, listaFiltrada);
            }

            var finPaso = await _controlProcesoRepository.FinalizarPaso(
                logTransaccionId,
                usuario,
                ProcesosDiccionario.COMISIONES,
                cicloId,
                paso
            );
            if (!finPaso.Success || !(finPaso.Data?.status ?? false))
                return (false, finPaso.Data?.mensaje ?? finPaso.Mensaje, listaFiltrada);

            pasoIniciado = false;
            return (true, "Se estan guardando los excedentes en segundo plano.", listaFiltrada);
        }
        catch (Exception ex)
        {
            if (pasoIniciado)
                await _controlProcesoRepository.CancelarPaso(
                    logTransaccionId,
                    usuario,
                    ProcesosDiccionario.COMISIONES,
                    cicloId,
                    paso
                );
            _log.Error(
                logTransaccionId,
                NombreArchivo,
                "GuardarExcedenteAsync()",
                "Fin de metodo",
                ex
            );
            return (false, ex.Message, listaFiltrada);
        }
    }

    public async Task<BonoResidualGuardadoResult> GuardarBonoResidualAsync(
        string usuario,
        int cicloId,
        string logTransaccionId,
        BonoResidualCalculoResult calculo,
        DateTime inicio
    )
    {
        const string paso = PasosDiccionario.COMISION_RESIDUAL;
        var pasoIniciado = false;
        try
        {
            var siguientePaso = await _controlProcesoRepository.GetSiguientePaso(
                logTransaccionId,
                usuario,
                ProcesosDiccionario.COMISIONES,
                cicloId
            );
            if (paso != siguientePaso.Data.nombre)
                return new BonoResidualGuardadoResult
                {
                    Mensaje =
                        "Esta paso ya se encuentra ejecutado para este ciclo, si quieres volver a a procesar debes reinicar el proceso para el ciclo",
                };

            var inicioPaso = await _controlProcesoRepository.IniciarPaso(
                logTransaccionId,
                usuario,
                ProcesosDiccionario.COMISIONES,
                cicloId,
                paso
            );
            if (!inicioPaso.Success || !(inicioPaso.Data?.status ?? false))
                return new BonoResidualGuardadoResult
                {
                    Mensaje = inicioPaso.Data?.mensaje ?? inicioPaso.Mensaje,
                };
            pasoIniciado = true;

            if (!calculo.Success)
            {
                await _controlProcesoRepository.CancelarPaso(
                    logTransaccionId,
                    usuario,
                    ProcesosDiccionario.COMISIONES,
                    cicloId,
                    paso
                );
                return new BonoResidualGuardadoResult { Mensaje = calculo.Mensaje };
            }

            var residual = calculo.ListadoResidual;
            if (residual.Count == 0)
            {
                var finSinDatos = await _controlProcesoRepository.FinalizarPaso(
                    logTransaccionId,
                    usuario,
                    ProcesosDiccionario.COMISIONES,
                    cicloId,
                    paso
                );
                if (!finSinDatos.Success || !(finSinDatos.Data?.status ?? false))
                    return new BonoResidualGuardadoResult
                    {
                        Mensaje = finSinDatos.Data?.mensaje ?? finSinDatos.Mensaje,
                    };
                pasoIniciado = false;
                return new BonoResidualGuardadoResult
                {
                    Success = true,
                    Mensaje = "No existen registros habilitados para generar bono residual.",
                    Data = new
                    {
                        listaCuota = calculo.TotalCuotas,
                        contacto = calculo.TotalContactos,
                        Residual = 0,
                        ResidualActivos = 0,
                        ResidualInactivos = 0,
                        inicio,
                        fin = DateTime.Now,
                    },
                };
            }

            var bonoCompleto = residual
                .GroupBy(x => new
                {
                    x.Nivel,
                    x.LContactoId,
                    x.LContactoIdHijo,
                    x.DocumentoHijo,
                    x.LComplejoId,
                })
                .Select(g => new ItemBonoCompleto
                {
                    Id = 0,
                    Nivel = g.Key.Nivel,
                    LContactoId = g.Key.LContactoId,
                    LContactoIdHijo = g.Key.LContactoIdHijo,
                    DocumentoHijo = g.Key.DocumentoHijo,
                    LComplejoId = g.Key.LComplejoId,
                    TotalBono = g.Sum(x => x.Bono),
                    TotalPago = g.Sum(x => x.BonoResidual),
                    Cantidad = g.Count(),
                    LCicloId = cicloId,
                })
                .ToList();

            var redEmpresaComplejo = residual
                .GroupBy(x => new { x.LContactoId, x.LComplejoId })
                .Select(g => new ItemRedEmpresaComplejo
                {
                    LRedEmpresaComplejoId = 0,
                    LCicloId = cicloId,
                    LContactoId = g.Key.LContactoId,
                    LComplejoId = g.Key.LComplejoId,
                    DMonto = g.Sum(x => x.BonoResidual),
                })
                .ToList();

            var listado = redEmpresaComplejo
                .GroupBy(x => x.LContactoId)
                .Select(g => new ItemAdministracionBonoResidual
                {
                    Usuario = usuario,
                    LBonoResidualId = 0,
                    LCicloId = cicloId,
                    LContactoId = g.Key,
                    DTotalBono = g.Sum(x => x.DMonto),
                    Detalle = residual
                        .Where(x => x.LContactoId == g.Key)
                        .Select(h => new AdministracionBonoResidualDetalle
                        {
                            LbonoresidualDetalleId = 0,
                            LbonoresidualId = 0,
                            LempresaId = 0,
                            LcomplejoId = h.LComplejoId,
                            Nivel = h.Nivel,
                            Producto = h.ProductoId,
                            Monto = h.Bono,
                            PorcentajeComision = h.PorcentajeComision,
                            Comision = h.BonoResidual,
                        })
                        .ToList(),
                })
                .ToList();

            var guardadoResidual =
                await _adminBonoResidualRepository.SaveAdministracionBonoResidual(
                    logTransaccionId,
                    usuario,
                    listado
                );
            var guardadoCompleto =
                await _adminBonoResidualRepository.SaveAdministracionBonoCompleto(
                    logTransaccionId,
                    usuario,
                    bonoCompleto
                );
            var guardadoRed =
                await _adminBonoResidualRepository.SaveAdministracionRedEmpresaComplejo(
                    logTransaccionId,
                    usuario,
                    redEmpresaComplejo
                );
            if (!guardadoResidual.Success || !guardadoCompleto.Success || !guardadoRed.Success)
            {
                await _controlProcesoRepository.CancelarPaso(
                    logTransaccionId,
                    usuario,
                    ProcesosDiccionario.COMISIONES,
                    cicloId,
                    paso
                );
                var mensaje =
                    !guardadoResidual.Success ? guardadoResidual.Mensaje
                    : !guardadoCompleto.Success ? guardadoCompleto.Mensaje
                    : guardadoRed.Mensaje;
                return new BonoResidualGuardadoResult { Mensaje = mensaje };
            }

            var finPaso = await _controlProcesoRepository.FinalizarPaso(
                logTransaccionId,
                usuario,
                ProcesosDiccionario.COMISIONES,
                cicloId,
                paso
            );
            if (!finPaso.Success || !(finPaso.Data?.status ?? false))
                return new BonoResidualGuardadoResult
                {
                    Mensaje = finPaso.Data?.mensaje ?? finPaso.Mensaje,
                };
            pasoIniciado = false;
            return new BonoResidualGuardadoResult
            {
                Success = true,
                Mensaje = "Se guardo correctamente el bono residual.",
                Data = new
                {
                    listaCuota = calculo.TotalCuotas,
                    contacto = calculo.TotalContactos,
                    Residual = residual.Count,
                    ResidualActivos = residual.Count(x => x.ActivoMes),
                    ResidualInactivos = residual.Count(x => !x.ActivoMes),
                    inicio,
                    fin = DateTime.Now,
                },
            };
        }
        catch (Exception ex)
        {
            if (pasoIniciado)
                await _controlProcesoRepository.CancelarPaso(
                    logTransaccionId,
                    usuario,
                    ProcesosDiccionario.COMISIONES,
                    cicloId,
                    paso
                );
            _log.Error(
                logTransaccionId,
                NombreArchivo,
                "GuardarBonoResidualAsync()",
                "Fin de metodo",
                ex
            );
            return new BonoResidualGuardadoResult { Mensaje = ex.Message };
        }
    }

    public async Task<BonoResidualGuardadoResult> ProcesarBonoResidualAsync(
        string usuario,
        int cicloId,
        string logTransaccionId
    )
    {
        var calculo = await ConstruirListadoResidualAsync(usuario, cicloId, logTransaccionId);
        return await GuardarBonoResidualAsync(
            usuario,
            cicloId,
            logTransaccionId,
            calculo,
            DateTime.Now
        );
    }

    public async Task<BonoResidualGuardadoResult> GuardarBonoParAsync(
        string usuario,
        int cicloId,
        string logTransaccionId
    )
    {
        var pasoIniciado = false;
        var pasoActual = string.Empty;
        try
        {
            var ciclo = await _administracionCicloRepository.GetCiclo(logTransaccionId, cicloId);
            if (!ciclo.Success || ciclo.Data.LCicloId <= 0)
                return new BonoResidualGuardadoResult
                {
                    Mensaje = $"No se encontró el ciclo {cicloId}.",
                };
            var inicio = ciclo.Data.DtFechaInicio ?? string.Empty;
            var fin = ciclo.Data.DtFechaFin ?? string.Empty;
            if (string.IsNullOrWhiteSpace(inicio) || string.IsNullOrWhiteSpace(fin))
                return new BonoResidualGuardadoResult
                {
                    Mensaje = $"El ciclo {cicloId} no tiene fechas configuradas.",
                };

            var siguientePaso = await _controlProcesoRepository.GetSiguientePaso(
                logTransaccionId,
                usuario,
                ProcesosDiccionario.COMISIONES,
                cicloId
            );
            if (!PasosDiccionario.EsBonoPar(siguientePaso.Data.nombre))
                return new BonoResidualGuardadoResult
                {
                    Mensaje =
                        "Esta paso ya se encuentra ejecutado para este ciclo, si quieres volver a a procesar debes reinicar el proceso para el ciclo",
                };
            pasoActual = siguientePaso.Data.nombre;

            var inicioPaso = await _controlProcesoRepository.IniciarPaso(
                logTransaccionId,
                usuario,
                ProcesosDiccionario.COMISIONES,
                cicloId,
                pasoActual
            );
            if (!inicioPaso.Success || !(inicioPaso.Data?.status ?? false))
                return new BonoResidualGuardadoResult
                {
                    Mensaje = inicioPaso.Data?.mensaje ?? inicioPaso.Mensaje,
                };
            pasoIniciado = true;

            var bonoPar = await _bonoParRepository.GetBonoPar(
                logTransaccionId,
                usuario,
                inicio,
                fin
            );
            var habilitaciones = await _habilitacionRepository.GetHabilitaciones(
                logTransaccionId,
                usuario,
                cicloId
            );
            var contratos =
                await _administracionContratoRepository.GetAdministracionContratoFechaVentaResidual(
                    logTransaccionId,
                    inicio,
                    fin
                );
            if (!bonoPar.Success || !habilitaciones.Success)
            {
                await _controlProcesoRepository.CancelarPaso(
                    logTransaccionId,
                    usuario,
                    ProcesosDiccionario.COMISIONES,
                    cicloId,
                    pasoActual
                );
                return new BonoResidualGuardadoResult
                {
                    Mensaje = !bonoPar.Success ? bonoPar.Mensaje : habilitaciones.Mensaje,
                };
            }

            var bloqueados = HabilitacionComisionHelper.GetContactosBloqueadosParaComision(
                habilitaciones.Data
            );
            var habilitados = HabilitacionComisionHelper.GetContactosHabilitadosQueGeneranComision(
                habilitaciones.Data
            );
            var contratosNormales = contratos
                .Data.Where(item =>
                    !HabilitacionComisionHelper.TiposContratoEspeciales.Contains(
                        item.LTipoContratoId
                    )
                )
                .Select(item => item.LAsesorId)
                .ToHashSet();
            var listado = bonoPar
                .Data.Where(item =>
                    !bloqueados.Contains(item.LContctoGanadorId)
                    && (
                        contratosNormales.Contains(item.LContctoGanadorId)
                        || habilitados.Contains(item.LContctoGanadorId)
                    )
                )
                .ToList();

            if (listado.Count == 0)
            {
                var finSinDatos = await _controlProcesoRepository.FinalizarPaso(
                    logTransaccionId,
                    usuario,
                    ProcesosDiccionario.COMISIONES,
                    cicloId,
                    pasoActual
                );
                if (!finSinDatos.Success || !(finSinDatos.Data?.status ?? false))
                    return new BonoResidualGuardadoResult
                    {
                        Mensaje = finSinDatos.Data?.mensaje ?? finSinDatos.Mensaje,
                    };
                pasoIniciado = false;
                return new BonoResidualGuardadoResult
                {
                    Success = true,
                    Mensaje = "No existen ganadores habilitados para generar bono par.",
                };
            }

            var guardado = await _bonoParRepository.SaveBonoPar(
                logTransaccionId,
                usuario,
                cicloId,
                listado
            );
            if (!guardado.Success)
            {
                await _controlProcesoRepository.CancelarPaso(
                    logTransaccionId,
                    usuario,
                    ProcesosDiccionario.COMISIONES,
                    cicloId,
                    pasoActual
                );
                return new BonoResidualGuardadoResult { Mensaje = guardado.Mensaje };
            }

            var finPaso = await _controlProcesoRepository.FinalizarPaso(
                logTransaccionId,
                usuario,
                ProcesosDiccionario.COMISIONES,
                cicloId,
                pasoActual
            );
            if (!finPaso.Success || !(finPaso.Data?.status ?? false))
                return new BonoResidualGuardadoResult
                {
                    Mensaje = finPaso.Data?.mensaje ?? finPaso.Mensaje,
                };
            pasoIniciado = false;
            return new BonoResidualGuardadoResult
            {
                Success = true,
                Mensaje = guardado.Mensaje,
                Data = "",
            };
        }
        catch (Exception ex)
        {
            if (pasoIniciado)
                await _controlProcesoRepository.CancelarPaso(
                    logTransaccionId,
                    usuario,
                    ProcesosDiccionario.COMISIONES,
                    cicloId,
                    pasoActual
                );
            _log.Error(
                logTransaccionId,
                NombreArchivo,
                "GuardarBonoParAsync()",
                "Fin de metodo",
                ex
            );
            return new BonoResidualGuardadoResult { Mensaje = ex.Message };
        }
    }
}
