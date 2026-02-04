using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IProcesoComisionesRepository
{
    Task<(ItemProcesoJon Data, bool Success, string Mensaje)> GetProceso(string LogTransaccionId, string Proceso);

}
