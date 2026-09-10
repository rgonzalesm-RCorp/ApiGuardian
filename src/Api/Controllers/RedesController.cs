using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RedesController : ControllerBase
{
    private readonly IRedesService _service;

    public RedesController(IRedesService service) => _service = service;

    [HttpGet("armar/red/comprimida/mes")]
    public async Task<IActionResult> GetDatos(
        [FromHeader(Name = "Usuario")] string usuario,
        [FromHeader(Name = "LCicloId")] int cicloId,
        [FromHeader(Name = "Inicio")] string inicio,
        [FromHeader(Name = "Fin")] string fin,
        [FromHeader(Name = "ControlPaso")] bool controlPaso = true
    )
    {
        var r = await _service.ObtenerRedComprimidaAsync(usuario, cicloId, inicio, fin, controlPaso);
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = r.Data });
    }

    [HttpGet("armar/red/cuotas")]
    public async Task<IActionResult> GetClientesCuotas([FromHeader(Name = "Usuario")] string usuario, [FromHeader(Name = "LCicloId")] int cicloId)
    {
        var r = await _service.ObtenerRedCuotasAsync(usuario, cicloId);
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = r.Data });
    }
}
