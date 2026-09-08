using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministracionDescuentoComisionController : ControllerBase
{
    private readonly IAdministracionDescuentoComisionService _service;

    public AdministracionDescuentoComisionController(IAdministracionDescuentoComisionService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAllAdministracionCObservacionComision(
        [FromHeader(Name = "lContactoId")] int lContactoId,
        [FromHeader(Name = "lCicloId")] int lCicloId,
        [FromHeader(Name = "lSemanaId")] int lSemanaId)
    {
        var r = await _service.ObtenerAsync(lContactoId, lCicloId, lSemanaId, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = r.Data });
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> EliminarDescuento(
        [FromHeader(Name = "lDescuentoDetalleId")] int lDescuentoDetalleId,
        [FromHeader(Name = "lContactoId")] int lContactoId,
        [FromHeader(Name = "lCicloId")] int lCicloId,
        [FromHeader(Name = "usuario")] string? usuario)
    {
        var r = await _service.EliminarAsync(lDescuentoDetalleId, lContactoId, lCicloId, usuario, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = "" });
    }

    [HttpPost("insert")]
    public async Task<IActionResult> InsertarDescuento(DataDescuento dataDescuento)
    {
        var r = await _service.InsertarAsync(dataDescuento, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = "" });
    }
}
