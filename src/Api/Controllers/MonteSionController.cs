using ApiGuardian.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class MonteSionController : ControllerBase
{
    private readonly IMonteSionService _service;

    public MonteSionController(IMonteSionService service) => _service = service;

    [HttpGet("rangos")]
    public async Task<IActionResult> CalcularRangos([FromHeader(Name = "LCicloId")] int cicloId)
    {
        var resultado = await _service.CalcularRangosAsync(cicloId);
        return Ok(new { status = resultado.Success, mensaje = resultado.Mensaje, data = resultado.Data });
    }

    [HttpGet("rangos/exportar")]
    public async Task<IActionResult> ExportarRangos([FromHeader(Name = "LCicloId")] int cicloId)
    {
        var resultado = await _service.ExportarRangosAsync(cicloId);
        return Ok(new { status = resultado.Success, mensaje = resultado.Mensaje, data = resultado.Data });
    }

    [HttpPost("rangos/guardar")]
    public async Task<IActionResult> GuardarRangos(
        [FromHeader(Name = "LCicloId")] int cicloId,
        [FromHeader] string? usuario)
    {
        var resultado = await _service.GuardarRangosAsync(cicloId, usuario);
        return Ok(new { status = resultado.Success, mensaje = resultado.Mensaje, data = resultado.Data });
    }
}
