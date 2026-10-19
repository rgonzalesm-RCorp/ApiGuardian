using ApiGuardian.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class MonteSionController : ControllerBase
{
    private readonly IMonteSionService _service;
    private readonly IMonteSionRepository _repository;

    public MonteSionController(IMonteSionService service, IMonteSionRepository repository)
    {
        _service = service;
        _repository = repository;
    }

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

    [HttpPost("migrar/4.10-a-10.40")]
    public async Task<IActionResult> MigrarDesdeServidor410(
        [FromHeader(Name = "ConfirmarMigracion")] string? confirmacion)
    {
        if (!string.Equals(confirmacion, "MIGRAR_MONTE_SION", StringComparison.Ordinal))
            return Ok(new
            {
                status = false,
                mensaje = "Confirme la operación enviando el encabezado ConfirmarMigracion: MIGRAR_MONTE_SION.",
                data = new { },
            });

        var resultado = await _repository.MigrarTablasAsync(Guid.NewGuid().ToString("N"));
        return Ok(new { status = resultado.Success, mensaje = resultado.Mensaje, data = resultado.Data });
    }
}
