using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministracionDetalleFacturaController : ControllerBase
{
    private readonly IAdministracionDetalleFacturaService _service;

    public AdministracionDetalleFacturaController(IAdministracionDetalleFacturaService service) => _service = service;

    [HttpGet("paginacion")]
    public async Task<IActionResult> GetPaginacion([FromHeader] int page, [FromHeader] int pageSize)
    {
        var r = await _service.ObtenerPaginadoAsync(page, pageSize, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = r.Data });
    }

    [HttpPost("insert")]
    public async Task<IActionResult> Insert([FromBody] AdministracionDetalleFactura data)
    {
        var r = await _service.InsertarAsync(data, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje });
    }

    [HttpPut("update")]
    public async Task<IActionResult> Update([FromBody] AdministracionDetalleFactura data)
    {
        var r = await _service.ActualizarAsync(data, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje });
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> Delete([FromHeader] int lDetalleFacturaId)
    {
        var r = await _service.EliminarAsync(lDetalleFacturaId, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje });
    }

    [HttpGet("tipo/comision")]
    public async Task<IActionResult> GetTipoComision()
    {
        var r = await _service.ObtenerTiposComisionAsync(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = r.Data });
    }
}
