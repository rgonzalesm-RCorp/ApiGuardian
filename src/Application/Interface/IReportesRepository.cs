using System.Data.SqlTypes;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Models;

namespace ApiGuardian.Application.Interfaces;

public interface IReportesRepository
{
    Task<(ReporteComisionesDto Data , bool Success, string Mensaje)> GetReporteComision(string LogTransaccionId, int lCicloId, int lCotactoId);
    Task<(RptAplicaciones Data , bool Success, string Mensaje)> GetReporteAplicacines(string LogTransaccionId, int lCicloId, int lCotactoId);
}
