using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IRetencionEmpresaRepository
{
    Task<(bool Success, string Mensaje, List<RetencionEmpresaItem> Data)> ObtenerAsync(
        string logTransaccionId,
        int cicloId
    );

    Task<(bool Success, string Mensaje, int Insertados)> InsertarAsync(
        string logTransaccionId,
        string usuario,
        List<RetencionEmpresaItem> retenciones
    );
}
