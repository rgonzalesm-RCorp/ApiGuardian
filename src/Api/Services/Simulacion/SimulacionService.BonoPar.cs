namespace ApiGuardian.Api.Services.Simulacion;

// F8-T1 y F8-T2: Bono Par (BONO PAR). Lógica copiada de BonoResidualService.GuardarBonoParAsync, sin control de proceso (R5).
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

        var contratos = await _contratoLectura.GetAdministracionContratoFechaVentaResidual(logId, inicio, fin);
        if (!contratos.Success)
            return (false, contratos.Mensaje);

        var bloqueados = HabilitacionComisionHelper.GetContactosBloqueadosParaComision(habilitaciones.Data);
        var ganadoresConVenta = contratos.Data.Select(x => x.LAsesorId).ToHashSet();
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
