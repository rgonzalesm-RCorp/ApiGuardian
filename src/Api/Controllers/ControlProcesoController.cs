using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ControlProcesoController : ControllerBase
{
    private readonly IControlProcesoService _service;
    public ControlProcesoController(IControlProcesoService service) => _service = service;

    [HttpGet("configuracion")]
    public async Task<IActionResult> GetConfiguracionProcesos([FromHeader(Name = "Usuario")] string usuario) => Respuesta(await _service.ObtenerConfiguracionAsync(usuario));
    [HttpPost("configuracion")]
    public async Task<IActionResult> GuardarConfiguracionProceso([FromHeader(Name = "Usuario")] string usuario, [FromBody] ControlProcesoConfiguracion request) => Respuesta(await _service.GuardarConfiguracionAsync(usuario, request));
    [HttpDelete("configuracion")]
    public async Task<IActionResult> DeleteConfiguracionProceso([FromHeader(Name = "Usuario")] string usuario, [FromHeader(Name = "ProcesoId")] int procesoId) => Respuesta(await _service.EliminarConfiguracionAsync(usuario, procesoId));
    [HttpGet("ciclo")]
    public async Task<IActionResult> GetControlProcesoCiclo([FromHeader(Name = "lCicloId")] int cicloId, [FromHeader(Name = "Usuario")] string usuario) => Respuesta(await _service.ObtenerCicloAsync(usuario, cicloId));
    [HttpPost("reset/ciclo")]
    public async Task<IActionResult> ResetControlProcesoCiclo([FromHeader(Name = "lCicloId")] int cicloId, [FromHeader(Name = "Usuario")] string usuario, [FromHeader(Name = "Inicio")] string inicio, [FromHeader(Name = "Fin")] string fin) => Respuesta(await _service.ReiniciarCicloAsync(usuario, cicloId, inicio, fin));
    [HttpPost("cerrar/ciclo")]
    public async Task<IActionResult> CerrarControlProcesoCiclo([FromHeader(Name = "lCicloId")] int cicloId, [FromHeader(Name = "Usuario")] string usuario) => Respuesta(await _service.CerrarCicloAsync(usuario, cicloId));

    private IActionResult Respuesta((bool Success, string Mensaje, object Data) r) => Ok(new { status = r.Success, mensaje = r.Mensaje, data = r.Data });
}
