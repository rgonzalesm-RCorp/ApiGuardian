using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Microsoft.AspNetCore.DataProtection.Repositories;
using DocumentFormat.OpenXml.Wordprocessing;
using Org.BouncyCastle.Ocsp;
using DocumentFormat.OpenXml.Office2019.Excel.RichData2;
using DocumentFormat.OpenXml.Drawing.Charts;
using ApiGuardian.Infrastructure.Services;
using DocumentFormat.OpenXml.Bibliography;
namespace CleanDapperApi.Api.Controllers;
[ApiController]
[Route("api/[controller]")]
public class BonoResidualController : ControllerBase
{
    private readonly ILogService _log;
    private readonly IBonoResidualRepository _bonoResidualRepository;
    private readonly IVentasCnxRepository _ventasCnxRepository;
    private readonly IBrConfiguracionRepository _brConfiguracionRepository;
    private readonly IAdministracionBonoResidualRepository _adminBonoResidualRepository;
    private readonly IAdministracionHabilitacionComisionRepository _habilitacionRepository;
    private readonly IAdministracionContratoRepository _administracionContratoRepository;
    private readonly IControlProcesoRepository _controlProcesoRepository;
    private readonly IBonoParRepository _bonoParRepository;
    private readonly IAdministracionComplejoRepository _administracionComplejoRepository;
    private readonly string NOMBREARCHIVO = "BonoResidualController.cs";
    public BonoResidualController(ILogService log
        , IBonoResidualRepository bonoResidualRepository
        , IVentasCnxRepository ventasCnxRepository
        , IBrConfiguracionRepository brConfiguracionRepository
        , IAdministracionBonoResidualRepository administracionBonoResidualRepository
        , IAdministracionHabilitacionComisionRepository habilitacionRepository
        , IAdministracionContratoRepository administracionContratoRepository
        , IControlProcesoRepository controlProcesoRepository
        , IBonoParRepository bonoParRepository
        , IAdministracionComplejoRepository administracionComplejoRepository
        )
    {
        _bonoResidualRepository = bonoResidualRepository;
        _ventasCnxRepository = ventasCnxRepository;
        _brConfiguracionRepository = brConfiguracionRepository;
        _adminBonoResidualRepository = administracionBonoResidualRepository;
        _habilitacionRepository = habilitacionRepository;
        _administracionContratoRepository = administracionContratoRepository;
        _controlProcesoRepository = controlProcesoRepository;
        _bonoParRepository = bonoParRepository;
        _administracionComplejoRepository = administracionComplejoRepository;
        _log = log;
    }
    [HttpGet("get/cartera")]
    public async Task<IActionResult> GetCartera([FromHeader(Name = "Usuario")] string Usuario, [FromHeader(Name = "LCicloId")] int LCicloId)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreMetodo = "GetCartera()";
        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreMetodo, "Inicio de metodo");
            var responseCarteraAll = await _bonoResidualRepository.GetCarteraAll(logTransaccionId.ToString(), Usuario);


            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreMetodo, $"Fin de metodo.");

            var resumenCartera = responseCarteraAll.ListaCartera.GroupBy(x => new {x.Estado})
            .Select(g => new
            {
                g.Key.Estado,
                Cantidad = g.Count()
            })
            .ToList();
            CarteraXls _carteraXls = new CarteraXls();
            var xlsCuota = await _carteraXls.GetCarteraXlS(responseCarteraAll.ListaCartera.ToList());
            var responseSiguientePaso = await _controlProcesoRepository.GetSiguientePaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, LCicloId);


            return Ok(new
            {
                status = responseCarteraAll.Success ? true : false,
                mensaje = responseCarteraAll.Mensaje,
                data = new {
                    resumenCartera,
                    base64 = xlsCuota.base64,
                    fileNameXls = $"Reporte de la cartera de clientes",
                    controlPasos = new {
                                        ejecutado = PasosDiccionario.OBTENER_CARTERA == responseSiguientePaso.Data.nombre ? false : true,
                                        data = responseSiguientePaso.Data
                                    }
                }
            });
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId.ToString(), NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return Ok(new
            {
                status = false,
                mensaje = ex.Message,
                data = ""
            });
        }
        
    }
    [HttpPost("save/cartera")]
    public async Task<IActionResult> GuardarCartera([FromHeader(Name = "Usuario")] string Usuario, [FromHeader(Name = "LCicloId")] int LCicloId)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "GuardarCartera()";
        bool pasoIniciado = false;
        try
        {
            DateTime ini = DateTime.Now;
            var responseSiguientePaso = await _controlProcesoRepository.GetSiguientePaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, LCicloId);
            if (PasosDiccionario.OBTENER_CARTERA != responseSiguientePaso.Data.nombre)
            {
                return Ok(new
                {
                    status = false,
                    mensaje = "Esta paso ya se encuentra ejecutado para este ciclo, si quieres volver a a procesar debes reinicar el proceso para el ciclo",
                    data = ""
                });
            }

            var responseInicioPaso = await _controlProcesoRepository.IniciarPaso(
                logTransaccionId.ToString(),
                Usuario,
                ProcesosDiccionario.COMISIONES,
                LCicloId,
                PasosDiccionario.OBTENER_CARTERA
            );

            if (!responseInicioPaso.Success || !(responseInicioPaso.Data?.status ?? false))
            {
                return Ok(new
                {
                    status = false,
                    mensaje = responseInicioPaso.Data?.mensaje ?? responseInicioPaso.Mensaje,
                    data = ""
                });
            }

            pasoIniciado = true;

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, "Inicio de metodo");
            var responseCartera = await _bonoResidualRepository.GetCarteraAll(logTransaccionId.ToString(), Usuario);

            if (!responseCartera.Success)
            {
                await _controlProcesoRepository.CancelarPaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, LCicloId, PasosDiccionario.OBTENER_CARTERA);
                pasoIniciado = false;

                return Ok(new
                {
                    status = false,
                    mensaje = responseCartera.Mensaje,
                    data = ""
                });
            }

            var responseGuardarCartera = await GuardarCarteraGrl(logTransaccionId.ToString(), Usuario, responseCartera.ListaCartera.ToList(), nombreArchivo);

            if (!responseGuardarCartera.Success)
            {
                await _controlProcesoRepository.CancelarPaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, LCicloId, PasosDiccionario.OBTENER_CARTERA);
                pasoIniciado = false;

                return Ok(new
                {
                    status = false,
                    mensaje = responseGuardarCartera.Mensaje,
                    data = ""
                });
            }

            var responseFinPaso = await _controlProcesoRepository.FinalizarPaso(
                logTransaccionId.ToString(),
                Usuario,
                ProcesosDiccionario.COMISIONES,
                LCicloId,
                PasosDiccionario.OBTENER_CARTERA
            );

            if (!responseFinPaso.Success || !(responseFinPaso.Data?.status ?? false))
            {
                return Ok(new
                {
                    status = false,
                    mensaje = responseFinPaso.Data?.mensaje ?? responseFinPaso.Mensaje,
                    data = ""
                });
            }

            pasoIniciado = false;
            DateTime fin = DateTime.Now;

            return Ok(new
            {
                status = true ,
                mensaje = "Se esta guardando la cartera en segundo plano.",
                data = new
                {
                    ini
                    , fin
                }
            });
        }
        catch (Exception ex)
        {
            if (pasoIniciado)
            {
                await _controlProcesoRepository.CancelarPaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, LCicloId, PasosDiccionario.OBTENER_CARTERA);
            }

            _log.Error(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, "Fin de metodo", ex);
            return Ok(new
            {
                status = false,
                mensaje = ex.Message,
                data = ""
            });
        }
        
    }
    private async Task<(bool Success, string Mensaje)> GuardarCarteraGrl(string logTransaccionId, string Usuario, List<TCartera> Cartera, string nombreArchivo)
    {
        var responseSaveCartera = await _bonoResidualRepository.GuardarCartera(logTransaccionId.ToString(), Usuario, Cartera);

        _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Fin de metodo.");
        return (responseSaveCartera.Success, responseSaveCartera.Mensaje);
    }
    [HttpGet("get/cuota")]
    public async Task<IActionResult> GetCuota([FromHeader(Name = "Usuario")] string Usuario, [FromHeader(Name = "Inicio")] string inicio, [FromHeader(Name = "Fin")] string fin, [FromHeader(Name = "LCicloId")] int LCicloId)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "GetCuota()";
        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Inicio de metodo [usuario: {Usuario} inicio: {inicio} fin: {fin}]");
            var responseCuota = await _bonoResidualRepository.GetCuota(logTransaccionId.ToString(), Usuario, inicio, fin);
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Fin de metodo.");
            if (!responseCuota.Success)
            {
                return Ok(new
                {
                    status = false,
                    mensaje = responseCuota.Mensaje,
                    data = ""
                });
            }
            var resumen = responseCuota.ListaCuota.GroupBy(x => new {x.Idtipopago, x.Descripcion})
            .Select(g => new
            {
                g.Key.Idtipopago,
                g.Key.Descripcion,
                TotalPago = g.Sum(x => x.Totalpago),
                Cantidad = g.Count()
            })
            .ToList();

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, "PDF generado correctamente.");
            CuotaXls _cuotaXls = new CuotaXls();
            var xlsCuota = await _cuotaXls.GetCuotaXlS(responseCuota.ListaCuota.ToList());
            var responseSiguientePaso = await _controlProcesoRepository.GetSiguientePaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, LCicloId);


            return Ok(new
            {
                status = responseCuota.Success ? true : false,
                mensaje = responseCuota.Mensaje,
                data = new {
                    resumen,
                    xlsCuota.base64,
                    fileNameXls = $"Reporte de cuotas del {inicio} al {fin}",
                    controlPasos = new {
                                        ejecutado = PasosDiccionario.OBTENER_CUOTAS == responseSiguientePaso.Data.nombre ? false : true,
                                        data = responseSiguientePaso.Data
                                    }
                }
            });
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, "Fin de metodo", ex);
            return Ok(new
            {
                status = false,
                mensaje = ex.Message,
                data = ""
            });
        }
    }
    [HttpPost("save/cuota")]
    public async Task<IActionResult> GuardarCuota([FromHeader(Name = "Usuario")] string Usuario, [FromHeader(Name = "Inicio")] string inicio, [FromHeader(Name = "Fin")] string fin, [FromHeader(Name = "LCicloId")] int LCicloId)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "GetCuota()";
        bool pasoIniciado = false;
        try
        {
            DateTime ini = DateTime.Now;
            var responseSiguientePaso = await _controlProcesoRepository.GetSiguientePaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, LCicloId);
            if (PasosDiccionario.OBTENER_CUOTAS != responseSiguientePaso.Data.nombre)
            {
                return Ok(new
                {
                    status = false,
                    mensaje = "Esta paso ya se encuentra ejecutado para este ciclo, si quieres volver a a procesar debes reinicar el proceso para el ciclo",
                    data = ""
                });
            }

            var responseInicioPaso = await _controlProcesoRepository.IniciarPaso(
                logTransaccionId.ToString(),
                Usuario,
                ProcesosDiccionario.COMISIONES,
                LCicloId,
                PasosDiccionario.OBTENER_CUOTAS
            );

            if (!responseInicioPaso.Success || !(responseInicioPaso.Data?.status ?? false))
            {
                return Ok(new
                {
                    status = false,
                    mensaje = responseInicioPaso.Data?.mensaje ?? responseInicioPaso.Mensaje,
                    data = ""
                });
            }

            pasoIniciado = true;

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Inicio de metodo [usuario: {Usuario} inicio: {inicio} fin: {fin}]");
            var responseCuota = await _bonoResidualRepository.GetCuota(logTransaccionId.ToString(), Usuario, inicio, fin);
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Fin de metodo.");
            if (!responseCuota.Success)
            {
                await _controlProcesoRepository.CancelarPaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, LCicloId, PasosDiccionario.OBTENER_CUOTAS);
                pasoIniciado = false;

                return Ok(new
                {
                    status = false,
                    mensaje = responseCuota.Mensaje,
                    data = ""
                });
            }
            List<TCuota> ListaCuota = responseCuota.ListaCuota.ToList();
            var responseGuardarCuota = await GuardarCuotaGrl(logTransaccionId.ToString(), Usuario, responseCuota.ListaCuota.ToList(), nombreArchivo);

            if (!responseGuardarCuota.Success)
            {
                await _controlProcesoRepository.CancelarPaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, LCicloId, PasosDiccionario.OBTENER_CUOTAS);
                pasoIniciado = false;

                return Ok(new
                {
                    status = false,
                    mensaje = responseGuardarCuota.Mensaje,
                    data = ""
                });
            }

            var responseFinPaso = await _controlProcesoRepository.FinalizarPaso(
                logTransaccionId.ToString(),
                Usuario,
                ProcesosDiccionario.COMISIONES,
                LCicloId,
                PasosDiccionario.OBTENER_CUOTAS
            );

            if (!responseFinPaso.Success || !(responseFinPaso.Data?.status ?? false))
            {
                return Ok(new
                {
                    status = false,
                    mensaje = responseFinPaso.Data?.mensaje ?? responseFinPaso.Mensaje,
                    data = ""
                });
            }

            pasoIniciado = false;
            DateTime fins = DateTime.Now;

            return Ok(new
            {
                status = true,
                mensaje = "Se estan guardando las cuotas en segundo plano.",
                data = new
                {
                    ini, fins
                }
            });
        }
        catch (Exception ex)
        {
            if (pasoIniciado)
            {
                await _controlProcesoRepository.CancelarPaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, LCicloId, PasosDiccionario.OBTENER_CUOTAS);
            }

            _log.Error(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, "Fin de metodo", ex);
            return Ok(new
            {
                status = false,
                mensaje = ex.Message,
                data = ""
            });
        }
    }
    private async Task<(bool Success, string Mensaje)> GuardarCuotaGrl(string logTransaccionId, string Usuario, List<TCuota> Cuota, string nombreArchivo)
    {
        var responseSaveCuota = await _bonoResidualRepository.GuardarCuota(logTransaccionId.ToString(), Usuario, Cuota);
        _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Fin de metodo.");
        return (responseSaveCuota.Success, responseSaveCuota.Mensaje);
    }
    [HttpGet("get/excedente")]
    public async Task<IActionResult> GetExcedente([FromHeader(Name = "Usuario")] string Usuario, [FromHeader(Name = "Inicio")] string inicio, [FromHeader(Name = "Fin")] string fin, [FromHeader(Name = "LCicloId")] int LCicloId)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "GetExcedente()";
        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Inicio de metodo [usuario: {Usuario} inicio: {inicio} fin: {fin}]");
            var responseVentaCnx = await _ventasCnxRepository.GetVentaCnx(logTransaccionId.ToString(), inicio, fin);
            List<ItemVentaCnx> dataVentasCnx = responseVentaCnx.Data.ToList();
            List<ItemVentaCnx> listaFiltrada = dataVentasCnx.Where(x => (x.SCuotaInicialOriginal - x.ValorCi) > Convert.ToDecimal(0.05) && !x.Glosa.Contains("UPGRADE")).ToList();


            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Fin de metodo.");
            if (!responseVentaCnx.Success)
            {
                return Ok(new
                {
                    status = false,
                    mensaje = responseVentaCnx.Mensaje,
                    data = ""
                });
            }
            var responseSiguientePaso = await _controlProcesoRepository.GetSiguientePaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, LCicloId);


            return Ok(new
            {
                status = true,
                mensaje = "Se estan guardando las cuotas en segundo plano.",
                data = new{
                    listaExcedente = listaFiltrada,
                    controlPasos = new {
                                        ejecutado = PasosDiccionario.OBTENER_EXCEDENTE == responseSiguientePaso.Data.nombre ? false : true,
                                        data = responseSiguientePaso.Data
                                    }
                }
            });
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, "Fin de metodo", ex);
            return Ok(new
            {
                status = false,
                mensaje = ex.Message,
                data = ""
            });
        }
    }
    [HttpPost("save/excedente")]
    public async Task<IActionResult> GuardarExcedente([FromHeader(Name = "Usuario")] string Usuario, [FromHeader(Name = "Inicio")] string inicio, [FromHeader(Name = "Fin")] string fin, [FromHeader(Name = "LCicloId")] int LCicloId)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "GuardarExcedente()";
        bool pasoIniciado = false;
        try
        {
            var responseSiguientePaso = await _controlProcesoRepository.GetSiguientePaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, LCicloId);
            if (PasosDiccionario.OBTENER_EXCEDENTE != responseSiguientePaso.Data.nombre)
            {
                return Ok(new
                {
                    status = false,
                    mensaje = "Esta paso ya se encuentra ejecutado para este ciclo, si quieres volver a a procesar debes reinicar el proceso para el ciclo",
                    data = ""
                });
            }

            var responseInicioPaso = await _controlProcesoRepository.IniciarPaso(
                logTransaccionId.ToString(),
                Usuario,
                ProcesosDiccionario.COMISIONES,
                LCicloId,
                PasosDiccionario.OBTENER_EXCEDENTE
            );

            if (!responseInicioPaso.Success || !(responseInicioPaso.Data?.status ?? false))
            {
                return Ok(new
                {
                    status = false,
                    mensaje = responseInicioPaso.Data?.mensaje ?? responseInicioPaso.Mensaje,
                    data = ""
                });
            }

            pasoIniciado = true;

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Inicio de metodo [usuario: {Usuario} inicio: {inicio} fin: {fin}]");
            var responseVentaCnx = await _ventasCnxRepository.GetVentaCnx(logTransaccionId.ToString(), inicio, fin);

            if (!responseVentaCnx.Success)
            {
                await _controlProcesoRepository.CancelarPaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, LCicloId, PasosDiccionario.OBTENER_EXCEDENTE);
                pasoIniciado = false;

                return Ok(new
                {
                    status = false,
                    mensaje = responseVentaCnx.Mensaje,
                    data = ""
                });
            }

            List<ItemVentaCnx> dataVentasCnx = responseVentaCnx.Data.ToList();
            List<ItemVentaCnx> listaFiltrada = dataVentasCnx.Where(x => (x.SCuotaInicialOriginal - x.ValorCi) > Convert.ToDecimal(0.05) && !x.Glosa.Contains("UPGRADE")).ToList();

            List<TCuota> ListaExcedente = new List<TCuota>();
            foreach (var item in listaFiltrada)
            {
                TCuota row = new TCuota
                {
                    Idproducto = item.Lote,
                    Idproyecto = item.LComplejoId,
                    LComplejoId = item.LComplejoId,
                    Proyecto =  item.Complejo ?? "",
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
                    Amortizacion = 0,
                    Capital = 0,
                    Interes = 0,
                    Seguro = 0,
                    Expensa = 0,
                    Multa = 0,
                    Fecha_Venta = item.DFecha,
                    Fecha_Pago = DateTime.Now,
                    Acuenta = 0,
                    Totalpago = 0,
                    Montodeuda = 0,
                    Pagosacuenta = 0,
                    Nrocuota = 0,
                    Empresa = item.Empresa ?? "",

                };
                ListaExcedente.Add(row);
            }

            var responseGuardarExcedente = await _bonoResidualRepository.GuardarCuota(logTransaccionId.ToString(), Usuario, ListaExcedente, true);

            if (!responseGuardarExcedente.Success)
            {
                await _controlProcesoRepository.CancelarPaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, LCicloId, PasosDiccionario.OBTENER_EXCEDENTE);
                pasoIniciado = false;

                return Ok(new
                {
                    status = false,
                    mensaje = responseGuardarExcedente.Mensaje,
                    data = ""
                });
            }

            var responseFinPaso = await _controlProcesoRepository.FinalizarPaso(
                logTransaccionId.ToString(),
                Usuario,
                ProcesosDiccionario.COMISIONES,
                LCicloId,
                PasosDiccionario.OBTENER_EXCEDENTE
            );

            if (!responseFinPaso.Success || !(responseFinPaso.Data?.status ?? false))
            {
                return Ok(new
                {
                    status = false,
                    mensaje = responseFinPaso.Data?.mensaje ?? responseFinPaso.Mensaje,
                    data = ""
                });
            }

            pasoIniciado = false;

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Fin de metodo.");

            return Ok(new
            {
                status = true,
                mensaje = "Se estan guardando los excedentes en segundo plano.",
                data = new{
                    listaExcedente = listaFiltrada
                }
            });
        }
        catch (Exception ex)
        {
            if (pasoIniciado)
            {
                await _controlProcesoRepository.CancelarPaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, LCicloId, PasosDiccionario.OBTENER_EXCEDENTE);
            }

            _log.Error(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, "Fin de metodo", ex);
            return Ok(new
            {
                status = false,
                mensaje = ex.Message,
                data = ""
            });
        }
    }
    [HttpGet("get/calculo/residual")]
    public async Task<IActionResult> GetBonoResidual([FromHeader(Name = "Usuario")] string Usuario, [FromHeader(Name = "LCicloId")] int LCicloId)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreMetodo = "GetBonoResidual()";
        try
        {
            var responserGetBonoResidual = await _bonoResidualRepository.GetDataCalculoBonoResidual(logTransaccionId.ToString(), Usuario, LCicloId);
            var responseHabilitaciones = await _habilitacionRepository.GetHabilitaciones(logTransaccionId.ToString(), Usuario, LCicloId);
            var responseConfiguracionBr = await _brConfiguracionRepository.GetConfiguracion(logTransaccionId.ToString(), Usuario);
            var responseCartera = await _bonoResidualRepository.GetCarteraGRD(logTransaccionId.ToString(), Usuario);

            if (!responserGetBonoResidual.Success || !responseHabilitaciones.Success || !responseConfiguracionBr.Success || !responseCartera.Success)
            {
                string mensajeError = !responserGetBonoResidual.Success
                    ? responserGetBonoResidual.Mensaje
                    : !responseHabilitaciones.Success
                        ? responseHabilitaciones.Mensaje
                        : !responseConfiguracionBr.Success
                            ? responseConfiguracionBr.Mensaje
                            : responseCartera.Mensaje;

                return Ok(new
                {
                    status = false,
                    mensaje = mensajeError,
                    data = ""
                });
            }

            List<DetailsBrConfiguracion> configuracions = responseConfiguracionBr.Data.Where(x => x.LCicloId == LCicloId).ToList();
            List<BrCalculoItem> listadoResidual = new List<BrCalculoItem>(); 

            var contactosDict = responserGetBonoResidual.ListaContacto.ToDictionary(x => x.LContactoId, x => x); 
            var personasHabilitadas = responseHabilitaciones.Data.ToList();
            var contactosBloqueados = HabilitacionComisionHelper.GetContactosBloqueadosParaComision(personasHabilitadas);
            var habilitadosSet = HabilitacionComisionHelper.GetContactosHabilitadosQueGeneranComision(personasHabilitadas);

            var activosSet = responserGetBonoResidual.ListaContactosActivos
                .Select(x => x.LContactoId)
                .Where(id => !contactosBloqueados.Contains(id))
                .Concat(habilitadosSet)
                .ToHashSet();

            var configDictTerreno = configuracions.Where(x => x.TipoProductoId == 1).ToDictionary(x => x.Nivel, x => x);
            var configDictMembresia = configuracions.Where(x => x.TipoProductoId == 2).ToDictionary(x => x.Nivel, x => x);
            var carteraList = responseCartera.ListaCartera
                .Where(c => string.Equals(c.Estado?.Trim(), "VENCIDO", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(c.DocId))
                .Select(c => c.DocId.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var item in responserGetBonoResidual.ListaCuotaRed)
            {
                var type = item.GetType();

                for (int i = 1; i <= 7; i++)
                {
                    var prop = type.GetProperty($"LPatrocinado{i}");
                    int patrocinadoId = 0;

                    if (prop != null)
                    {
                        var value = prop.GetValue(item);
                        patrocinadoId = value == null ? 0 : Convert.ToInt32(value);
                    }

                    contactosDict.TryGetValue(patrocinadoId, out var contacto);
                    configDictTerreno.TryGetValue(i, out var objConfigTerreno);
                    configDictMembresia.TryGetValue(i, out var objConfigMembresia);

                    var porcentaje = item.Empresa != "ADVEL" ? objConfigTerreno?.PorcentajeComision : objConfigMembresia?.PorcentajeComision ?? 0;
                    var documentoContacto = contacto?.SCedulaIdentidad?.Trim();

                    BrCalculoItem rows = new BrCalculoItem
                    {
                        LContactoId = patrocinadoId,
                        NombreCompleto = contacto?.SNombreCompleto ?? "",
                        Documento = contacto?.SCedulaIdentidad ?? "",
                        LContactoIdHijo = 0,
                        NombreCompletoHijo = item.Cliente,
                        DocumentoHijo = item.DocumentoCliente,
                        Nivel = i,
                        Bono = item.Bono,
                        BonoResidual = item.Bono * (decimal)porcentaje / 100,
                        ActivoMes = activosSet.Contains(patrocinadoId),
                        PorcentajeComision = (decimal)porcentaje,
                        LComplejoId = item.ProyectoId,
                        Complejo = item.Proyecto,
                        Empresa = item.Empresa,
                        ProductoId = item.ProductoId,
                        EstaAlDia = string.IsNullOrWhiteSpace(documentoContacto) || !carteraList.Contains(documentoContacto)
                    };

                    listadoResidual.Add(rows);
                }
            }

            listadoResidual = listadoResidual.Where(x => x.ActivoMes && x.EstaAlDia).ToList();
            //List<BrCalculoItem> estadoAldiaList = listadoResidual.Where(x => x.EstaAlDia).ToList();
            //List<BrCalculoItem> estadoVencidoList = listadoResidual.Where(x => !x.EstaAlDia).ToList();
            var ListadoResumenPorEmpresa = listadoResidual.GroupBy(x => new {x.Empresa })
            .Select(g => new
            {
                g.Key.Empresa,
                TotalPago = g.Sum(x => x.Bono),
                TotalResidual = g.Sum(x => x.BonoResidual),
                listadoProyecto = listadoResidual.Where(d => d.Empresa == g.Key.Empresa).GroupBy(x => new {x.Complejo})
                                    .Select(g => new
                                    {
                                        g.Key.Complejo,
                                        TotalPago = g.Sum(x => x.Bono),
                                        TotalResidual = g.Sum(x => x.BonoResidual)
                                    })
                                    .ToList()
            })
            .ToList();

            ComisionResidualXls _ins = new ComisionResidualXls();
            var reponseXLS = await _ins.GetComisionResidualXls (listadoResidual);
            var responseSiguientePaso = await _controlProcesoRepository.GetSiguientePaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, LCicloId);

            return Ok(new
            {
                status = responserGetBonoResidual.Success,
                mensaje = responserGetBonoResidual.Mensaje,
                data =new
                {
                    ListadoResumenPorEmpresa,
                    counter = listadoResidual.Count,
                    personasHabilitadas,
                    base64 = reponseXLS.base64,
                    controlPasos = new {
                                        ejecutado = PasosDiccionario.COMISION_RESIDUAL == responseSiguientePaso.Data.nombre ? false : true,
                                        data = responseSiguientePaso.Data
                                    }
                }
            });
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId.ToString(), NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return Ok(new
            {
                status = false,
                mensaje = ex.Message,
                data = ""
            });
        }
        
    }
    [HttpPost("save/calculo/residual")]
    public async Task<IActionResult> GuardarBonoResidual([FromHeader(Name = "Usuario")] string Usuario, [FromHeader(Name = "LCicloId")] int LCicloId)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        DateTime inicio = DateTime.Now;
        string nombreMetodo = "GetBonoResidual()";
        bool pasoIniciado = false;
        try
        {
            var responseSiguientePaso = await _controlProcesoRepository.GetSiguientePaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, LCicloId);
            if (PasosDiccionario.COMISION_RESIDUAL != responseSiguientePaso.Data.nombre)
            {
                return Ok(new
                {
                    status = false,
                    mensaje = "Esta paso ya se encuentra ejecutado para este ciclo, si quieres volver a a procesar debes reinicar el proceso para el ciclo",
                    data = ""
                });
            }

            var responseInicioPaso = await _controlProcesoRepository.IniciarPaso(
                logTransaccionId.ToString(),
                Usuario,
                ProcesosDiccionario.COMISIONES,
                LCicloId,
                PasosDiccionario.COMISION_RESIDUAL
            );

            if (!responseInicioPaso.Success || !(responseInicioPaso.Data?.status ?? false))
            {
                return Ok(new
                {
                    status = false,
                    mensaje = responseInicioPaso.Data?.mensaje ?? responseInicioPaso.Mensaje,
                    data = ""
                });
            }

            pasoIniciado = true;

            var responserGetBonoResidual = await _bonoResidualRepository.GetDataCalculoBonoResidual(logTransaccionId.ToString(), Usuario, LCicloId);
            var responseHabilitaciones = await _habilitacionRepository.GetHabilitaciones(logTransaccionId.ToString(), Usuario, LCicloId);
            var responseConfiguracionBr = await _brConfiguracionRepository.GetConfiguracion(logTransaccionId.ToString(), Usuario);

            if (!responserGetBonoResidual.Success || !responseConfiguracionBr.Success || !responseHabilitaciones.Success)
            {
                await _controlProcesoRepository.CancelarPaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, LCicloId, PasosDiccionario.COMISION_RESIDUAL);
                pasoIniciado = false;

                string mensajeError = !responserGetBonoResidual.Success
                    ? responserGetBonoResidual.Mensaje
                    : !responseConfiguracionBr.Success
                        ? responseConfiguracionBr.Mensaje
                        : responseHabilitaciones.Mensaje;

                return Ok(new
                {
                    status = false,
                    mensaje = mensajeError,
                    data = ""
                });
            }

            List<DetailsBrConfiguracion> configuracions = responseConfiguracionBr.Data.Where(x => x.LCicloId == LCicloId).ToList();
            List<BrCalculoItem> listadoResidual = new List<BrCalculoItem>();

            var contactosDict = responserGetBonoResidual.ListaContacto.ToDictionary(x => x.LContactoId, x => x);
            var contactosBloqueados = HabilitacionComisionHelper.GetContactosBloqueadosParaComision(responseHabilitaciones.Data);
            var habilitadosSet = HabilitacionComisionHelper.GetContactosHabilitadosQueGeneranComision(responseHabilitaciones.Data);

            var activosSet = responserGetBonoResidual.ListaContactosActivos
                .Select(x => x.LContactoId)
                .Where(id => !contactosBloqueados.Contains(id))
                .Concat(habilitadosSet)
                .ToHashSet();

            var configDict = configuracions.Where(x => x.TipoProductoId == 1).ToDictionary(x => x.Nivel, x => x);

            foreach (var item in responserGetBonoResidual.ListaCuotaRed)
            {
                var type = item.GetType();

                for (int i = 1; i <= 7; i++)
                {

                    var prop = type.GetProperty($"LPatrocinado{i}");
                    int patrocinadoId = 0;

                    if (prop != null)
                    {
                        var value = prop.GetValue(item);
                        patrocinadoId = value == null ? 0 : Convert.ToInt32(value);
                    }

                    contactosDict.TryGetValue(patrocinadoId, out var contacto);
                    configDict.TryGetValue(i, out var objConfig);

                    var porcentaje = objConfig?.PorcentajeComision ?? 0;

                    BrCalculoItem rows = new BrCalculoItem
                    {
                        LContactoId = patrocinadoId,
                        NombreCompleto = contacto?.SNombreCompleto ?? "",
                        Documento = contacto?.SCedulaIdentidad ?? "",
                        LContactoIdHijo = item.LContactoId,
                        NombreCompletoHijo = item.Cliente,
                        DocumentoHijo = item.DocumentoCliente,
                        Nivel = i,
                        Bono = item.Bono,
                        BonoResidual = item.Bono * porcentaje / 100,
                        ActivoMes = activosSet.Contains(patrocinadoId),
                        PorcentajeComision = porcentaje,
                        LComplejoId = item.ProyectoId,
                        Complejo = item.Proyecto,
                        Empresa = item.Empresa
                    };

                    listadoResidual.Add(rows);
                }
            }
            listadoResidual = listadoResidual.Where(x => x.ActivoMes).ToList();

            if (listadoResidual.Count == 0)
            {
                var responseFinPasoSinDatos = await _controlProcesoRepository.FinalizarPaso(
                    logTransaccionId.ToString(),
                    Usuario,
                    ProcesosDiccionario.COMISIONES,
                    LCicloId,
                    PasosDiccionario.COMISION_RESIDUAL
                );

                if (!responseFinPasoSinDatos.Success || !(responseFinPasoSinDatos.Data?.status ?? false))
                {
                    return Ok(new
                    {
                        status = false,
                        mensaje = responseFinPasoSinDatos.Data?.mensaje ?? responseFinPasoSinDatos.Mensaje,
                        data = ""
                    });
                }

                pasoIniciado = false;

                return Ok(new
                {
                    status = true,
                    mensaje = "No existen registros habilitados para generar bono residual.",
                    data = new
                    {
                        listaCuota = responserGetBonoResidual.ListaCuotaRed.Count(),
                        contacto = responserGetBonoResidual.ListaContacto.Count(),
                        Residual = 0,
                        ResidualActivos = 0,
                        ResidualInactivos = 0,
                        inicio,
                        fin = DateTime.Now
                    }
                });
            }

            var listadoBonoCompleto = listadoResidual.GroupBy(x => new {x.Nivel, x.LContactoId, x.LContactoIdHijo, x.DocumentoHijo, x.LComplejoId})
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
                LCicloId = LCicloId
            })
            .ToList();

            var ListadoRedEmpresaComplejo = listadoResidual.GroupBy(x => new {x.LContactoId, x.LComplejoId})
            .Select(g => new ItemRedEmpresaComplejo
            {
                LRedEmpresaComplejoId = 0,
                LCicloId = LCicloId,
                LContactoId = g.Key.LContactoId,
                LComplejoId = g.Key.LComplejoId,
                DMonto = g.Sum(x => x.BonoResidual)
            })
            .ToList();

            List<ItemAdministracionBonoResidual> listado = ListadoRedEmpresaComplejo.GroupBy(x => new {x.LContactoId})
            .Select(g=> new ItemAdministracionBonoResidual
            {
                Usuario = Usuario
                , LBonoResidualId = 0
                , LCicloId = LCicloId
                , LContactoId = g.Key.LContactoId
                , DTotalBono = g.Sum(x => x.DMonto)
            })
            .ToList();
            
            var responseAdministacionBonoResidual = await _adminBonoResidualRepository.SaveAdministracionBonoResidual(logTransaccionId.ToString(), Usuario, listado);
            var responseAdministacionBonoCompleto = await _adminBonoResidualRepository.SaveAdministracionBonoCompleto(logTransaccionId.ToString(), Usuario, listadoBonoCompleto );
            var responseAdministacionRedEmpresaComplejo= await _adminBonoResidualRepository.SaveAdministracionRedEmpresaComplejo(logTransaccionId.ToString(), Usuario, ListadoRedEmpresaComplejo );

            if (!responseAdministacionBonoResidual.Success || !responseAdministacionBonoCompleto.Success || !responseAdministacionRedEmpresaComplejo.Success)
            {
                await _controlProcesoRepository.CancelarPaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, LCicloId, PasosDiccionario.COMISION_RESIDUAL);
                pasoIniciado = false;

                string mensajeError = !responseAdministacionBonoResidual.Success
                    ? responseAdministacionBonoResidual.Mensaje
                    : !responseAdministacionBonoCompleto.Success
                        ? responseAdministacionBonoCompleto.Mensaje
                        : responseAdministacionRedEmpresaComplejo.Mensaje;

                return Ok(new
                {
                    status = false,
                    mensaje = mensajeError,
                    data = ""
                });
            }

            var responseFinPaso = await _controlProcesoRepository.FinalizarPaso(
                logTransaccionId.ToString(),
                Usuario,
                ProcesosDiccionario.COMISIONES,
                LCicloId,
                PasosDiccionario.COMISION_RESIDUAL
            );

            if (!responseFinPaso.Success || !(responseFinPaso.Data?.status ?? false))
            {
                return Ok(new
                {
                    status = false,
                    mensaje = responseFinPaso.Data?.mensaje ?? responseFinPaso.Mensaje,
                    data = ""
                });
            }

            pasoIniciado = false;
            
            DateTime fin = DateTime.Now;
            
            return Ok(new
            {
                status = responserGetBonoResidual.Success,
                mensaje = "Se guardo correctamente el bono residual.",
                data =new
                {
                    listaCuota = responserGetBonoResidual.ListaCuotaRed.Count(),
                    contacto = responserGetBonoResidual.ListaContacto.Count(),
                    Residual = listadoResidual.Count,
                    ResidualActivos = listadoResidual.Where(x => x.ActivoMes == true).ToList().Count,
                    ResidualInactivos = listadoResidual.Where(x => x.ActivoMes == false).ToList().Count,
                    inicio,
                    fin
                }
            });
        }
        catch (Exception ex)
        {
            if (pasoIniciado)
            {
                await _controlProcesoRepository.CancelarPaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, LCicloId, PasosDiccionario.COMISION_RESIDUAL);
            }

            _log.Error(logTransaccionId.ToString(), NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return Ok(new
            {
                status = false,
                mensaje = ex.Message,
                data = ""
            });
        }
        
    }

    [HttpGet("get/bono/par")]
    public async Task<IActionResult> ObtenerBonoPar([FromHeader(Name = "Usuario")] string Usuario, [FromHeader(Name = "LCicloId")] int LCicloId, [FromHeader(Name = "Inicio")] string Inicio, [FromHeader(Name = "Fin")] string Fin)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        string nombreMetodo = "ObtenerBonoPar()";
        try
        {
            var responseSiguientePaso = await _controlProcesoRepository.GetSiguientePaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, LCicloId);
            var ResponseObtenerBonoPar = await _bonoParRepository.GetBonoPar(logTransaccionId.ToString(), Usuario, Inicio, Fin);
            var responseHabilitaciones = await _habilitacionRepository.GetHabilitaciones(logTransaccionId.ToString(), Usuario, LCicloId);
            var responseContratos = await _administracionContratoRepository.GetAdministracionContratoFechaVentaResidual(logTransaccionId.ToString(), Inicio, Fin);

            if (!responseHabilitaciones.Success)
            {
                return Ok(new
                {
                    status = false,
                    mensaje = responseHabilitaciones.Mensaje,
                    data = ""
                });
            }

            var contactosBloqueados = HabilitacionComisionHelper.GetContactosBloqueadosParaComision(responseHabilitaciones.Data);
            var habilitadosSet = HabilitacionComisionHelper.GetContactosHabilitadosQueGeneranComision(responseHabilitaciones.Data);
            var contratosNormalesSet = responseContratos.Data
                .Where(item => !HabilitacionComisionHelper.TiposContratoEspeciales.Contains(item.LTipoContratoId))
                .Select(item => item.LAsesorId)
                .ToHashSet();
            var listaBonoPar = ResponseObtenerBonoPar.Data
                .Where(item =>
                    !contactosBloqueados.Contains(item.LContctoGanadorId)
                    && (contratosNormalesSet.Contains(item.LContctoGanadorId) || habilitadosSet.Contains(item.LContctoGanadorId)))
                .ToList();

            foreach (var item in listaBonoPar)
            {
                item.EsHabilitado = !contratosNormalesSet.Contains(item.LContctoGanadorId)
                    && habilitadosSet.Contains(item.LContctoGanadorId);
            }

            BonoParXls bonoParXls = new BonoParXls();
            var ResponseObtenerXls = await bonoParXls.GetBonoParXls(listaBonoPar);
            
            return Ok(new
            {
                status = ResponseObtenerBonoPar.Success,
                mensaje = ResponseObtenerBonoPar.Mensaje,
                data =new
                {
                    ListaBonoPar = listaBonoPar,
                    xls = ResponseObtenerXls.base64,
                    controlPasos = new
                    {
                        ejecutado = PasosDiccionario.EsBonoPar(responseSiguientePaso.Data.nombre) ? false : true,
                        data = responseSiguientePaso.Data
                    }
                    
                }
            });
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId.ToString(), NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return Ok(new
            {
                status = false,
                mensaje = ex.Message,
                data = ""
            });
        }
        
    }
    [HttpPost("save/bono/par")]
    public async Task<IActionResult> GuardarBonoPar([FromHeader(Name = "Usuario")] string Usuario, [FromHeader(Name = "LCicloId")] int LCicloId, [FromHeader(Name = "Inicio")] string Inicio, [FromHeader(Name = "Fin")] string Fin)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreMetodo = "GetBonoResidual()";
        bool pasoIniciado = false;
        string pasoActual = string.Empty;
        try
        {
            var responseSiguientePaso = await _controlProcesoRepository.GetSiguientePaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, LCicloId);
            if (!PasosDiccionario.EsBonoPar(responseSiguientePaso.Data.nombre))
            {
                return Ok(new
                {
                    status = false,
                    mensaje = "Esta paso ya se encuentra ejecutado para este ciclo, si quieres volver a a procesar debes reinicar el proceso para el ciclo",
                    data = ""
                });
            }

            pasoActual = responseSiguientePaso.Data.nombre;

            var responseInicioPaso = await _controlProcesoRepository.IniciarPaso(
                logTransaccionId.ToString(),
                Usuario,
                ProcesosDiccionario.COMISIONES,
                LCicloId,
                pasoActual
            );

            if (!responseInicioPaso.Success || !(responseInicioPaso.Data?.status ?? false))
            {
                return Ok(new
                {
                    status = false,
                    mensaje = responseInicioPaso.Data?.mensaje ?? responseInicioPaso.Mensaje,
                    data = ""
                });
            }

            pasoIniciado = true;

            var responseBonoPar = await _bonoParRepository.GetBonoPar(logTransaccionId.ToString(), Usuario, Inicio, Fin);
            var responseHabilitaciones = await _habilitacionRepository.GetHabilitaciones(logTransaccionId.ToString(), Usuario, LCicloId);
            var responseContratos = await _administracionContratoRepository.GetAdministracionContratoFechaVentaResidual(logTransaccionId.ToString(), Inicio, Fin);

            if (!responseBonoPar.Success || !responseHabilitaciones.Success)
            {
                await _controlProcesoRepository.CancelarPaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, LCicloId, pasoActual);
                pasoIniciado = false;

                return Ok(new
                {
                    status = false,
                    mensaje = !responseBonoPar.Success
                        ? responseBonoPar.Mensaje
                        : responseHabilitaciones.Mensaje,
                    data = ""
                });
            }

            var contactosBloqueados = HabilitacionComisionHelper.GetContactosBloqueadosParaComision(responseHabilitaciones.Data);
            var habilitadosSet = HabilitacionComisionHelper.GetContactosHabilitadosQueGeneranComision(responseHabilitaciones.Data);
            var contratosNormalesSet = responseContratos.Data
                .Where(item => !HabilitacionComisionHelper.TiposContratoEspeciales.Contains(item.LTipoContratoId))
                .Select(item => item.LAsesorId)
                .ToHashSet();
            var listadoBonoPar = responseBonoPar.Data
                .Where(item =>
                    !contactosBloqueados.Contains(item.LContctoGanadorId)
                    && (contratosNormalesSet.Contains(item.LContctoGanadorId) || habilitadosSet.Contains(item.LContctoGanadorId)))
                .ToList();

            if (listadoBonoPar.Count == 0)
            {
                var responseFinPasoSinDatos = await _controlProcesoRepository.FinalizarPaso(
                    logTransaccionId.ToString(),
                    Usuario,
                    ProcesosDiccionario.COMISIONES,
                    LCicloId,
                    pasoActual
                );

                if (!responseFinPasoSinDatos.Success || !(responseFinPasoSinDatos.Data?.status ?? false))
                {
                    return Ok(new
                    {
                        status = false,
                        mensaje = responseFinPasoSinDatos.Data?.mensaje ?? responseFinPasoSinDatos.Mensaje,
                        data = ""
                    });
                }

                pasoIniciado = false;

                return Ok(new
                {
                    status = true,
                    mensaje = "No existen ganadores habilitados para generar bono par.",
                    data = ""
                });
            }

            var responseSaveBonoPar = await _bonoParRepository.SaveBonoPar(logTransaccionId.ToString(), Usuario,LCicloId, listadoBonoPar);

            if (!responseSaveBonoPar.Success)
            {
                await _controlProcesoRepository.CancelarPaso(
                    logTransaccionId.ToString(),
                    Usuario,
                    ProcesosDiccionario.COMISIONES,
                    LCicloId,
                    pasoActual
                );

                pasoIniciado = false;

                return Ok(new
                {
                    status = false,
                    mensaje = responseSaveBonoPar.Mensaje,
                    data = ""
                });
            }

            var responseFinPaso = await _controlProcesoRepository.FinalizarPaso(
                logTransaccionId.ToString(),
                Usuario,
                ProcesosDiccionario.COMISIONES,
                LCicloId,
                pasoActual
            );

            if (!responseFinPaso.Success || !(responseFinPaso.Data?.status ?? false))
            {
                return Ok(new
                {
                    status = false,
                    mensaje = responseFinPaso.Data?.mensaje ?? responseFinPaso.Mensaje,
                    data = ""
                });
            }

            pasoIniciado = false;

            return Ok(new
            {
                status = responseSaveBonoPar.Success,
                mensaje = responseSaveBonoPar.Mensaje,
                data = ""
            });
        }
        catch (Exception ex)
        {
            if (pasoIniciado)
            {
                await _controlProcesoRepository.CancelarPaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, LCicloId, pasoActual);
            }

            _log.Error(logTransaccionId.ToString(), NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return Ok(new
            {
                status = false,
                mensaje = ex.Message,
                data = ""
            });
        }
        
    }
}
