using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using Newtonsoft.Json;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministracionObservacionComisionController : ControllerBase
{
    private readonly IAdministracionObservacionComisionRepository _repository;
    private readonly string NOMBREARCHIVO = "AdministracionObservacionComisionController.cs";
    private readonly ILogService _log;
    public AdministracionObservacionComisionController(IAdministracionObservacionComisionRepository repository, ILogService log)
    {
        _repository = repository;
        _log = log;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAdministracionCObservacionComision(
        [FromHeader(Name = "page")] int page,
        [FromHeader(Name = "pageSize")] int pageSize,
        [FromHeader(Name = "search")] string? search,
        [FromHeader(Name = "lCicloId")] int lCicloId
    )
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "GetAllAdministracionCObservacionComision()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Inicio de metodo [page:{page}, pageSize:{pageSize}, search:{search}, lCicloId:{lCicloId}]");

            var response = await _repository.GetAllAdministracionCObservacionComisionAsync(logTransaccionId.ToString(), page, pageSize, search, lCicloId);

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo,
                $"Fin de metodo: {response.Success} - {response.Mensaje}");

            return Ok(new
            {
                status = response.Success,
                mensaje = response.Mensaje,
                data = new
                {
                    data = response.Data,
                    total = response.Total
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
    [HttpPost("register")]
    public async Task<IActionResult> InsertAdministracionObservacionComision(AdministracionObservacionComision data)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "InsertAdministracionObservacionComision()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Inicio de metodo AdministracionObservacionComision:{JsonConvert.SerializeObject(data, Formatting.Indented)}");

            var responseInsert = await _repository.InsertAdministracionObservacionComision(logTransaccionId.ToString(), data);

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo,
                $"Fin de metodo: {responseInsert.succes} - {responseInsert.mensaje}");

            return Ok(new
            {
                status = responseInsert.succes,
                mensaje = responseInsert.mensaje,
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
    [HttpPut("update")]
    public async Task<IActionResult> UpdateAdministracionObservacionComision(AdministracionObservacionComision data)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "UpdateAdministracionObservacionComision()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Inicio de metodo AdministracionObservacionComision:{JsonConvert.SerializeObject(data, Formatting.Indented)}");

            var responseUpdate = await _repository.UpdateAdministracionObservacionComision(logTransaccionId.ToString(), data);

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo,
                $"Fin de metodo: {responseUpdate.succes} - {responseUpdate.mensaje}");

            return Ok(new
            {
                status = responseUpdate.succes,
                mensaje = responseUpdate.mensaje,
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
    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteAdministracionObservacionComision(
        [FromHeader(Name = "lObservacionId")] int lObservacionId,
        [FromHeader(Name = "usuario")] string? usuario)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "DeleteAdministracionObservacionComision()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Inicio de metodo [lObservacionId:{lObservacionId}, usuario:{usuario}]");

            var responseDelete = await _repository.DeleteAdministracionObservacionComision(logTransaccionId.ToString(), lObservacionId, usuario);

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo,
                $"Fin de metodo: {responseDelete.succes} - {responseDelete.mensaje}");

            return Ok(new
            {
                status = responseDelete.succes,
                mensaje = responseDelete.mensaje,
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
