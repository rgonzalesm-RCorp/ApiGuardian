using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface ICuotasVentaResidualRepository
{
    Task<(bool Success, string Mensaje, IEnumerable<VentaResidual> ListadoCuotasVentasResidual)> GetCuotasVentasResidual(string LogTransaccionId, string Usuario, string Inicio, string Fin);
    Task<(bool Success, string Mensaje, IEnumerable<ProductosPagarMensuales> ListadoProductosPagarMensuales)> GetProductosPagarMensuales(string LogTransaccionId, string Usuario);
    //Task<(bool Success, string Mensaje)> SaveProductosDetalleCuotas(string LogTransaccionId, string Usuario, List<ProductosDetalleCuotas> listado);
    Task<(bool Success, string Mensaje)> SaveCuotasVentasProductosPagarMensual(string LogTransaccionId, string Usuario, List<VentaResidual> listado);
    Task<(bool Success, string Mensaje)> SaveControlProductos(string LogTransaccionId, string Usuario, List<ProductosPagarMensualUpdate> ProductosPagarMensualUpdate);
    Task<(bool Success, string Mensaje)> InsertProductosPagarMensuales(string LogTransaccionId, string Usuario, List<ProductosPagarMensuales> Listado);
}
