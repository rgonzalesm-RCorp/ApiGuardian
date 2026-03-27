using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IAdministracionContratoRepository
{
    Task<(IEnumerable<ListaAdministracionContrato> Data, bool Success, string Mensaje, int Total)> GetAllAdministracionContrato(string LogTransaccionId, int page, int pageSize, string? search);
    Task<(IEnumerable<ItemVentaComision> Data, bool Success, string Mensaje)> GetContratoFecha(string LogTransaccionId, string inicio, string fin);
    Task<( bool Success, string Mensaje)> InsertContrato(string LogTransaccionId, AdministracionContrato data);
    Task<(bool Success, string Mensaje)> UpdateContrato(string LogTransaccionId, AdministracionContrato data);
    Task<(IEnumerable<ListaAdministracionContrato> Data, bool Success, string Mensaje)> GetContratoXNroVenta(string LogTransaccionId, string sLote, string inicio, string fin);
}
