using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/MigracionAplicacionesProrrateo")]
public sealed class MigracionAplicacionesProrrateoController : ControllerBase
{
    private readonly IMigracionAplicacionesProrrateoRepository _repository;
    public MigracionAplicacionesProrrateoController(IMigracionAplicacionesProrrateoRepository repository) => _repository = repository;

    [HttpPost("vista-previa")]
    public async Task<IActionResult> VistaPrevia([FromBody] SolicitudMigracionAplicacionesProrrateo solicitud)
    {
        var r = await _repository.VistaPreviaAsync(solicitud);
        return Ok(new { estado = r.Exito, mensaje = r.Mensaje, datos = r.Datos });
    }

    [HttpPost("ejecutar")]
    public async Task<IActionResult> Ejecutar([FromBody] SolicitudMigracionAplicacionesProrrateo solicitud)
    {
        var r = await _repository.EjecutarAsync(solicitud);
        return Ok(new { estado = r.Exito, mensaje = r.Mensaje, datos = r.Datos });
    }
}
