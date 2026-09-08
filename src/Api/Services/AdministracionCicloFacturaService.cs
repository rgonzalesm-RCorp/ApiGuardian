using ApiGuardian.Application.Interfaces;

namespace CleanDapperApi.Api.Services;

public sealed class AdministracionCicloFacturaService : IAdministracionCicloFacturaService
{
    private readonly IAdministracionCicloFacturaRepository _repository;

    public AdministracionCicloFacturaService(IAdministracionCicloFacturaRepository repository) =>
        _repository = repository;

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerAsync(
        int page,
        int pageSize,
        int cicloId,
        string logTransaccionId
    )
    {
        try
        {
            var r = await _repository.GetAllAdministracionCiclofactura(
                logTransaccionId,
                page,
                pageSize,
                cicloId
            );
            return (r.Success, r.Mensaje, new { dataList = r.Data, total = r.Total });
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje)> InsertarAsync(
        AdministracionCicloFactura data,
        string logTransaccionId
    )
    {
        try
        {
            var r = await _repository.InsertAdministracionCiclofactura(logTransaccionId, data);
            return (r.success, r.mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Mensaje)> EliminarAsync(
        int cicloFacturaId,
        string? usuario,
        string logTransaccionId
    )
    {
        try
        {
            var r = await _repository.DeleteAdministracionCiclofactura(
                logTransaccionId,
                cicloFacturaId,
                usuario
            );
            return (r.succes, r.mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
