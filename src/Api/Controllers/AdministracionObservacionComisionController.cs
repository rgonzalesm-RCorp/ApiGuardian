using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministracionObservacionComisionController : ControllerBase
{
    private readonly IAdministracionObservacionComisionService _service;

    public AdministracionObservacionComisionController(IAdministracionObservacionComisionService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAllAdministracionCObservacionComision([FromHeader(Name = "page")] int page, [FromHeader(Name = "pageSize")] int pageSize, [FromHeader(Name = "search")] string? search, [FromHeader(Name = "lCicloId")] int cicloId)
    {
        var r = await _service.ObtenerPaginadoAsync(page, pageSize, search, cicloId, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = r.Data });
    }

    [HttpPost("register")]
    public async Task<IActionResult> InsertAdministracionObservacionComision(AdministracionObservacionComision data)
    {
        var r = await _service.InsertarAsync(data, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = "" });
    }

    [HttpPut("update")]
    public async Task<IActionResult> UpdateAdministracionObservacionComision(AdministracionObservacionComision data)
    {
        var r = await _service.ActualizarAsync(data, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = "" });
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteAdministracionObservacionComision([FromHeader(Name = "lObservacionId")] int observacionId, [FromHeader(Name = "usuario")] string? usuario)
    {
        var r = await _service.EliminarAsync(observacionId, usuario, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = "" });
    }
}
