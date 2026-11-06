using ApiGuardian.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class MonteSionArbolController : ControllerBase
{
    private readonly IMonteSionArbolService _service;

    public MonteSionArbolController(IMonteSionArbolService service) => _service = service;

    [HttpPost("armar")]
    public async Task<IActionResult> Armar(
        [FromHeader(Name = "LCicloId")] int cicloId,
        [FromHeader] string? usuario)
    {
        var resultado = await _service.ArmarAsync(cicloId, usuario ?? string.Empty);
        return Ok(new { status = resultado.Success, mensaje = resultado.Mensaje, data = new { total = resultado.Total } });
    }

    [HttpGet("raices")]
    public async Task<IActionResult> Raices([FromHeader(Name = "LCicloId")] int cicloId)
    {
        var resultado = await _service.RaicesAsync(cicloId);
        return Ok(new { status = resultado.Success, mensaje = resultado.Mensaje, data = resultado.Data });
    }

    [HttpGet("hijos")]
    public async Task<IActionResult> Hijos(
        [FromHeader(Name = "LCicloId")] int cicloId,
        [FromHeader(Name = "LPadreContactoId")] int padreId)
    {
        var resultado = await _service.HijosAsync(cicloId, padreId);
        return Ok(new { status = resultado.Success, mensaje = resultado.Mensaje, data = resultado.Data });
    }

    [HttpGet("persona")]
    public async Task<IActionResult> Persona(
        [FromHeader(Name = "LCicloId")] int cicloId,
        [FromHeader(Name = "LContactoId")] int contactoId)
    {
        var resultado = await _service.ConsultarPersonaAsync(cicloId, contactoId);
        return Ok(new { status = resultado.Success, mensaje = resultado.Mensaje, data = resultado.Data });
    }

    [HttpGet("buscar")]
    public async Task<IActionResult> Buscar(
        [FromHeader(Name = "LCicloId")] int cicloId,
        [FromHeader(Name = "Texto")] string? texto)
    {
        var resultado = await _service.BuscarAsync(cicloId, Uri.UnescapeDataString(texto ?? string.Empty));
        return Ok(new { status = resultado.Success, mensaje = resultado.Mensaje, data = resultado.Data });
    }
}
