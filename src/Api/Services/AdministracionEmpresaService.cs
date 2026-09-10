using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Services;

public sealed class AdministracionEmpresaService : IAdministracionEmpresaService
{
    private readonly IAdministracionEmpresaRepository _repository;

    public AdministracionEmpresaService(IAdministracionEmpresaRepository repository) =>
        _repository = repository;

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerAsync(string id)
    {
        try
        {
            var r = await _repository.GetEmpresas(id);
            return (r.Success, r.Mensaje, new { r.Empresas });
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerPaginadoAsync(
        int page,
        int pageSize,
        string? search,
        string id
    )
    {
        try
        {
            var r = await _repository.GetEmpresasPagination(id, page, pageSize, search);
            return (r.Success, r.Mensaje, new { r.Empresas, r.Total });
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje)> InsertarAsync(
        AdministracionEmpresa data,
        string id
    )
    {
        try
        {
            var r = await _repository.GuardarEmpresa(id, data);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Mensaje)> ActualizarAsync(
        AdministracionEmpresa data,
        string id
    )
    {
        try
        {
            var r = await _repository.ModificarEmpresa(id, data);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Mensaje)> EliminarAsync(int empresaId, string id)
    {
        try
        {
            var r = await _repository.EliminarEmpresa(id, empresaId);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
