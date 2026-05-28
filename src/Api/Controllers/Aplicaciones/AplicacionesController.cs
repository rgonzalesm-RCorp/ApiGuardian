using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/aplicaciones")]
public class AplicacionesController : ControllerBase
{
    private readonly IAplicacionesRepository _aplicacionesRepository;
    private readonly ILogService _log;
    private const string NombreArchivo = "AplicacionesController.cs";

    public AplicacionesController(IAplicacionesRepository aplicacionesRepository, ILogService log)
    {
        _aplicacionesRepository = aplicacionesRepository;
        _log = log;
    }

    [HttpGet("preview")]
    public async Task<IActionResult> Preview([FromHeader(Name = "lCicloId")] int lCicloId)
    {
        var logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        const string metodo = "Preview()";

        try
        {
            _log.Info(logTransaccionId, NombreArchivo, metodo, $"Inicio preview de aplicaciones. lCicloId:{lCicloId}");
            var response = await _aplicacionesRepository.Preview(logTransaccionId, lCicloId);

            return Ok(new
            {
                status = response.Success,
                mensaje = response.Mensaje,
                data = response.Data
            });
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId, NombreArchivo, metodo, "Error en preview de aplicaciones", ex);
            return Ok(new
            {
                status = false,
                mensaje = ex.Message,
                data = new AplicacionesPreviewResponse
                {
                    LCicloId = lCicloId,
                    Preview = true,
                    ErrorGrave = true,
                    ErrorGraveMensaje = ex.Message
                }
            });
        }
    }

    [HttpPost("apply")]
    public async Task<IActionResult> Apply([FromBody] AplicacionesExecuteRequest request)
    {
        var logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        const string metodo = "Apply()";

        try
        {
            _log.Info(logTransaccionId, NombreArchivo, metodo, $"Inicio apply de aplicaciones. lCicloId:{request.LCicloId}");
            var response = await _aplicacionesRepository.Apply(logTransaccionId, request.LCicloId);

            return Ok(new
            {
                status = response.Success,
                mensaje = response.Mensaje,
                data = response.Data
            });
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId, NombreArchivo, metodo, "Error en apply de aplicaciones", ex);
            return Ok(new
            {
                status = false,
                mensaje = ex.Message,
                data = new AplicacionesApplyResponse
                {
                    LCicloId = request.LCicloId,
                    Preview = false,
                    ErrorGrave = true,
                    ErrorGraveMensaje = ex.Message
                }
            });
        }
    }
}
