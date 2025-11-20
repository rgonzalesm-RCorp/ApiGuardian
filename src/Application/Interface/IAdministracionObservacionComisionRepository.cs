using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IAdministracionObservacionComisionRepository
{
    Task<(IEnumerable<ListaAdministracionObservacionComision> Data, bool Success, string Mensaje, int Total)> GetAllAdministracionCObservacionComisionAsync(int page, int pageSize, string? search, int lCicloId);
    Task<(bool succes, string mensaje)> InsertAdministracionObservacionComision(AdministracionObservacionComision data);
    Task<(bool succes, string mensaje)> UpdateAdministracionObservacionComision(AdministracionObservacionComision data);
    Task<(bool succes, string mensaje)> DeleteAdministracionObservacionComision(int lObservacionId, string? usuario);
}
