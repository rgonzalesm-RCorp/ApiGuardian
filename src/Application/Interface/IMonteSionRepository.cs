using ApiGuardian.Domain.DTO;

namespace ApiGuardian.Application.Interfaces;

public interface IMonteSionRepository
{
    Task<(IEnumerable<MonteSionContactoRed> Contactos, IEnumerable<MonteSionContactoRed> Evaluables, IEnumerable<MonteSionProduccionContacto> Producciones, IEnumerable<MonteSionRangoHistorico> HistorialRangos, IEnumerable<int> EmprendedoresPrimeraVenta, bool Success, string Mensaje)>
        ObtenerDatosCicloAsync(string logTransaccionId, int cicloId);

    Task<(IEnumerable<MonteSionRangoActual> Data, bool Success, string Mensaje)> ObtenerRangosActualesAsync(string logTransaccionId, int cicloId);
    Task<(IEnumerable<MonteSionNivel> Data, bool Success, string Mensaje)> ObtenerNivelesAsync(string logTransaccionId);

    Task<(bool Success, string Mensaje, MonteSionGuardadoResultado Data)> GuardarRangosAsync(
        string logTransaccionId, int cicloId, string usuario, IReadOnlyCollection<MonteSionResultadoRango> resultados);
}
