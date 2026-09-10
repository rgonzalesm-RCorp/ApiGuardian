using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Services;

public sealed class AdministracionTipoContactoService : IAdministracionTipoContactoService
{
    private readonly IAdministracionTipoContactoRepository _repository;

    public AdministracionTipoContactoService(IAdministracionTipoContactoRepository repository) =>
        _repository = repository;

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerAsync(string id)
    {
        try
        {
            var r = await _repository.GetTipoContacto(id);
            return (r.Success, r.Mensaje, new { r.TipoContacto });
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
            var r = await _repository.GetTipoContactoPagination(id, page, pageSize, search);
            return (r.Success, r.Mensaje, new { r.TipoContacto, r.Total });
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje)> InsertarAsync(
        AdministracionTipoContacto data,
        string id
    )
    {
        try
        {
            var r = await _repository.GuardarTipoContacto(id, data);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Mensaje)> ActualizarAsync(
        AdministracionTipoContacto data,
        string id
    )
    {
        try
        {
            var r = await _repository.ModificarTipoContacto(id, data);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Mensaje)> EliminarAsync(int tipoContactoId, string id)
    {
        try
        {
            var r = await _repository.EliminarTipoContacto(id, tipoContactoId);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
