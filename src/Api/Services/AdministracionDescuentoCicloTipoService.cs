using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Services;

public sealed class AdministracionDescuentoCicloTipoService
    : IAdministracionDescuentoCicloTipoService
{
    private readonly IAdministracionDescuentoCicloTipoRepository _repository;

    public AdministracionDescuentoCicloTipoService(
        IAdministracionDescuentoCicloTipoRepository repository
    ) => _repository = repository;

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerPaginadoAsync(
        int page,
        int pageSize,
        string? search,
        string id
    )
    {
        try
        {
            var r = await _repository.GetDescuentoCicloTipoPagination(id, page, pageSize, search);
            return (r.Success, r.Mensaje, new { r.Tipos, r.Total });
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje)> InsertarAsync(
        AdministracionDescuentoCicloTipo data,
        string id
    )
    {
        try
        {
            var r = await _repository.GuardarDescuentoCicloTipo(id, data);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Mensaje)> ActualizarAsync(
        AdministracionDescuentoCicloTipo data,
        string id
    )
    {
        try
        {
            var r = await _repository.ModificarDescuentoCicloTipo(id, data);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Mensaje)> EliminarAsync(
        int descuentoCicloTipoId,
        string id
    )
    {
        try
        {
            var r = await _repository.EliminarDescuentoCicloTipo(id, descuentoCicloTipoId);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
