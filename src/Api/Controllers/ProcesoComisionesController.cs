using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProcesoComisionesController : ControllerBase
{
    private readonly IProcesoComisionesService _service;
    public ProcesoComisionesController(IProcesoComisionesService service) => _service = service;

    [HttpGet("vta/cnx")]
    public async Task<IActionResult> GetVentaCnx([FromHeader(Name = "lCicloId")] int cicloId) => Respuesta(await _service.ObtenerVentaCnxAsync(cicloId));
    [HttpPost("ejemplo")]
    public Task<IActionResult> Ejecutar() => Task.FromResult<IActionResult>(Ok(new { ex = true }));
    [HttpGet("vta/rezagadas")]
    public async Task<IActionResult> GetVtaRezagadas([FromHeader(Name = "lCicloId")] int cicloId, [FromHeader(Name = "Usuario")] string usuario) => Respuesta(await _service.ObtenerVtaRezagadasAsync(usuario, cicloId));
    [HttpGet("venta/personal")]
    public async Task<IActionResult> GetVentaPersonal([FromHeader(Name = "lCicloId")] int cicloId, [FromHeader(Name = "Usuario")] string usuario) => Respuesta(await _service.ObtenerVentaPersonalAsync(usuario, cicloId));
    [HttpPost("save/vta/proceso")]
    public async Task<IActionResult> SaveVenta(RequestGuardarVentaGRD data) { var r = await _service.GuardarVentaAsync(data, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()); return Ok(new { status = r.Success, mensaje = r.Mensaje, data = "" }); }
    [HttpPost("save/vta/personal")]
    public async Task<IActionResult> SaveVtaPersonal(RequestSaveVtaPersonal request) { var r = await _service.GuardarVentaPersonalAsync(request); return Ok(new { status = r.Success, mensaje = r.Mensaje, data = "" }); }
    [HttpGet("venta/grupo")]
    public async Task<IActionResult> GetVentaGrupo([FromHeader(Name = "lCicloId")] int cicloId, [FromHeader(Name = "Usuario")] string usuario) => Respuesta(await _service.ObtenerVentaGrupoAsync(usuario, cicloId));
    [HttpPost("save/vta/grupo")]
    public async Task<IActionResult> SaveVtaGrupo(RequestGuardarVentaGrupo request) { var r = await _service.GuardarVentaGrupoAsync(request); return Ok(new { status = r.Success, mensaje = r.Mensaje, data = "" }); }

    private IActionResult Respuesta((bool Success, string Mensaje, object Data) r) => Ok(new { status = r.Success, mensaje = r.Mensaje, data = r.Data });
}
