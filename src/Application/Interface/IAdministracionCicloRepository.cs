using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces
{
    public interface IAdministracionCicloRepository
    {
        Task<(IEnumerable<AdministracionCicloABM> Ciclos, bool Success, string Mensaje)> GetCiclos(string log);
        Task<(IEnumerable<AdministracionCicloABM> Ciclos, bool Success, string Mensaje, int Total)> GetCiclosPagination(string log, int page, int pageSize, string? search);
        Task<(bool Success, string Mensaje)> GuardarCiclo(string log, AdministracionCicloABM ciclo);
        Task<(bool Success, string Mensaje)> ModificarCiclo(string log, AdministracionCicloABM ciclo);
        Task<(bool Success, string Mensaje)> EliminarCiclo(string log, int LCicloId);
    }
}
