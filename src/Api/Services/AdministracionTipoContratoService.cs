using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Services;

public sealed class AdministracionTipoContratoService : IAdministracionTipoContratoService
{
    private readonly IAdministracionTipoContratoRepository _repository;

    public AdministracionTipoContratoService(IAdministracionTipoContratoRepository repository) =>
        _repository = repository;

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerAsync(string id)
    {
        try
        {
            var r = await _repository.GetTipoContrato(id);
            return (r.Success, r.Mensaje, new { r.TipoContrato });
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
            var r = await _repository.GetTipoContratoPagination(id, page, pageSize, search);
            return (r.Success, r.Mensaje, new { r.TipoContrato, r.Total });
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje)> InsertarAsync(
        AdministracionTipoContratoABM data,
        string id
    )
    {
        try
        {
            var r = await _repository.GuardarTipoContrato(id, data);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Mensaje)> ActualizarAsync(
        AdministracionTipoContratoABM data,
        string id
    )
    {
        try
        {
            var r = await _repository.ModificarTipoContrato(id, data);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Mensaje)> EliminarAsync(int tipoContratoId, string id)
    {
        try
        {
            var r = await _repository.EliminarTipoContrato(id, tipoContratoId);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
