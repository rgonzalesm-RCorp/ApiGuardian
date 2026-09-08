using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministracionHabilitacionComisionController : ControllerBase
{
    private readonly IAdministracionHabilitacionComisionService _service;

    public AdministracionHabilitacionComisionController(IAdministracionHabilitacionComisionService service) => _service = service;

    [HttpGet("GetHabilitaciones")]
    public async Task<IActionResult> GetHabilitaciones([FromHeader(Name = "LogTransaccionId")] string? logTransaccionId, [FromHeader(Name = "Usuario")] string usuario, [FromHeader(Name = "LCicloId")] int cicloId)
    {
        var r = await _service.ObtenerAsync(logTransaccionId, usuario, cicloId);
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = r.Data });
    }

    [HttpPost("SaveHabilitaciones")]
    public async Task<IActionResult> SaveHabilitaciones([FromHeader(Name = "LogTransaccionId")] string? logTransaccionId, [FromHeader(Name = "Usuario")] string usuario, [FromHeader(Name = "LCicloId")] int cicloId, [FromBody] List<ItemHabilitacionComision> listado)
    {
        var r = await _service.GuardarAsync(logTransaccionId, usuario, cicloId, listado);
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = "" });
    }

    [HttpPut("UpdateHabilitacion")]
    public async Task<IActionResult> UpdateHabilitacion([FromHeader(Name = "LogTransaccionId")] string? logTransaccionId, [FromHeader(Name = "Usuario")] string usuario, [FromBody] ItemHabilitacionComision data)
    {
        var r = await _service.ActualizarAsync(logTransaccionId, usuario, data);
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = "" });
    }

    [HttpDelete("DeleteHabilitacion")]
    public async Task<IActionResult> DeleteHabilitacion([FromHeader(Name = "LogTransaccionId")] string? logTransaccionId, [FromHeader(Name = "Usuario")] string usuario, [FromHeader(Name = "LHabilitacionId")] int habilitacionId)
    {
        var r = await _service.EliminarAsync(logTransaccionId, usuario, habilitacionId);
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = "" });
    }
}
