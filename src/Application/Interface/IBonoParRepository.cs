using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IBonoParRepository
{
    Task<(IEnumerable<ItemBonoPar> Data, bool Success, string Mensaje)> GetBonoPar(string LogTransaccionId, string Usuario, string Inicio, string Fin);
    Task<(bool Success, string Mensaje)> SaveBonoPar(string LogTransaccionId, string Usuario,int LCicloId,  List<ItemBonoPar> Listado);

}
