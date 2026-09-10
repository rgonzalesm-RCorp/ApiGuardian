using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministracionDescuentoCicloTipoController : ControllerBase
{
    private readonly IAdministracionDescuentoCicloTipoService _service;

    public AdministracionDescuentoCicloTipoController(IAdministracionDescuentoCicloTipoService service) => _service = service;

    [HttpGet("paginacion")]
    public async Task<IActionResult> GetPaginacion([FromHeader] int page, [FromHeader] int pageSize, [FromHeader] string? search)
    {
        var r = await _service.ObtenerPaginadoAsync(page, pageSize, search, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = r.Data });
    }

    [HttpPost("insert")]
    public async Task<IActionResult> Insert([FromBody] AdministracionDescuentoCicloTipo data)
    {
        var r = await _service.InsertarAsync(data, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje });
    }

    [HttpPut("update")]
    public async Task<IActionResult> Update([FromBody] AdministracionDescuentoCicloTipo data)
    {
        var r = await _service.ActualizarAsync(data, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje });
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> Delete([FromHeader] int lDescuentoCicloTipoId)
    {
        var r = await _service.EliminarAsync(lDescuentoCicloTipoId, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje });
    }
}
