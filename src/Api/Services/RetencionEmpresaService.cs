using ApiGuardian.Application.Interfaces;

namespace CleanDapperApi.Api.Services;

public sealed class RetencionEmpresaService : IRetencionEmpresaService
{
    private const decimal PorcentajeRetencionSinFactura = 16m;
    private readonly IRetencionEmpresaRepository _repository;

    public RetencionEmpresaService(IRetencionEmpresaRepository repository)
    {
        _repository = repository;
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

        var resultado = await ConstruirAsync(cicloId);
        if (!resultado.Success)
            return (false, resultado.Mensaje, "");

        var guardado = await _repository.InsertarAsync(
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(),
            "SistemaRetencionEmpresa",
            resultado.Data
        );
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
