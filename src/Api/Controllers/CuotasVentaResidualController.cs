using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CuotasVentaResidualController : ControllerBase
{
    private readonly ICuotasVentaResidualService _service;

    public CuotasVentaResidualController(ICuotasVentaResidualService service) => _service = service;

    [HttpGet("cuotas/venta/residual")]
    public async Task<IActionResult> GetDatos([FromHeader(Name = "Usuario")] string usuario, [FromHeader(Name = "LCicloId")] int cicloId)
    {
        var r = await _service.ObtenerAsync(usuario, cicloId);
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = r.Data });
    }

    [HttpPost("cuotas/venta/residual")]
    public async Task<IActionResult> Guardar([FromHeader(Name = "Usuario")] string usuario, [FromHeader(Name = "LCicloId")] int cicloId)
    {
        var r = await _service.GuardarAsync(usuario, cicloId);
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = r.Data });
    }
}
