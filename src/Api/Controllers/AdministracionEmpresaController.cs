using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministracionEmpresaController : ControllerBase
{
    private readonly IAdministracionEmpresaService _service;

    public AdministracionEmpresaController(IAdministracionEmpresaService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetSemana()
    {
        var r = await _service.ObtenerAsync(DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = r.Data });
    }

    [HttpGet("paginacion")]
    public async Task<IActionResult> GetPaginacion([FromHeader] int page, [FromHeader] int pageSize, [FromHeader] string? search)
    {
        var r = await _service.ObtenerPaginadoAsync(page, pageSize, search, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = r.Data });
    }

    [HttpPost("insert")]
    public async Task<IActionResult> Insert([FromBody] AdministracionEmpresa data)
    {
        var r = await _service.InsertarAsync(data, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje });
    }

    [HttpPut("update")]
    public async Task<IActionResult> Update([FromBody] AdministracionEmpresa data)
    {
        var r = await _service.ActualizarAsync(data, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje });
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> Delete([FromHeader] int lEmpresaId)
    {
        var r = await _service.EliminarAsync(lEmpresaId, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje });
    }
}
