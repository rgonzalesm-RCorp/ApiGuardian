using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IAdministracionDescuentoCicloTipoRepository
{
    Task<(IEnumerable<AdministracionDescuentoCicloTipo> Tipos, bool Success, string Mensaje)>
        GetDescuentoCicloTipo(string LogTransaccionId);

    Task<(IEnumerable<AdministracionDescuentoCicloTipo> Tipos, bool Success, string Mensaje, int Total)>
        GetDescuentoCicloTipoPagination(string LogTransaccionId, int page, int pageSize, string? search);

    Task<(bool Success, string Mensaje)> GuardarDescuentoCicloTipo(string LogTransaccionId, AdministracionDescuentoCicloTipo data);

    Task<(bool Success, string Mensaje)> ModificarDescuentoCicloTipo(string LogTransaccionId, AdministracionDescuentoCicloTipo data);

    Task<(bool Success, string Mensaje)> EliminarDescuentoCicloTipo(string LogTransaccionId, int LDescuentoCicloTipoId);
}
