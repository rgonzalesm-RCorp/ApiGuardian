using ApiGuardian.Application.Interfaces.Simulacion;
using Microsoft.Extensions.Logging;
using Quartz;

namespace ApiGuardian.Api.Jobs.Simulacion;

// Job de Quartz del proceso light. Fase 1: sin disparadores y sin lógica.
// No se registra MiCronJob en Quartz (R-15).
[DisallowConcurrentExecution]
public sealed class SimulacionJob : IJob
{
    private readonly ISimulacionService _servicio;
    private readonly ILogger<SimulacionJob> _logger;

    public SimulacionJob(ISimulacionService servicio, ILogger<SimulacionJob> logger)
    {
        _servicio = servicio;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var (exito, mensaje) = await _servicio.EjecutarCorridaAsync("cron", context.CancellationToken);
        if (!exito)
        {
            _logger.LogWarning("SimulacionJob sin ejecución: {Mensaje}", mensaje);
        }
    }
}
