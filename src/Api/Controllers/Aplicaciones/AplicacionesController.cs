using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Controller]
[Route("api/aplicaciones")]
public class AplicacionesController : ControllerBase
{
    private readonly IAplicacionesRepositorio _repositorioAplicaciones;
    private readonly ILogService _registro;
    private const string NombreArchivo = "AplicacionesController.cs";

    public AplicacionesController(IAplicacionesRepositorio repositorioAplicaciones, ILogService registro)
    {
        _repositorioAplicaciones = repositorioAplicaciones;
        _registro = registro;
    }

    [HttpGet("vista-previa")]
    public async Task<IActionResult> VistaPrevia([FromHeader(Name = "lCicloId")] int lCicloId)
    {
        var idTransaccionLog = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        const string nombreMetodo = "VistaPrevia()";

        try
        {
            _registro.Info(idTransaccionLog, NombreArchivo, nombreMetodo, $"Inicio vista previa de aplicaciones. lCicloId:{lCicloId}");
            var respuesta = await _repositorioAplicaciones.VistaPrevia(idTransaccionLog, lCicloId);

            return Ok(new
            {
                estado = respuesta.Exito,
                mensaje = respuesta.Mensaje,
                datos = respuesta.Datos
            });
        }
        catch (Exception excepcion)
        {
            _registro.Error(idTransaccionLog, NombreArchivo, nombreMetodo, "Error en vista previa de aplicaciones", excepcion);
            return Ok(new
            {
                estado = false,
                mensaje = excepcion.Message,
                datos = new RespuestaVistaPreviaAplicaciones
                {
                    LCicloId = lCicloId,
                    VistaPrevia = true,
                    ErrorGrave = true,
                    ErrorGraveMensaje = excepcion.Message
                }
            });
        }
    }

    [HttpPost("aplicar")]
    public async Task<IActionResult> Aplicar([FromBody] SolicitudEjecucionAplicaciones solicitud)
    {
        var idTransaccionLog = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        const string nombreMetodo = "Aplicar()";

        try
        {
            _registro.Info(idTransaccionLog, NombreArchivo, nombreMetodo, $"Inicio aplicación de aplicaciones. lCicloId:{solicitud.LCicloId}");
            var respuesta = await _repositorioAplicaciones.Aplicar(idTransaccionLog, solicitud.LCicloId);

            return Ok(new
            {
                estado = respuesta.Exito,
                mensaje = respuesta.Mensaje,
                datos = respuesta.Datos
            });
        }
        catch (Exception excepcion)
        {
            _registro.Error(idTransaccionLog, NombreArchivo, nombreMetodo, "Error en aplicación de aplicaciones", excepcion);
            return Ok(new
            {
                estado = false,
                mensaje = excepcion.Message,
                datos = new RespuestaEjecucionAplicaciones
                {
                    LCicloId = solicitud.LCicloId,
                    VistaPrevia = false,
                    ErrorGrave = true,
                    ErrorGraveMensaje = excepcion.Message
                }
            });
        }
    }
}
