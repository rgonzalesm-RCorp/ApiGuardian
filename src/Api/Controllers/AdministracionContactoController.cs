using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministracionContactoController : ControllerBase
{
    private readonly IAdministracionContactoService _service;
    public AdministracionContactoController(IAdministracionContactoService service) => _service = service;
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page, [FromQuery] int pageSize, [FromQuery] string? search)
    { var r = await _service.ObtenerAsync(page, pageSize, search, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()); return Ok(new { status = r.Success, mensaje = r.Mensaje, data = r.Data }); }
    [HttpPost("insert")]
    public async Task<IActionResult> InsertContacto(AdministracionContacto data)
    { var r = await _service.InsertarAsync(data, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()); return Ok(new { status = r.Success, mensaje = r.Mensaje, data = "" }); }
    [HttpPut("update")]
    public async Task<IActionResult> UpdateContacto(AdministracionContacto data)
    { var r = await _service.ActualizarAsync(data, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()); return Ok(new { status = r.Success, mensaje = r.Mensaje, data = "" }); }
    [HttpDelete("baja")]
    public async Task<IActionResult> BajaContacto(AdministracionContactoBaja data)
    { var r = await _service.DarDeBajaAsync(data, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()); return Ok(new { status = r.Success, mensaje = r.Mensaje, data = "" }); }
    [HttpGet("verificar/estado")]
    public async Task<IActionResult> VerificarEstadoContacto([FromHeader(Name = "Usuario")] string Usuario, [FromHeader(Name = "Documento")] string Documento)
    { var r = await _service.VerificarEstadoAsync(Usuario, Documento, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()); return Ok(new { status = r.Success, mensaje = r.Mensaje, data = r.Data }); }
}
