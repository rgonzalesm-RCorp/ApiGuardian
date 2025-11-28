using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IAdministracionCicloFacturaRepository
{
    Task<(IEnumerable<ListaAdministracionCicloFactura> Data, bool Success, string Mensaje, int Total)> GetAllAdministracionCiclofactura(string LogTransaccionId, int page, int pageSize, int lCicloId);
    Task<(bool success, string mensaje)> InsertAdministracionCiclofactura(string LogTransaccionId, AdministracionCicloFactura data);
    Task<(bool succes, string mensaje)> DeleteAdministracionCiclofactura(string LogTransaccionId, int lciclofactura, string? usuario);
}
