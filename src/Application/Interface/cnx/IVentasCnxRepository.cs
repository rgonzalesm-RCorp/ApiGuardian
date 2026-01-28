namespace ApiGuardian.Application.Interfaces;
public interface IVentasCnxRepository
{
    Task<(IEnumerable<ItemVentaCnx> Data, bool Success, string Mensaje)> GetVentaCnx(string LogTransaccionId, string inicio, string fin);
    Task<(ItemVentaCnx Data, bool Success, string Mensaje)> GetClienteDocId(string LogTransaccionId, string docId);
}
