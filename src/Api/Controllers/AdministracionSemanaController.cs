using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using Newtonsoft.Json;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministracionSemanaController : ControllerBase
{
    private readonly IAdministracionSemanaRepository _repository;
    private readonly ILogService _log;
    private readonly string NOMBREARCHIVO = "AdministracionSemanaController.cs";

    public AdministracionSemanaController(IAdministracionSemanaRepository repository, ILogService log)
    {
        _repository = repository;
        _log = log;
    }

    [HttpGet]
    public async Task<IActionResult> GetSemana()
    {
        long logId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string metodo = "GetSemana()";

        _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, "Inicio de método");

        try
        {
            var resp = await _repository.GetSemana(logId.ToString());

            _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, $"Fin de método: Success={resp.Success} - Msg={resp.Mensaje}");

            return Ok(new { status = resp.Success, mensaje = resp.Mensaje, data = new { resp.Semana } });
        }
        catch (Exception ex)
        {
            _log.Error(logId.ToString(), NOMBREARCHIVO, metodo, "Error en GetSemana", ex);
            return Ok(new { status = false, mensaje = ex.Message, data = "" });
        }
    }

    [HttpGet("paginacion")]
    public async Task<IActionResult> GetSemanaPaginacion(
        [FromHeader(Name = "page")] int page,
        [FromHeader(Name = "pageSize")] int pageSize,
        [FromHeader(Name = "search")] string? search)
    {
        long logId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string metodo = "GetSemanaPaginacion()";

        _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, $"Inicio paginación - page={page}, pageSize={pageSize}, search={search}");

        try
        {
            var resp = await _repository.GetSemanaPagination(logId.ToString(), page, pageSize, search);

            _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, $"Fin paginación - Success={resp.Success} - Total={resp.Total}");

            return Ok(new { status = resp.Success, mensaje = resp.Mensaje, data = new { resp.Semana, resp.Total } });
        }
        catch (Exception ex)
        {
            _log.Error(logId.ToString(), NOMBREARCHIVO, metodo, "Error en paginación", ex);
            return Ok(new { status = false, mensaje = ex.Message, data = "" });
        }
    }

    [HttpPost("insert")]
    public async Task<IActionResult> GuardarSemana([FromBody] AdministracionSemana data)
    {
        long logId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string metodo = "GuardarSemana()";

        _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, $"Inicio inserción - Data: {JsonConvert.SerializeObject(data)}");

        try
        {
            var resp = await _repository.GuardarSemana(logId.ToString(), data);

            _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, $"Fin inserción - Success={resp.Success} - Msg={resp.Mensaje}");

            return Ok(new { status = resp.Success, mensaje = resp.Mensaje });
        }
        catch (Exception ex)
        {
            _log.Error(logId.ToString(), NOMBREARCHIVO, metodo, "Error en inserción", ex);
            return Ok(new { status = false, mensaje = ex.Message });
        }
    }

    [HttpPut("update")]
    public async Task<IActionResult> ModificarSemana([FromBody] AdministracionSemana data)
    {
        long logId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string metodo = "ModificarSemana()";

        _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, $"Inicio actualización - Data: {JsonConvert.SerializeObject(data)}");

        try
        {
            var resp = await _repository.ModificarSemana(logId.ToString(), data);

            _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, $"Fin actualización - Success={resp.Success} - Msg={resp.Mensaje}");

            return Ok(new { status = resp.Success, mensaje = resp.Mensaje });
        }
        catch (Exception ex)
        {
            _log.Error(logId.ToString(), NOMBREARCHIVO, metodo, "Error en actualización", ex);
            return Ok(new { status = false, mensaje = ex.Message });
        }
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> EliminarSemana([FromHeader(Name = "lSemanaId")] int lSemanaId)
    {
        long logId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string metodo = "EliminarSemana()";

        _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, $"Inicio eliminación - lSemanaId={lSemanaId}");

        try
        {
            var resp = await _repository.EliminarSemana(logId.ToString(), lSemanaId);

            _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, $"Fin eliminación - Success={resp.Success} - Msg={resp.Mensaje}");

            return Ok(new { status = resp.Success, mensaje = resp.Mensaje });
        }
        catch (Exception ex)
        {
            _log.Error(logId.ToString(), NOMBREARCHIVO, metodo, "Error en eliminación", ex);
            return Ok(new { status = false, mensaje = ex.Message });
        }
    }
}
