using ApiGuardian.Application.Interfaces.Simulacion;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Quartz;

namespace ApiGuardian.Api.Controllers.Simulacion;

// Disparo manual del job de Simulacion. Responde de inmediato: el trabajo corre en Quartz.
// Desactivado por defecto (Simulacion:HabilitarEndpointManual). La API no tiene autenticacion:
// no activarlo fuera de pruebas sin una proteccion adicional.
[ApiController]
[Route("api/simulacion")]
public sealed class SimulacionController : ControllerBase
{
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly ConfiguracionSimulacion _configuracion;

    public SimulacionController(ISchedulerFactory schedulerFactory, IOptions<ConfiguracionSimulacion> configuracion)
    {
        _schedulerFactory = schedulerFactory;
        _configuracion = configuracion.Value;
    }

    [HttpPost("ejecutar")]
    public async Task<IActionResult> Ejecutar()
    {
        if (!_configuracion.HabilitarEndpointManual)
            return StatusCode(403, new { status = false, mensaje = "El disparo manual de Simulación está desactivado." });

        var scheduler = await _schedulerFactory.GetScheduler();
        await scheduler.TriggerJob(new JobKey("SimulacionJob"));

        return Accepted(new { status = true, mensaje = "Corrida de Simulación disparada. Revisar comisiones_light_ejecucion." });
    }
}
