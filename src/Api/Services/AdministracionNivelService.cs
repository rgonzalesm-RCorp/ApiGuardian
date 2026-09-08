using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Services;

public sealed class AdministracionNivelService : IAdministracionNivelService
{
    private readonly IAdministracionNivelRepository _repository;

    public AdministracionNivelService(IAdministracionNivelRepository repository) =>
        _repository = repository;

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerAsync(string id)
    {
        try
        {
            var r = await _repository.GetNivel(id);
            return (r.Success, r.Mensaje, new { r.Nivel });
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
            var r = await _repository.GetNivelPagination(id, page, pageSize, search);
            return (r.Success, r.Mensaje, new { r.Nivel, r.Total });
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje)> InsertarAsync(
        AdministracionNivel data,
        string id
    )
    {
        try
        {
            var r = await _repository.GuardarNivel(id, data);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Mensaje)> ActualizarAsync(
        AdministracionNivel data,
        string id
    )
    {
        try
        {
            var r = await _repository.ModificarNivel(id, data);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Mensaje)> EliminarAsync(int nivelId, string id)
    {
        try
        {
            var r = await _repository.EliminarNivel(id, nivelId);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
