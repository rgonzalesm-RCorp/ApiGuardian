using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministracionCicloController : ControllerBase
{
    private readonly IAdministracionCicloService _service;

    public AdministracionCicloController(IAdministracionCicloService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var r = await _service.ObtenerAsync(DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = r.Data });
    }

    [HttpGet("paginacion")]
    public async Task<IActionResult> GetPagination([FromHeader] int page, [FromHeader] int pageSize, [FromHeader] string? search)
    {
        var r = await _service.ObtenerPaginadoAsync(page, pageSize, search, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = r.Data });
    }

    [HttpPost("insert")]
    public async Task<IActionResult> Insert(AdministracionCicloABM ciclo)
    {
        var r = await _service.InsertarAsync(ciclo, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje });
    }

    [HttpPut("update")]
    public async Task<IActionResult> Update(AdministracionCicloABM ciclo)
    {
        var r = await _service.ActualizarAsync(ciclo, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje });
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> Delete([FromHeader(Name = "LCicloId")] int cicloId)
    {
        var r = await _service.EliminarAsync(cicloId, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje });
    }
}
