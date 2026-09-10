using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ProcesoFacturacionController : ControllerBase
{
    private readonly IProcesoFacturacionService _service;
    public ProcesoFacturacionController(IProcesoFacturacionService service) => _service = service;

    [HttpPost("asesores")]
    public async Task<IActionResult> GuardarAsesores([FromBody] SolicitudGuardarAsesoresFacturacion solicitud)
    {
        var resultado = await _service.GuardarAsesoresAsync(solicitud);
        return Ok(new { status = resultado.Success, mensaje = resultado.Mensaje, data = resultado.Data });
    }
}
