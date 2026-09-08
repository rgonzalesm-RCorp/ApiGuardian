using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
namespace CleanDapperApi.Api.Controllers;
[ApiController]
[Route("api/[controller]")]
public class AdministracionCuentaBancoController : ControllerBase
{
    private readonly IAdministracionCuentaBancoService _service;
    public AdministracionCuentaBancoController(IAdministracionCuentaBancoService service) => _service = service;
    [HttpGet("id")]
    public async Task<IActionResult> GetCuentaBanco([FromHeader(Name = "lContactoId")] int lContactoId)
    { var r = await _service.ObtenerAsync(lContactoId, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()); return Ok(new { status = r.Success, mensaje = r.Mensaje, data = r.Data }); }
    [HttpGet]
    public async Task<IActionResult> GetAllCuentaBanco([FromHeader(Name = "page")] int page, [FromHeader(Name = "pageSize")] int pageSize, [FromHeader(Name = "search")] string? search)
    { var r = await _service.ObtenerPaginadoAsync(page, pageSize, search, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()); return Ok(new { status = r.Success, mensaje = r.Mensaje, data = r.Data }); }
    [HttpPut("update")]
    public async Task<IActionResult> UpdateCuentaBanco(DataCuentaBanco data)
    { var r = await _service.ActualizarAsync(data, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()); return Ok(new { status = r.Success, mensaje = r.Mensaje, data = "" }); }
}
