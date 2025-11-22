using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IAdministracionContactoRepository
{
    Task<(IEnumerable<ListaAdministracionContacto> Data, bool Success, string Mensaje, int Total)> GetAllAdministracionContacto(int page, int pageSize, string? search);
}
