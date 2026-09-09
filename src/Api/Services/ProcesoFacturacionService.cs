using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Services;

public sealed class ProcesoFacturacionService : IProcesoFacturacionService
{
    private readonly IProcesoFacturacionRepository _repository;
    private readonly IControlProcesoRepository _controlProcesoRepository;

    public ProcesoFacturacionService(
        IProcesoFacturacionRepository repository,
        IControlProcesoRepository controlProcesoRepository
    )
    {
        _repository = repository;
        _controlProcesoRepository = controlProcesoRepository;
    }

    public async Task<(bool Success, string Mensaje, object Data)> GuardarAsesoresAsync(
        SolicitudGuardarAsesoresFacturacion solicitud
    )
    {
        if (solicitud.LCicloId <= 0 || string.IsNullOrWhiteSpace(solicitud.Usuario))
            return (false, "Ciclo y usuario son obligatorios.", new ResultadoGuardarAsesoresFacturacion());

        var logId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        var siguiente = await _controlProcesoRepository.GetSiguientePaso(
            logId, solicitud.Usuario, ProcesosDiccionario.COMISIONES, solicitud.LCicloId);
        if (!siguiente.Success || siguiente.Data.nombre != PasosDiccionario.FACTURACION)
            return (false, "El paso Facturación no está habilitado para este ciclo.", new ResultadoGuardarAsesoresFacturacion());

        var inicio = await _controlProcesoRepository.IniciarPaso(
            logId, solicitud.Usuario, ProcesosDiccionario.COMISIONES, solicitud.LCicloId, PasosDiccionario.FACTURACION);
        if (!inicio.Success || !(inicio.Data?.status ?? false))
            return (false, inicio.Data?.mensaje ?? inicio.Mensaje, new ResultadoGuardarAsesoresFacturacion());

        var guardado = await _repository.GuardarAsesoresAsync(logId, solicitud);
        if (!guardado.Success)
        {
            await _controlProcesoRepository.CancelarPaso(
                logId, solicitud.Usuario, ProcesosDiccionario.COMISIONES, solicitud.LCicloId, PasosDiccionario.FACTURACION);
            return (false, guardado.Mensaje, guardado.Data);
        }

        var fin = await _controlProcesoRepository.FinalizarPaso(
            logId, solicitud.Usuario, ProcesosDiccionario.COMISIONES, solicitud.LCicloId, PasosDiccionario.FACTURACION);
        return (
            fin.Success && (fin.Data?.status ?? false),
            fin.Success && (fin.Data?.status ?? false) ? guardado.Mensaje : fin.Data?.mensaje ?? fin.Mensaje,
            guardado.Data
        );
    }
}
