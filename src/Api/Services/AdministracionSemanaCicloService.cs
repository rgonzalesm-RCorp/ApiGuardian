using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Services;

public sealed class AdministracionSemanaCicloService : IAdministracionSemanaCicloService
{
    private readonly IAdministracionSemanaCicloRepository _repository;

    public AdministracionSemanaCicloService(IAdministracionSemanaCicloRepository repository) =>
        _repository = repository;

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerPaginadoAsync(
        int page,
        int pageSize,
        string? search,
        string id
    )
    {
        try
        {
            var r = await _repository.GetSemanaCicloPagination(id, page, pageSize, search);
            return (r.Success, r.Mensaje, new { r.Semanas, r.Total });
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje)> InsertarAsync(
        AdministracionSemanaCicloABM data,
        string id
    )
    {
        try
        {
            var r = await _repository.GuardarSemanaCiclo(id, data);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Mensaje)> ActualizarAsync(
        AdministracionSemanaCicloABM data,
        string id
    )
    {
        try
        {
            var r = await _repository.ModificarSemanaCiclo(id, data);
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
            var r = await _repository.EliminarSemanaCiclo(id, semanaId);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
