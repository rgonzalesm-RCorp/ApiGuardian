using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IProcesoComisionesRepository
{
    Task<(ItemProcesoJon Data, bool Success, string Mensaje)> GetProceso(string LogTransaccionId, string Proceso);
    Task<(IEnumerable<VentaPersonalComisionDto> Data, IEnumerable<VentaPersonalComisionDto> ListaVtaPersonal, bool Success, string Mensaje)> GetCalculoVentaPersonal(string LogTransaccionId,string Usuario, string Inicio, string Fin, int LCicloId);
    Task<(bool Success, string Mensaje)> GuardarVtaRezagadas(string LogTransaccionId, List<ItemVentaCnx> Data, string Usuario);
    Task<(bool Success, string Mensaje)> UpdateVtaRezagadas(string LogTransaccionId, ItemVentaCnx Data, string Usuario, int LCicloId);
    Task<(bool Success, string Mensaje, IEnumerable<ItemVentaCnx> Data)> GetVtaRezada(string LogTransaccionId, string Usuario);
    Task<(IEnumerable<ItemComisionVentaGrupoDto> Data, bool Success, string Mensaje)> GetCalculoVentaGrupo(string LogTransaccionId,string Usuario, string Inicio, string Fin, int LCicloId);
    
}
