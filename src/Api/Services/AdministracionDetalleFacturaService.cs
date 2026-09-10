using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Services;

public sealed class AdministracionDetalleFacturaService : IAdministracionDetalleFacturaService
{
    private readonly IAdministracionDetalleFacturaRepository _repository;

    public AdministracionDetalleFacturaService(
        IAdministracionDetalleFacturaRepository repository
    ) => _repository = repository;

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerPaginadoAsync(
        int page,
        int pageSize,
        string id
    )
    {
        try
        {
            var r = await _repository.GetDetalleFacturaPagination(id, page, pageSize);
            return (r.Success, r.Mensaje, new { r.Data, r.Total });
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje)> InsertarAsync(
        AdministracionDetalleFactura data,
        string id
    )
    {
        try
        {
            var r = await _repository.GuardarDetalleFactura(id, data);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Mensaje)> ActualizarAsync(
        AdministracionDetalleFactura data,
        string id
    )
    {
        try
        {
            var r = await _repository.ModificarDetalleFactura(id, data);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Mensaje)> EliminarAsync(int detalleFacturaId, string id)
    {
        try
        {
            var r = await _repository.EliminarDetalleFactura(id, detalleFacturaId);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerTiposComisionAsync(
        string id
    )
    {
        try
        {
            var r = await _repository.GetTipoComision(id);
            return (r.Success, r.Mensaje, new { tipoComision = r.Data });
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }
}
