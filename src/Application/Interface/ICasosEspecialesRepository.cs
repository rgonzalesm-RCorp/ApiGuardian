using System.Data.SqlTypes;
using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface ICasosEspecialesRepository
{
    Task<(IEnumerable<ItemVentaCnx> VentasCasosEspeciales, bool Success, string Mensaje)> GetVentasCasosEspeciales(string LogTransaccionId, string Usuario, string Inicio, string Fin);
    Task<(IEnumerable<UpgradeSolicitudDto> Lista, bool Success, string Mensaje)> GetUpgradeSolicitudPorVentasCnx(string LogTransaccionId, string Usuario, string UpgVentaIds);
    Task<(bool Success, string Mensaje)> SaveUpgradeSolicitud(string LogTransaccionId, string Usuario, int LCicloId, List<UpgradeSolicitudDto> Listado);
    Task<(IEnumerable<UpgradeSolicitudDto> Lista, bool Success, string Mensaje)> GetUpgradeSolicitudGrd(string LogTransaccionId, string Usuario, int LCicloId);
}
