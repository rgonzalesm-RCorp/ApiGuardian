using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IAdministracionVentaPersonalRepository
{
    Task<( bool Success, string Mensaje)> InsertVentaPersonal(string LogTransaccionId, List<AdministracionVentaPersonal> data);
}
