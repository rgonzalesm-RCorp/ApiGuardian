using ApiGuardian.Domain.DTO;

namespace ApiGuardian.Application.Interfaces;

public interface IMonteSionService
{
    Task<(bool Success, string Mensaje, IEnumerable<MonteSionResultadoRango> Data)> CalcularRangosAsync(int cicloId);
    Task<(bool Success, string Mensaje, object Data)> ExportarRangosAsync(int cicloId);
    Task<(bool Success, string Mensaje, MonteSionGuardadoResultado Data)> GuardarRangosAsync(int cicloId, string? usuario);
}
