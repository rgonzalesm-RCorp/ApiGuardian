using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IConfiguracionProcesoComisionesRepository
{
    Task<(bool Success, string Mensaje)> GuardarConfiguracionComisionVentaPersonal(string LogTransaccionId, PC_ConfigVtaPersonal pC_ConfigVtaPersonal);
    Task<(bool Success, string Mensaje, IEnumerable<PC_ConfigVtaPersonal> pC_ConfigVtaPersonal)> GETConfiguracionComisionVentaPersonal(string LogTransaccionId);
    Task<(bool Success, string Mensaje)> DeleteConfiguracionComisionVentaPersonal(string LogTransaccionId, string usuario, int PC_ConfigVtaPersonalId);
    Task<(bool Success, string Mensaje, IEnumerable<PC_VerificarListaComplejos> Listado)> VerificarComplejos(string LogTransaccionId, string complejosId, int LCicloId);

}
