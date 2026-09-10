using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Services;

public sealed class ConfiguracionProcesoComisionesService : IConfiguracionProcesoComisionesService
{
    private readonly IConfiguracionProcesoComisionesRepository _repository;

    public ConfiguracionProcesoComisionesService(
        IConfiguracionProcesoComisionesRepository repository
    ) => _repository = repository;

    private static string Id() => DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

    public async Task<(bool Success, string Mensaje, object Data, bool Swall)> GuardarAsync(
        PC_ConfigVtaPersonal data
    )
    {
        try
        {
            var complejos = string.Join(",", data.Complejos.Select(x => x.LComplejo_id));
            var validacion = await _repository.VerificarComplejos(Id(), complejos, data.LCiclo_id);
            if (!validacion.Success)
                return (false, "No se logro realizar la verificacion de los complejos.", "", false);
            if (validacion.Listado.Any())
                return (
                    false,
                    "LOS SIGUIENTES COMPLEJOS YA SE ENCUENTRA EN OTRA CONFIGURACION DEL MISMO CICLO.",
                    validacion.Listado,
                    true
                );
            var r = await _repository.GuardarConfiguracionComisionVentaPersonal(Id(), data);
            return (r.Success, r.Mensaje, "", false);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "", false);
        }
    }

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerAsync()
    {
        try
        {
            var r = await _repository.GETConfiguracionComisionVentaPersonal(Id());
            return (r.Success, r.Mensaje, r.pC_ConfigVtaPersonal);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje)> EliminarAsync(
        string usuario,
        int configuracionId
    )
    {
        try
        {
            var r = await _repository.DeleteConfiguracionComisionVentaPersonal(
                Id(),
                usuario,
                configuracionId
            );
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
