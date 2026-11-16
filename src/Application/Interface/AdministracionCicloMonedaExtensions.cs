using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

// Moneda del ciclo para la conversión de montos traídos de Conexión (ver CambioDolarService).
public static class AdministracionCicloMonedaExtensions
{
    public static string MensajeMonedaInvalida(int cicloId, string? moneda) =>
        $"El ciclo {cicloId} no tiene una moneda válida ('{moneda}'). Debe ser BS o SUS.";

    // Valida la moneda de un ciclo ya leído.
    public static (bool Success, string Mensaje, MonedaCiclo Moneda) ValidarMoneda(int cicloId, string? moneda)
    {
        return MonedaCicloHelper.TryParse(moneda, out var resultado)
            ? (true, string.Empty, resultado)
            : (false, MensajeMonedaInvalida(cicloId, moneda), MonedaCiclo.SUS);
    }

    // Lee el ciclo y valida su moneda.
    public static async Task<(bool Success, string Mensaje, MonedaCiclo Moneda)> ObtenerMonedaAsync(
        this IAdministracionCicloRepository repository,
        string logTransaccionId,
        int cicloId
    )
    {
        var ciclo = await repository.GetCiclo(logTransaccionId, cicloId);
        if (!ciclo.Success || ciclo.Data == null || ciclo.Data.LCicloId <= 0)
            return (false, $"No se encontró el ciclo {cicloId}.", MonedaCiclo.SUS);

        return ValidarMoneda(cicloId, ciclo.Data.Moneda);
    }
}
