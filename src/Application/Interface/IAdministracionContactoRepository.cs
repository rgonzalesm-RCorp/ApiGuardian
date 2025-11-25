using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IAdministracionContactoRepository
{
    Task<(IEnumerable<ListaAdministracionContacto> Data, bool Success, string Mensaje, int Total)> GetAllAdministracionContacto(int page, int pageSize, string? search);
    Task<(bool Success, string Mensaje)> InsertContacto(AdministracionContacto data);
    Task<(bool Success, string Mensaje)> UpdateContacto(AdministracionContacto data);
    Task<(bool Success, string Mensaje)> BajaContacto(AdministracionContactoBaja data);
}
