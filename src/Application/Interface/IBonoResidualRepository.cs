using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IBonoResidualRepository
{
    Task<(IEnumerable<TCartera> ListaCartera, bool Success, string Mensaje, int counter)> GetCartera(string LogTransaccionId,string Usuario, int page, int pageSize);
    Task<(IEnumerable<TCartera> ListaCartera, bool Success, string Mensaje, int counter)> GetCarteraAll(string LogTransaccionId,string Usuario);
    Task<(bool Success, string Mensaje)> GuardarCartera(string LogTransaccionId,string Usuario, List<TCartera> ListadoCartera);

    Task<(IEnumerable<TCuota> ListaCuota, bool Success, string Mensaje)> GetCuota(string LogTransaccionId,string Usuario, string inicio, string fin);
    Task<(bool Success, string Mensaje)> GuardarCuota(string LogTransaccionId,string Usuario, List<TCuota> ListaCuota);
}
