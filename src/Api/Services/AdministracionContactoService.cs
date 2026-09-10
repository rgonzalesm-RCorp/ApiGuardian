using ApiGuardian.Application.Interfaces;

namespace CleanDapperApi.Api.Services;

public sealed class AdministracionContactoService : IAdministracionContactoService
{
    private readonly IAdministracionContactoRepository _repository;

    public AdministracionContactoService(IAdministracionContactoRepository repository) =>
        _repository = repository;

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerAsync(
        int page,
        int pageSize,
        string? search,
        string id
    )
    {
        try
        {
            var r = await _repository.GetAllAdministracionContacto(id, page, pageSize, search);
            return (r.Success, r.Mensaje, new { listaContacto = r.Data, total = r.Total });
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje)> InsertarAsync(
        AdministracionContacto data,
        string id
    )
    {
        try
        {
            var r = await _repository.InsertContacto(id, data);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Mensaje)> ActualizarAsync(
        AdministracionContacto data,
        string id
    )
    {
        try
        {
            var r = await _repository.UpdateContacto(id, data);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Mensaje)> DarDeBajaAsync(
        AdministracionContactoBaja data,
        string id
    )
    {
        try
        {
            var r = await _repository.BajaContacto(id, data);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Mensaje, object Data)> VerificarEstadoAsync(
        string usuario,
        string documento,
        string id
    )
    {
        try
        {
            var r = await _repository.VerificarEstadoContacto(id, usuario, documento);
            return (r.Success, r.Mensaje, new { Estado = r.Estado });
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }
}
