using ApiGuardian.Application.Interfaces;

namespace CleanDapperApi.Api.Services;

public sealed class RetencionEmpresaService : IRetencionEmpresaService
{
    private const decimal PorcentajeRetencionSinFactura = 16m;
    private readonly IRetencionEmpresaRepository _repository;
    private readonly IControlProcesoRepository _controlProcesoRepository;

    public RetencionEmpresaService(
        IRetencionEmpresaRepository repository,
        IControlProcesoRepository controlProcesoRepository
    )
    {
        _repository = repository;
        _controlProcesoRepository = controlProcesoRepository;
    }

    public async Task<(bool Success, string Mensaje, object Data)> VerAsync(int cicloId)
    {
        if (cicloId <= 0)
            return (false, "El lciclo_id debe ser mayor a cero.", "");

        var resultado = await ConstruirAsync(cicloId);
        return (resultado.Success, resultado.Mensaje, resultado.Data);
    }

    public async Task<(bool Success, string Mensaje, object Data)> GuardarAsync(int cicloId)
    {
        if (cicloId <= 0)
            return (false, "El lciclo_id debe ser mayor a cero.", "");

        var logId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        var siguiente = await _controlProcesoRepository.GetSiguientePaso(
            logId, "SistemaRetencionEmpresa", ProcesosDiccionario.COMISIONES, cicloId);
        if (!siguiente.Success || siguiente.Data.nombre != PasosDiccionario.RETENCION)
            return (false, "El paso Retención no está habilitado para este ciclo.", "");

        var inicio = await _controlProcesoRepository.IniciarPaso(
            logId, "SistemaRetencionEmpresa", ProcesosDiccionario.COMISIONES, cicloId, PasosDiccionario.RETENCION);
        if (!inicio.Success || !(inicio.Data?.status ?? false))
            return (false, inicio.Data?.mensaje ?? inicio.Mensaje, "");

        var resultado = await ConstruirAsync(cicloId);
        if (!resultado.Success)
        {
            await _controlProcesoRepository.CancelarPaso(
                logId, "SistemaRetencionEmpresa", ProcesosDiccionario.COMISIONES, cicloId, PasosDiccionario.RETENCION);
            return (false, resultado.Mensaje, "");
        }

        var guardado = await _repository.InsertarAsync(
            logId,
            "SistemaRetencionEmpresa",
            resultado.Data
        );
        if (!guardado.Success)
        {
            await _controlProcesoRepository.CancelarPaso(
                logId, "SistemaRetencionEmpresa", ProcesosDiccionario.COMISIONES, cicloId, PasosDiccionario.RETENCION);
            return (false, guardado.Mensaje, "");
        }

        var fin = await _controlProcesoRepository.FinalizarPaso(
            logId, "SistemaRetencionEmpresa", ProcesosDiccionario.COMISIONES, cicloId, PasosDiccionario.RETENCION);
        if (!fin.Success || !(fin.Data?.status ?? false))
            return (false, fin.Data?.mensaje ?? fin.Mensaje, "");

        return (
            guardado.Success,
            guardado.Mensaje,
            new { calculadas = resultado.Data.Count, insertadas = guardado.Insertados }
        );
    }

    private async Task<(bool Success, string Mensaje, List<ApiGuardian.Domain.Entities.RetencionEmpresaItem> Data)> ConstruirAsync(int cicloId)
    {
        var resultado = await _repository.ObtenerAsync(
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(),
            cicloId
        );
        if (!resultado.Success)
            return resultado;

        foreach (var item in resultado.Data)
        {
            item.PorcentajeRetencion = item.LPresentaFactura == 1
                ? 0m
                : PorcentajeRetencionSinFactura;
            item.MontoRetencion = decimal.Round(
                item.MontoComision * item.PorcentajeRetencion / 100m,
                2,
                MidpointRounding.AwayFromZero
            );
            item.TotalComision = item.MontoComision - item.MontoRetencion;
        }

        return resultado;
    }
}
