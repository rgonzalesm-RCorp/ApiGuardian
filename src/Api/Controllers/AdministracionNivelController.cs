using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministracionNivelController : ControllerBase
{
    private readonly IAdministracionNivelService _service;

    public AdministracionNivelController(IAdministracionNivelService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetNivel()
    {
        var r = await _service.ObtenerAsync(DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = r.Data });
    }

    [HttpGet("paginacion")]
    public async Task<IActionResult> GetNivelPaginacion([FromHeader(Name = "page")] int page, [FromHeader(Name = "pageSize")] int pageSize, [FromHeader(Name = "search")] string? search)
    {
        var r = await _service.ObtenerPaginadoAsync(page, pageSize, search, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = r.Data });
    }

    [HttpPost("insert")]
    public async Task<IActionResult> GuardarNivel(AdministracionNivel data)
    {
        var r = await _service.InsertarAsync(data, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = "" });
    }

    [HttpPut("update")]
    public async Task<IActionResult> ModificarNivel(AdministracionNivel data)
    {
        var r = await _service.ActualizarAsync(data, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = "" });
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> ModificarNivel([FromHeader(Name = "lNivelId")] int lNivelId)
    {
        var r = await _service.EliminarAsync(lNivelId, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = "" });
    }
}
