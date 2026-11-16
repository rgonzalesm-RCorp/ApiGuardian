using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;
public interface IVentasCnxRepository
{
    Task<(IEnumerable<ItemVentaCnx> Data, bool Success, string Mensaje)> GetVentaCnx(string LogTransaccionId, string inicio, string fin, MonedaCiclo moneda = MonedaCiclo.SUS);
    Task<(ItemVentaCnx Data, bool Success, string Mensaje)> GetClienteDocId(string LogTransaccionId, string docId);
}
