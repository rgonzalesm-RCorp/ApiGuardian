using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IAdministracionBonoResidualRepository
{
    Task<( bool Success, string Mensaje)> SaveAdministracionBonoResidual(string LogTransaccionId, string Usuario, List<ItemAdministracionBonoResidual> data);
    Task<( bool Success, string Mensaje)> SaveAdministracionBonoCompleto(string LogTransaccionId, string Usuario, List<ItemBonoCompleto> data);
    Task<( bool Success, string Mensaje)> SaveAdministracionRedEmpresaComplejo(string LogTransaccionId, string Usuario, List<ItemRedEmpresaComplejo> data);
}
