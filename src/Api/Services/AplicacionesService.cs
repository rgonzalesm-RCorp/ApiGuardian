using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Services;

public sealed class AplicacionesService : IAplicacionesService
{
    private readonly IAplicacionesRepositorio _repository;

    public AplicacionesService(IAplicacionesRepositorio repository) => _repository = repository;

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

    public async Task<(bool Exito, string Mensaje, object Datos)> AplicarAsync(int cicloId)
    {
        try
        {
            var r = await _repository.Aplicar(Id(), cicloId);
            return (r.Exito, r.Mensaje, r.Datos);
        }
        catch (Exception ex)
        {
            return (
                false,
                ex.Message,
                new RespuestaEjecucionAplicaciones
                {
                    LCicloId = cicloId,
                    VistaPrevia = false,
                    ErrorGrave = true,
                    ErrorGraveMensaje = ex.Message,
                }
            );
        }
    }
}
