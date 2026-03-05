using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IAdministracionVentaGrupoRepository
{
    Task<( bool Success, string Mensaje)> InsertAdministracionVentaGrupo(string LogTransaccionId, List<ItemVentaGrupo> data);
}
