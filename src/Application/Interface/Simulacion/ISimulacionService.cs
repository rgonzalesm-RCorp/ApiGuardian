namespace ApiGuardian.Application.Interfaces.Simulacion;

public interface ISimulacionService
{
    // Corrida completa del job (fase 9). Sin conectar todavía.
    Task<(bool Exito, string Mensaje)> EjecutarCorridaAsync(string usuario, CancellationToken cancellationToken);

    // Fase 2: Obtener Ventas normales del ciclo (rango de fechas del ciclo).
    Task<(bool Exito, string Mensaje)> ObtenerVentasAsync(string logId, string usuario, string inicio, string fin);

    // F1-T6: candado por ciclo en la tabla de ejecución (D-21).
    // Iniciada = true: la corrida puede seguir y su Id debe cerrarse con TerminarCorridaAsync.
    Task<(bool Iniciada, long Id, string Mensaje)> IniciarCorridaAsync(string logId, int lCicloId, string usuario);

    Task<bool> TerminarCorridaAsync(string logId, long id, bool exito, string? pasoFallido, string? mensajeError);
}
