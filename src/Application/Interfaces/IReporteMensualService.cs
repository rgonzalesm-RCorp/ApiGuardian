namespace ApiGuardian.Application.Interfaces;

public interface IReporteMensualService
{
    Task<(bool Success, string Mensaje, object Data)> ReporteMensualAsync(int cicloId, int contactoId);
}
