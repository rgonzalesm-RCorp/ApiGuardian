using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Services;

public sealed class AdministracionCicloService : IAdministracionCicloService
{
    private readonly IAdministracionCicloRepository _repository;

    public AdministracionCicloService(IAdministracionCicloRepository repository) =>
        _repository = repository;

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerAsync(string id)
    {
        try
        {
            var r = await _repository.GetCiclos(id);
            return (r.Success, r.Mensaje, r.Ciclos);
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
            var r = await _repository.GetCiclosPagination(id, page, pageSize, search);
            return (r.Success, r.Mensaje, new { ciclos = r.Ciclos, total = r.Total });
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje)> InsertarAsync(
        AdministracionCicloABM data,
        string id
    )
    {
        try
        {
            var r = await _repository.GuardarCiclo(id, data);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Mensaje)> ActualizarAsync(
        AdministracionCicloABM data,
        string id
    )
    {
        try
        {
            var r = await _repository.ModificarCiclo(id, data);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Mensaje)> EliminarAsync(int cicloId, string id)
    {
        try
        {
            var r = await _repository.EliminarCiclo(id, cicloId);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
