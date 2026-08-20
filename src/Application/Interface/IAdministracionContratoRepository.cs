using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IAdministracionContratoRepository
{
    Task<(IEnumerable<ListaAdministracionContrato> Data, bool Success, string Mensaje, int Total)> GetAllAdministracionContrato(string LogTransaccionId, int page, int pageSize, string? search, DateTime fechaInicio, DateTime fechaFin);
    Task<(IEnumerable<ListaAdministracionContrato> Data, bool Success, string Mensaje)> GetReporteAdministracionContrato(string LogTransaccionId, string? search, DateTime fechaInicio, DateTime fechaFin);
    Task<(IEnumerable<ItemVentaComision> Data, bool Success, string Mensaje)> GetContratoFecha(string LogTransaccionId, string inicio, string fin);
    Task<( bool Success, string Mensaje)> InsertContrato(string LogTransaccionId, AdministracionContrato data);
    Task<(bool Success, string Mensaje)> UpdateContrato(string LogTransaccionId, AdministracionContrato data);
    Task<(bool Success, string Mensaje)> DeleteContrato(string LogTransaccionId, int lContratoId);
    Task<(IEnumerable<ListaAdministracionContrato> Data, bool Success, string Mensaje)> GetContratoXNroVenta(string LogTransaccionId, string sLote, string inicio, string fin);
    Task<(IEnumerable<ListaAdministracionContrato> Data, bool Success, string Mensaje, int Total)> GetAdministracionContratoFechaVentaResidual(string LogTransaccionId, string inicio, string fin);
}
