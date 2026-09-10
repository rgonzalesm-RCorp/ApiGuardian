using ApiGuardian.Application.Interfaces;

namespace CleanDapperApi.Api.Services;

public sealed class AdministracionBuscarAsesorService : IAdministracionBuscarAsesorService
{
    private readonly IAdministracionBuscarAsesorRepository _repository;
    private readonly ILogService _log;

    public AdministracionBuscarAsesorService(
        IAdministracionBuscarAsesorRepository repository,
        ILogService log
    )
    {
        _repository = repository;
        _log = log;
    }

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerAsesoresAsync(
        int contactoId,
        string logTransaccionId
    )
    {
        try
        {
            var response = await _repository.GetAsesoreSieteNiveles(logTransaccionId, contactoId);
            return (
                response.Success,
                response.Mensaje,
                new { dataFijos = response.DataFijos, dataActivos = response.DataActivos }
            );
        }
        catch (Exception ex)
        {
            _log.Error(
                logTransaccionId,
                nameof(AdministracionBuscarAsesorService),
                nameof(ObtenerAsesoresAsync),
                "Fin de metodo",
                ex
            );
            return (false, ex.Message, "");
        }
    }
}
