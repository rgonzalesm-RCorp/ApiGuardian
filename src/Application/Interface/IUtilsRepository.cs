using System.Data.SqlTypes;
using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IUtilsRepository
{
    Task<(IEnumerable<AdministracionCiclo> Ciclos, bool Success, string Mensaje)> GetCiclos();
    Task<(IEnumerable<AdministracionSemanaCiclo> Semanas, bool Success, string Mensaje)> GetSemanaCiclosAsync(int lCicloId);
    Task<(IEnumerable<AdministracionComplejo> Complejo, bool Success, string Mensaje)> GetComplejo();
    Task<(IEnumerable<BasePaisDepartamento> Departamento, bool Success, string Mensaje)> GetDepartamento(int lPaisId);
    Task<(IEnumerable<AdministracionTipoContrato> TipoContrato, bool Success, string Mensaje)> GetTipoContrato();
    Task<(IEnumerable<AdministracionEstadoContrato> EstadoContrato, bool Success, string Mensaje)> GetEstadoContrato();
    Task<(IEnumerable<ListaAdministracionNivel> Nivel, bool Success, string Mensaje)> GetNivel();
    Task<(IEnumerable<ListaAdministracionPais> Pais, bool Success, string Mensaje)> GetPais();
    Task<(IEnumerable<ListaAdministracionTipoBaja> TipoBaja, bool Success, string Mensaje)> GetTipoBaja();
    Task<(IEnumerable<ListaTipoDescuento> TipoDescuento, bool Success, string Mensaje)> GetTipoDescuento();

}
