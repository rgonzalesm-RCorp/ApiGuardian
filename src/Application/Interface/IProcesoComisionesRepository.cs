using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IProcesoComisionesRepository
{
    Task<(ItemProcesoJon Data, bool Success, string Mensaje)> GetProceso(string LogTransaccionId, string Proceso);
    Task<(IEnumerable<VentaPersonalComisionDto> Data, bool Success, string Mensaje)> GetCalculoVentaPersonal(string LogTransaccionId,string Usuario, string Inicio, string Fin, int LCicloId);

}
