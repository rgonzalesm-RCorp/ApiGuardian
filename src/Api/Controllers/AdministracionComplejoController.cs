using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministracionComplejoController : ControllerBase
{
    private readonly IAdministracionComplejoService _service;
    public AdministracionComplejoController(IAdministracionComplejoService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetComplejo()
    { var r = await _service.ObtenerAsync(DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()); return Ok(new { status = r.Success, mensaje = r.Mensaje, data = r.Data }); }
    [HttpGet("paginacion")]
    public async Task<IActionResult> GetComplejoPagination([FromHeader(Name = "page")] int page, [FromHeader(Name = "pageSize")] int pageSize, [FromHeader(Name = "search")] string? search)
    { var r = await _service.ObtenerPaginadoAsync(page, pageSize, search, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()); return Ok(new { status = r.Success, mensaje = r.Mensaje, data = r.Data }); }
    [HttpPost("insert")]
    public async Task<IActionResult> GuardarComplejo(AdministracionComplejoABM data)
    { var r = await _service.InsertarAsync(data, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()); return Ok(new { status = r.Success, mensaje = r.Mensaje, data = "" }); }
    [HttpPut("update")]
    public async Task<IActionResult> ModificarComplejo(AdministracionComplejoABM data)
    { var r = await _service.ActualizarAsync(data, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()); return Ok(new { status = r.Success, mensaje = r.Mensaje, data = "" }); }
    [HttpDelete("delete")]
    public async Task<IActionResult> EliminarComplejo([FromHeader(Name = "lComplejoId")] int complejoId)
    { var r = await _service.EliminarAsync(complejoId, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()); return Ok(new { status = r.Success, mensaje = r.Mensaje, data = "" }); }
}
