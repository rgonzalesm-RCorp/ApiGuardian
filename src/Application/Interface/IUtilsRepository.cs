using System.Data.SqlTypes;
using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IUtilsRepository
{
    Task<(IEnumerable<AdministracionSemanaCiclo> Semanas, bool Success, string Mensaje)> GetSemanaCiclosAsync(string LogTransaccionId, int lCicloId);
    Task<(IEnumerable<BasePaisDepartamento> Departamento, bool Success, string Mensaje)> GetDepartamento(string LogTransaccionId, int lPaisId);
    Task<(IEnumerable<AdministracionTipoContrato> TipoContrato, bool Success, string Mensaje)> GetTipoContrato(string LogTransaccionId);
    Task<(IEnumerable<AdministracionEstadoContrato> EstadoContrato, bool Success, string Mensaje)> GetEstadoContrato(string LogTransaccionId);
    Task<(IEnumerable<ListaAdministracionPais> Pais, bool Success, string Mensaje)> GetPais(string LogTransaccionId);
    Task<(IEnumerable<ListaAdministracionTipoBaja> TipoBaja, bool Success, string Mensaje)> GetTipoBaja(string LogTransaccionId);
    Task<(IEnumerable<ListaTipoDescuento> TipoDescuento, bool Success, string Mensaje)> GetTipoDescuento(string LogTransaccionId);

}
