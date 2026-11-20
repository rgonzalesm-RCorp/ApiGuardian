namespace ApiGuardian.Api.Services.Simulacion;

// F8-T1 y F8-T2: Bono Par (BONO PAR). Basado en BonoResidualService.GuardarBonoParAsync, sin control de proceso (R5).
// Diferencias de la simulación:
// - Las ventas que se cuentan son las del ciclo, pero el ganador solo necesita ventas tipo 1 o 2 (crédito/contado)
//   en los últimos 3 meses hasta el fin del ciclo (misma ventana que la red comprimida).
// - Consulta propia (SimulacionRepositorio.SqlBonoPar): 10 personas que vendieron pagan 2000.
public sealed partial class SimulacionService
{
    public async Task<(bool Exito, string Mensaje)> BonoParAsync(string logId, string usuario, int lCicloId, string inicio, string fin)
    {
        var bonoPar = await _repositorio.GetBonoParAsync(logId, inicio, fin);
        if (!bonoPar.Success)
            return (false, bonoPar.Mensaje);

        var habilitaciones = await _habilitacionLectura.GetHabilitaciones(logId, usuario, lCicloId);
        if (!habilitaciones.Success)
            return (false, habilitaciones.Mensaje);

        // Ventana de 3 meses hasta el fin del ciclo (incluido). Ej.: fin 20261031 -> 20260801 a 20261031.
        var finFecha = DateTime.ParseExact(fin, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
        var inicioVentana = finFecha.AddMonths(-3).AddDays(1).ToString("yyyyMMdd");
        var conVenta = await _repositorio.GetAsesoresConVentaAsync(logId, inicioVentana, fin);
        if (!conVenta.Success)
            return (false, conVenta.Mensaje);

        var bloqueados = HabilitacionComisionHelper.GetContactosBloqueadosParaComision(habilitaciones.Data);
        var ganadoresConVenta = conVenta.Asesores;
        var listado = bonoPar.Data
            .Where(x => !bloqueados.Contains(x.LContctoGanadorId) && ganadoresConVenta.Contains(x.LContctoGanadorId))
            .ToList();

        if (listado.Count == 0)
            return (true, "No existen ganadores habilitados para generar bono par.");

        var guardado = await _repositorio.InsertBonoParAsync(logId, usuario, lCicloId, listado);
        if (!guardado.Success)
            return (false, guardado.Mensaje);

        return (true, $"Bono par: {listado.Count} ganadores.");
    }
}
