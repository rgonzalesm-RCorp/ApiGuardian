using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IAplicacionesRepository
{
    Task<(AplicacionesPreviewResponse Data, bool Success, string Mensaje)> Preview(string logTransaccionId, int lCicloId);
    Task<(AplicacionesApplyResponse Data, bool Success, string Mensaje)> Apply(string logTransaccionId, int lCicloId);
}
