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
    private readonly string NOMBREARCHIVO = "UtilsController.cs";

    public ProcesoComisionesController(IVentasCnxRepository ventasCnxRepository, ILogService log)
    {
        _ventasCnxRepository = ventasCnxRepository;
        _log = log;
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

}
