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
    private readonly IControlProcesoRepository _controlProcesoRepository;
    private readonly IBonoParRepository _bonoParRepository;
    private readonly string NOMBREARCHIVO = "BonoResidualController.cs";
    public BonoResidualController(ILogService log
        , IBonoResidualRepository bonoResidualRepository
        , IVentasCnxRepository ventasCnxRepository
        , IBrConfiguracionRepository brConfiguracionRepository
        , IAdministracionBonoResidualRepository administracionBonoResidualRepository
        , IControlProcesoRepository controlProcesoRepository
        , IBonoParRepository bonoParRepository)
    {
        _bonoResidualRepository = bonoResidualRepository;
        _ventasCnxRepository = ventasCnxRepository;
        _brConfiguracionRepository = brConfiguracionRepository;
        _adminBonoResidualRepository = administracionBonoResidualRepository;
        _controlProcesoRepository = controlProcesoRepository;
        _bonoParRepository = bonoParRepository;
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
        try
        {
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
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, "Inicio de metodo");
            var responseCartera = await _bonoResidualRepository.GetCarteraAll(logTransaccionId.ToString(), Usuario);
            var t = GuardarCarteraGrl (logTransaccionId.ToString(),  Usuario,  LCicloId, responseCartera.ListaCartera.ToList(), nombreArchivo);
            /*var responseSaveCartera = _bonoResidualRepository.GuardarCartera(logTransaccionId.ToString(), Usuario, responseCartera.ListaCartera.ToList());

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Fin de metodo.");
            await _controlProcesoRepository.EjecutarPaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, LCicloId,  PasosDiccionario.OBTENER_CARTERA);
            */
            return Ok(new
            {
                status = true ,
                mensaje = "Se esta guardando la cartera en segundo plano.",
                data = ""
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
    private async Task<bool> GuardarCarteraGrl(string logTransaccionId, string Usuario, int LCicloId, List<TCartera> Cartera, string nombreArchivo)
    {
        var responseSaveCartera = await _bonoResidualRepository.GuardarCartera(logTransaccionId.ToString(), Usuario, Cartera);

        _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Fin de metodo.");
        await _controlProcesoRepository.EjecutarPaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, LCicloId,  PasosDiccionario.OBTENER_CARTERA);
        return true;
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
        try
        {
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
            //var responseSaveCuota = _bonoResidualRepository.GuardarCuota(logTransaccionId.ToString(), Usuario, responseCuota.ListaCuota.ToList());
            var t = GuardarCuotaGrl(logTransaccionId.ToString(), Usuario, LCicloId, responseCuota.ListaCuota.ToList(), nombreArchivo);

            return Ok(new
            {
                status = true,
                mensaje = "Se estan guardando las cuotas en segundo plano.",
                data = ""
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
    private async Task<bool> GuardarCuotaGrl(string logTransaccionId, string Usuario, int LCicloId, List<TCuota> Cuota, string nombreArchivo)
    {
        var responseSaveCuota = await _bonoResidualRepository.GuardarCuota(logTransaccionId.ToString(), Usuario, Cuota);
        _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Fin de metodo.");
        await _controlProcesoRepository.EjecutarPaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, LCicloId,  PasosDiccionario.OBTENER_CUOTAS);
        return true;
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
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Inicio de metodo [usuario: {Usuario} inicio: {inicio} fin: {fin}]");
            var responseVentaCnx = await _ventasCnxRepository.GetVentaCnx(logTransaccionId.ToString(), inicio, fin);
            List<ItemVentaCnx> dataVentasCnx = responseVentaCnx.Data.ToList();
            List<ItemVentaCnx> listaFiltrada = dataVentasCnx.Where(x => (x.SCuotaInicialOriginal - x.ValorCi) > Convert.ToDecimal(0.05) && !x.Glosa.Contains("UPGRADE")).ToList();

            List<TCuota> ListaExcedente = new List<TCuota>();
            foreach (var item in listaFiltrada)
            {
                TCuota row = new TCuota
                {
                    Idproducto = item.Lote,
                    Idproyecto = item.LComplejoId,
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

                };
                ListaExcedente.Add(row);
            }

            var responseGuardarExcedente = await _bonoResidualRepository.GuardarCuota(logTransaccionId.ToString(), Usuario, ListaExcedente, true);
            await _controlProcesoRepository.EjecutarPaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, LCicloId,  PasosDiccionario.OBTENER_EXCEDENTE);

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
            var responseConfiguracionBr = await _brConfiguracionRepository.GetConfiguracion(logTransaccionId.ToString(), Usuario);

            List<DetailsBrConfiguracion> configuracions = responseConfiguracionBr.Data.Where(x => x.LCicloId == LCicloId).ToList();
            List<BrCalculoItem> listadoResidual = new List<BrCalculoItem>(); 

            var contactosDict = responserGetBonoResidual.ListaContacto.ToDictionary(x => x.LContactoId, x => x);

            var activosSet = responserGetBonoResidual.ListaContactosActivos.Select(x => x.LContactoId).ToHashSet();

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
                        LContactoIdHijo = 0,
                        NombreCompletoHijo = item.Cliente,
                        DocumentoHijo = item.DocumentoCliente,
                        Nivel = i,
                        Bono = item.Bono,
                        BonoResidual = item.Bono * porcentaje / 100,
                        ActivoMes = activosSet.Contains(patrocinadoId),
                        PorcentajeComision = porcentaje,
                        LComplejoId = item.ProyectoId,
                        Complejo = item.Proyecto,
                        Empresa = item.Empresa,
                        ProductoId = item.ProductoId
                    };

                    listadoResidual.Add(rows);
                }
            }

            listadoResidual = listadoResidual.Where(x => x.ActivoMes).ToList();
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
            var responserGetBonoResidual = await _bonoResidualRepository.GetDataCalculoBonoResidual(logTransaccionId.ToString(), Usuario, LCicloId);
            var responseConfiguracionBr = await _brConfiguracionRepository.GetConfiguracion(logTransaccionId.ToString(), Usuario);

            List<DetailsBrConfiguracion> configuracions = responseConfiguracionBr.Data.Where(x => x.LCicloId == LCicloId).ToList();
            List<BrCalculoItem> listadoResidual = new List<BrCalculoItem>();

            var contactosDict = responserGetBonoResidual.ListaContacto.ToDictionary(x => x.LContactoId, x => x);

            var activosSet = responserGetBonoResidual.ListaContactosActivos.Select(x => x.LContactoId).ToHashSet();

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
            await _controlProcesoRepository.EjecutarPaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, LCicloId,  PasosDiccionario.COMISION_RESIDUAL);
            
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
            /*var responseSiguientePaso = await _controlProcesoRepository.GetSiguientePaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, LCicloId);
            if (PasosDiccionario.COMISION_RESIDUAL != responseSiguientePaso.Data.nombre)
            {
                return Ok(new
                {
                    status = false,
                    mensaje = "Esta paso ya se encuentra ejecutado para este ciclo, si quieres volver a a procesar debes reinicar el proceso para el ciclo",
                    data = ""
                });
            }*/
            var ResponseObtenerBonoPar = await _bonoParRepository.GetBonoPar(logTransaccionId.ToString(), Usuario, Inicio, Fin);
            BonoParXls bonoParXls = new BonoParXls();
            var ResponseObtenerXls = await bonoParXls.GetBonoParXls(ResponseObtenerBonoPar.Data.ToList());
            
            return Ok(new
            {
                status = ResponseObtenerBonoPar.Success,
                mensaje = ResponseObtenerBonoPar.Mensaje,
                data =new
                {
                    ListaBonoPar = ResponseObtenerBonoPar.Data,
                    xls = ResponseObtenerXls.base64
                    
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

}
