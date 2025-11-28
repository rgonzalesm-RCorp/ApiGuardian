using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IAdministracionObservacionComisionRepository
{
    Task<(IEnumerable<ListaAdministracionObservacionComision> Data, bool Success, string Mensaje, int Total)> GetAllAdministracionCObservacionComisionAsync(string LogTransaccionId, int page, int pageSize, string? search, int lCicloId);
    Task<(bool succes, string mensaje)> InsertAdministracionObservacionComision(string LogTransaccionId, AdministracionObservacionComision data);
    Task<(bool succes, string mensaje)> UpdateAdministracionObservacionComision(string LogTransaccionId, AdministracionObservacionComision data);
    Task<(bool succes, string mensaje)> DeleteAdministracionObservacionComision(string LogTransaccionId, int lObservacionId, string? usuario);
}
