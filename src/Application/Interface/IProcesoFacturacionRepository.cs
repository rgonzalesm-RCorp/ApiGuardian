using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IProcesoFacturacionRepository
{
    Task<(bool Success, string Mensaje, ResultadoGuardarAsesoresFacturacion Data)> GuardarAsesoresAsync(
        string logTransaccionId,
        SolicitudGuardarAsesoresFacturacion solicitud
    );
}
