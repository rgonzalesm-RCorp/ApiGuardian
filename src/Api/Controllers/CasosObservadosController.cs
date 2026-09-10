using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CasosObservadosController : ControllerBase
{
    private readonly ICasosObservadosService _service;

    public CasosObservadosController(ICasosObservadosService service) => _service = service;

    [HttpGet("casos/observados")]
    public async Task<IActionResult> Get([FromHeader(Name = "Usuario")] string usuario, [FromHeader(Name = "LCicloId")] int cicloId, [FromHeader(Name = "Inicio")] DateTime? inicio, [FromHeader(Name = "Fin")] DateTime? fin)
    {
        var r = await _service.ObtenerAsync(usuario, cicloId, inicio, fin);
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = r.Data });
    }

    [HttpPost("procesar")]
    public async Task<IActionResult> Procesar([FromHeader(Name = "Usuario")] string usuario, [FromHeader(Name = "LCicloId")] int cicloId)
    {
        var r = await _service.ProcesarAsync(usuario, cicloId);
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = r.Data });
    }
}
