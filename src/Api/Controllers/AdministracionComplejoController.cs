using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using Newtonsoft.Json;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministracionComplejoController : ControllerBase
{
    private readonly ILogService _log;
    private readonly IAdministracionComplejoRepository _repository;
    private readonly string NOMBREARCHIVO = "AdministracionComplejoController.cs";

    public AdministracionComplejoController(IAdministracionComplejoRepository repository, ILogService log)
    {
        _repository = repository;
        _log = log;
    }

    // ================================
    // GET LISTADO
    // ================================
    [HttpGet]
    public async Task<IActionResult> GetComplejo()
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreMetodo = "GetComplejo()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreMetodo, "Inicio de método");

            var response = await _repository.GetComplejo(logTransaccionId.ToString());

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreMetodo,
                $"Fin de método: {response.Success} - {response.Mensaje}");

            return Ok(new
            {
                status = response.Success,
                mensaje = response.Mensaje,
                data = new { response.Complejos }
            });
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId.ToString(), NOMBREARCHIVO, nombreMetodo, "Fin con error", ex);

            return Ok(new
            {
                status = false,
                mensaje = ex.Message,
                data = ""
            });
        }
    }

    // ================================
    // GET PAGINACIÓN
    // ================================
    [HttpGet("paginacion")]
    public async Task<IActionResult> GetComplejoPagination(
        [FromHeader(Name = "page")] int page,
        [FromHeader(Name = "pageSize")] int pageSize,
        [FromHeader(Name = "search")] string? search
    )
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreMetodo = "GetComplejoPagination()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreMetodo,
                $"Inicio de método [page: {page}, pageSize: {pageSize}, search: {search}]");

            var response = await _repository.GetComplejoPagination(logTransaccionId.ToString(), page, pageSize, search);

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreMetodo,
                $"Fin de método: {response.Success} - {response.Mensaje}");

            return Ok(new
            {
                status = response.Success,
                mensaje = response.Mensaje,
                data = new { response.Complejos, response.Total }
            });
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId.ToString(), NOMBREARCHIVO, nombreMetodo, "Fin con error", ex);

            return Ok(new
            {
                status = false,
                mensaje = ex.Message,
                data = ""
            });
        }
    }

    // ================================
    // INSERTAR
    // ================================
    [HttpPost("insert")]
    public async Task<IActionResult> GuardarComplejo(AdministracionComplejoABM data)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreMetodo = "GuardarComplejo()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreMetodo,
                $"Inicio de método [data: {JsonConvert.SerializeObject(data, Formatting.Indented)}]");

            var response = await _repository.GuardarComplejo(logTransaccionId.ToString(), data);

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreMetodo,
                $"Fin de método: {response.Success} - {response.Mensaje}");

            return Ok(new
            {
                status = response.Success,
                mensaje = response.Mensaje,
                data = ""
            });
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId.ToString(), NOMBREARCHIVO, nombreMetodo, "Fin con error", ex);

            return Ok(new { status = false, mensaje = ex.Message, data = "" });
        }
    }

    // ================================
    // ACTUALIZAR
    // ================================
    [HttpPut("update")]
    public async Task<IActionResult> ModificarComplejo(AdministracionComplejoABM data)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreMetodo = "ModificarComplejo()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreMetodo,
                $"Inicio de método [data: {JsonConvert.SerializeObject(data, Formatting.Indented)}]");

            var response = await _repository.ModificarComplejo(logTransaccionId.ToString(), data);

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreMetodo,
                $"Fin de método: {response.Success} - {response.Mensaje}");

            return Ok(new
            {
                status = response.Success,
                mensaje = response.Mensaje,
                data = ""
            });
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId.ToString(), NOMBREARCHIVO, nombreMetodo, "Fin con error", ex);

            return Ok(new { status = false, mensaje = ex.Message, data = "" });
        }
    }

    // ================================
    // ELIMINAR
    // ================================
    [HttpDelete("delete")]
    public async Task<IActionResult> EliminarComplejo(
        [FromHeader(Name = "lComplejoId")] int LComplejoId
    )
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreMetodo = "EliminarComplejo()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreMetodo,
                $"Inicio de método [lComplejoId: {LComplejoId}]");

            var response = await _repository.EliminarComplejo(logTransaccionId.ToString(), LComplejoId);

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreMetodo,
                $"Fin de método: {response.Success} - {response.Mensaje}");

            return Ok(new
            {
                status = response.Success,
                mensaje = response.Mensaje,
                data = ""
            });
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId.ToString(), NOMBREARCHIVO, nombreMetodo, "Fin con error", ex);

            return Ok(new { status = false, mensaje = ex.Message, data = "" });
        }
    }
}
