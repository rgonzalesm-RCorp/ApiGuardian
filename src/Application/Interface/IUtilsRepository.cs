using System.Data.SqlTypes;
using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IUtilsRepository
{
    Task<ConfiguracionUtils> GetCountContacto(string? search);
    Task<(IEnumerable<AdministracionCiclo> Ciclos, bool Success, string Mensaje)> GetCiclos();
    Task<(IEnumerable<AdministracionSemanaCiclo> Semanas, bool Success, string Mensaje)> GetSemanaCiclosAsync(int lCicloId);
    Task<(IEnumerable<AdministracionComplejo> Complejo, bool Success, string Mensaje)> GetComplejo();
    Task<(IEnumerable<BasePaisDepartamento> Departamento, bool Success, string Mensaje)> GetDepartamento();
    Task<(IEnumerable<AdministracionTipoContrato> TipoContrato, bool Success, string Mensaje)> GetTipoContrato();
    Task<(IEnumerable<AdministracionEstadoContrato> EstadoContrato, bool Success, string Mensaje)> GetEstadoContrato();

}
