using ApiGuardian.Application.Interfaces;

namespace CleanDapperApi.Api.Services;

public sealed class AdministracionComplejoService : IAdministracionComplejoService
{
    private readonly IAdministracionComplejoRepository _repository;

    public AdministracionComplejoService(IAdministracionComplejoRepository repository) =>
        _repository = repository;

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerAsync(string id)
    {
        try
        {
            var r = await _repository.GetComplejo(id);
            return (r.Success, r.Mensaje, new { r.Complejos });
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
            var r = await _repository.GetComplejoPagination(id, page, pageSize, search);
            return (r.Success, r.Mensaje, new { r.Complejos, r.Total });
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje)> InsertarAsync(
        AdministracionComplejoABM data,
        string id
    )
    {
        try
        {
            var r = await _repository.GuardarComplejo(id, data);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Mensaje)> ActualizarAsync(
        AdministracionComplejoABM data,
        string id
    )
    {
        try
        {
            var r = await _repository.ModificarComplejo(id, data);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Mensaje)> EliminarAsync(int complejoId, string id)
    {
        try
        {
            var r = await _repository.EliminarComplejo(id, complejoId);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
