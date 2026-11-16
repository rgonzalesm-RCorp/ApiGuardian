namespace ApiGuardian.Application.Interfaces;

/// <summary>
/// Fuente única de la regla "solo especiales": personas cuyas ventas del ciclo son todas de tipo especial
/// (upgrade, recuperación, recompra, casos especiales). Cobran solo comisión directa y no entran a la red comprimida.
/// </summary>
public interface IContactosSoloEspecialesService
{
    /// <summary>
    /// Devuelve los contactos "solo especiales" del ciclo, sin los que tienen habilitación manual que genera comisiones.
    /// Un conjunto vacío es válido. Si Success es false, el cálculo que llama debe detenerse.
    /// </summary>
    Task<(bool Success, string Mensaje, HashSet<int> Contactos)> ObtenerAsync(string logTransaccionId, string usuario, int cicloId);

    /// <summary>
    /// Igual que ObtenerAsync, pero evaluando los contratos de un período distinto al del ciclo.
    /// Lo usa la simulación, cuyos activos salen de una ventana de varios meses.
    /// </summary>
    Task<(bool Success, string Mensaje, HashSet<int> Contactos)> ObtenerPorPeriodoAsync(string logTransaccionId, string usuario, int cicloId, string inicio, string fin);
}
