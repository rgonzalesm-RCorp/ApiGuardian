using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using Newtonsoft.Json;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministracionDescuentoComisionController : ControllerBase
{
    private readonly IAdministracionDescuentoComisionRepository _repository;
    private readonly string NOMBREARCHIVO = "AdministracionDescuentoComisionController.cs";
    private readonly ILogService _log;
    public AdministracionDescuentoComisionController(IAdministracionDescuentoComisionRepository repository, ILogService log)
    {
        _repository = repository;
        _log = log;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAdministracionCObservacionComision(
        [FromHeader(Name = "lContactoId")] int lContactoId,
        [FromHeader(Name = "lCicloId")] int lCicloId,
        [FromHeader(Name = "lSemanaId")] int lSemanaId
    )
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "GetAllAdministracionCObservacionComision()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Inicio de metodo [lContactoId:{lContactoId}, lCicloId:{lCicloId}, lSemanaId:{lSemanaId}]");

            var responseComision = await _repository.GetComision(logTransaccionId.ToString(),lContactoId, lCicloId, lSemanaId);
            var responseDetalle = await _repository.GetDetalleDescuentoCiclo(logTransaccionId.ToString(),lCicloId, lContactoId);

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo,
                $"Fin de metodo: {responseComision.Success} - {responseComision.Mensaje}");

            return Ok(new
            {
                status = responseComision.Success,
                mensaje = responseComision.Mensaje,
                data = new
                {
                    comision = responseComision.Data,
                    detalleDescuento = responseDetalle.Data
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
    [HttpDelete("delete")]
    public async Task<IActionResult> EliminarDescuento(
        [FromHeader(Name = "lDescuentoDetalleId")] int LDescuentoDetalleId,
        [FromHeader(Name = "lContactoId")] int LContactoId,
        [FromHeader(Name = "lCicloId")] int LCicloId,
        [FromHeader(Name = "usuario")] string? Usuario
        //int LContactoId, int LCicloId
    )
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "EliminarDescuento()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Inicio de metodo [LDescuentoDetalleId:{LDescuentoDetalleId}, LContactoId:{LContactoId}, LCicloId:{LCicloId}, Usuario:{Usuario}]");

            var responseDescuento = await _repository.EliminarDescuento(logTransaccionId.ToString(),LDescuentoDetalleId, LContactoId, LCicloId, Usuario);

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo,
                $"Fin de metodo: {responseDescuento.Success} - {responseDescuento.Mensaje}");

            return Ok(new
            {
                status = responseDescuento.Success,
                mensaje = responseDescuento.Mensaje,
                data = ""
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
    [HttpPost("insert")]
    public async Task<IActionResult> InsertarDescuento( DataDescuento DataDescuento)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "InsertarDescuento()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Inicio de metodo DataDescuento:{JsonConvert.SerializeObject(DataDescuento, Formatting.Indented)}");

            var responseDescuento = await _repository.InsertarDescuento(logTransaccionId.ToString(), DataDescuento);

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo,
                $"Fin de metodo: {responseDescuento.Success} - {responseDescuento.Mensaje}");

            return Ok(new
            {
                status = responseDescuento.Success,
                mensaje = responseDescuento.Mensaje,
                data = ""
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
