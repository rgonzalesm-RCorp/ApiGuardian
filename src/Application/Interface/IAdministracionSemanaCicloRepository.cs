using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IAdministracionSemanaCicloRepository
{
    Task<(IEnumerable<AdministracionSemanaCicloList> Semanas, bool Success, string Mensaje)>
        GetSemanaCiclo(string LogTransaccionId);

    Task<(IEnumerable<AdministracionSemanaCicloList> Semanas, bool Success, string Mensaje, int Total)>
        GetSemanaCicloPagination(string LogTransaccionId, int page, int pageSize, string? search);

    Task<(bool Success, string Mensaje)> GuardarSemanaCiclo(string LogTransaccionId, AdministracionSemanaCicloABM data);

    Task<(bool Success, string Mensaje)> ModificarSemanaCiclo(string LogTransaccionId, AdministracionSemanaCicloABM data);

    Task<(bool Success, string Mensaje)> EliminarSemanaCiclo(string LogTransaccionId, int LSemanaId);
}
