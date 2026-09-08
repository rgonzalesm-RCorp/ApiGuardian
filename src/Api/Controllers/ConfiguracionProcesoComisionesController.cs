using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfiguracionProcesoComisionesController : ControllerBase
{
    private readonly IConfiguracionProcesoComisionesService _service;
    public ConfiguracionProcesoComisionesController(IConfiguracionProcesoComisionesService service) => _service = service;

    [HttpPost("vta/cnx")]
    public async Task<IActionResult> GuardarConfiguracionVentaPersona([FromBody] PC_ConfigVtaPersonal data)
    {
        var r = await _service.GuardarAsync(data);
        return Ok(new { status = r.Success, mensaje = r.Mensaje, swall = r.Swall, data = r.Data });
    }
    [HttpGet("get/vta/cnx")]
    public async Task<IActionResult> GetConfiguracionVentaPersona()
    {
        var r = await _service.ObtenerAsync();
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = r.Data });
    }
    [HttpDelete("delete/vta/cnx")]
    public async Task<IActionResult> DeleteConfiguracionVentaPersona([FromHeader(Name = "usuario")] string usuario, [FromHeader(Name = "ConfigVtaPersonalId")] int configuracionId)
    {
        var r = await _service.EliminarAsync(usuario, configuracionId);
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = "" });
    }
}
