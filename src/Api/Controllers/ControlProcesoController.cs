using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ControlProcesoController : ControllerBase
{
    private readonly ILogService _log;
    private readonly IControlProcesoRepository _controlProcesoRepository;
    private readonly string NOMBREARCHIVO = "ControlProcesoController.cs";

    public ControlProcesoController(ILogService log, IControlProcesoRepository controlProcesoRepository)
    {
        _log = log;
        _controlProcesoRepository = controlProcesoRepository;
    }

    [HttpGet("configuracion")]
    public async Task<IActionResult> GetConfiguracionProcesos([FromHeader(Name = "Usuario")] string Usuario)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreMetodo = "GetConfiguracionProcesos()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [usuario:{Usuario}]");

            var response = await _controlProcesoRepository.GetConfiguracionProcesos(logTransaccionId.ToString(), Usuario);

            return Ok(new
            {
                status = response.Success,
                mensaje = response.Mensaje,
                data = response.Data
            });
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId.ToString(), NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return Ok(new
            {
                status = false,
                mensaje = ex.Message,
                data = ""
            });
        }
    }

    [HttpPost("configuracion")]
    public async Task<IActionResult> GuardarConfiguracionProceso([FromHeader(Name = "Usuario")] string Usuario, [FromBody] ControlProcesoConfiguracion request)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreMetodo = "GuardarConfiguracionProceso()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [ProcesoId:{request.ProcesoId}]");

            var response = await _controlProcesoRepository.GuardarConfiguracionProceso(
                logTransaccionId.ToString(),
                Usuario,
                request
            );

            return Ok(new
            {
                status = response.Success,
                mensaje = response.Mensaje,
                data = response.Data
            });
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId.ToString(), NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return Ok(new
            {
                status = false,
                mensaje = ex.Message,
                data = ""
            });
        }
    }

    [HttpDelete("configuracion")]
    public async Task<IActionResult> DeleteConfiguracionProceso([FromHeader(Name = "Usuario")] string Usuario, [FromHeader(Name = "ProcesoId")] int ProcesoId)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreMetodo = "DeleteConfiguracionProceso()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [ProcesoId:{ProcesoId}, usuario:{Usuario}]");

            var response = await _controlProcesoRepository.DeleteConfiguracionProceso(
                logTransaccionId.ToString(),
                Usuario,
                ProcesoId
            );

            return Ok(new
            {
                status = response.Success,
                mensaje = response.Mensaje,
                data = ""
            });
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId.ToString(), NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return Ok(new
            {
                status = false,
                mensaje = ex.Message,
                data = ""
            });
        }
    }

    [HttpGet("ciclo")]
    public async Task<IActionResult> GetControlProcesoCiclo([FromHeader(Name = "lCicloId")] int lCicloId, [FromHeader(Name = "Usuario")] string Usuario)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreMetodo = "GetControlProcesoCiclo()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [lCicloId:{lCicloId}, usuario:{Usuario}]");

            var response = await _controlProcesoRepository.GetResumenProcesoCiclo(
                logTransaccionId.ToString(),
                Usuario,
                ProcesosDiccionario.COMISIONES,
                lCicloId
            );

            return Ok(new
            {
                status = response.Success,
                mensaje = response.Mensaje,
                data = response.Data
            });
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId.ToString(), NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return Ok(new
            {
                status = false,
                mensaje = ex.Message,
                data = ""
            });
        }
    }

    [HttpPost("reset/ciclo")]
    public async Task<IActionResult> ResetControlProcesoCiclo([FromHeader(Name = "lCicloId")] int lCicloId, [FromHeader(Name = "Usuario")] string Usuario)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreMetodo = "ResetControlProcesoCiclo()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [lCicloId:{lCicloId}, usuario:{Usuario}]");

            var response = await _controlProcesoRepository.ReiniciarCiclo(
                logTransaccionId.ToString(),
                Usuario,
                ProcesosDiccionario.COMISIONES,
                lCicloId
            );

            string mensaje = !string.IsNullOrWhiteSpace(response.Data.mensaje)
                ? response.Data.mensaje
                : response.Mensaje;

            return Ok(new
            {
                status = response.Success && response.Data.status,
                mensaje,
                data = response.Data
            });
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId.ToString(), NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return Ok(new
            {
                status = false,
                mensaje = ex.Message,
                data = ""
            });
        }
    }

    [HttpPost("cerrar/ciclo")]
    public async Task<IActionResult> CerrarControlProcesoCiclo([FromHeader(Name = "lCicloId")] int lCicloId, [FromHeader(Name = "Usuario")] string Usuario)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreMetodo = "CerrarControlProcesoCiclo()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [lCicloId:{lCicloId}, usuario:{Usuario}]");

            var response = await _controlProcesoRepository.CerrarCiclo(
                logTransaccionId.ToString(),
                Usuario,
                ProcesosDiccionario.COMISIONES,
                lCicloId
            );

            string mensaje = !string.IsNullOrWhiteSpace(response.Data.mensaje)
                ? response.Data.mensaje
                : response.Mensaje;

            return Ok(new
            {
                status = response.Success && response.Data.status,
                mensaje,
                data = response.Data
            });
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId.ToString(), NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return Ok(new
            {
                status = false,
                mensaje = ex.Message,
                data = ""
            });
        }
    }
}
