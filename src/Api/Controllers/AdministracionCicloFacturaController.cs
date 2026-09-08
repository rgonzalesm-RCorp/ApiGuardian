using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministracionCicloFacturaController : ControllerBase
{
    private readonly IAdministracionCicloFacturaService _service;
    public AdministracionCicloFacturaController(IAdministracionCicloFacturaService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAllAdministracionCiclofactura([FromHeader(Name = "page")] int page, [FromHeader(Name = "pageSize")] int pageSize, [FromHeader(Name = "lCicloId")] int lCicloId)
    {
        var r = await _service.ObtenerAsync(page, pageSize, lCicloId, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = r.Data });
    }

    [HttpPost("register")]
    public async Task<IActionResult> InsertAdministracionCiclofactura(AdministracionCicloFactura data)
    {
        var r = await _service.InsertarAsync(data, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = "" });
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteAdministracionCiclofactura([FromHeader(Name = "lciclofactura")] int lciclofactura, [FromHeader(Name = "usuario")] string? usuario)
    {
        var r = await _service.EliminarAsync(lciclofactura, usuario, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = "" });
    }
}
