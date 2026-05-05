using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface ICuotasVentaResidualRepository
{
    Task<(bool Success, string Mensaje, IEnumerable<VentaResidual> ListadoCuotasVentasResidual)> GetCuotasVentasResidual(string LogTransaccionId, string Usuario, string Inicio, string Fin);

}
