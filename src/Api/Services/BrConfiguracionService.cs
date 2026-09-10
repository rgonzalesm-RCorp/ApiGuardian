using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Services;

public sealed class BrConfiguracionService : IBrConfiguracionService
{
    private readonly IBrConfiguracionRepository _repository;

    public BrConfiguracionService(IBrConfiguracionRepository repository) =>
        _repository = repository;

    private static string Id() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerDatosAsync(string usuario)
    {
        try
        {
            var niveles = await _repository.GetNivel(Id(), usuario);
            var productos = await _repository.GetTipoProducto(Id(), usuario);
            return (
                niveles.Success,
                productos.Mensaje,
                new { Nivel = niveles.Data, TipoProducto = productos.Data }
            );
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerConfiguracionAsync()
    {
        try
        {
            var r = await _repository.GetConfiguracion(Id(), "");
            var resumen = r
                .Data.GroupBy(x => new
                {
                    x.BrConfiguracionId,
                    x.LCicloId,
                    x.Ciclo,
                    x.TipoProducto,
                    x.TipoProductoId,
                })
                .Select(g => new
                {
                    g.Key.BrConfiguracionId,
                    g.Key.LCicloId,
                    g.Key.Ciclo,
                    g.Key.TipoProducto,
                    g.Key.TipoProductoId,
                    Details = r
                        .Data.Where(x => x.BrConfiguracionId == g.Key.BrConfiguracionId)
                        .Select(x => new
                        {
                            x.BrConfiguracionDetalleId,
                            x.NombreNivel,
                            x.Nivel,
                            x.PorcentajeComision,
                        })
                        .ToList(),
                })
                .ToList();
            return (r.Success, r.Mensaje, new { lista = resumen });
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje)> GuardarAsync(BrConfiguracion data)
    {
        try
        {
            var id = Id();
            if (data.BrConfiguracionId == 0)
            {
                var validacion = await _repository.ValidarRegistro(
                    id,
                    data.Usuario ?? "",
                    data.LCicloId,
                    data.TipoProductoId
                );
                if (!validacion.Success || validacion.existe)
                    return (false, validacion.Mensaje);
            }
            var r = await _repository.GuardarConfiguracion(id, "", data);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Mensaje)> EliminarAsync(
        string usuario,
        int configuracionId
    )
    {
        try
        {
            var r = await _repository.EliminarConfiguracion(Id(), usuario, configuracionId);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
