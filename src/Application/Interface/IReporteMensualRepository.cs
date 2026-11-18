using ApiGuardian.Models;

namespace ApiGuardian.Application.Interfaces;

public interface IReporteMensualRepository
{
    Task<(ReporteComisionesDto Data, bool Success, string Mensaje)> GetReporteMensual(string LogTransaccionId, int lCicloId, int lContactoId);
}
