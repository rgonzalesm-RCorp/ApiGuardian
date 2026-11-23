using CleanDapperApi.Api.Services;

namespace ApiGuardian.Api.Services.Simulacion;

// Armado del árbol Monte Sion de la simulación. Misma lectura y mismo cálculo que MonteSionArbolService.ArmarAsync;
// el guardado es propio (SimulacionRepositorio.SincronizarArbolAsync): solo escribe la diferencia con lo guardado.
public sealed partial class SimulacionService
{
    private const int LongitudMaximaRutaArbol = 500;

    public async Task<(bool Success, string Mensaje, int Total)> ArmarArbolAsync(string logId, int cicloId, string usuario)
    {
        if (cicloId <= 0)
            return (false, "El ciclo es obligatorio.", 0);

        var origen = await _monteSionArbolLectura.ObtenerContactosAsync(logId);
        if (!origen.Success)
            return (false, origen.Mensaje, 0);

        var nodos = MonteSionArbolService.CalcularNodos(origen.Contactos);
        if (nodos.Any(nodo => nodo.Ruta.Length > LongitudMaximaRutaArbol))
            return (false, "La profundidad del árbol supera el límite de la ruta.", 0);

        return await _repositorio.SincronizarArbolAsync(
            logId,
            cicloId,
            string.IsNullOrWhiteSpace(usuario) ? "sistema" : usuario,
            nodos);
    }
}
