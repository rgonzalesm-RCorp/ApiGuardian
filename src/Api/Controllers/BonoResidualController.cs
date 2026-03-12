using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Microsoft.AspNetCore.DataProtection.Repositories;
using DocumentFormat.OpenXml.Wordprocessing;
using Org.BouncyCastle.Ocsp;
using DocumentFormat.OpenXml.Office2019.Excel.RichData2;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BonoResidualController : ControllerBase
{
    private readonly ILogService _log;
    private readonly IBonoResidualRepository _bonoResidualRepository;
    private readonly string NOMBREARCHIVO = "BonoResidualController.cs";
    public BonoResidualController(ILogService log, IBonoResidualRepository bonoResidualRepository)
    {
        _bonoResidualRepository = bonoResidualRepository;
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
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, "Inicio de metodo");
            var responseCuota = await _bonoResidualRepository.GetCuota(logTransaccionId.ToString(), Usuario, 1, 1, inicio, fin);
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
                    responseCuota.counter,
                    resumen,
                    base64 = xlsCuota.base64,
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
}
