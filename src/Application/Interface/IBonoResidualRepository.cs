using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IBonoResidualRepository
{
    Task<(IEnumerable<TCartera> ListaCartera, bool Success, string Mensaje, int counter)> GetCartera(string LogTransaccionId,string Usuario, int page, int pageSize);
    Task<(IEnumerable<TCartera> ListaCartera, bool Success, string Mensaje, int counter)> GetCarteraAll(string LogTransaccionId,string Usuario);
    Task<(bool Success, string Mensaje)> GuardarCartera(string LogTransaccionId,string Usuario, List<TCartera> ListadoCartera);

    Task<(IEnumerable<TCuota> ListaCuota, bool Success, string Mensaje, int counter)> GetCuota(string LogTransaccionId,string Usuario, int page, int pageSize, string inicio, string fin);
    Task<(IEnumerable<TCuota> ListaCuota, bool Success, string Mensaje)> GetCuotaAll(string LogTransaccionId,string Usuario, string inicio, string fin);

}
