using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IAdministracionBonoResidualRepository
{
    Task<( bool Success, string Mensaje)> SaveAdministracionBonoResidual(string LogTransaccionId, string Usuario, List<ItemAdministracionBonoResidual> data);
}
