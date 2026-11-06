using ApiGuardian.Domain.DTO;

namespace ApiGuardian.Application.Interfaces;

public interface IMonteSionArbolRepository
{
    Task<(bool Success, string Mensaje, IReadOnlyList<MonteSionArbolOrigen> Contactos)> ObtenerContactosAsync(string logTransaccionId);

    Task<(bool Success, string Mensaje, int Total)> ReemplazarArbolAsync(
        string logTransaccionId,
        int cicloId,
        string usuario,
        IReadOnlyList<MonteSionArbolNodo> nodos
    );

    Task<(bool Success, string Mensaje, MonteSionArbolNodo? Persona)> ObtenerPersonaAsync(
        string logTransaccionId,
        int cicloId,
        int contactoId
    );

    Task<(bool Success, string Mensaje, IReadOnlyList<MonteSionArbolNodo> Nodos)> ObtenerPorIdsAsync(
        string logTransaccionId,
        int cicloId,
        IReadOnlyList<int> contactoIds
    );

    Task<(bool Success, string Mensaje, IReadOnlyList<MonteSionArbolNodo> Nodos)> ObtenerRaicesAsync(
        string logTransaccionId,
        int cicloId
    );

    Task<(bool Success, string Mensaje, IReadOnlyList<MonteSionArbolNodo> Nodos)> ObtenerHijosAsync(
        string logTransaccionId,
        int cicloId,
        int padreId
    );

    Task<(bool Success, string Mensaje, IReadOnlyList<MonteSionArbolNodo> Nodos)> BuscarAsync(
        string logTransaccionId,
        int cicloId,
        string texto
    );
}
