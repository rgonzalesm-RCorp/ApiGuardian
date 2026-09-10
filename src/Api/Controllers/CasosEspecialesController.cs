using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CasosEspecialesController : ControllerBase
{
    private readonly ICasosEspecialesService _service;
    public CasosEspecialesController(ICasosEspecialesService service) => _service = service;

    [HttpGet("casos/especiales")]
    public async Task<IActionResult> Get([FromHeader(Name = "Usuario")] string usuario, [FromHeader(Name = "LCicloId")] int cicloId, [FromHeader(Name = "Inicio")] string inicio, [FromHeader(Name = "Fin")] string fin)
    {
        var r = await _service.ObtenerAsync(usuario, cicloId, inicio, fin);
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = r.Data });
    }
}
