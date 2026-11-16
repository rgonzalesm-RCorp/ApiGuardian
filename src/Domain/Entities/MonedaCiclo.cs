namespace ApiGuardian.Domain.Entities;

// Moneda configurada en administracionciclo.moneda. Define cómo se convierten los montos traídos de Conexión.
public enum MonedaCiclo
{
    // Dólares: lógica actual de CambioDolarService.
    SUS,
    // Bolivianos: proyectos BOB sin cambio; proyectos USD × tipoCambioHistorico.
    BS
}

public static class MonedaCicloHelper
{
    // Acepta solo "BS" o "SUS" (sin distinguir mayúsculas ni espacios).
    public static bool TryParse(string? valor, out MonedaCiclo moneda)
    {
        switch (valor?.Trim().ToUpperInvariant())
        {
            case "SUS":
                moneda = MonedaCiclo.SUS;
                return true;
            case "BS":
                moneda = MonedaCiclo.BS;
                return true;
            default:
                moneda = MonedaCiclo.SUS;
                return false;
        }
    }
}
