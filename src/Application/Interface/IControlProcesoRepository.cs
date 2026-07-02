using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IControlProcesoRepository
{
    Task<(ItemControlProceso Data, bool Success, string Mensaje)> GetControlProceso(string LogTransaccionId, string Usuario, string Paso, int LCicloId);
    Task<(bool Success, string Mensaje)> GuardarControlProceso(string LogTransaccionId, string Usuario, ItemControlProceso Data);
    Task<(bool Success, string Mensaje)> UpdateControlProceso(string LogTransaccionId, string Usuario, string Paso, int LCicloId);


    Task<(bool Success, string Mensaje, IEnumerable<ControlProcesoConfiguracion> Data)> GetConfiguracionProcesos(string LogTransaccionId, string Usuario);
    Task<(bool Success, string Mensaje, ControlProcesoConfiguracion Data)> GuardarConfiguracionProceso(string LogTransaccionId, string Usuario, ControlProcesoConfiguracion Data);
    Task<(bool Success, string Mensaje)> DeleteConfiguracionProceso(string LogTransaccionId, string Usuario, int ProcesoId);
    Task<(bool Success, string Mensaje, ItemControlProcesoNext Data)> GetSiguientePaso(string LogTransaccionId, string Usuario, string proceso, int LCicloId);
    Task<(bool Success, string Mensaje, ItemControlProcesoPrincipal Data)> IniciarPaso(string LogTransaccionId, string Usuario, string proceso, int LCicloId, string paso);
    Task<(bool Success, string Mensaje, ItemControlProcesoPrincipal Data)> FinalizarPaso(string LogTransaccionId, string Usuario, string proceso, int LCicloId, string paso);
    Task<(bool Success, string Mensaje, ItemControlProcesoPrincipal Data)> CancelarPaso(string LogTransaccionId, string Usuario, string proceso, int LCicloId, string paso);
    Task<(bool Success, string Mensaje, ItemControlProcesoPrincipal Data)> EjecutarPaso(string LogTransaccionId, string Usuario, string proceso, int LCicloId, string paso);
    Task<(bool Success, string Mensaje, ItemControlProcesoResumen Data)> GetResumenProcesoCiclo(string LogTransaccionId, string Usuario, string proceso, int LCicloId);
    Task<(bool Success, string Mensaje, ItemControlProcesoPrincipal Data)> ReiniciarCiclo(string LogTransaccionId, string Usuario, string proceso, int LCicloId, string Inicio, string Fin);
    Task<(bool Success, string Mensaje, ItemControlProcesoPrincipal Data)> CerrarCiclo(string LogTransaccionId, string Usuario, string proceso, int LCicloId);
    
}
