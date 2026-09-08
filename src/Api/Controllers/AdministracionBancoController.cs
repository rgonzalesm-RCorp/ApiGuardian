using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministracionBancoController : ControllerBase
{
    private readonly IAdministracionBancoService _service;
    public AdministracionBancoController(IAdministracionBancoService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAllCuentaBanco()
    {
        var r = await _service.ObtenerBancosAsync(DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = r.Data });
    }
    [HttpGet("moneda")]
    public async Task<IActionResult> GetAllMoneda()
    {
        var r = await _service.ObtenerMonedasAsync(DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = r.Data });
    }
    [HttpPut("update")]
    public async Task<IActionResult> UpdateBanco(AdministracionBanco data)
    {
        var r = await _service.ActualizarAsync(data, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = "" });
    }
    [HttpPost("insert")]
    public async Task<IActionResult> InsertBanco(AdministracionBanco data)
    {
        var r = await _service.InsertarAsync(data, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = "" });
    }
    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteBanco([FromHeader(Name = "lBancoId")] int lBancoId, [FromHeader(Name = "usuario")] string? usuario)
    {
        var r = await _service.EliminarAsync(lBancoId, usuario, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        return Ok(new { status = r.Success, mensaje = r.Mensaje, data = "" });
    }
}
