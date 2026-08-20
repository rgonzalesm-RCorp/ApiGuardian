using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CasosObservadosController : ControllerBase
{
    private readonly ICasosObservadosRepository _repository;
    private readonly IControlProcesoRepository _controlProcesoRepository;
    private readonly ILogService _log;
    private const string NOMBREARCHIVO = "CasosObservadosController.cs";

    public CasosObservadosController(
        ICasosObservadosRepository repository,
        IControlProcesoRepository controlProcesoRepository,
        ILogService log
    )
    {
        _repository = repository;
        _controlProcesoRepository = controlProcesoRepository;
        _log = log;
    }

    [HttpGet("casos/observados")]
    public async Task<IActionResult> Get(
        [FromHeader(Name = "Usuario")] string Usuario,
        [FromHeader(Name = "LCicloId")] int LCicloId,
        [FromHeader(Name = "Inicio")] DateTime? Inicio,
        [FromHeader(Name = "Fin")] DateTime? Fin
    )
    {
        string logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        string nombreMetodo = "Get()";

        try
        {
            if (LCicloId <= 0 || !Inicio.HasValue || !Fin.HasValue)
            {
                return Ok(new
                {
                    status = false,
                    mensaje = "El ciclo, la fecha de inicio y la fecha de fin son obligatorios.",
                    data = ""
                });
            }

            if (Inicio.Value.Date > Fin.Value.Date)
            {
                return Ok(new
                {
                    status = false,
                    mensaje = "La fecha de inicio no puede ser mayor que la fecha de fin.",
                    data = ""
                });
            }

            var fechaInicio = Inicio.Value.Date;
            var fechaFin = Fin.Value.Date;

            _log.Info(logTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Inicio de metodo [Usuario:{Usuario}, LCicloId:{LCicloId}, Inicio:{fechaInicio:yyyy-MM-dd}, Fin:{fechaFin:yyyy-MM-dd}]");

            var response = await _repository.GetCasosObservados(
                logTransaccionId,
                Usuario,
                LCicloId,
                fechaInicio,
                fechaFin
            );
            var responseSiguientePaso = await _controlProcesoRepository.GetSiguientePaso(
                logTransaccionId,
                Usuario,
                ProcesosDiccionario.COMISIONES,
                LCicloId
            );

            return Ok(new
            {
                status = response.Success,
                mensaje = response.Mensaje,
                data = new
                {
                    casosObservados = response.Data,
                    resumen = response.Resumen,
                    controlPasos = new
                    {
                        ejecutado = !string.Equals(
                            PasosDiccionario.CASOS_OBSERVADOS,
                            responseSiguientePaso.Data.nombre,
                            StringComparison.OrdinalIgnoreCase
                        ),
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

    [HttpPost("procesar")]
    public async Task<IActionResult> Procesar(
        [FromHeader(Name = "Usuario")] string Usuario,
        [FromHeader(Name = "LCicloId")] int LCicloId
    )
    {
        string logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        string nombreMetodo = "Procesar()";
        bool pasoIniciado = false;

        try
        {
            _log.Info(logTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [Usuario:{Usuario}, LCicloId:{LCicloId}]");

            var responseSiguientePaso = await _controlProcesoRepository.GetSiguientePaso(
                logTransaccionId,
                Usuario,
                ProcesosDiccionario.COMISIONES,
                LCicloId
            );

            if (!string.Equals(
                PasosDiccionario.CASOS_OBSERVADOS,
                responseSiguientePaso.Data.nombre,
                StringComparison.OrdinalIgnoreCase
            ))
            {
                return Ok(new
                {
                    status = false,
                    mensaje = "El paso Casos Observados no se encuentra habilitado para este ciclo.",
                    data = ""
                });
            }

            var responseInicioPaso = await _controlProcesoRepository.IniciarPaso(
                logTransaccionId,
                Usuario,
                ProcesosDiccionario.COMISIONES,
                LCicloId,
                PasosDiccionario.CASOS_OBSERVADOS
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

            var response = await _repository.ProcesarCasosObservados(logTransaccionId, Usuario, LCicloId);

            if (!response.Success)
            {
                await _controlProcesoRepository.CancelarPaso(
                    logTransaccionId,
                    Usuario,
                    ProcesosDiccionario.COMISIONES,
                    LCicloId,
                    PasosDiccionario.CASOS_OBSERVADOS
                );
                pasoIniciado = false;

                return Ok(new
                {
                    status = false,
                    mensaje = response.Mensaje,
                    data = ""
                });
            }

            var responseFinPaso = await _controlProcesoRepository.FinalizarPaso(
                logTransaccionId,
                Usuario,
                ProcesosDiccionario.COMISIONES,
                LCicloId,
                PasosDiccionario.CASOS_OBSERVADOS
            );

            if (!responseFinPaso.Success || !(responseFinPaso.Data?.status ?? false))
            {
                await _controlProcesoRepository.CancelarPaso(
                    logTransaccionId,
                    Usuario,
                    ProcesosDiccionario.COMISIONES,
                    LCicloId,
                    PasosDiccionario.CASOS_OBSERVADOS
                );
                pasoIniciado = false;

                return Ok(new
                {
                    status = false,
                    mensaje = responseFinPaso.Data?.mensaje ?? responseFinPaso.Mensaje,
                    data = ""
                });
            }

            pasoIniciado = false;

            return Ok(new
            {
                status = true,
                mensaje = response.Mensaje,
                data = ""
            });
        }
        catch (Exception ex)
        {
            if (pasoIniciado)
            {
                await _controlProcesoRepository.CancelarPaso(
                    logTransaccionId,
                    Usuario,
                    ProcesosDiccionario.COMISIONES,
                    LCicloId,
                    PasosDiccionario.CASOS_OBSERVADOS
                );
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
}
