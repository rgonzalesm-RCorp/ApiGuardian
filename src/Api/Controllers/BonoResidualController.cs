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
    private readonly string NOMBREARCHIVO = "BonoResidualController.cs";
    public BonoResidualController(ILogService log, IBonoResidualRepository bonoResidualRepository, IVentasCnxRepository ventasCnxRepository, IBrConfiguracionRepository brConfiguracionRepository, IAdministracionBonoResidualRepository administracionBonoResidualRepository)
    {
        _bonoResidualRepository = bonoResidualRepository;
        _ventasCnxRepository = ventasCnxRepository;
        _brConfiguracionRepository = brConfiguracionRepository;
        _adminBonoResidualRepository = administracionBonoResidualRepository;
        _log = log;
    }
    [HttpGet("get/cartera")]
    public async Task<IActionResult> GetCartera([FromHeader(Name = "Usuario")] string Usuario)
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

            return Ok(new
            {
                status = responseCarteraAll.Success ? true : false,
                mensaje = responseCarteraAll.Mensaje,
                data = new {
                    resumenCartera,
                    base64 = xlsCuota.base64,
                    fileNameXls = $"Reporte de la cartera de clientes"
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
    public async Task<IActionResult> GuardarCartera([FromHeader(Name = "Usuario")] string Usuario)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "GuardarCartera()";
        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, "Inicio de metodo");
            var responseCartera = await _bonoResidualRepository.GetCarteraAll(logTransaccionId.ToString(), Usuario);
            var responseSaveCartera = _bonoResidualRepository.GuardarCartera(logTransaccionId.ToString(), Usuario, responseCartera.ListaCartera.ToList());

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Fin de metodo.");

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
    [HttpGet("get/cuota")]
    public async Task<IActionResult> GetCuota([FromHeader(Name = "Usuario")] string Usuario, [FromHeader(Name = "Inicio")] string inicio, [FromHeader(Name = "Fin")] string fin)
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

            return Ok(new
            {
                status = responseCuota.Success ? true : false,
                mensaje = responseCuota.Mensaje,
                data = new {
                    resumen,
                    xlsCuota.base64,
                    fileNameXls = $"Reporte de cuotas del {inicio} al {fin}"
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
    public async Task<IActionResult> GuardarCuota([FromHeader(Name = "Usuario")] string Usuario, [FromHeader(Name = "Inicio")] string inicio, [FromHeader(Name = "Fin")] string fin)
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
            var responseSaveCuota = _bonoResidualRepository.GuardarCuota(logTransaccionId.ToString(), Usuario, responseCuota.ListaCuota.ToList());


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


    [HttpGet("get/excedente")]
    public async Task<IActionResult> GetExcedente([FromHeader(Name = "Usuario")] string Usuario, [FromHeader(Name = "Inicio")] string inicio, [FromHeader(Name = "Fin")] string fin)
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
             

            return Ok(new
            {
                status = true,
                mensaje = "Se estan guardando las cuotas en segundo plano.",
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
    [HttpPost("save/excedente")]
    public async Task<IActionResult> GuardarExcedente([FromHeader(Name = "Usuario")] string Usuario, [FromHeader(Name = "Inicio")] string inicio, [FromHeader(Name = "Fin")] string fin)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "GuardarExcedente()";
        try
        {
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
            return Ok(new
            {
                status = responserGetBonoResidual.Success,
                mensaje = responserGetBonoResidual.Mensaje,
                data =new
                {
                    ListadoResumenPorEmpresa,
                    counter = listadoResidual.Count,
                    base64 = reponseXLS.base64
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
                        Empresa = item.Empresa
                    };

                    listadoResidual.Add(rows);
                }
            }

            var listadoBonoCompleto = listadoResidual.GroupBy(x => new {x.Nivel, x.LContactoId, x.DocumentoHijo, x.LComplejoId})
            .Select(g => new
            {
                g.Key.Nivel,
                g.Key.LContactoId,
                g.Key.DocumentoHijo,
                g.Key.LComplejoId,
                TotalPago = g.Sum(x => x.BonoResidual)
            })
            .ToList();

            var ListadoRedEmpresaComplejo = listadoResidual.GroupBy(x => new {x.LContactoId, x.LComplejoId})
            .Select(g => new
            {
                g.Key.LContactoId,
                g.Key.LComplejoId,
                TotalPago = g.Sum(x => x.BonoResidual)
            })
            .ToList();

            List<ItemAdministracionBonoResidual> listado = ListadoRedEmpresaComplejo.GroupBy(x => new {x.LContactoId})
            .Select(g=> new ItemAdministracionBonoResidual
            {
                Usuario = Usuario
                , LBonoResidualId = 0
                , LCicloId = LCicloId
                , LContactoId = g.Key.LContactoId
                , DTotalBono = g.Sum(x => x.TotalPago)
            })
            .ToList();
            
            var responseAdministacionBonoResidual = await _adminBonoResidualRepository.SaveAdministracionBonoResidual(logTransaccionId.ToString(), Usuario, listado);
            
            DateTime fin = DateTime.Now;
            
            return Ok(new
            {
                status = responserGetBonoResidual.Success,
                mensaje = responserGetBonoResidual.Mensaje,
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


}
