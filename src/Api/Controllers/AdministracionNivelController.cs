using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Microsoft.AspNetCore.DataProtection.Repositories;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministracionNivelController : ControllerBase
{
    private readonly ILogService _log;
    private readonly IAdministracionNivelRepository _repository;
    private readonly string NOMBREARCHIVO = "AdministracionNivelController.cs";

    public AdministracionNivelController(IAdministracionNivelRepository repository, ILogService log)
    {
        _repository = repository;
        _log = log;
    }
    [HttpGet]
    public async Task<IActionResult> GetNivel()
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "GetNivel()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, "Inicio de metodo");

            var responseNivel = await _repository.GetNivel(logTransaccionId.ToString());

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo,
                $"Fin de metodo: {responseNivel.Success} - {responseNivel.Mensaje}");

            return Ok(new
            {
                status = responseNivel.Success,
                mensaje = responseNivel.Mensaje,
                data = new
                {
                    responseNivel.Nivel
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
    [HttpGet("paginacion")]
    public async Task<IActionResult> GetNivelPaginacion(
        [FromHeader(Name = "page")] int page,
        [FromHeader(Name = "pageSize")] int pageSize,
        [FromHeader(Name = "search")] string? search
    )
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "GetNivel()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, "Inicio de metodo");

            var responseNivel = await _repository.GetNivelPagination(logTransaccionId.ToString(), page, pageSize, search);

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo,
                $"Fin de metodo: {responseNivel.Success} - {responseNivel.Mensaje}");

            return Ok(new
            {
                status = responseNivel.Success,
                mensaje = responseNivel.Mensaje,
                data = new
                {
                    responseNivel.Nivel,
                    responseNivel.Total
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
    [HttpPost("insert")]
    public async Task<IActionResult> GuardarNivel(AdministracionNivel data)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "GuardarNivel()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Inicio de metodo [data: {JsonConvert.SerializeObject(data, Formatting.Indented)}]");

            var responseNivel = await _repository.GuardarNivel(logTransaccionId.ToString(), data);

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo,
                $"Fin de metodo: {responseNivel.Success} - {responseNivel.Mensaje}");

            return Ok(new
            {
                status = responseNivel.Success,
                mensaje = responseNivel.Mensaje,
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
    public async Task<IActionResult> ModificarNivel(AdministracionNivel data)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "ModificarNivel()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Inicio de metodo [data: {JsonConvert.SerializeObject(data, Formatting.Indented)}]");

            var responseNivel = await _repository.ModificarNivel(logTransaccionId.ToString(), data);

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo,
                $"Fin de metodo: {responseNivel.Success} - {responseNivel.Mensaje}");

            return Ok(new
            {
                status = responseNivel.Success,
                mensaje = responseNivel.Mensaje,
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
    public async Task<IActionResult> ModificarNivel(
        [FromHeader(Name = "lNivelId")] int lNivelId
    )
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "ModificarNivel()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Inicio de metodo [lNivelId: {lNivelId}]");

            var responseNivel = await _repository.EliminarNivel(logTransaccionId.ToString(), lNivelId);

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo,
                $"Fin de metodo: {responseNivel.Success} - {responseNivel.Mensaje}");

            return Ok(new
            {
                status = responseNivel.Success,
                mensaje = responseNivel.Mensaje,
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
