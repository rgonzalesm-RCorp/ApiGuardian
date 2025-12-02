using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IAdministracionEmpresaRepository
{
    Task<(IEnumerable<AdministracionEmpresa> Empresas, bool Success, string Mensaje)>
        GetEmpresas(string LogTransaccionId);

    Task<(IEnumerable<AdministracionEmpresa> Empresas, bool Success, string Mensaje, int Total)>
        GetEmpresasPagination(string LogTransaccionId, int page, int pageSize, string? search);

    Task<(bool Success, string Mensaje)> GuardarEmpresa(string LogTransaccionId, AdministracionEmpresa data);

    Task<(bool Success, string Mensaje)> ModificarEmpresa(string LogTransaccionId, AdministracionEmpresa data);

    Task<(bool Success, string Mensaje)> EliminarEmpresa(string LogTransaccionId, int LEmpresaId);
}
