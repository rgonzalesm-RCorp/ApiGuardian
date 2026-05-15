using System.Data.SqlTypes;
using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface ICasosEspecialesRepository
{
    Task<(IEnumerable<ItemVentaCnx> VentasCasosEspeciales, bool Success, string Mensaje)> GetVentasCasosEspeciales(string LogTransaccionId, string Usuario, string Inicio, string Fin);

}
