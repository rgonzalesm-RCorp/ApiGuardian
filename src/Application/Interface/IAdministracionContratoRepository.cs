using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IAdministracionContratoRepository
{
    Task<(IEnumerable<ListaAdministracionContrato> Data, bool Success, string Mensaje, int Total)> GetAllAdministracionContrato(string LogTransaccionId, int page, int pageSize, string? search);
    Task<( bool Success, string Mensaje)> InsertContrato(string LogTransaccionId, AdministracionContrato data);
    Task<(bool Success, string Mensaje)> UpdateContrato(string LogTransaccionId, AdministracionContrato data);
}
