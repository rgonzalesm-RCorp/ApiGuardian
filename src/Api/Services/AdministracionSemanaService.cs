using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Services;

public sealed class AdministracionSemanaService : IAdministracionSemanaService
{
    private readonly IAdministracionSemanaRepository _repository;

    public AdministracionSemanaService(IAdministracionSemanaRepository repository) =>
        _repository = repository;

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerAsync(string id)
    {
        try
        {
            var r = await _repository.GetSemana(id);
            return (r.Success, r.Mensaje, new { r.Semana });
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
            var r = await _repository.GetSemanaPagination(id, page, pageSize, search);
            return (r.Success, r.Mensaje, new { r.Semana, r.Total });
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje)> InsertarAsync(
        AdministracionSemana data,
        string id
    )
    {
        try
        {
            var r = await _repository.GuardarSemana(id, data);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Mensaje)> ActualizarAsync(
        AdministracionSemana data,
        string id
    )
    {
        try
        {
            var r = await _repository.ModificarSemana(id, data);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Mensaje)> EliminarAsync(int semanaId, string id)
    {
        try
        {
            var r = await _repository.EliminarSemana(id, semanaId);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
