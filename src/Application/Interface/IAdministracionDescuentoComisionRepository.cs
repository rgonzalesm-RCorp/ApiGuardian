using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IAdministracionDescuentoComisionRepository
{
    Task<(DataComision Data, bool Success, string Mensaje)> GetComision(int LContactoId, int LCicloId, int LSemanaId);
    Task<(IEnumerable<ListaAdministracionDescuentoCiclo> Data, bool Success, string Mensaje)> GetDetalleDescuentoCiclo(int LCicloId, int LContactoId);
    Task<(bool Success, string Mensaje)> EliminarDescuento(int LDescuentoDetalleId, int lContactoId, int LCicloId, string? Usuario);
    Task<(bool Success, string Mensaje)> InsertarDescuento(DataDescuento DataDescuento);
}
