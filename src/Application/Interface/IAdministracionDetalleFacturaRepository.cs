using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IAdministracionDetalleFacturaRepository
{
    Task<(IEnumerable<AdministracionDetalleFactura> Data, bool Success, string Mensaje)>GetDetalleFactura(string logId);
    Task<(IEnumerable<AdministracionTipoComision> Data, bool Success, string Mensaje)>GetTipoComision(string logId);
    Task<(IEnumerable<ItemAdministracionDetalleFactura> Data, bool Success, string Mensaje, int Total)>GetDetalleFacturaPagination(string logId, int page, int pageSize);
    Task<(bool Success, string Mensaje)> GuardarDetalleFactura(string logId, AdministracionDetalleFactura data);
    Task<(bool Success, string Mensaje)> ModificarDetalleFactura(string logId, AdministracionDetalleFactura data);
    Task<(bool Success, string Mensaje)> EliminarDetalleFactura(string logId, int lDetalleFacturaId);
}
