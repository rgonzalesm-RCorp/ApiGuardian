using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IAdministracionBancoRepository
{
    Task<(IEnumerable<ListaAdministracionBanco> Data, bool Success, string Mensaje)> GetAllBanco();
    Task<( bool Success, string Mensaje)> UpdateBanco(AdministracionBanco data);
    Task<( bool Success, string Mensaje)> InsertBanco(AdministracionBanco data);
    Task<( bool Success, string Mensaje)> DeleteBanco(int lBancoId);
    Task<(IEnumerable<ListaMoneda> Data, bool Success, string Mensaje)> GetAllMoneda();
}
