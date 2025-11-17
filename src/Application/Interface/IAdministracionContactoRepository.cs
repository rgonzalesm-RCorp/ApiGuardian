using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IAdministracionContactoRepository
{
    Task<IEnumerable<AdministracionContacto>> GetAllAdministracionContacto(int page, int pageSize, string? search);
}
