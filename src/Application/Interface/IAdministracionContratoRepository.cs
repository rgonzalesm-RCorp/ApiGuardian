using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IAdministracionContratoRepository
{
    Task<IEnumerable<AdministracionContrato>> GetAllAdministracionContrato(int page, int pageSize, string? search);
}
