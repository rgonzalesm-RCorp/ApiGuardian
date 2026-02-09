namespace ApiGuardian.Application.Interfaces;
public interface IAdministracionBancoRepository
{
    Task<(IEnumerable<ListaAdministracionBanco> Data, bool Success, string Mensaje)> GetAllBanco(string LogTransaccionId);
    Task<( bool Success, string Mensaje)> UpdateBanco(string LogTransaccionId, AdministracionBanco data);
    Task<( bool Success, string Mensaje)> InsertBanco(string LogTransaccionId, AdministracionBanco data);
    Task<( bool Success, string Mensaje)> DeleteBanco(string LogTransaccionId, int lBancoId, string? usuario);
    Task<(IEnumerable<ListaMoneda> Data, bool Success, string Mensaje)> GetAllMoneda(string LogTransaccionId);
}
