using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministracionContratoController : ControllerBase
{
    private readonly IAdministracionContratoService _service;

    public AdministracionContratoController(IAdministracionContratoService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromHeader(Name = "page")] int page, [FromHeader(Name = "pageSize")] int pageSize, [FromHeader(Name = "search")] string? search, [FromHeader(Name = "tipoBusqueda")] int tipoBusqueda, [FromHeader(Name = "fechaInicio")] DateTime? fechaInicio, [FromHeader(Name = "fechaFin")] DateTime? fechaFin)
    {
        var r = await _service.ObtenerAsync(page, pageSize, search, tipoBusqueda, fechaInicio, fechaFin, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = r.Data });
    }

    [HttpPost("insert")]
    public async Task<IActionResult> InsertContrato(AdministracionContrato data)
    {
        var r = await _service.InsertarAsync(data, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = "" });
    }

    [HttpPut("update")]
    public async Task<IActionResult> UpdateContrato(AdministracionContrato data)
    {
        var r = await _service.ActualizarAsync(data, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = "" });
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteContrato([FromHeader(Name = "lContratoId")] int contratoId)
    {
        var r = await _service.EliminarAsync(contratoId, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = "" });
    }
}
