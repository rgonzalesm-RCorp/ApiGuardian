using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministracionTipoContratoController : ControllerBase
{
    private readonly IAdministracionTipoContratoService _service;

    public AdministracionTipoContratoController(IAdministracionTipoContratoService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetTipoContrato()
    {
        var r = await _service.ObtenerAsync(DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = r.Data });
    }

    [HttpGet("paginacion")]
    public async Task<IActionResult> GetPaginacion([FromHeader] int page, [FromHeader] int pageSize, [FromHeader] string? search)
    {
        var r = await _service.ObtenerPaginadoAsync(page, pageSize, search, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = r.Data });
    }

    [HttpPost("insert")]
    public async Task<IActionResult> Insert(AdministracionTipoContratoABM data)
    {
        var r = await _service.InsertarAsync(data, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje });
    }

    [HttpPut("update")]
    public async Task<IActionResult> Update(AdministracionTipoContratoABM data)
    {
        var r = await _service.ActualizarAsync(data, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje });
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> Delete([FromHeader] int lTipoContratoId)
    {
        var r = await _service.EliminarAsync(lTipoContratoId, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje });
    }
}
