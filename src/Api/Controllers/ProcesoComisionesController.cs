using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Microsoft.AspNetCore.DataProtection.Repositories;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProcesoComisionesController : ControllerBase
{
    private readonly ILogService _log;
    private readonly IVentasCnxRepository _ventasCnxRepository;
    private readonly MiCronJob _miCronJob;
    private readonly IProcesoComisionesRepository _procesoComisionesRepository;
    private readonly string NOMBREARCHIVO = "UtilsController.cs";

    public ProcesoComisionesController(IVentasCnxRepository ventasCnxRepository, ILogService log, MiCronJob miCronJob, IProcesoComisionesRepository procesoComisionesRepository)
    {
        _ventasCnxRepository = ventasCnxRepository;
        _log = log;
        _procesoComisionesRepository = procesoComisionesRepository;
        _miCronJob = miCronJob;
    }
    [HttpGet("vta/cnx")]
    public async Task<IActionResult> GetVentaCnx([FromHeader(Name = "lCicloId")] int lCicloId)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "GetVentaCnx()";
        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, "Inicio de metodo");
            var responseVtaCnx = await _ventasCnxRepository.GetVentaCnx(logTransaccionId.ToString(), "", "");
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Fin de metodo.");

            return Ok(new
            {
                status = responseVtaCnx.Success ? true : false,
                mensaje = responseVtaCnx.Mensaje,
                data = responseVtaCnx.Data
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
    [HttpPost("ejemplo")]
    public async Task<IActionResult> Ejecutar()
    {
        

        var t = _miCronJob.Proceso();
        
        return Ok(new
        {
            ex = true
        });
    }
    [HttpGet("venta/personal")]
    public async Task<IActionResult> GetVentaPersonal([FromHeader(Name = "lCicloId")] int lCicloId, [FromHeader(Name = "Inicio")] string Inicio, [FromHeader(Name = "Fin")] string Fin, [FromHeader(Name = "Usuario")] string Usuario)
    {
        var responseVentaPersonal = await _procesoComisionesRepository.GetCalculoVentaPersonal("", Usuario, Inicio, Fin, lCicloId);
        return Ok(new{
            status = responseVentaPersonal.Success,
            mensaje = responseVentaPersonal.Mensaje,
           responseVentaPersonal.Data 
        });
    }
}
