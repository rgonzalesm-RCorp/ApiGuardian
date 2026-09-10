using ApiGuardian.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class RetencionEmpresaController : ControllerBase
{
    private readonly IRetencionEmpresaService _service;

    public RetencionEmpresaController(IRetencionEmpresaService service)
    {
        _service = service;
    }

    [HttpGet("ver")]
    public async Task<IActionResult> Ver([FromHeader(Name = "LCicloId")] int cicloId)
    {
        var resultado = await _service.VerAsync(cicloId);
        return Ok(new { status = resultado.Success, mensaje = resultado.Mensaje, data = resultado.Data });
    }

    [HttpPost("guardar")]
    public async Task<IActionResult> Guardar([FromHeader(Name = "LCicloId")] int cicloId)
    {
        var resultado = await _service.GuardarAsync(cicloId);
        return Ok(new { status = resultado.Success, mensaje = resultado.Mensaje, data = resultado.Data });
    }
}
