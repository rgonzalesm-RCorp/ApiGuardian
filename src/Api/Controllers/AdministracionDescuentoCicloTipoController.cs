using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using Newtonsoft.Json;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministracionDescuentoCicloTipoController : ControllerBase
{
    private readonly IAdministracionDescuentoCicloTipoRepository _repository;
    private readonly ILogService _log;
    private readonly string NOMBREARCHIVO = "AdministracionDescuentoCicloTipoController.cs";

    public AdministracionDescuentoCicloTipoController(
        IAdministracionDescuentoCicloTipoRepository repository,
        ILogService log)
    {
        _repository = repository;
        _log = log;
    }

    // ============================================================
    // PAGINACIÓN
    // ============================================================
    [HttpGet("paginacion")]
    public async Task<IActionResult> GetPaginacion(
        [FromHeader] int page,
        [FromHeader] int pageSize,
        [FromHeader] string? search)
    {
        long logId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string metodo = "GetPaginacion()";

        _log.Info(
            logId.ToString(),
            NOMBREARCHIVO,
            metodo,
            $"Inicio paginación - Página={page}, PageSize={pageSize}, Search={search}"
        );

        try
        {
            var resp = await _repository.GetDescuentoCicloTipoPagination(logId.ToString(), page, pageSize, search);

            _log.Info(
                logId.ToString(),
                NOMBREARCHIVO,
                metodo,
                $"Respuesta exitosa - Total={resp.Total}, Success={resp.Success}"
            );

            return Ok(new
            {
                status = resp.Success,
                mensaje = resp.Mensaje,
                data = new { resp.Tipos, resp.Total }
            });
        }
        catch (Exception ex)
        {
            _log.Error(logId.ToString(), NOMBREARCHIVO, metodo, "Error en paginación", ex);
            return Ok(new { status = false, mensaje = ex.Message });
        }
    }

    // ============================================================
    // INSERTAR
    // ============================================================
    [HttpPost("insert")]
    public async Task<IActionResult> Insert([FromBody] AdministracionDescuentoCicloTipo data)
    {
        long logId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string metodo = "Insert()";

        _log.Info(
            logId.ToString(),
            NOMBREARCHIVO,
            metodo,
            $"Inicio inserción - Data: {JsonConvert.SerializeObject(data)}"
        );

        try
        {
            var resp = await _repository.GuardarDescuentoCicloTipo(logId.ToString(), data);

            _log.Info(
                logId.ToString(),
                NOMBREARCHIVO,
                metodo,
                $"Resultado inserción - Success={resp.Success}, Msg={resp.Mensaje}"
            );

            return Ok(new { status = resp.Success, mensaje = resp.Mensaje });
        }
        catch (Exception ex)
        {
            _log.Error(logId.ToString(), NOMBREARCHIVO, metodo, "Error en inserción", ex);
            return Ok(new { status = false, mensaje = ex.Message });
        }
    }

    // ============================================================
    // MODIFICAR
    // ============================================================
    [HttpPut("update")]
    public async Task<IActionResult> Update([FromBody] AdministracionDescuentoCicloTipo data)
    {
        long logId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string metodo = "Update()";

        _log.Info(
            logId.ToString(),
            NOMBREARCHIVO,
            metodo,
            $"Inicio actualización - Data: {JsonConvert.SerializeObject(data)}"
        );

        try
        {
            var resp = await _repository.ModificarDescuentoCicloTipo(logId.ToString(), data);

            _log.Info(
                logId.ToString(),
                NOMBREARCHIVO,
                metodo,
                $"Resultado actualización - Success={resp.Success}, Msg={resp.Mensaje}"
            );

            return Ok(new { status = resp.Success, mensaje = resp.Mensaje });
        }
        catch (Exception ex)
        {
            _log.Error(logId.ToString(), NOMBREARCHIVO, metodo, "Error en actualización", ex);
            return Ok(new { status = false, mensaje = ex.Message });
        }
    }

    // ============================================================
    // ELIMINAR
    // ============================================================
    [HttpDelete("delete")]
    public async Task<IActionResult> Delete([FromHeader] int lDescuentoCicloTipoId)
    {
        long logId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string metodo = "Delete()";

        _log.Info(
            logId.ToString(),
            NOMBREARCHIVO,
            metodo,
            $"Inicio eliminación - ID={lDescuentoCicloTipoId}"
        );

        try
        {
            var resp = await _repository.EliminarDescuentoCicloTipo(logId.ToString(), lDescuentoCicloTipoId);

            _log.Info(
                logId.ToString(),
                NOMBREARCHIVO,
                metodo,
                $"Resultado eliminación - Success={resp.Success}, Msg={resp.Mensaje}"
            );

            return Ok(new { status = resp.Success, mensaje = resp.Mensaje });
        }
        catch (Exception ex)
        {
            _log.Error(logId.ToString(), NOMBREARCHIVO, metodo, "Error en eliminación", ex);
            return Ok(new { status = false, mensaje = ex.Message });
        }
    }
}
