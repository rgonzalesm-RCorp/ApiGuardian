using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Services;

public sealed class AplicacionesService : IAplicacionesService
{
    private readonly IAplicacionesRepositorio _repository;
    private readonly IControlProcesoRepository _controlProcesoRepository;
    private readonly IAplicacionesBackgroundQueue _backgroundQueue;

    public AplicacionesService(
        IAplicacionesRepositorio repository,
        IControlProcesoRepository controlProcesoRepository,
        IAplicacionesBackgroundQueue backgroundQueue
    )
    {
        _repository = repository;
        _controlProcesoRepository = controlProcesoRepository;
        _backgroundQueue = backgroundQueue;
    }

    private static string Id() => DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

    public async Task<(bool Exito, string Mensaje, object Datos)> VistaPreviaAsync(int cicloId)
    {
        try
        {
            var r = await _repository.VistaPrevia(Id(), cicloId);
            return (r.Exito, r.Mensaje, r.Datos);
        }
        catch (Exception ex)
        {
            return (
                false,
                ex.Message,
                new RespuestaVistaPreviaAplicaciones
                {
                    LCicloId = cicloId,
                    VistaPrevia = true,
                    ErrorGrave = true,
                    ErrorGraveMensaje = ex.Message,
                }
            );
        }
    }

    public async Task<(bool Exito, string Mensaje, object Datos)> IniciarAplicacionAsync(int cicloId)
    {
        var logId = Id();
        var siguiente = await _controlProcesoRepository.GetSiguientePaso(
            logId, "SistemaAplicaciones", ProcesosDiccionario.COMISIONES, cicloId);
        if (!siguiente.Success || siguiente.Data.nombre != PasosDiccionario.APLICACION)
            return (false, "El paso Aplicación no está habilitado para este ciclo.", new RespuestaEjecucionAplicaciones { LCicloId = cicloId });

        var inicio = await _controlProcesoRepository.IniciarPaso(
            logId, "SistemaAplicaciones", ProcesosDiccionario.COMISIONES, cicloId, PasosDiccionario.APLICACION);
        if (!inicio.Success || !(inicio.Data?.status ?? false))
            return (false, inicio.Data?.mensaje ?? inicio.Mensaje, new RespuestaEjecucionAplicaciones { LCicloId = cicloId });

        _backgroundQueue.Encolar(cicloId);
        return (true, "La aplicación de pagos fue iniciada y se ejecuta en segundo plano.", new { LCicloId = cicloId, EnSegundoPlano = true });
    }

    public async Task EjecutarEnSegundoPlanoAsync(int cicloId)
    {
        var logId = Id();
        try
        {
            var r = await _repository.Aplicar(logId, cicloId);
            if (!r.Exito)
            {
                await _controlProcesoRepository.CancelarPaso(logId, "SistemaAplicaciones", ProcesosDiccionario.COMISIONES, cicloId, PasosDiccionario.APLICACION);
                return;
            }

            var finAplicacion = await _controlProcesoRepository.FinalizarPaso(
                logId, "SistemaAplicaciones", ProcesosDiccionario.COMISIONES, cicloId, PasosDiccionario.APLICACION);
            if (!finAplicacion.Success || !(finAplicacion.Data?.status ?? false))
                return;
        }
        catch
        {
            await _controlProcesoRepository.CancelarPaso(logId, "SistemaAplicaciones", ProcesosDiccionario.COMISIONES, cicloId, PasosDiccionario.APLICACION);
        }
    }

    public async Task<(bool Exito, string Mensaje, object Datos)> ObtenerComisionadosAsync(int cicloId)
    {
        var r = await _repository.ObtenerComisionadosAsync(cicloId);
        return (r.Exito, r.Mensaje, r.Datos);
    }
}
