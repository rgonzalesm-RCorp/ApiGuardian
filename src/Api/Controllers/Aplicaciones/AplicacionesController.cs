using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/aplicaciones")]
public class AplicacionesController : ControllerBase
{
    private readonly IAplicacionesService _service;
    public AplicacionesController(IAplicacionesService service) => _service = service;

    [HttpGet("vista-previa")]
    public async Task<IActionResult> VistaPrevia([FromHeader(Name = "lCicloId")] int cicloId)
    {
        var r = await _service.VistaPreviaAsync(cicloId);
        return Ok(new { estado = r.Exito, mensaje = r.Mensaje, datos = r.Datos });
    }

    [HttpPost("aplicar")]
    public async Task<IActionResult> Aplicar([FromBody] SolicitudEjecucionAplicaciones solicitud)
    {
        var r = await _service.AplicarAsync(solicitud.LCicloId);
        return Ok(new { estado = r.Exito, mensaje = r.Mensaje, datos = r.Datos });
    }
}
