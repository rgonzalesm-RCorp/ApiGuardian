using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IAdministracionCicloFacturaRepository
{
    Task<(IEnumerable<ListaAdministracionCicloFactura> Data, bool Success, string Mensaje, int Total)> GetAllAdministracionCiclofactura(int page, int pageSize, int lCicloId);
    Task<(bool success, string mensaje)> InsertAdministracionCiclofactura(AdministracionCicloFactura data);
    Task<(bool succes, string mensaje)> DeleteAdministracionCiclofactura(int lciclofactura, string? usuario);
}
