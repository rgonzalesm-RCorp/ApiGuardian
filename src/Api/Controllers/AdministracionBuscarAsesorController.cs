using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministracionBuscarAsesorController : ControllerBase
{
    private readonly IAdministracionBuscarAsesorRepository _repository;
    private readonly ILogService _log;
    private readonly string NOMBREARCHIVO = "AdministracionBuscarAsesorController.cs";
    public AdministracionBuscarAsesorController(IAdministracionBuscarAsesorRepository repository, ILogService log)
    {
        _repository = repository;
        _log = log;
    }
    [HttpGet]
    public async Task<IActionResult> GetAsesoreSieteNiveles([FromHeader(Name = "lContactoId")] int lContactoId)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "GetAsesoreSieteNiveles()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Inicio de metodo [lContactoId:{lContactoId}]");

            var responseCicloFactura = await _repository.GetAsesoreSieteNiveles(logTransaccionId.ToString(), lContactoId);

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo,
                $"Fin de metodo: {responseCicloFactura.Success} - {responseCicloFactura.Mensaje}");

            return Ok(new
            {
                status = responseCicloFactura.Success,
                mensaje = responseCicloFactura.Mensaje,
                data = new
                {
                    dataFijos = responseCicloFactura.DataFijos,
                    dataActivos = responseCicloFactura.DataActivos
                }
            });
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, "Fin de metodo", ex);

            return Ok(new
            {
                status = false,
                mensaje = ex.Message,
                data = ""
            });
        }
    }
}
