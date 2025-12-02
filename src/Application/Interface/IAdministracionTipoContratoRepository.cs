using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IAdministracionTipoContratoRepository
{
    Task<(IEnumerable<AdministracionTipoContratoABM> TipoContrato, bool Success, string Mensaje)> GetTipoContrato(string LogTransaccionId);

    Task<(IEnumerable<AdministracionTipoContratoABM> TipoContrato, bool Success, string Mensaje, int Total)>
        GetTipoContratoPagination(string LogTransaccionId, int page, int pageSize, string? search);

    Task<(bool Success, string Mensaje)> GuardarTipoContrato(string LogTransaccionId, AdministracionTipoContratoABM data);
    Task<(bool Success, string Mensaje)> ModificarTipoContrato(string LogTransaccionId, AdministracionTipoContratoABM data);
    Task<(bool Success, string Mensaje)> EliminarTipoContrato(string LogTransaccionId, int LTipoContratoId);
}
