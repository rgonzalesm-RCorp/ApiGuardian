using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IAdministracionBuscarAsesorRepository
{
    Task<(IEnumerable<ListaBuscarAsesor> DataFijos, IEnumerable<ListaBuscarAsesor> DataActivos, bool Success, string Mensaje)> GetAsesoreSieteNiveles(string LogTransaccionId, int lContactoId);

}
