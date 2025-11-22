using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IAdministracionContratoRepository
{
    Task<(IEnumerable<ListaAdministracionContrato> Data, bool Success, string Mensaje, int Total)> GetAllAdministracionContrato(int page, int pageSize, string? search);
    Task<( bool Success, string Mensaje)> InsertContrato(AdministracionContrato data);
    Task<(bool Success, string Mensaje)> UpdateContrato(AdministracionContrato data);
}
