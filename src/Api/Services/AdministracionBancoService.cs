using ApiGuardian.Application.Interfaces;

namespace CleanDapperApi.Api.Services;

public sealed class AdministracionBancoService : IAdministracionBancoService
{
    private readonly IAdministracionBancoRepository _repository;

    public AdministracionBancoService(IAdministracionBancoRepository repository) =>
        _repository = repository;

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerBancosAsync(string id)
    {
        try
        {
            var r = await _repository.GetAllBanco(id);
            return (r.Success, r.Mensaje, new { listaBanco = r.Data });
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerMonedasAsync(string id)
    {
        try
        {
            var r = await _repository.GetAllMoneda(id);
            return (r.Success, r.Mensaje, new { listaMoneda = r.Data });
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje)> InsertarAsync(
        AdministracionBanco data,
        string id
    )
    {
        try
        {
            var r = await _repository.InsertBanco(id, data);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Mensaje)> ActualizarAsync(
        AdministracionBanco data,
        string id
    )
    {
        try
        {
            var r = await _repository.UpdateBanco(id, data);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Mensaje)> EliminarAsync(
        int bancoId,
        string? usuario,
        string id
    )
    {
        try
        {
            var r = await _repository.DeleteBanco(id, bancoId, usuario);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
