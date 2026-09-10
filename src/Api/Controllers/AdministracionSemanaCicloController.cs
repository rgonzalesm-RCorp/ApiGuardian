using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministracionSemanaCicloController : ControllerBase
{
    private readonly IAdministracionSemanaCicloService _service;

    public AdministracionSemanaCicloController(IAdministracionSemanaCicloService service) => _service = service;

    [HttpGet("paginacion")]
    public async Task<IActionResult> GetPaginacion([FromHeader] int page, [FromHeader] int pageSize, [FromHeader] string? search)
    {
        var r = await _service.ObtenerPaginadoAsync(page, pageSize, search, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = r.Data });
    }

    [HttpPost("insert")]
    public async Task<IActionResult> Insert([FromBody] AdministracionSemanaCicloABM data)
    {
        var r = await _service.InsertarAsync(data, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje });
    }

    [HttpPut("update")]
    public async Task<IActionResult> Update([FromBody] AdministracionSemanaCicloABM data)
    {
        var r = await _service.ActualizarAsync(data, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje });
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> Delete([FromHeader] int lSemanaId)
    {
        var r = await _service.EliminarAsync(lSemanaId, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje });
    }
}
