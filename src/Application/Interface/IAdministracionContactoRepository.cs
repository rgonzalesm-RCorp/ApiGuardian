using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IAdministracionContactoRepository
{
    Task<(IEnumerable<ListaAdministracionContacto> Data, bool Success, string Mensaje, int Total)> GetAllAdministracionContacto(string LogTransaccionId, int page, int pageSize, string? search);
    Task<(bool Success, string Mensaje)> InsertContacto(string LogTransaccionId, AdministracionContacto data);
    Task<(bool Success, string Mensaje)> UpdateContacto(string LogTransaccionId, AdministracionContacto data);
    Task<(bool Success, string Mensaje)> BajaContacto(string LogTransaccionId, AdministracionContactoBaja data);
}
