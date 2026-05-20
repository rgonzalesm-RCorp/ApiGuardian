using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using Newtonsoft.Json;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministracionHabilitacionComisionController : ControllerBase
{
    private readonly IAdministracionHabilitacionComisionRepository _repository;
    private readonly IControlProcesoRepository _controlProcesoRepository;
    private readonly ILogService _log;
    private readonly string NOMBREARCHIVO = "AdministracionHabilitacionComisionController.cs";

    public AdministracionHabilitacionComisionController(
        IAdministracionHabilitacionComisionRepository repository,
        IControlProcesoRepository controlProcesoRepository,
        ILogService log
    )
    {
        _repository = repository;
        _controlProcesoRepository = controlProcesoRepository;
        _log = log;
    }

    [HttpGet("GetHabilitaciones")]
    public async Task<IActionResult> GetHabilitaciones(
        [FromHeader(Name = "LogTransaccionId")] string? LogTransaccionId,
        [FromHeader(Name = "Usuario")] string Usuario,
        [FromHeader(Name = "LCicloId")] int LCicloId
    )
    {
        string logTransaccionId = string.IsNullOrWhiteSpace(LogTransaccionId)
            ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()
            : LogTransaccionId;
        string nombreMetodo = "GetHabilitaciones()";

        try
        {
            _log.Info(
                logTransaccionId,
                NOMBREARCHIVO,
                nombreMetodo,
                $"Inicio de metodo [Usuario:{Usuario}, LCicloId:{LCicloId}]"
            );

            var response = await _repository.GetHabilitaciones(logTransaccionId, Usuario, LCicloId);
            var responseSiguientePaso = await _controlProcesoRepository.GetSiguientePaso(
                logTransaccionId,
                Usuario,
                ProcesosDiccionario.COMISIONES,
                LCicloId
            );

            bool ejecutado = PasoRegistroHabilitacionesEjecutado(responseSiguientePaso.Data);

            return Ok(new
            {
                status = response.Success,
                mensaje = response.Mensaje,
                data = new
                {
                    habilitaciones = response.Data,
                    controlPasos = new
                    {
                        ejecutado,
                        data = responseSiguientePaso.Data
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return Ok(new
            {
                status = false,
                mensaje = ex.Message,
                data = ""
            });
        }
    }

    [HttpPost("SaveHabilitaciones")]
    public async Task<IActionResult> SaveHabilitaciones(
        [FromHeader(Name = "LogTransaccionId")] string? LogTransaccionId,
        [FromHeader(Name = "Usuario")] string Usuario,
        [FromHeader(Name = "LCicloId")] int LCicloId,
        [FromBody] List<ItemHabilitacionComision> listado
    )
    {
        string logTransaccionId = string.IsNullOrWhiteSpace(LogTransaccionId)
            ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()
            : LogTransaccionId;
        string nombreMetodo = "SaveHabilitaciones()";
        bool pasoIniciado = false;

        try
        {
            _log.Info(
                logTransaccionId,
                NOMBREARCHIVO,
                nombreMetodo,
                $"Inicio de metodo [Usuario:{Usuario}, LCicloId:{LCicloId}, data:{JsonConvert.SerializeObject(listado, Formatting.Indented)}]"
            );

            var responseSiguientePaso = await _controlProcesoRepository.GetSiguientePaso(
                logTransaccionId,
                Usuario,
                ProcesosDiccionario.COMISIONES,
                LCicloId
            );

            if (!PuedeGuardarHabilitaciones(responseSiguientePaso.Data))
            {
                return Ok(new
                {
                    status = false,
                    mensaje = "Debe completar los pasos previos antes de registrar habilitaciones.",
                    data = ""
                });
            }

            bool debeEjecutarPaso = string.Equals(
                responseSiguientePaso.Data?.nombre,
                PasosDiccionario.REGISTRO_HABILITACIONES,
                StringComparison.OrdinalIgnoreCase
            );

            if (debeEjecutarPaso)
            {
                var responseInicioPaso = await _controlProcesoRepository.IniciarPaso(
                    logTransaccionId,
                    Usuario,
                    ProcesosDiccionario.COMISIONES,
                    LCicloId,
                    PasosDiccionario.REGISTRO_HABILITACIONES
                );

                if (!responseInicioPaso.Success || !(responseInicioPaso.Data?.status ?? false))
                {
                    return Ok(new
                    {
                        status = false,
                        mensaje = responseInicioPaso.Data?.mensaje ?? responseInicioPaso.Mensaje,
                        data = ""
                    });
                }

                pasoIniciado = true;
            }

            var response = await _repository.SaveHabilitaciones(logTransaccionId, Usuario, LCicloId, listado);

            if (!response.Success)
            {
                if (pasoIniciado)
                {
                    await _controlProcesoRepository.CancelarPaso(logTransaccionId, Usuario, ProcesosDiccionario.COMISIONES, LCicloId, PasosDiccionario.REGISTRO_HABILITACIONES);
                    pasoIniciado = false;
                }

                return Ok(new
                {
                    status = response.Success,
                    mensaje = response.Mensaje,
                    data = ""
                });
            }

            if (debeEjecutarPaso)
            {
                var responsePaso = await _controlProcesoRepository.FinalizarPaso(
                    logTransaccionId,
                    Usuario,
                    ProcesosDiccionario.COMISIONES,
                    LCicloId,
                    PasosDiccionario.REGISTRO_HABILITACIONES
                );

                if (!responsePaso.Success || !(responsePaso.Data?.status ?? false))
                {
                    string mensajePaso = responsePaso.Data?.mensaje
                        ?? responsePaso.Data?.mensajes
                        ?? "Las habilitaciones fueron guardadas, pero no se pudo actualizar el paso del proceso.";

                    return Ok(new
                    {
                        status = false,
                        mensaje = mensajePaso,
                        data = ""
                    });
                }

                pasoIniciado = false;
            }

            return Ok(new
            {
                status = response.Success,
                mensaje = response.Mensaje,
                data = ""
            });
        }
        catch (Exception ex)
        {
            if (pasoIniciado)
            {
                await _controlProcesoRepository.CancelarPaso(logTransaccionId, Usuario, ProcesosDiccionario.COMISIONES, LCicloId, PasosDiccionario.REGISTRO_HABILITACIONES);
            }

            _log.Error(logTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return Ok(new
            {
                status = false,
                mensaje = ex.Message,
                data = ""
            });
        }
    }

    [HttpPut("UpdateHabilitacion")]
    public async Task<IActionResult> UpdateHabilitacion(
        [FromHeader(Name = "LogTransaccionId")] string? LogTransaccionId,
        [FromHeader(Name = "Usuario")] string Usuario,
        [FromBody] ItemHabilitacionComision data
    )
    {
        string logTransaccionId = string.IsNullOrWhiteSpace(LogTransaccionId)
            ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()
            : LogTransaccionId;
        string nombreMetodo = "UpdateHabilitacion()";

        try
        {
            _log.Info(
                logTransaccionId,
                NOMBREARCHIVO,
                nombreMetodo,
                $"Inicio de metodo [Usuario:{Usuario}, data:{JsonConvert.SerializeObject(data, Formatting.Indented)}]"
            );

            var response = await _repository.UpdateHabilitacion(logTransaccionId, Usuario, data);

            return Ok(new
            {
                status = response.Success,
                mensaje = response.Mensaje,
                data = ""
            });
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return Ok(new
            {
                status = false,
                mensaje = ex.Message,
                data = ""
            });
        }
    }

    [HttpDelete("DeleteHabilitacion")]
    public async Task<IActionResult> DeleteHabilitacion(
        [FromHeader(Name = "LogTransaccionId")] string? LogTransaccionId,
        [FromHeader(Name = "Usuario")] string Usuario,
        [FromHeader(Name = "LHabilitacionId")] int LHabilitacionId
    )
    {
        string logTransaccionId = string.IsNullOrWhiteSpace(LogTransaccionId)
            ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()
            : LogTransaccionId;
        string nombreMetodo = "DeleteHabilitacion()";

        try
        {
            _log.Info(
                logTransaccionId,
                NOMBREARCHIVO,
                nombreMetodo,
                $"Inicio de metodo [Usuario:{Usuario}, LHabilitacionId:{LHabilitacionId}]"
            );

            var response = await _repository.DeleteHabilitacion(logTransaccionId, Usuario, LHabilitacionId);

            return Ok(new
            {
                status = response.Success,
                mensaje = response.Mensaje,
                data = ""
            });
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return Ok(new
            {
                status = false,
                mensaje = ex.Message,
                data = ""
            });
        }
    }

    private static bool PuedeGuardarHabilitaciones(ItemControlProcesoNext? siguientePaso)
    {
        if (siguientePaso == null || string.IsNullOrWhiteSpace(siguientePaso.nombre))
        {
            return true;
        }

        return !EsPasoPrevioRegistroHabilitaciones(siguientePaso.nombre);
    }

    private static bool PasoRegistroHabilitacionesEjecutado(ItemControlProcesoNext? siguientePaso)
    {
        if (siguientePaso == null || string.IsNullOrWhiteSpace(siguientePaso.nombre))
        {
            return true;
        }

        if (string.Equals(
            siguientePaso.nombre,
            PasosDiccionario.REGISTRO_HABILITACIONES,
            StringComparison.OrdinalIgnoreCase
        ))
        {
            return false;
        }

        return !EsPasoPrevioRegistroHabilitaciones(siguientePaso.nombre);
    }

    private static bool EsPasoPrevioRegistroHabilitaciones(string? paso)
    {
        if (string.IsNullOrWhiteSpace(paso))
        {
            return false;
        }

        return string.Equals(paso, PasosDiccionario.OBTENER_VENTAS, StringComparison.OrdinalIgnoreCase)
            || string.Equals(paso, PasosDiccionario.ADICIONAR_VENTAS, StringComparison.OrdinalIgnoreCase)
            || string.Equals(paso, PasosDiccionario.VENTAS_ESPECIALES, StringComparison.OrdinalIgnoreCase)
            || string.Equals(paso, PasosDiccionario.COMISION_DIRECTA, StringComparison.OrdinalIgnoreCase);
    }
}
