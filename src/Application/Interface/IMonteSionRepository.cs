using ApiGuardian.Domain.DTO;
using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IMonteSionRepository
{
    Task<(IEnumerable<MonteSionContactoRed> Contactos, IEnumerable<MonteSionContactoRed> Evaluables, IEnumerable<MonteSionProduccionContacto> Producciones, IEnumerable<MonteSionRedNivel1> RedNivel1, IEnumerable<MonteSionRangoHistorico> HistorialRangos, IEnumerable<int> EmprendedoresPrimeraVenta, IEnumerable<int> VendedoresAntesDeMayo2026, bool Success, string Mensaje)>
        ObtenerDatosCicloAsync(string logTransaccionId, int cicloId);

    // Niveles de administracionnivel con su plan de carrera vigente para la moneda indicada.
    // Falla si algún nivel (lnivel_id > 0) no tiene configuración para esa moneda.
    Task<(IEnumerable<MonteSionNivel> Data, bool Success, string Mensaje)> ObtenerNivelesAsync(string logTransaccionId, MonedaCiclo moneda);

    // esSimulacion = true: guarda en las mismas tablas, pero no actualiza administracioncontacto.lnivel_id.
    Task<(bool Success, string Mensaje, MonteSionGuardadoResultado Data)> GuardarRangosAsync(
        string logTransaccionId, int cicloId, string usuario, IReadOnlyCollection<MonteSionResultadoRango> resultados, bool esSimulacion = false);
    Task<(bool Success, string Mensaje, MonteSionMigracionResultado Data)> MigrarTablasAsync(string logTransaccionId);
}
