using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;

namespace ApiGuardian.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReporteMensualController : ControllerBase
{
    private readonly IReporteMensualService _service;

    public ReporteMensualController(IReporteMensualService service) => _service = service;

    [HttpGet("comisiones")]
    public async Task<IActionResult> ReporteMensual([FromHeader] int lCicloId, [FromHeader] int lContactoId, [FromHeader] string? usuario) => Respuesta(await _service.ReporteMensualAsync(lCicloId, lContactoId));

    private IActionResult Respuesta((bool Success, string Mensaje, object Data) r) => Ok(new { status = r.Success, mensaje = r.Mensaje, data = r.Data });
}
