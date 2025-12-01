using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IAdministracionComplejoRepository
{
    Task<(IEnumerable<AdministracionComplejoABM> Complejos, bool Success, string Mensaje)> GetComplejo(string LogTransaccionId);

    Task<(IEnumerable<AdministracionComplejoABM> Complejos, bool Success, string Mensaje, int Total)>
        GetComplejoPagination(string LogTransaccionId, int page, int pageSize, string? search);

    Task<(bool Success, string Mensaje)> GuardarComplejo(string LogTransaccionId, AdministracionComplejoABM data);
    Task<(bool Success, string Mensaje)> ModificarComplejo(string LogTransaccionId, AdministracionComplejoABM data);
    Task<(bool Success, string Mensaje)> EliminarComplejo(string LogTransaccionId, int LComplejoId);
}
