using ApiGuardian.Application.Interfaces;

namespace CleanDapperApi.Api.Services;

public sealed class UtilsService : IUtilsService
{
    private readonly IUtilsRepository _repository;

    public UtilsService(IUtilsRepository repository) => _repository = repository;

    private static string Id() => DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerSemanaCicloAsync(
        int cicloId
    )
    {
        try
        {
            var r = await _repository.GetSemanaCiclosAsync(Id(), cicloId);
            return (r.Success, r.Mensaje, r.Semanas);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerDepartamentoAsync(
        int paisId
    )
    {
        try
        {
            var r = await _repository.GetDepartamento(Id(), paisId);
            return (r.Success, r.Mensaje, new { r.Departamento });
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerTipoContratoAsync()
    {
        try
        {
            var r = await _repository.GetTipoContrato(Id());
            return (r.Success, r.Mensaje, new { r.TipoContrato });
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerEstadoContratoAsync()
    {
        try
        {
            var r = await _repository.GetEstadoContrato(Id());
            return (r.Success, r.Mensaje, new { r.EstadoContrato });
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerTipoBajaAsync()
    {
        try
        {
            var r = await _repository.GetTipoBaja(Id());
            return (r.Success, r.Mensaje, new { r.TipoBaja });
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerPaisAsync()
    {
        try
        {
            var r = await _repository.GetPais(Id());
            return (r.Success, r.Mensaje, new { r.Pais });
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerTipoDescuentoAsync()
    {
        try
        {
            var r = await _repository.GetTipoDescuento(Id());
            return (r.Success, r.Mensaje, new { r.TipoDescuento });
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }
}
