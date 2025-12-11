using System.Data.SqlTypes;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Models;

namespace ApiGuardian.Application.Interfaces;

public interface IReportesRepository
{
    Task<(ReporteComisionesDto Data , bool Success, string Mensaje)> GetReporteComision(string LogTransaccionId, int lCicloId, int lContactoId);
    Task<(RptAplicaciones Data , bool Success, string Mensaje)> GetReporteAplicacines(string LogTransaccionId, int lCicloId, int lContactoId);
    Task<(IEnumerable<RptDescuentoEmpresa> Data , bool Success, string Mensaje)> GetReporteDecuentoEmpresa(string LogTransaccionId, int lCicloId, int Empresaid);
    Task<(IEnumerable<RptFacturacion> Data , bool Success, string Mensaje)> GetReporteFacturacion(string LogTransaccionId, int lCicloId, int LContactoId);
    Task<(IEnumerable<RptProrrateo> Data , bool Success, string Mensaje)> GetReporteProrrateo(string LogTransaccionId, int lCicloId);
    Task<(IEnumerable<RptComisionServicio> Data , bool Success, string Mensaje)> GetReporteComisionServicio(string LogTransaccionId, int LCicloId, int EmpresaId);
}
