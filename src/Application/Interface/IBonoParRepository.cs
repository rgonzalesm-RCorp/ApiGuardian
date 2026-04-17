using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IBonoParRepository
{
    Task<(IEnumerable<ItemBonoPar> Data, bool Success, string Mensaje)> GetBonoPar(string LogTransaccionId, string Usuario, string Inicio, string Fin);

}
