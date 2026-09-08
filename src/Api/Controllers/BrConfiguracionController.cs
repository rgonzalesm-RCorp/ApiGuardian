using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BrConfiguracionController : ControllerBase
{
    private readonly IBrConfiguracionService _service;
    public BrConfiguracionController(IBrConfiguracionService service) => _service = service;

    [HttpGet("get/datos")]
    public async Task<IActionResult> GetDatos([FromHeader(Name = "Usuario")] string usuario) => Respuesta(await _service.ObtenerDatosAsync(usuario));
    [HttpGet("get/configuracion")]
    public async Task<IActionResult> Get() => Respuesta(await _service.ObtenerConfiguracionAsync());
    [HttpPost("save/configuracion")]
    public async Task<IActionResult> Save([FromBody] BrConfiguracion data)
    {
        var r = await _service.GuardarAsync(data);
        return Ok(new { status = r.Success, mensaje = r.Mensaje });
    }
    [HttpDelete("delete/configuracion")]
    public async Task<IActionResult> Delete([FromHeader(Name = "Usuario")] string usuario, [FromHeader(Name = "brConfiguracionId")] int configuracionId)
    {
        var r = await _service.EliminarAsync(usuario, configuracionId);
        return Ok(new { status = r.Success, mensaje = r.Mensaje });
    }
    private IActionResult Respuesta((bool Success, string Mensaje, object Data) r) => Ok(new { status = r.Success, mensaje = r.Mensaje, data = r.Data });
}
