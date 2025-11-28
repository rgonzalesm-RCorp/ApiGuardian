using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IAdministracionCuentaBancoRepository
{
    Task<(CuentaBanco Data, bool Success, string Mensaje)> GetCuentaBanco(string LogTransaccionId, int lContactoId);
    Task<(IEnumerable<CuentaBanco> Data, bool Success, string Mensaje, int Total)> GetAllCuentaBanco(string LogTransaccionId, int page, int pageSize, string? search);
    Task<( bool Success, string Mensaje)> UpdateCuentaBanco(string LogTransaccionId, DataCuentaBanco data);

}
