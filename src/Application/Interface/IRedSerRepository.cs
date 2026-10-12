using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IRedSerRepository
{
    Task<(bool Success, string Mensaje, IReadOnlyList<int> Vendedores)> ObtenerVendedoresAsync(
        string logTransaccionId,
        DateTime inicio,
        DateTime fin
    );

    Task<(bool Success, string Mensaje, IReadOnlyList<RedSerContacto> Contactos)> ObtenerContactosAsync(
        string logTransaccionId
    );

    Task<(bool Success, string Mensaje)> GuardarAsync(
        string logTransaccionId,
        string usuario,
        DateTime inicio,
        DateTime fin,
        IReadOnlyCollection<RedSerNodo> nodos
    );

    Task<(bool Success, string Mensaje, IReadOnlyList<RedSerNodo> Nodos)> ObtenerAsync(
        string logTransaccionId,
        DateTime inicio,
        DateTime fin
    );

    Task<(bool Success, string Mensaje, DateTime? Inicio, DateTime? Fin, IReadOnlyList<RedSerNodo> Nodos)> ObtenerUltimaAsync(
        string logTransaccionId
    );
}
