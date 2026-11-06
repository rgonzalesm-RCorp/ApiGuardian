using ApiGuardian.Domain.DTO;

namespace ApiGuardian.Application.Interfaces;

public interface IMonteSionArbolService
{
    Task<(bool Success, string Mensaje, int Total)> ArmarAsync(int cicloId, string usuario);
    Task<(bool Success, string Mensaje, IReadOnlyList<MonteSionArbolNodo> Data)> RaicesAsync(int cicloId);
    Task<(bool Success, string Mensaje, IReadOnlyList<MonteSionArbolNodo> Data)> HijosAsync(int cicloId, int padreId);
    Task<(bool Success, string Mensaje, MonteSionArbolConsulta? Data)> ConsultarPersonaAsync(int cicloId, int contactoId);
    Task<(bool Success, string Mensaje, IReadOnlyList<MonteSionArbolNodo> Data)> BuscarAsync(int cicloId, string texto);
}
