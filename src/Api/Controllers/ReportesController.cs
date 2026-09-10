using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;

namespace ApiGuardian.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportesController : ControllerBase
{
    private readonly IReportesService _service;

    public ReportesController(IReportesService service) => _service = service;

    [HttpGet("comisiones")]
    public async Task<IActionResult> ReporteComisiones([FromHeader] int lCicloId, [FromHeader] int lContactoId, [FromHeader] string? usuario) => Respuesta(await _service.ReporteComisionesAsync(lCicloId, lContactoId));

    [HttpGet("aplicaciones")]
    public async Task<IActionResult> ReporteAplicaciones([FromHeader] int lCicloId, [FromHeader] int lContactoId, [FromHeader] string? usuario) => Respuesta(await _service.ReporteAplicacionesAsync(lCicloId, lContactoId));

    [HttpGet("descuento/empresa")]
    public async Task<IActionResult> ReporteDescuentoEmpresa([FromHeader] int lCicloId, [FromHeader] int empresaId, [FromHeader] string? usuario) => Respuesta(await _service.ReporteDescuentoEmpresaAsync(lCicloId, empresaId));

    [HttpGet("facturacion")]
    public async Task<IActionResult> ReporteFacturacion([FromHeader] int lCicloId, [FromHeader] int lContactoId, [FromHeader] string? usuario) => Respuesta(await _service.ReporteFacturacionAsync(lCicloId, lContactoId));

    [HttpGet("prorrateo")]
    public async Task<IActionResult> ReporteProrrateo([FromHeader] int lCicloId, [FromHeader] string? usuario) => Respuesta(await _service.ReporteProrrateoAsync(lCicloId));

    [HttpGet("comision/servicio")]
    public async Task<IActionResult> ReporteComisionServicio([FromHeader] int lCicloId, [FromHeader] int empresaId, [FromHeader] string? usuario) => Respuesta(await _service.ReporteComisionServicioAsync(lCicloId, empresaId));

    [HttpGet("pagar/comision")]
    public async Task<IActionResult> ReportePagarComision([FromHeader] int lCicloId, [FromHeader] string? usuario) => Respuesta(await _service.ReportePagarComisionAsync(lCicloId));

    [HttpGet("plan/carrera")]
    public async Task<IActionResult> ReportePlanCarrera([FromHeader] int lCicloId, [FromHeader] string? usuario) => Respuesta(await _service.ReportePlanCarreraAsync(lCicloId));

    [HttpGet("ascenso/rango")]
    public async Task<IActionResult> ReporteAscensoRango([FromHeader] int lCicloId, [FromHeader] string? usuario) => Respuesta(await _service.ReporteAscensoRangoAsync(lCicloId));

    private IActionResult Respuesta((bool Success, string Mensaje, object Data) r) => Ok(new { status = r.Success, mensaje = r.Mensaje, data = r.Data });
}
