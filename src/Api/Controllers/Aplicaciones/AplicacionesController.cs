using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using CleanDapperApi.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/aplicaciones")]
public class AplicacionesController : ControllerBase
{
    private readonly IAplicacionesService _service;
    private readonly IAplicacionesBackgroundQueue _backgroundQueue;

    public AplicacionesController(IAplicacionesService service, IAplicacionesBackgroundQueue backgroundQueue)
    {
        _service = service;
        _backgroundQueue = backgroundQueue;
    }

    [HttpGet("vista-previa")]
    public async Task<IActionResult> VistaPrevia([FromHeader(Name = "lCicloId")] int cicloId)
    {
        var r = await _service.VistaPreviaAsync(cicloId);
        return Ok(new { estado = r.Exito, mensaje = r.Mensaje, datos = r.Datos, enProceso = _backgroundQueue.EstaEnProceso(cicloId) });
    }

    [HttpPost("aplicar")]
    public async Task<IActionResult> Aplicar([FromBody] SolicitudEjecucionAplicaciones solicitud)
    {
        var r = await _service.IniciarAplicacionAsync(solicitud.LCicloId);
        return Ok(new { estado = r.Exito, mensaje = r.Mensaje, datos = r.Datos });
    }

    [HttpGet("comisionados")]
    public async Task<IActionResult> ObtenerComisionados([FromHeader(Name = "lCicloId")] int cicloId)
    {
        var r = await _service.ObtenerComisionadosAsync(cicloId);
        return Ok(new { estado = r.Exito, mensaje = r.Mensaje, datos = r.Datos, enProceso = _backgroundQueue.EstaEnProceso(cicloId) });
    }
}
