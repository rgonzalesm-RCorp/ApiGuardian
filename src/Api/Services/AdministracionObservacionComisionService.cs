using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Services;

public sealed class AdministracionObservacionComisionService
    : IAdministracionObservacionComisionService
{
    private readonly IAdministracionObservacionComisionRepository _repository;

    public AdministracionObservacionComisionService(
        IAdministracionObservacionComisionRepository repository
    ) => _repository = repository;

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerPaginadoAsync(
        int page,
        int pageSize,
        string? search,
        int cicloId,
        string id
    )
    {
        try
        {
            var r = await _repository.GetAllAdministracionCObservacionComisionAsync(
                id,
                page,
                pageSize,
                search,
                cicloId
            );
            return (r.Success, r.Mensaje, new { data = r.Data, total = r.Total });
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje)> InsertarAsync(
        AdministracionObservacionComision data,
        string id
    )
    {
        try
        {
            var r = await _repository.InsertAdministracionObservacionComision(id, data);
            return (r.succes, r.mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Mensaje)> ActualizarAsync(
        AdministracionObservacionComision data,
        string id
    )
    {
        try
        {
            var r = await _repository.UpdateAdministracionObservacionComision(id, data);
            return (r.succes, r.mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Mensaje)> EliminarAsync(
        int observacionId,
        string? usuario,
        string id
    )
    {
        try
        {
            var r = await _repository.DeleteAdministracionObservacionComision(
                id,
                observacionId,
                usuario
            );
            return (r.succes, r.mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
