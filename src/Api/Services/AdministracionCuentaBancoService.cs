using ApiGuardian.Application.Interfaces;

namespace CleanDapperApi.Api.Services;

public sealed class AdministracionCuentaBancoService : IAdministracionCuentaBancoService
{
    private readonly IAdministracionCuentaBancoRepository _repository;

    public AdministracionCuentaBancoService(IAdministracionCuentaBancoRepository repository) =>
        _repository = repository;

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerAsync(
        int contactoId,
        string id
    )
    {
        try
        {
            var r = await _repository.GetCuentaBanco(id, contactoId);
            return (r.Success, r.Mensaje, new { cuentaBanco = r.Data });
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
            var r = await _repository.GetAllCuentaBanco(id, page, pageSize, search);
            return (r.Success, r.Mensaje, new { listaCuentaBanco = r.Data, total = r.Total });
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje)> ActualizarAsync(
        DataCuentaBanco data,
        string id
    )
    {
        try
        {
            var r = await _repository.UpdateCuentaBanco(id, data);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
