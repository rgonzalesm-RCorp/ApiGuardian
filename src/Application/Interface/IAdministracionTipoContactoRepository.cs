using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IAdministracionTipoContactoRepository
{
    Task<(IEnumerable<AdministracionTipoContacto> TipoContacto, bool Success, string Mensaje)> GetTipoContacto(string LogTransaccionId);

    Task<(IEnumerable<AdministracionTipoContacto> TipoContacto, bool Success, string Mensaje, int Total)>
        GetTipoContactoPagination(string LogTransaccionId, int page, int pageSize, string? search);

    Task<(bool Success, string Mensaje)> GuardarTipoContacto(string LogTransaccionId, AdministracionTipoContacto data);
    Task<(bool Success, string Mensaje)> ModificarTipoContacto(string LogTransaccionId, AdministracionTipoContacto data);
    Task<(bool Success, string Mensaje)> EliminarTipoContacto(string LogTransaccionId, int LTipoContactoId);
}
