using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IControlProcesoRepository
{
    Task<(ItemControlProceso Data, bool Success, string Mensaje)> GetControlProceso(string LogTransaccionId, string Usuario, string Paso, int LCicloId);
    Task<(bool Success, string Mensaje)> GuardarControlProceso(string LogTransaccionId, string Usuario, ItemControlProceso Data);
    Task<(bool Success, string Mensaje)> UpdateControlProceso(string LogTransaccionId, string Usuario, string Paso, int LCicloId);
    
}
