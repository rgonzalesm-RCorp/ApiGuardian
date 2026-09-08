using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministracionBuscarAsesorController : ControllerBase
{
    private readonly IAdministracionBuscarAsesorService _service;
    public AdministracionBuscarAsesorController(IAdministracionBuscarAsesorService service)
    {
        _service = service;
    }
    [HttpGet]
    public async Task<IActionResult> GetAsesoreSieteNiveles([FromHeader(Name = "lContactoId")] int lContactoId)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var resultado = await _service.ObtenerAsesoresAsync(lContactoId, logTransaccionId.ToString());
        return Ok(new { status = resultado.Success, mensaje = resultado.Mensaje, data = resultado.Data });
    }
}
