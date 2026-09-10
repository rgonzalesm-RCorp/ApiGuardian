using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Services;

public sealed class ControlProcesoService : IControlProcesoService
{
    private readonly IControlProcesoRepository _repository;

    public ControlProcesoService(IControlProcesoRepository repository) => _repository = repository;

    private static string Id() => DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerConfiguracionAsync(
        string usuario
    )
    {
        try
        {
            var r = await _repository.GetConfiguracionProcesos(Id(), usuario);
            return (r.Success, r.Mensaje, r.Data);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje, object Data)> GuardarConfiguracionAsync(
        string usuario,
        ControlProcesoConfiguracion data
    )
    {
        try
        {
            var r = await _repository.GuardarConfiguracionProceso(Id(), usuario, data);
            return (r.Success, r.Mensaje, r.Data);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje, object Data)> EliminarConfiguracionAsync(
        string usuario,
        int procesoId
    )
    {
        try
        {
            var r = await _repository.DeleteConfiguracionProceso(Id(), usuario, procesoId);
            return (r.Success, r.Mensaje, "");
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerCicloAsync(
        string usuario,
        int cicloId
    )
    {
        try
        {
            var r = await _repository.GetResumenProcesoCiclo(
                Id(),
                usuario,
                ProcesosDiccionario.COMISIONES,
                cicloId
            );
            return (r.Success, r.Mensaje, r.Data);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje, object Data)> ReiniciarCicloAsync(
        string usuario,
        int cicloId,
        string inicio,
        string fin
    )
    {
        try
        {
            var r = await _repository.ReiniciarCiclo(
                Id(),
                usuario,
                ProcesosDiccionario.COMISIONES,
                cicloId,
                inicio,
                fin
            );
            return (
                r.Success && r.Data.status,
                string.IsNullOrWhiteSpace(r.Data.mensaje) ? r.Mensaje : r.Data.mensaje,
                r.Data
            );
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje, object Data)> CerrarCicloAsync(
        string usuario,
        int cicloId
    )
    {
        try
        {
            var r = await _repository.CerrarCiclo(
                Id(),
                usuario,
                ProcesosDiccionario.COMISIONES,
                cicloId
            );
            return (
                r.Success && r.Data.status,
                string.IsNullOrWhiteSpace(r.Data.mensaje) ? r.Mensaje : r.Data.mensaje,
                r.Data
            );
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }
}
