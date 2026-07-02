using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text.Json;

namespace ApiGuardian.Infrastructure.Repositories;

public partial class AplicacionesRepositorio : IAplicacionesRepositorio
{
    private readonly DapperContext _guardianContext;
    private readonly DapperContextSqlServer _sqlContext;
    private readonly ILogService _registro;
    private readonly IConfiguration _configuracion;
    private readonly IHttpClientFactory _fabricaHttpClient;
    private readonly ConfiguracionAplicaciones _configuracionAplicaciones;
    private const string NombreArchivo = "AplicacionesRepositorio.cs";

    public AplicacionesRepositorio(
        DapperContext guardianContext,
        DapperContextSqlServer sqlContext,
        ILogService registro,
        IConfiguration configuracion,
        IHttpClientFactory fabricaHttpClient
    )
    {
        _guardianContext = guardianContext;
        _sqlContext = sqlContext;
        _registro = registro;
        _configuracion = configuracion;
        _fabricaHttpClient = fabricaHttpClient;
        _configuracionAplicaciones = _configuracion.GetSection("Aplicaciones").Get<ConfiguracionAplicaciones>() ?? new ConfiguracionAplicaciones();
    }

    public async Task<(RespuestaVistaPreviaAplicaciones Datos, bool Exito, string Mensaje)> VistaPrevia(string logTransaccionId, int lCicloId)
    {
        var resultado = await EjecutarProcesoAsync(logTransaccionId, lCicloId, soloVistaPrevia: true);
        var respuesta = ConstruirRespuestaVistaPrevia(resultado.Datos ?? new EstadoProcesoAplicaciones(lCicloId, true));
        return (respuesta, resultado.Exito, resultado.Mensaje);
    }

    public async Task<(RespuestaEjecucionAplicaciones Datos, bool Exito, string Mensaje)> Aplicar(string logTransaccionId, int lCicloId)
    {
        var resultado = await EjecutarProcesoAsync(logTransaccionId, lCicloId, soloVistaPrevia: false);
        var respuesta = ConstruirRespuestaEjecucion(resultado.Datos ?? new EstadoProcesoAplicaciones(lCicloId, false));
        return (respuesta, resultado.Exito, resultado.Mensaje);
    }

    private async Task<ResultadoAplicaciones<EstadoProcesoAplicaciones>> EjecutarProcesoAsync(
        string logTransaccionId,
        int ciclo,
        bool soloVistaPrevia
    )
    {
        var estado = new EstadoProcesoAplicaciones(ciclo, soloVistaPrevia);
        var metodo = soloVistaPrevia ? "VistaPrevia" : "Aplicar";

        try
        {
            _registro.Info(logTransaccionId, NombreArchivo, metodo, $"Inicio proceso aplicaciones. ciclo:{ciclo}, vistaPrevia:{soloVistaPrevia}");

            var resultadoValidacion = await ValidarConexionesAsync();
            if (!resultadoValidacion.Exito)
            {
                return ConstruirEstadoFallido(estado, resultadoValidacion.Mensaje, resultadoValidacion.EsFatal);
            }

            if (soloVistaPrevia)
            {
                estado.Notas.Add(
                    "La vista previa no ejecuta RetencionEmpresa(), no inserta prioridades ni sincroniza tablas; usa el estado actual y simulacion en memoria."
                );
            }
            else
            {
                var resultadoLimpiarDatosCiclo = await LimpiarDatosCicloAsync(ciclo);
                if (!resultadoLimpiarDatosCiclo.Exito)
                {
                    return ConstruirEstadoFallido(estado, resultadoLimpiarDatosCiclo.Mensaje, resultadoLimpiarDatosCiclo.EsFatal);
                }

                estado.Notas.Add(
                    "Antes de ejecutar aplicar se limpiaron por ciclo las tablas derivadas de BDQISHUR y grdsion."
                );

                var resultadoPreparacion = await CargarUltimosDatosComisionAsync();
                if (!resultadoPreparacion.Exito)
                {
                    return ConstruirEstadoFallido(estado, resultadoPreparacion.Mensaje, resultadoPreparacion.EsFatal);
                }

                resultadoPreparacion = await CargarPrioridadesFaltantesAsync();
                if (!resultadoPreparacion.Exito)
                {
                    return ConstruirEstadoFallido(estado, resultadoPreparacion.Mensaje, resultadoPreparacion.EsFatal);
                }
            }

            var resultadoPagosSesion = await ObtenerPagosPorCicloAsync(ciclo);
            if (!resultadoPagosSesion.Exito || resultadoPagosSesion.Datos is null)
            {
                return ConstruirEstadoFallido(estado, resultadoPagosSesion.Mensaje, resultadoPagosSesion.EsFatal);
            }

            estado.PagosSesion = resultadoPagosSesion.Datos;

            var resultadoComisionesEmpresaExistentes = await ExistenComisionesEmpresaAsync(ciclo);
            if (!resultadoComisionesEmpresaExistentes.Exito)
            {
                return ConstruirEstadoFallido(estado, resultadoComisionesEmpresaExistentes.Mensaje, resultadoComisionesEmpresaExistentes.EsFatal);
            }

            estado.ExistenComisionesPorEmpresa = resultadoComisionesEmpresaExistentes.Datos;

            if (!soloVistaPrevia && !estado.ExistenComisionesPorEmpresa)
            {
                var resultadoSincronizacion = await SincronizarComisionesEmpresaAsync(ciclo);
                if (!resultadoSincronizacion.Exito)
                {
                    return ConstruirEstadoFallido(estado, resultadoSincronizacion.Mensaje, resultadoSincronizacion.EsFatal);
                }

                estado.ExistenComisionesPorEmpresa = true;
            }
            else if (soloVistaPrevia && !estado.ExistenComisionesPorEmpresa)
            {
                estado.Notas.Add(
                    "AplicacionesComisionPorEmpresa no existe para este ciclo; la vista previa construye los montos en memoria desde Guardian."
                );
            }

            var resultadoComisionadosGuardian = await ObtenerComisionadosGuardianAsync(ciclo);
            if (!resultadoComisionadosGuardian.Exito || resultadoComisionadosGuardian.Datos is null)
            {
                return ConstruirEstadoFallido(estado, resultadoComisionadosGuardian.Mensaje, resultadoComisionadosGuardian.EsFatal);
            }

            estado.TotalComisionadosGuardian = resultadoComisionadosGuardian.Datos.Count;

            var resultadoTotales = await ResolverTotalesEmpresaAsync(ciclo, soloVistaPrevia);
            if (!resultadoTotales.Exito || resultadoTotales.Datos is null)
            {
                return ConstruirEstadoFallido(estado, resultadoTotales.Mensaje, resultadoTotales.EsFatal);
            }

            var resultadoComisionadosExistentes = await ExistenComisionadosRegistradosAsync(ciclo);
            if (!resultadoComisionadosExistentes.Exito)
            {
                return ConstruirEstadoFallido(estado, resultadoComisionadosExistentes.Mensaje, resultadoComisionadosExistentes.EsFatal);
            }

            estado.AplicacionesComisionadoExiste = resultadoComisionadosExistentes.Datos;

            List<ComisionadoPendienteAplicaciones> comisionadosPendientes;
            if (estado.AplicacionesComisionadoExiste)
            {
                var resultadoPendientes = await ObtenerComisionadosPendientesAsync(ciclo);
                if (!resultadoPendientes.Exito || resultadoPendientes.Datos is null)
                {
                    return ConstruirEstadoFallido(estado, resultadoPendientes.Mensaje, resultadoPendientes.EsFatal);
                }

                comisionadosPendientes = resultadoPendientes.Datos;
            }
            else
            {
                estado.RequiereRegistrarComisionados = true;

                var resultadoConstruccionComisionados = ConstruirComisionadosRegistrar(
                    ciclo,
                    resultadoComisionadosGuardian.Datos,
                    resultadoTotales.Datos
                );
                if (!resultadoConstruccionComisionados.Exito || resultadoConstruccionComisionados.Datos is null)
                {
                    return ConstruirEstadoFallido(estado, resultadoConstruccionComisionados.Mensaje, resultadoConstruccionComisionados.EsFatal);
                }

                if (soloVistaPrevia)
                {
                    comisionadosPendientes = ConstruirComisionadosPendientesVistaPrevia(resultadoConstruccionComisionados.Datos, estado.PagosSesion);
                    estado.Notas.Add("La vista previa no inserta en AplicacionesComisionado; el listado pendiente es simulado.");
                }
                else
                {
                    var resultadoRegistro = await RegistrarComisionadosAsync(resultadoConstruccionComisionados.Datos);
                    if (!resultadoRegistro.Exito)
                    {
                        return ConstruirEstadoFallido(estado, resultadoRegistro.Mensaje, resultadoRegistro.EsFatal);
                    }

                    var resultadoPendientes = await ObtenerComisionadosPendientesAsync(ciclo);
                    if (!resultadoPendientes.Exito || resultadoPendientes.Datos is null)
                    {
                        return ConstruirEstadoFallido(estado, resultadoPendientes.Mensaje, resultadoPendientes.EsFatal);
                    }

                    comisionadosPendientes = resultadoPendientes.Datos;
                    estado.AplicacionesComisionadoExiste = true;
                }
            }

            estado.TotalPendientes = comisionadosPendientes.Count;
            estado.TotalPendienteAplicar = comisionadosPendientes.Sum(item => item.MontoRestante);
            int contador = 0;
            foreach (var comisionado in comisionadosPendientes)
            {
                var resultadoProceso = await ProcesarComisionadoAsync(logTransaccionId, comisionado, estado);
                if (!resultadoProceso.Exito)
                {
                    estado.TotalErrores++;
                    if (resultadoProceso.EsFatal)
                    {
                        return ConstruirEstadoFallido(estado, resultadoProceso.Mensaje, true);
                    }
                }
                
                _registro.Info(logTransaccionId, NombreArchivo, metodo, $"Fin proceso aplicaciones. ciclo:{ciclo}, comisionado:{JsonSerializer.Serialize(comisionado)}, contador:{contador}");
                contador++;
                if (contador == 167)
                {
                    _registro.Info(logTransaccionId, NombreArchivo, metodo, $"Pausa intencional para monitoreo. ciclo:{ciclo}, contador:{contador}");
                    await Task.Delay(5000);
                    
                }
            }

            estado.Notas.Add(soloVistaPrevia
                ? "Vista previa finalizada. No se escribio informacion en base de datos."
                : "Fin del proceso, se dejara listo el envio futuro del informe.");

            _registro.Info(logTransaccionId, NombreArchivo, metodo, $"Fin proceso aplicaciones. ciclo:{ciclo}, vistaPrevia:{soloVistaPrevia}");
            return ResultadoAplicaciones<EstadoProcesoAplicaciones>.Ok(estado, "Proceso de aplicaciones ejecutado correctamente.");
        }
        catch (Exception ex)
        {
            _registro.Error(logTransaccionId, NombreArchivo, metodo, "Error en proceso de aplicaciones", ex);
            return ConstruirEstadoFallido(estado, ex.Message, true);
        }
    }

    private async Task<ResultadoAplicaciones<Dictionary<string, decimal>>> ResolverTotalesEmpresaAsync(int ciclo, bool soloVistaPrevia)
    {
        if (!soloVistaPrevia)
        {
            return await ObtenerTotalesEmpresaPorDocumentoAsync(ciclo);
        }

        var resultadoExiste = await ExistenComisionesEmpresaAsync(ciclo);
        if (!resultadoExiste.Exito)
        {
            return ResultadoAplicaciones<Dictionary<string, decimal>>.Fail(resultadoExiste.Mensaje, resultadoExiste.EsFatal);
        }

        if (resultadoExiste.Datos)
        {
            return await ObtenerTotalesEmpresaPorDocumentoAsync(ciclo);
        }

        var resultadoFilasOrigen = await ConstruirRegistrosComisionEmpresaDesdeOrigenAsync(ciclo);
        if (!resultadoFilasOrigen.Exito || resultadoFilasOrigen.Datos is null)
        {
            return ResultadoAplicaciones<Dictionary<string, decimal>>.Fail(
                resultadoFilasOrigen.Mensaje,
                resultadoFilasOrigen.EsFatal
            );
        }

        var totales = resultadoFilasOrigen.Datos
            .GroupBy(item => item.NumeroDocumento.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(item => item.MontoNeto),
                StringComparer.OrdinalIgnoreCase
            );

        return ResultadoAplicaciones<Dictionary<string, decimal>>.Ok(totales);
    }

    private ResultadoAplicaciones<List<ComisionadoAplicaciones>> ConstruirComisionadosRegistrar(
        int ciclo,
        IReadOnlyCollection<ComisionadoGuardianAplicaciones> comisionadosGuardian,
        IReadOnlyDictionary<string, decimal> totalesPorDocumento
    )
    {
        if (_configuracionAplicaciones.RequerirCoincidenciaCantidadComisionados && totalesPorDocumento.Count != comisionadosGuardian.Count)
        {
            return ResultadoAplicaciones<List<ComisionadoAplicaciones>>.Fail(
                $"La cantidad de comisionados de Guardian ({comisionadosGuardian.Count}) no coincide con los totales por empresa ({totalesPorDocumento.Count}) para el ciclo {ciclo}.",
                true
            );
        }

        var comisionados = new List<ComisionadoAplicaciones>();
        foreach (var comisionadoGuardian in comisionadosGuardian)
        {
            if (!totalesPorDocumento.TryGetValue(comisionadoGuardian.NumeroDocumento.Trim(), out var totalAplicar))
            {
                return ResultadoAplicaciones<List<ComisionadoAplicaciones>>.Fail(
                    $"No se encontro TotalAplicar para el comisionado {comisionadoGuardian.NumeroDocumento}.",
                    true
                );
            }

            comisionados.Add(
                new ComisionadoAplicaciones
                {
                    Ciclo = ciclo,
                    ContactoId = comisionadoGuardian.ContactoId,
                    Codigo = comisionadoGuardian.Codigo,
                    NumeroDocumento = comisionadoGuardian.NumeroDocumento.Trim(),
                    NombreCompleto = comisionadoGuardian.NombreCompleto,
                    TotalAplicar = totalAplicar,
                    FechaRegistro = DateTime.Now,
                    Estado = 0,
                    Observacion = string.Empty
                }
            );
        }

        return ResultadoAplicaciones<List<ComisionadoAplicaciones>>.Ok(comisionados);
    }

    private static List<ComisionadoPendienteAplicaciones> ConstruirComisionadosPendientesVistaPrevia(
        IEnumerable<ComisionadoAplicaciones> comisionados,
        IEnumerable<RegistroPagoAplicaciones> pagosExistentes
    )
    {
        var totalesAplicados = pagosExistentes
            .GroupBy(item => item.DocumentoCliente.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Monto), StringComparer.OrdinalIgnoreCase);

        return comisionados
            .Select(item =>
            {
                totalesAplicados.TryGetValue(item.NumeroDocumento.Trim(), out var totalAplicado);
                return new ComisionadoPendienteAplicaciones
                {
                    Id = item.Id,
                    Ciclo = item.Ciclo,
                    ContactoId = item.ContactoId,
                    Codigo = item.Codigo,
                    NumeroDocumento = item.NumeroDocumento,
                    NombreCompleto = item.NombreCompleto,
                    TotalAplicar = item.TotalAplicar,
                    FechaRegistro = item.FechaRegistro,
                    Estado = item.Estado,
                    Observacion = item.Observacion,
                    TotalAplicado = totalAplicado,
                    MontoRestante = item.TotalAplicar - totalAplicado
                };
            })
            .Where(item => item.MontoRestante != 0)
            .Where(item => !item.NumeroDocumento.Equals("4823437", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private async Task<ResultadoAplicaciones> ProcesarComisionadoAsync(string logTransaccionId, ComisionadoPendienteAplicaciones comisionado, EstadoProcesoAplicaciones estado)
    {
        var resultadoComisionado = new ResultadoComisionadoAplicaciones
        {
            Carnet = comisionado.NumeroDocumento,
            NombreCompleto = comisionado.NombreCompleto,
            TotalAplicar = comisionado.TotalAplicar,
            SaldoInicial = comisionado.MontoRestante,
            SaldoFinal = comisionado.MontoRestante
        };
        estado.Comisionados.Add(resultadoComisionado);

        var resultadoGrupoSion = await AplicarGrupoSionAsync(logTransaccionId, comisionado, estado, resultadoComisionado);
        if (!resultadoGrupoSion.Exito)
        {
            resultadoComisionado.ErrorGrave = resultadoGrupoSion.EsFatal;
            resultadoComisionado.Mensaje = resultadoGrupoSion.Mensaje;
            return resultadoGrupoSion;
        }

        var resultadoCarta = await AplicarCartaAsync(logTransaccionId, comisionado, estado, resultadoComisionado);
        if (!resultadoCarta.Exito)
        {
            resultadoComisionado.ErrorGrave = resultadoCarta.EsFatal;
            resultadoComisionado.Mensaje = resultadoCarta.Mensaje;
            return resultadoCarta;
        }

        var resultadoDescuentos = await AplicarDescuentosAsync(logTransaccionId, comisionado, estado, resultadoComisionado);
        if (!resultadoDescuentos.Exito)
        {
            resultadoComisionado.ErrorGrave = resultadoDescuentos.EsFatal;
            resultadoComisionado.Mensaje = resultadoDescuentos.Mensaje;
            return resultadoDescuentos;
        }

        var resultadoProrrateo = await AplicarProrrateoAsync(logTransaccionId, comisionado, estado, resultadoComisionado);
        if (!resultadoProrrateo.Exito)
        {
            resultadoComisionado.ErrorGrave = resultadoProrrateo.EsFatal;
            resultadoComisionado.Mensaje = resultadoProrrateo.Mensaje;
            return resultadoProrrateo;
        }

        if (estado.SoloVistaPrevia)
        {
            resultadoComisionado.Operaciones.Add(
                new OperacionAplicacion
                {
                    Paso = "Procesado",
                    Estado = "Planificado",
                    Observacion = "El comisionado seria marcado como procesado al ejecutar aplicar."
                }
            );
            resultadoComisionado.Mensaje = "VistaPrevia generado correctamente.";
        }
        else
        {
            var resultadoMarcado = await MarcarProcesadoAsync(comisionado.Ciclo, comisionado.NumeroDocumento);
            if (!resultadoMarcado.Exito)
            {
                resultadoComisionado.ErrorGrave = resultadoMarcado.EsFatal;
                resultadoComisionado.Mensaje = resultadoMarcado.Mensaje;
                return resultadoMarcado;
            }

            resultadoComisionado.Operaciones.Add(
                new OperacionAplicacion
                {
                    Paso = "Procesado",
                    Estado = "Aplicado",
                    Observacion = "Comisionado marcado como procesado."
                }
            );
            resultadoComisionado.Procesado = true;
            estado.TotalProcesados++;
            resultadoComisionado.Mensaje = "Comisionado procesado correctamente.";
        }

        resultadoComisionado.SaldoFinal = comisionado.MontoRestante;
        return ResultadoAplicaciones.Ok();
    }

    private async Task<ResultadoAplicaciones> AplicarGrupoSionAsync(string logTransaccionId, ComisionadoPendienteAplicaciones comisionado, EstadoProcesoAplicaciones estado, ResultadoComisionadoAplicaciones resultadoComisionado)
    {
        var saldoRestante = comisionado.MontoRestante;
        var primeraIteracion = true;
        var debeContinuar = true;
        var expensasPerdonadas = false;

        var resultadoClavesReprogramadas = await ObtenerClavesProductosReprogramadosAsync(comisionado.NumeroDocumento);
        if (!resultadoClavesReprogramadas.Exito || resultadoClavesReprogramadas.Datos is null)
        {
            return ResultadoAplicaciones.Fail(resultadoClavesReprogramadas.Mensaje, resultadoClavesReprogramadas.EsFatal);
        }

        while (saldoRestante > 0 && debeContinuar)
        {
            var resultadoProductos = await ObtenerCarteraProductosAsync(comisionado.NumeroDocumento);
            if (!resultadoProductos.Exito || resultadoProductos.Datos is null)
            {
                return ResultadoAplicaciones.Fail(resultadoProductos.Mensaje, resultadoProductos.EsFatal);
            }

            var productos = FiltrarProductosElegibles(resultadoProductos.Datos, logTransaccionId, "GrupoSion", comisionado.NumeroDocumento);
            if (productos.Count == 0)
            {
                comisionado.MontoRestante = saldoRestante;
                return ResultadoAplicaciones.Ok("El comisionado no tiene productos elegibles en Grupo Sion.");
            }

            var tieneProductosVencidos = productos.Any(item => item.CuotasVencidas > 0);
            if (primeraIteracion && tieneProductosVencidos)
            {
                foreach (var producto in productos.Where(item => item.CuotasVencidas > 0))
                {
                    var resultadoCuotas = await ObtenerCuotasAsync(producto.EmpresaId, producto.VentaId, DateTime.Now, producto.CuotasVencidas);
                    if (!resultadoCuotas.Exito || resultadoCuotas.Datos is null)
                    {
                        return ResultadoAplicaciones.Fail(resultadoCuotas.Mensaje, resultadoCuotas.EsFatal);
                    }

                    var primeraCuota = resultadoCuotas.Datos.FirstOrDefault();
                    if (primeraCuota is null)
                    {
                        continue;
                    }

                    var resultadoAplicacion = await AplicarCuotaAsync(logTransaccionId, "GrupoSion", producto, primeraCuota, saldoRestante, comisionado, estado, resultadoComisionado);
                    if (!resultadoAplicacion.Exito)
                    {
                        return resultadoAplicacion;
                    }

                    saldoRestante = resultadoAplicacion.Datos;
                    if (saldoRestante <= 0)
                    {
                        break;
                    }
                }

                primeraIteracion = false;
                continue;
            }

            if (tieneProductosVencidos)
            {
                var resultadoCandidatosVencidos = await ConstruirCandidatosVencidosAsync(productos);
                if (!resultadoCandidatosVencidos.Exito || resultadoCandidatosVencidos.Datos is null)
                {
                    return ResultadoAplicaciones.Fail(resultadoCandidatosVencidos.Mensaje, resultadoCandidatosVencidos.EsFatal);
                }

                if (resultadoCandidatosVencidos.Datos.Count == 0)
                {
                    expensasPerdonadas = true;
                }

                var resultadoAplicacionVencidos = await AplicarCandidatosAsync(
                    logTransaccionId,
                    "GrupoSion",
                    resultadoCandidatosVencidos.Datos,
                    saldoRestante,
                    comisionado,
                    estado,
                    resultadoComisionado
                );
                if (!resultadoAplicacionVencidos.Exito)
                {
                    return ResultadoAplicaciones.Fail(resultadoAplicacionVencidos.Mensaje, resultadoAplicacionVencidos.EsFatal);
                }

                saldoRestante = resultadoAplicacionVencidos.Datos;
            }

            var clienteEstaAlDia = !productos.Any(item => item.CuotasVencidas > 0) || expensasPerdonadas;
            if (saldoRestante > 0 && clienteEstaAlDia)
            {
                var resultadoMesActual = await ConstruirCandidatosCuotaAsync(
                    productos,
                    cuota => cuota.FechaVencimiento.Month == DateTime.Now.Month && cuota.FechaVencimiento.Year == DateTime.Now.Year
                );
                if (!resultadoMesActual.Exito || resultadoMesActual.Datos is null)
                {
                    return ResultadoAplicaciones.Fail(resultadoMesActual.Mensaje, resultadoMesActual.EsFatal);
                }

                var resultadoAplicacionMesActual = await AplicarCandidatosAsync(
                    logTransaccionId,
                    "GrupoSion",
                    resultadoMesActual.Datos,
                    saldoRestante,
                    comisionado,
                    estado,
                    resultadoComisionado
                );
                if (!resultadoAplicacionMesActual.Exito)
                {
                    return ResultadoAplicaciones.Fail(resultadoAplicacionMesActual.Mensaje, resultadoAplicacionMesActual.EsFatal);
                }

                saldoRestante = resultadoAplicacionMesActual.Datos;

                var resultadoProximoMes = await ConstruirCandidatosCuotaAsync(
                    productos,
                    cuota => cuota.NumeroCuota == 2 && EsProximoMes(cuota.FechaVencimiento, DateTime.Now)
                );
                if (!resultadoProximoMes.Exito || resultadoProximoMes.Datos is null)
                {
                    return ResultadoAplicaciones.Fail(resultadoProximoMes.Mensaje, resultadoProximoMes.EsFatal);
                }

                var resultadoAplicacionProximoMes = await AplicarCandidatosAsync(
                    logTransaccionId,
                    "GrupoSion",
                    resultadoProximoMes.Datos,
                    saldoRestante,
                    comisionado,
                    estado,
                    resultadoComisionado
                );
                if (!resultadoAplicacionProximoMes.Exito)
                {
                    return ResultadoAplicaciones.Fail(resultadoAplicacionProximoMes.Mensaje, resultadoAplicacionProximoMes.EsFatal);
                }

                saldoRestante = resultadoAplicacionProximoMes.Datos;

                var productosReprogramados = productos
                    .Where(item => resultadoClavesReprogramadas.Datos.Contains(item.ClaveProducto))
                    .ToList();
                var resultadoReprogramados = await ConstruirCandidatosCuotaAsync(
                    productosReprogramados,
                    cuota => cuota.FechaVencimiento.Date <= DateTime.Now.Date
                );
                if (!resultadoReprogramados.Exito || resultadoReprogramados.Datos is null)
                {
                    return ResultadoAplicaciones.Fail(resultadoReprogramados.Mensaje, resultadoReprogramados.EsFatal);
                }

                var resultadoAplicacionReprogramados = await AplicarCandidatosAsync(
                    logTransaccionId,
                    "GrupoSion",
                    resultadoReprogramados.Datos,
                    saldoRestante,
                    comisionado,
                    estado,
                    resultadoComisionado
                );
                if (!resultadoAplicacionReprogramados.Exito)
                {
                    return ResultadoAplicaciones.Fail(resultadoAplicacionReprogramados.Mensaje, resultadoAplicacionReprogramados.EsFatal);
                }

                saldoRestante = resultadoAplicacionReprogramados.Datos;
                debeContinuar = resultadoMesActual.Datos.Count > 0;

                if (!debeContinuar && saldoRestante > 0 && saldoRestante < _configuracionAplicaciones.MontoMinimoParaPagoACuenta)
                {
                    var resultadoACuenta = await ConstruirCandidatosCuotaAsync(
                        productos,
                        cuota => cuota.FechaVencimiento >= DateTime.Now.AddMonths(1) || cuota.NumeroCuota == 3
                    );
                    if (!resultadoACuenta.Exito || resultadoACuenta.Datos is null)
                    {
                        return ResultadoAplicaciones.Fail(resultadoACuenta.Mensaje, resultadoACuenta.EsFatal);
                    }

                    var resultadoAplicacionACuenta = await AplicarCandidatosAsync(
                        logTransaccionId,
                        "GrupoSion",
                        resultadoACuenta.Datos,
                        saldoRestante,
                        comisionado,
                        estado,
                        resultadoComisionado
                    );
                    if (!resultadoAplicacionACuenta.Exito)
                    {
                        return ResultadoAplicaciones.Fail(resultadoAplicacionACuenta.Mensaje, resultadoAplicacionACuenta.EsFatal);
                    }

                    saldoRestante = resultadoAplicacionACuenta.Datos;
                }
            }
            else
            {
                debeContinuar = false;
            }
        }

        comisionado.MontoRestante = saldoRestante;
        return ResultadoAplicaciones.Ok();
    }

    private async Task<ResultadoAplicaciones> AplicarCartaAsync(
        string logTransaccionId,
        ComisionadoPendienteAplicaciones comisionado,
        EstadoProcesoAplicaciones estado,
        ResultadoComisionadoAplicaciones resultadoComisionado
    )
    {
        var saldoRestante = comisionado.MontoRestante;
        if (saldoRestante <= 0)
        {
            comisionado.MontoRestante = saldoRestante;
            return ResultadoAplicaciones.Ok();
        }

        var resultadoCartas = await ObtenerCartasAsync(comisionado.NumeroDocumento);
        if (!resultadoCartas.Exito || resultadoCartas.Datos is null)
        {
            return ResultadoAplicaciones.Fail(resultadoCartas.Mensaje, resultadoCartas.EsFatal);
        }

        var cartas = resultadoCartas.Datos
            .Where(item => item.FechaFin.Date >= DateTime.Now.Date)
            .Where(item => item.CuotasAplicar != 0)
            .OrderByDescending(item => item.CuotasAplicar)
            .ToList();

        foreach (var carta in cartas)
        {
            if (saldoRestante <= 0)
            {
                break;
            }

            var ilimitado = carta.CuotasAplicar < 0;
            var iteraciones = 0;

            while (saldoRestante > 0 && (ilimitado || iteraciones < carta.CuotasAplicar))
            {
                var resultadoCuota = await ObtenerCuotasAsync(carta.EmpresaId, carta.VentaId, DateTime.Now, 1);
                if (!resultadoCuota.Exito || resultadoCuota.Datos is null)
                {
                    return ResultadoAplicaciones.Fail(resultadoCuota.Mensaje, resultadoCuota.EsFatal);
                }

                var cuota = resultadoCuota.Datos.FirstOrDefault();
                if (cuota is null)
                {
                    break;
                }

                var resultadoBeneficiario = await ObtenerClientePorDocumentoAsync(carta.DocumentoBeneficiario);
                if (!resultadoBeneficiario.Exito || resultadoBeneficiario.Datos is null)
                {
                    return ResultadoAplicaciones.Fail(resultadoBeneficiario.Mensaje, resultadoBeneficiario.EsFatal);
                }

                if (DebeOmitirCartaYaRegistrada(estado.PagosSesion, comisionado.NumeroDocumento, carta, cuota.MontoPago))
                {
                    break;
                }

                var resultadoEjecucion = await EjecutarPagoAsync(
                    logTransaccionId,
                    "Carta",
                    new ProductoCarteraAplicaciones
                    {
                        EmpresaId = carta.EmpresaId,
                        ProyectoId = carta.ProyectoId,
                        VentaId = carta.VentaId,
                        ClienteId = resultadoBeneficiario.Datos.ClienteId,
                        NumeroDocumento = resultadoBeneficiario.Datos.NumeroDocumento,
                        CodigoLote = carta.CodigoProducto
                    },
                    cuota,
                    saldoRestante,
                    new ContextoEjecucionPagoAplicaciones
                    {
                        Ciclo = comisionado.Ciclo,
                        ClienteBeneficiarioId = resultadoBeneficiario.Datos.ClienteId,
                        DocumentoBeneficiario = resultadoBeneficiario.Datos.NumeroDocumento,
                        ProductoId = carta.CodigoProducto,
                        DocumentoComisionadoContable = comisionado.NumeroDocumento,
                        Observacion =
                            $"Carta de C.I.: {comisionado.NumeroDocumento} a favor de {resultadoBeneficiario.Datos.NombreCompleto.Trim()} con C.I.: {carta.DocumentoBeneficiario}",
                        TipoPagoId = -2,
                        BanderaIntercompania = 1
                    },
                    estado,
                    resultadoComisionado
                );

                if (!resultadoEjecucion.Exito)
                {
                    if (resultadoEjecucion.EsFatal)
                    {
                        return ResultadoAplicaciones.Fail(resultadoEjecucion.Mensaje, true);
                    }

                    break;
                }

                saldoRestante = resultadoEjecucion.Datos!.SaldoRestante;

                var resultadoPagado = await EstaProductoPagadoAsync(carta.DocumentoBeneficiario, carta.CodigoProducto);
                if (!resultadoPagado.Exito)
                {
                    return ResultadoAplicaciones.Fail(resultadoPagado.Mensaje, resultadoPagado.EsFatal);
                }

                if (resultadoPagado.Datos)
                {
                    break;
                }

                iteraciones++;
            }
        }

        comisionado.MontoRestante = saldoRestante;
        return ResultadoAplicaciones.Ok();
    }

    private async Task<ResultadoAplicaciones> AplicarDescuentosAsync(
        string logTransaccionId,
        ComisionadoPendienteAplicaciones comisionado,
        EstadoProcesoAplicaciones estado,
        ResultadoComisionadoAplicaciones resultadoComisionado
    )
    {
        var saldoRestante = comisionado.MontoRestante;
        if (saldoRestante <= 0)
        {
            comisionado.MontoRestante = saldoRestante;
            return ResultadoAplicaciones.Ok();
        }

        var discountsResult = await ObtenerDescuentosActivosAsync();
        if (!discountsResult.Exito || discountsResult.Datos is null)
        {
            return ResultadoAplicaciones.Fail(discountsResult.Mensaje, discountsResult.EsFatal);
        }

        var resultadoCliente = await ObtenerClientePorDocumentoAsync(comisionado.NumeroDocumento);
        if (!resultadoCliente.Exito || resultadoCliente.Datos is null)
        {
            return ResultadoAplicaciones.Fail(resultadoCliente.Mensaje, resultadoCliente.EsFatal);
        }

        var descuentosOrdenados = OrdenarDescuentos(discountsResult.Datos)
            .Where(item => string.Equals(item.DocumentoComisionado.Trim(), comisionado.NumeroDocumento.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var descuento in descuentosOrdenados)
        {
            if (saldoRestante <= 0)
            {
                break;
            }

            var monto = descuento.EsPorcentaje
                ? decimal.Round(comisionado.TotalAplicar * descuento.MontoOPorcentaje / 100m, 2, MidpointRounding.AwayFromZero)
                : descuento.MontoOPorcentaje;

            var observacion = $"Descuento por {descuento.Descripcion}";
            if (saldoRestante < monto)
            {
                monto = saldoRestante;
                observacion = $"{observacion}- A cuenta";
            }

            if (monto <= 0)
            {
                continue;
            }

            var yaRegistrado = estado.PagosSesion.Any(item =>
                item.EmpresaId == descuento.EmpresaId
                && string.Equals(item.DocumentoCliente.Trim(), comisionado.NumeroDocumento.Trim(), StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(item.ProductoId)
                && item.Monto == monto
            );

            if (yaRegistrado)
            {
                continue;
            }

            var registroPago = new RegistroPagoAplicaciones
            {
                Ciclo = comisionado.Ciclo,
                EmpresaId = descuento.EmpresaId,
                VentaId = 0,
                ClienteId = resultadoCliente.Datos.ClienteId,
                DocumentoCliente = comisionado.NumeroDocumento,
                ProductoId = string.Empty,
                Expensa = 0,
                Monto = monto,
                FechaCreacion = DateTime.Now,
                ReciboId = 0,
                FacturaId = -1,
                Observacion = observacion,
                TipoPagoId = descuento.TipoPagoId,
                BanderaIntercompania = descuento.BanderaIntercompania
            };

            if (estado.SoloVistaPrevia)
            {
                resultadoComisionado.Operaciones.Add(
                    new OperacionAplicacion
                    {
                        Paso = "Descuento",
                        Estado = "Planificado",
                        EmpresaId = descuento.EmpresaId,
                        Monto = monto,
                        Observacion = observacion
                    }
                );
                estado.PagosSesion.Add(ClonarRegistroPago(registroPago));
            }
            else
            {
                var resultadoRegistro = await RegistrarReciboPagoAsync(registroPago);
                if (!resultadoRegistro.Exito || resultadoRegistro.Datos <= 0)
                {
                    return ResultadoAplicaciones.Fail(
                        resultadoRegistro.Mensaje.Length > 0 ? resultadoRegistro.Mensaje : "No se pudo registrar el descuento en la bitacora.",
                        true
                    );
                }

                registroPago.Id = resultadoRegistro.Datos;
                estado.PagosSesion.Add(ClonarRegistroPago(registroPago));
                resultadoComisionado.Operaciones.Add(
                    new OperacionAplicacion
                    {
                        Paso = "Descuento",
                        Estado = "Aplicado",
                        EmpresaId = descuento.EmpresaId,
                        Monto = monto,
                        Observacion = observacion,
                        ReciboId = 0,
                        FacturaId = -1
                    }
                );
            }

            saldoRestante = Math.Max(saldoRestante - monto, 0);
        }

        comisionado.MontoRestante = saldoRestante;
        return ResultadoAplicaciones.Ok();
    }

    private async Task<ResultadoAplicaciones> AplicarProrrateoAsync(
        string logTransaccionId,
        ComisionadoPendienteAplicaciones comisionado,
        EstadoProcesoAplicaciones estado,
        ResultadoComisionadoAplicaciones resultadoComisionado
    )
    {
        var resultadoExistente = await ObtenerProrrateosActivosAsync(comisionado.Ciclo, comisionado.NumeroDocumento);
        if (!resultadoExistente.Exito || resultadoExistente.Datos is null)
        {
            return ResultadoAplicaciones.Fail(resultadoExistente.Mensaje, resultadoExistente.EsFatal);
        }

        if (resultadoExistente.Datos.Any(item => item.ComprobanteReciboId != 0))
        {
            resultadoComisionado.Operaciones.Add(
                new OperacionAplicacion
                {
                    Paso = "Prorrateo",
                    Estado = "Omitido",
                    Observacion = "El prorrateo ya fue procesado previamente."
                }
            );
            return ResultadoAplicaciones.Ok();
        }

        if (resultadoExistente.Datos.Count > 0 && !estado.SoloVistaPrevia)
        {
            var resultadoDeshabilitacion = await DeshabilitarProrrateosAsync(comisionado.Ciclo, comisionado.NumeroDocumento);
            if (!resultadoDeshabilitacion.Exito)
            {
                return ResultadoAplicaciones.Fail(resultadoDeshabilitacion.Mensaje, true);
            }
        }

        var resultadoComisionesEmpresa = await ObtenerComisionesEmpresaPorDocumentoAsync(comisionado.Ciclo, comisionado.NumeroDocumento);
        if (!resultadoComisionesEmpresa.Exito || resultadoComisionesEmpresa.Datos is null)
        {
            return ResultadoAplicaciones.Fail(resultadoComisionesEmpresa.Mensaje, resultadoComisionesEmpresa.EsFatal);
        }

        var aplicaciones = estado.PagosSesion
            .Where(item => string.Equals(item.DocumentoCliente.Trim(), comisionado.NumeroDocumento.Trim(), StringComparison.OrdinalIgnoreCase))
            .Select(ClonarRegistroPago)
            .ToList();

        if (aplicaciones.Count == 0)
        {
            resultadoComisionado.Operaciones.Add(
                new OperacionAplicacion
                {
                    Paso = "Prorrateo",
                    Estado = "Omitido",
                    Observacion = "No existen aplicaciones para prorratear."
                }
            );
            return ResultadoAplicaciones.Ok();
        }

        var comisiones = resultadoComisionesEmpresa.Datos
            .Select(ClonarComisionEmpresa)
            .ToList();

        foreach (var aplicacion in aplicaciones.ToList())
        {
            foreach (var comision in comisiones.ToList())
            {
                if (aplicacion.Monto <= 0 || comision.MontoNeto <= 0)
                {
                    continue;
                }

                if (comision.EmpresaId != aplicacion.EmpresaId || comision.MontoNeto < aplicacion.Monto)
                {
                    continue;
                }

                var monto = aplicacion.Monto;
                aplicacion.Monto -= monto;
                comision.MontoNeto -= monto;

                await RegistrarOperacionProrrateoAsync(logTransaccionId, estado, resultadoComisionado, comisionado, aplicacion, comision, monto);
            }
        }

        aplicaciones = aplicaciones.Where(item => item.Monto > 0).ToList();
        comisiones = comisiones.Where(item => item.MontoNeto > 0).ToList();

        foreach (var aplicacion in aplicaciones)
        {
            while (aplicacion.Monto > 0 && comisiones.Any(item => item.MontoNeto > 0))
            {
                var prestamista = comisiones.OrderByDescending(item => item.MontoNeto).First();
                var monto = Math.Min(prestamista.MontoNeto, aplicacion.Monto);

                aplicacion.Monto -= monto;
                prestamista.MontoNeto -= monto;

                await RegistrarOperacionProrrateoAsync(logTransaccionId, estado, resultadoComisionado, comisionado, aplicacion, prestamista, monto);
            }
        }

        return ResultadoAplicaciones.Ok();
    }

    private async Task RegistrarOperacionProrrateoAsync(
        string logTransaccionId,
        EstadoProcesoAplicaciones estado,
        ResultadoComisionadoAplicaciones resultadoComisionado,
        ComisionadoPendienteAplicaciones comisionado,
        RegistroPagoAplicaciones aplicacion,
        ComisionEmpresaAplicaciones comision,
        decimal monto
    )
    {
        var registro = new RegistroProrrateoAplicaciones
        {
            DocumentoCliente = comisionado.NumeroDocumento,
            Ciclo = comisionado.Ciclo,
            EmpresaPrestaId = comision.EmpresaId,
            EmpresaRecibeId = aplicacion.EmpresaId,
            ClienteId = aplicacion.ClienteId,
            ReciboId = aplicacion.ReciboId ?? 0,
            Monto = monto,
            Habilitado = true,
            ComprobanteReciboId = 0,
            BanderaIntercompania = aplicacion.BanderaIntercompania,
            TipoPagoId = aplicacion.TipoPagoId
        };

        if (estado.SoloVistaPrevia)
        {
            resultadoComisionado.Operaciones.Add(
                new OperacionAplicacion
                {
                    Paso = "Prorrateo",
                    Estado = "Planificado",
                    EmpresaId = comision.EmpresaId,
                    VentaId = aplicacion.VentaId,
                    Monto = monto,
                    Observacion = $"Empresa presta:{comision.EmpresaId}, empresa recibe:{aplicacion.EmpresaId}"
                }
            );
            return;
        }

        var resultadoInsercion = await InsertarProrrateoAsync(registro);
        if (resultadoInsercion.Exito)
        {
            resultadoComisionado.Operaciones.Add(
                new OperacionAplicacion
                {
                    Paso = "Prorrateo",
                    Estado = "Aplicado",
                    EmpresaId = comision.EmpresaId,
                    VentaId = aplicacion.VentaId,
                    Monto = monto,
                    Observacion = $"Empresa presta:{comision.EmpresaId}, empresa recibe:{aplicacion.EmpresaId}"
                }
            );
            return;
        }

        _registro.Error(logTransaccionId, NombreArchivo, "Prorrateo", resultadoInsercion.Mensaje, new Exception(resultadoInsercion.Mensaje));
        resultadoComisionado.Operaciones.Add(
            new OperacionAplicacion
            {
                Paso = "Prorrateo",
                Estado = "Error",
                EmpresaId = comision.EmpresaId,
                VentaId = aplicacion.VentaId,
                Monto = monto,
                Observacion = resultadoInsercion.Mensaje
            }
        );
    }

    private async Task<ResultadoAplicaciones<List<CandidatoPagoAplicaciones>>> ConstruirCandidatosVencidosAsync(
        IReadOnlyCollection<ProductoCarteraAplicaciones> productos
    )
    {
        var candidatos = new List<CandidatoPagoAplicaciones>();

        foreach (var producto in productos.Where(item => item.CuotasVencidas > 0))
        {
            var resultadoCuotas = await ObtenerCuotasAsync(producto.EmpresaId, producto.VentaId, DateTime.Now, producto.CuotasVencidas);
            if (!resultadoCuotas.Exito || resultadoCuotas.Datos is null)
            {
                return ResultadoAplicaciones<List<CandidatoPagoAplicaciones>>.Fail(
                    resultadoCuotas.Mensaje,
                    resultadoCuotas.EsFatal
                );
            }

            foreach (var cuota in resultadoCuotas.Datos
                         .Where(item => item.FechaVencimiento.Date <= DateTime.Now.Date)
                         .OrderBy(item => item.FechaVencimiento)
                         .ThenBy(item => item.NumeroCuota))
            {
                candidatos.Add(new CandidatoPagoAplicaciones(producto, cuota));
            }
        }

        return ResultadoAplicaciones<List<CandidatoPagoAplicaciones>>.Ok(
            candidatos.OrderBy(item => item.Producto.Prioridad)
                .ThenBy(item => item.Cuota.FechaVencimiento)
                .ThenBy(item => item.Cuota.NumeroCuota)
                .ToList()
        );
    }

    private async Task<ResultadoAplicaciones<List<CandidatoPagoAplicaciones>>> ConstruirCandidatosCuotaAsync(
        IReadOnlyCollection<ProductoCarteraAplicaciones> productos,
        Func<CuotaAplicaciones, bool> criterio
    )
    {
        var candidatos = new List<CandidatoPagoAplicaciones>();

        foreach (var producto in productos)
        {
            var resultadoCuotas = await ObtenerCuotasAsync(producto.EmpresaId, producto.VentaId, DateTime.Now, 1);
            if (!resultadoCuotas.Exito || resultadoCuotas.Datos is null)
            {
                return ResultadoAplicaciones<List<CandidatoPagoAplicaciones>>.Fail(
                    resultadoCuotas.Mensaje,
                    resultadoCuotas.EsFatal
                );
            }

            var cuota = resultadoCuotas.Datos.FirstOrDefault(criterio);
            if (cuota is null)
            {
                continue;
            }

            candidatos.Add(new CandidatoPagoAplicaciones(producto, cuota));
        }

        return ResultadoAplicaciones<List<CandidatoPagoAplicaciones>>.Ok(
            candidatos.OrderBy(item => item.Producto.Prioridad)
                .ThenBy(item => item.Cuota.FechaVencimiento)
                .ThenBy(item => item.Cuota.NumeroCuota)
                .ToList()
        );
    }

    private async Task<ResultadoAplicaciones<decimal>> AplicarCandidatosAsync(
        string logTransaccionId,
        string paso,
        IReadOnlyCollection<CandidatoPagoAplicaciones> candidatos,
        decimal saldoRestante,
        ComisionadoPendienteAplicaciones comisionado,
        EstadoProcesoAplicaciones estado,
        ResultadoComisionadoAplicaciones resultadoComisionado
    )
    {
        foreach (var candidato in candidatos)
        {
            if (saldoRestante <= 0)
            {
                break;
            }

            var resultadoAplicacion = await AplicarCuotaAsync(
                logTransaccionId,
                paso,
                candidato.Producto,
                candidato.Cuota,
                saldoRestante,
                comisionado,
                estado,
                resultadoComisionado
            );

            if (!resultadoAplicacion.Exito)
            {
                return resultadoAplicacion;
            }

            saldoRestante = resultadoAplicacion.Datos;
        }

        return ResultadoAplicaciones<decimal>.Ok(saldoRestante);
    }

    private async Task<ResultadoAplicaciones<decimal>> AplicarCuotaAsync(
        string logTransaccionId,
        string paso,
        ProductoCarteraAplicaciones producto,
        CuotaAplicaciones cuota,
        decimal saldoRestante,
        ComisionadoPendienteAplicaciones comisionado,
        EstadoProcesoAplicaciones estado,
        ResultadoComisionadoAplicaciones resultadoComisionado
    )
    {
        var resultadoLimiteFactura = await ValidarLimiteErroresFacturacionAsync(comisionado.Ciclo);
        if (!resultadoLimiteFactura.Exito)
        {
            return ResultadoAplicaciones<decimal>.Fail(resultadoLimiteFactura.Mensaje, resultadoLimiteFactura.EsFatal);
        }

        var resultadoPago = await EjecutarPagoAsync(
            logTransaccionId,
            paso,
            producto,
            cuota,
            saldoRestante,
            new ContextoEjecucionPagoAplicaciones
            {
                Ciclo = comisionado.Ciclo,
                ClienteBeneficiarioId = producto.ClienteId,
                DocumentoBeneficiario = producto.NumeroDocumento,
                ProductoId = producto.CodigoLote,
                Observacion = "Pago de Cuota",
                TipoPagoId = -1,
                BanderaIntercompania = 1
            },
            estado,
            resultadoComisionado
        );

        if (!resultadoPago.Exito)
        {
            if (resultadoPago.EsFatal)
            {
                return ResultadoAplicaciones<decimal>.Fail(resultadoPago.Mensaje, true);
            }

            return ResultadoAplicaciones<decimal>.Ok(saldoRestante, resultadoPago.Mensaje);
        }

        return ResultadoAplicaciones<decimal>.Ok(resultadoPago.Datos!.SaldoRestante, resultadoPago.Mensaje);
    }

    private async Task<ResultadoAplicaciones> ValidarLimiteErroresFacturacionAsync(int ciclo)
    {
        var failuresResult = await ContarErroresFacturacionAsync(ciclo);
        if (!failuresResult.Exito)
        {
            return ResultadoAplicaciones.Fail(failuresResult.Mensaje, failuresResult.EsFatal);
        }

        return failuresResult.Datos >= _configuracionAplicaciones.LimiteErroresFacturacion
            ? ResultadoAplicaciones.Fail(
                $"Se llego al limite de errores de facturacion para el ciclo {ciclo}. Total errores: {failuresResult.Datos}.",
                true
            )
            : ResultadoAplicaciones.Ok();
    }

    private async Task<ResultadoAplicaciones<ResultadoEjecucionPagoAplicaciones>> EjecutarPagoAsync(
        string logTransaccionId,
        string paso,
        ProductoCarteraAplicaciones producto,
        CuotaAplicaciones cuota,
        decimal montoDisponible,
        ContextoEjecucionPagoAplicaciones context,
        EstadoProcesoAplicaciones estado,
        ResultadoComisionadoAplicaciones resultadoComisionado
    )
    {
        if (montoDisponible <= 0)
        {
            return ResultadoAplicaciones<ResultadoEjecucionPagoAplicaciones>.Ok(
                new ResultadoEjecucionPagoAplicaciones { SaldoRestante = montoDisponible },
                "No hay saldo disponible para aplicar."
            );
        }

        var decisionPago = DecidirPago(cuota, montoDisponible, DateTime.Now);
        if (decisionPago.MontoPagar <= 0)
        {
            return ResultadoAplicaciones<ResultadoEjecucionPagoAplicaciones>.Ok(
                new ResultadoEjecucionPagoAplicaciones { SaldoRestante = montoDisponible },
                "La cuota no pudo convertirse en una decisionPago de pago valida."
            );
        }

        var resultadoEmpresa = await ObtenerBaseDatosEmpresaAsync(producto.EmpresaId);
        if (!resultadoEmpresa.Exito || resultadoEmpresa.Datos is null)
        {
            return ResultadoAplicaciones<ResultadoEjecucionPagoAplicaciones>.Fail(
                resultadoEmpresa.Mensaje,
                resultadoEmpresa.EsFatal
            );
        }

        if (estado.SoloVistaPrevia)
        {
            var clienteContableVistaPrevia = await ResolverClienteContableAsync(context);
            if (!clienteContableVistaPrevia.Exito || clienteContableVistaPrevia.Datos is null)
            {
                return ResultadoAplicaciones<ResultadoEjecucionPagoAplicaciones>.Fail(
                    clienteContableVistaPrevia.Mensaje,
                    clienteContableVistaPrevia.EsFatal
                );
            }

            var observacionVistaPrevia = ConstruirObservacionFinal(context.Observacion, decisionPago.SufijoObservacion);
            estado.PagosSesion.Add(
                new RegistroPagoAplicaciones
                {
                    Ciclo = context.Ciclo,
                    EmpresaId = producto.EmpresaId,
                    VentaId = producto.VentaId,
                    ClienteId = clienteContableVistaPrevia.Datos.ClienteId,
                    DocumentoCliente = clienteContableVistaPrevia.Datos.NumeroDocumento,
                    ProductoId = context.ProductoId,
                    Expensa = cuota.Expensa,
                    Monto = decisionPago.MontoPagar,
                    FechaCreacion = DateTime.Now,
                    ReciboId = 0,
                    FacturaId = 0,
                    Observacion = observacionVistaPrevia,
                    TipoPagoId = context.TipoPagoId,
                    BanderaIntercompania = context.BanderaIntercompania
                }
            );

            resultadoComisionado.Operaciones.Add(
                new OperacionAplicacion
                {
                    Paso = paso,
                    Estado = "Planificado",
                    EmpresaId = producto.EmpresaId,
                    VentaId = producto.VentaId,
                    ProductoId = context.ProductoId,
                    Monto = decisionPago.MontoPagar,
                    Observacion = observacionVistaPrevia,
                    TipoPago = decisionPago.Modo,
                    TiempoPago = decisionPago.Tiempo
                }
            );

            return ResultadoAplicaciones<ResultadoEjecucionPagoAplicaciones>.Ok(
                new ResultadoEjecucionPagoAplicaciones
                {
                    SaldoRestante = Math.Max(montoDisponible - decisionPago.MontoPagar, 0),
                    ReciboId = 0,
                    FacturaId = 0,
                    Observacion = observacionVistaPrevia
                },
                "Pago simulado correctamente."
            );
        }

        var resultadoPago = await EjecutarPagoSionAsync(
            resultadoEmpresa.Datos,
            producto.VentaId,
            decisionPago.FechaPagoEfectiva,
            ConstruirNumeroTransaccionExterna(),
            decisionPago.MontoPagar
        );
        if (!resultadoPago.Exito || resultadoPago.Datos <= 0)
        {
            return ResultadoAplicaciones<ResultadoEjecucionPagoAplicaciones>.Fail(
                resultadoPago.Mensaje.Length > 0 ? resultadoPago.Mensaje : "No se pudo ejecutar el pago en Sion.",
                resultadoPago.EsFatal
            );
        }

        var clienteContable = await ResolverClienteContableAsync(context);
        if (!clienteContable.Exito || clienteContable.Datos is null)
        {
            return ResultadoAplicaciones<ResultadoEjecucionPagoAplicaciones>.Fail(
                clienteContable.Mensaje,
                clienteContable.EsFatal
            );
        }

        var observacion = ConstruirObservacionFinal(context.Observacion, decisionPago.SufijoObservacion);
        var registroPago = new RegistroPagoAplicaciones
        {
            Ciclo = context.Ciclo,
            EmpresaId = producto.EmpresaId,
            VentaId = producto.VentaId,
            ClienteId = clienteContable.Datos.ClienteId,
            DocumentoCliente = clienteContable.Datos.NumeroDocumento,
            ProductoId = context.ProductoId,
            Expensa = cuota.Expensa,
            Monto = decisionPago.MontoPagar,
            FechaCreacion = DateTime.Now,
            ReciboId = resultadoPago.Datos,
            FacturaId = -1,
            Observacion = observacion,
            TipoPagoId = context.TipoPagoId,
            BanderaIntercompania = context.BanderaIntercompania
        };

        var resultadoRegistroContable = await RegistrarReciboPagoAsync(registroPago);
        if (!resultadoRegistroContable.Exito || resultadoRegistroContable.Datos <= 0)
        {
            return ResultadoAplicaciones<ResultadoEjecucionPagoAplicaciones>.Fail(
                resultadoRegistroContable.Mensaje.Length > 0
                    ? resultadoRegistroContable.Mensaje
                    : "No se pudo registrar el recibo en AplicacionesPagos.",
                true
            );
        }

        registroPago.Id = resultadoRegistroContable.Datos;

        var facturaId = 0;
        var observacionFinal = observacion;
        var resultadoFactura = await GenerarFacturaAsync(
            resultadoEmpresa.Datos.EmpresaServicioWebId,
            producto.ProyectoId,
            producto.VentaId,
            resultadoPago.Datos,
            context.ProductoId
        );

        if (resultadoFactura.Exito && resultadoFactura.Datos is not null)
        {
            facturaId = resultadoFactura.Datos.EjecutadoCorrectamente ? resultadoFactura.Datos.FacturaId : -1;
            if (!resultadoFactura.Datos.EjecutadoCorrectamente && !string.IsNullOrWhiteSpace(resultadoFactura.Datos.MensajeError))
            {
                observacionFinal = $"{observacion} - Error En Facturacion= {resultadoFactura.Datos.MensajeError}";
            }
        }
        else
        {
            facturaId = -1;
            observacionFinal = $"{observacion} - Error En Facturacion= {resultadoFactura.Mensaje}";
        }

        var resultadoActualizacion = await ActualizarFacturaAsync(producto.EmpresaId, producto.VentaId, resultadoPago.Datos, facturaId, observacionFinal);
        if (!resultadoActualizacion.Exito)
        {
            return ResultadoAplicaciones<ResultadoEjecucionPagoAplicaciones>.Fail(resultadoActualizacion.Mensaje, resultadoActualizacion.EsFatal);
        }

        registroPago.FacturaId = facturaId;
        registroPago.Observacion = observacionFinal;
        estado.PagosSesion.Add(ClonarRegistroPago(registroPago));

        resultadoComisionado.Operaciones.Add(
            new OperacionAplicacion
            {
                Paso = paso,
                Estado = "Aplicado",
                EmpresaId = producto.EmpresaId,
                VentaId = producto.VentaId,
                ProductoId = context.ProductoId,
                Monto = decisionPago.MontoPagar,
                Observacion = observacionFinal,
                ReciboId = resultadoPago.Datos,
                FacturaId = facturaId,
                TipoPago = decisionPago.Modo,
                TiempoPago = decisionPago.Tiempo
            }
        );

        return ResultadoAplicaciones<ResultadoEjecucionPagoAplicaciones>.Ok(
            new ResultadoEjecucionPagoAplicaciones
            {
                SaldoRestante = Math.Max(montoDisponible - decisionPago.MontoPagar, 0),
                ReciboId = resultadoPago.Datos,
                FacturaId = facturaId,
                Observacion = observacionFinal
            },
            "Pago aplicado correctamente."
        );
    }

    private async Task<ResultadoAplicaciones<ClienteAplicaciones>> ResolverClienteContableAsync(
        ContextoEjecucionPagoAplicaciones context
    )
    {
        if (string.IsNullOrWhiteSpace(context.DocumentoComisionadoContable))
        {
            return ResultadoAplicaciones<ClienteAplicaciones>.Ok(
                new ClienteAplicaciones
                {
                    ClienteId = context.ClienteBeneficiarioId,
                    NumeroDocumento = context.DocumentoBeneficiario,
                    NombreCompleto = string.Empty
                }
            );
        }

        return await ObtenerClientePorDocumentoAsync(context.DocumentoComisionadoContable);
    }

    private static DecisionPagoAplicaciones DecidirPago(
        CuotaAplicaciones cuota,
        decimal montoDisponible,
        DateTime ahora
    )
    {
        var montoPagar = Math.Min(cuota.MontoPago, montoDisponible);
        var pagoCompleto = montoDisponible >= cuota.MontoPago;
        var fechaValor = PuedePagarAFechaValor(cuota.FechaVencimiento, ahora);

        return new DecisionPagoAplicaciones
        {
            MontoPagar = montoPagar,
            FechaPagoEfectiva = fechaValor ? cuota.FechaVencimiento : ahora,
            Modo = pagoCompleto ? "Completo" : "A Cuenta",
            Tiempo = fechaValor ? "Fecha Valor" : "Normal",
            SufijoObservacion = ConstruirSufijoObservacion(pagoCompleto, fechaValor)
        };
    }

    private static bool PuedePagarAFechaValor(DateTime fechaVencimiento, DateTime ahora)
    {
        var firstDayPreviousMonth = new DateTime(ahora.Year, ahora.Month, 1).AddMonths(-1);
        return fechaVencimiento.Date >= firstDayPreviousMonth.Date && fechaVencimiento.Date <= ahora.Date;
    }

    private static string ConstruirSufijoObservacion(bool pagoCompleto, bool fechaValor)
    {
        if (pagoCompleto && !fechaValor)
        {
            return string.Empty;
        }

        if (pagoCompleto && fechaValor)
        {
            return " A Fecha Valor";
        }

        if (!pagoCompleto && !fechaValor)
        {
            return " A Cuenta";
        }

        return " A Cuenta - A Fecha Valor";
    }

    private static string ConstruirObservacionFinal(string observacionBase, string sufijo)
    {
        return string.IsNullOrWhiteSpace(sufijo) ? observacionBase.Trim() : $"{observacionBase.Trim()} {sufijo.Trim()}".Trim();
    }

    private static List<ProductoCarteraAplicaciones> FiltrarProductosElegibles(
        IReadOnlyCollection<ProductoCarteraAplicaciones> productos,
        string logTransaccionId,
        string paso,
        string numeroDocumento
    )
    {
        var filtered = productos
            .Where(item => !(item.DeudaTotal == 0 && item.CuotasPendientes == 0))
            .ToList();

        return filtered
            .Where(item => item.EmpresaId != 17 && item.EmpresaId != 21)
            .ToList();
    }

    private static bool EsProximoMes(DateTime fechaVencimiento, DateTime ahora)
    {
        var next = ahora.AddMonths(1);
        return fechaVencimiento.Month == next.Month && fechaVencimiento.Year == next.Year;
    }

    private static IEnumerable<DefinicionDescuentoAplicaciones> OrdenarDescuentos(IEnumerable<DefinicionDescuentoAplicaciones> discounts)
    {
        var royal = discounts.Where(item => item.Descripcion.Contains("Royal", StringComparison.OrdinalIgnoreCase));
        var cards = discounts.Where(item => item.Descripcion.Contains("Tarjeta", StringComparison.OrdinalIgnoreCase));
        var others = discounts.Where(item =>
            !item.Descripcion.Contains("Royal", StringComparison.OrdinalIgnoreCase)
            && !item.Descripcion.Contains("Tarjeta", StringComparison.OrdinalIgnoreCase)
        );

        return royal.Concat(cards).Concat(others);
    }

    private static bool DebeOmitirCartaYaRegistrada(
        IReadOnlyCollection<RegistroPagoAplicaciones> pagosExistentes,
        string documentoComisionado,
        InstruccionCartaAplicaciones carta,
        decimal monto
    )
    {
        return pagosExistentes.Any(item =>
            item.EmpresaId == carta.EmpresaId
            && string.Equals(item.DocumentoCliente.Trim(), documentoComisionado.Trim(), StringComparison.OrdinalIgnoreCase)
            && item.VentaId == carta.VentaId
            && item.Monto == monto
            && item.Observacion.Contains("Carta", StringComparison.OrdinalIgnoreCase)
        );
    }

    private static RegistroPagoAplicaciones ClonarRegistroPago(RegistroPagoAplicaciones origen)
    {
        return new RegistroPagoAplicaciones
        {
            Id = origen.Id,
            Ciclo = origen.Ciclo,
            EmpresaId = origen.EmpresaId,
            VentaId = origen.VentaId,
            ClienteId = origen.ClienteId,
            DocumentoCliente = origen.DocumentoCliente,
            ProductoId = origen.ProductoId,
            Expensa = origen.Expensa,
            Monto = origen.Monto,
            FechaCreacion = origen.FechaCreacion,
            ReciboId = origen.ReciboId,
            FacturaId = origen.FacturaId,
            Observacion = origen.Observacion,
            TipoPagoId = origen.TipoPagoId,
            BanderaIntercompania = origen.BanderaIntercompania
        };
    }

    private static ComisionEmpresaAplicaciones ClonarComisionEmpresa(ComisionEmpresaAplicaciones origen)
    {
        return new ComisionEmpresaAplicaciones
        {
            Id = origen.Id,
            Ciclo = origen.Ciclo,
            NumeroDocumento = origen.NumeroDocumento,
            EmpresaId = origen.EmpresaId,
            EmpresaServicioWebId = origen.EmpresaServicioWebId,
            NombreBaseDatosEmpresa = origen.NombreBaseDatosEmpresa,
            VentasPersonales = origen.VentasPersonales,
            VentasGrupales = origen.VentasGrupales,
            Residual = origen.Residual,
            MontoComision = origen.MontoComision,
            MontoRetencion = origen.MontoRetencion,
            MontoNeto = origen.MontoNeto,
            MontoBruto = origen.MontoBruto,
            MontoTrecePorCiento = origen.MontoTrecePorCiento,
            RequiereFactura = origen.RequiereFactura
        };
    }

    private static string ConstruirNumeroTransaccionExterna()
    {
        return DateTime.Now.ToString("HHmmssffffff");
    }

    private static RespuestaVistaPreviaAplicaciones ConstruirRespuestaVistaPrevia(EstadoProcesoAplicaciones estado)
    {
        return new RespuestaVistaPreviaAplicaciones
        {
            LCicloId = estado.Ciclo,
            VistaPrevia = true,
            AplicacionesComisionadoExiste = estado.AplicacionesComisionadoExiste,
            ExistenComisionesPorEmpresa = estado.ExistenComisionesPorEmpresa,
            RequiereRegistrarComisionados = estado.RequiereRegistrarComisionados,
            ErrorGrave = estado.ErrorGrave,
            ErrorGraveMensaje = estado.ErrorGraveMensaje,
            TotalComisionadosGuardian = estado.TotalComisionadosGuardian,
            TotalPendientes = estado.TotalPendientes,
            TotalPendienteAplicar = estado.TotalPendienteAplicar,
            Notas = estado.Notas,
            Comisionados = estado.Comisionados
        };
    }

    private static RespuestaEjecucionAplicaciones ConstruirRespuestaEjecucion(EstadoProcesoAplicaciones estado)
    {
        return new RespuestaEjecucionAplicaciones
        {
            LCicloId = estado.Ciclo,
            VistaPrevia = false,
            AplicacionesComisionadoExiste = estado.AplicacionesComisionadoExiste,
            ExistenComisionesPorEmpresa = estado.ExistenComisionesPorEmpresa,
            RequiereRegistrarComisionados = estado.RequiereRegistrarComisionados,
            ErrorGrave = estado.ErrorGrave,
            ErrorGraveMensaje = estado.ErrorGraveMensaje,
            TotalComisionadosGuardian = estado.TotalComisionadosGuardian,
            TotalPendientes = estado.TotalPendientes,
            TotalPendienteAplicar = estado.TotalPendienteAplicar,
            TotalProcesados = estado.TotalProcesados,
            TotalErrores = estado.TotalErrores,
            Notas = estado.Notas,
            Comisionados = estado.Comisionados
        };
    }

    private static ResultadoAplicaciones<EstadoProcesoAplicaciones> ConstruirEstadoFallido(
        EstadoProcesoAplicaciones estado,
        string mensaje,
        bool esFatal
    )
    {
        estado.ErrorGrave = esFatal;
        estado.ErrorGraveMensaje = mensaje;
        return new ResultadoAplicaciones<EstadoProcesoAplicaciones>
        {
            Exito = false,
            EsFatal = esFatal,
            Mensaje = mensaje,
            Datos = estado
        };
    }
}

internal sealed class EstadoProcesoAplicaciones
{
    public EstadoProcesoAplicaciones(int ciclo, bool soloVistaPrevia)
    {
        Ciclo = ciclo;
        SoloVistaPrevia = soloVistaPrevia;
    }

    public int Ciclo { get; }
    public bool SoloVistaPrevia { get; }
    public bool AplicacionesComisionadoExiste { get; set; }
    public bool ExistenComisionesPorEmpresa { get; set; }
    public bool RequiereRegistrarComisionados { get; set; }
    public bool ErrorGrave { get; set; }
    public string ErrorGraveMensaje { get; set; } = string.Empty;
    public int TotalComisionadosGuardian { get; set; }
    public int TotalPendientes { get; set; }
    public decimal TotalPendienteAplicar { get; set; }
    public int TotalProcesados { get; set; }
    public int TotalErrores { get; set; }
    public List<string> Notas { get; set; } = new();
    public List<ResultadoComisionadoAplicaciones> Comisionados { get; set; } = new();
    public List<RegistroPagoAplicaciones> PagosSesion { get; set; } = new();
}

internal sealed class CandidatoPagoAplicaciones
{
    public CandidatoPagoAplicaciones(ProductoCarteraAplicaciones producto, CuotaAplicaciones cuota)
    {
        Producto = producto;
        Cuota = cuota;
    }

    public ProductoCarteraAplicaciones Producto { get; }
    public CuotaAplicaciones Cuota { get; }
}

internal sealed class DecisionPagoAplicaciones
{
    public decimal MontoPagar { get; set; }
    public DateTime FechaPagoEfectiva { get; set; }
    public string Modo { get; set; } = string.Empty;
    public string Tiempo { get; set; } = string.Empty;
    public string SufijoObservacion { get; set; } = string.Empty;
}

internal sealed class ContextoEjecucionPagoAplicaciones
{
    public int Ciclo { get; set; }
    public int ClienteBeneficiarioId { get; set; }
    public string DocumentoBeneficiario { get; set; } = string.Empty;
    public string ProductoId { get; set; } = string.Empty;
    public string DocumentoComisionadoContable { get; set; } = string.Empty;
    public string Observacion { get; set; } = string.Empty;
    public int TipoPagoId { get; set; }
    public int BanderaIntercompania { get; set; }
}

internal sealed class ResultadoEjecucionPagoAplicaciones
{
    public decimal SaldoRestante { get; set; }
    public int ReciboId { get; set; }
    public int FacturaId { get; set; }
    public string Observacion { get; set; } = string.Empty;
}

internal class ResultadoAplicaciones
{
    public bool Exito { get; init; }
    public bool EsFatal { get; init; }
    public string Mensaje { get; init; } = string.Empty;

    public static ResultadoAplicaciones Ok(string mensaje = "")
    {
        return new ResultadoAplicaciones
        {
            Exito = true,
            Mensaje = mensaje
        };
    }

    public static ResultadoAplicaciones Fail(string mensaje, bool esFatal = false)
    {
        return new ResultadoAplicaciones
        {
            Exito = false,
            EsFatal = esFatal,
            Mensaje = mensaje
        };
    }
}

internal class ResultadoAplicaciones<T> : ResultadoAplicaciones
{
    public T? Datos { get; init; }

    public static ResultadoAplicaciones<T> Ok(T data, string mensaje = "")
    {
        return new ResultadoAplicaciones<T>
        {
            Exito = true,
            Datos = data,
            Mensaje = mensaje
        };
    }

    public static new ResultadoAplicaciones<T> Fail(string mensaje, bool esFatal = false)
    {
        return new ResultadoAplicaciones<T>
        {
            Exito = false,
            EsFatal = esFatal,
            Mensaje = mensaje
        };
    }
}
