using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UtilsController : ControllerBase
{
    private readonly IUtilsService _service;
    public UtilsController(IUtilsService service) => _service = service;

    [HttpGet("administracion/semana/ciclo")]
    public async Task<IActionResult> GetAdministracionSemanaCiclo([FromHeader(Name = "lCicloId")] int cicloId) => Respuesta(await _service.ObtenerSemanaCicloAsync(cicloId));
    [HttpGet("administracion/departamento")]
    public async Task<IActionResult> GetDepartamento([FromHeader(Name = "lPaisId")] int paisId = 2) => Respuesta(await _service.ObtenerDepartamentoAsync(paisId));
    [HttpGet("administracion/tipo/contrato")]
    public async Task<IActionResult> GetTipoContrato() => Respuesta(await _service.ObtenerTipoContratoAsync());
    [HttpGet("administracion/estado/contrato")]
    public async Task<IActionResult> GetEstadoContrato() => Respuesta(await _service.ObtenerEstadoContratoAsync());
    [HttpGet("administracion/tipo/baja")]
    public async Task<IActionResult> GetTipoBaja() => Respuesta(await _service.ObtenerTipoBajaAsync());
    [HttpGet("administracion/pais")]
    public async Task<IActionResult> GetPais() => Respuesta(await _service.ObtenerPaisAsync());
    [HttpGet("tipo/descuento")]
    public async Task<IActionResult> GetTipoDescuento() => Respuesta(await _service.ObtenerTipoDescuentoAsync());

    private IActionResult Respuesta((bool Success, string Mensaje, object Data) r) => Ok(new { status = r.Success, mensaje = r.Mensaje, data = r.Data });
}
