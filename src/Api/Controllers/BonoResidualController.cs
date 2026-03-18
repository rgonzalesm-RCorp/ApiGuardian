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

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BonoResidualController : ControllerBase
{
    private readonly ILogService _log;
    private readonly IBonoResidualRepository _bonoResidualRepository;
    private readonly IVentasCnxRepository _ventasCnxRepository;
    private readonly string NOMBREARCHIVO = "BonoResidualController.cs";
    public BonoResidualController(ILogService log, IBonoResidualRepository bonoResidualRepository, IVentasCnxRepository ventasCnxRepository)
    {
        _bonoResidualRepository = bonoResidualRepository;
        _ventasCnxRepository = ventasCnxRepository;
        _log = log;
    }
    [HttpGet("get/cartera")]
    public async Task<IActionResult> GetCartera([FromHeader(Name = "Usuario")] string Usuario)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "GetCartera()";
        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, "Inicio de metodo");
            var responseCarteraAll = await _bonoResidualRepository.GetCarteraAll(logTransaccionId.ToString(), Usuario);


            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Fin de metodo.");

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
            _log.Error(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, "Fin de metodo", ex);
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
}
