using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using Newtonsoft.Json;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministracionEmpresaController : ControllerBase
{
    private readonly IAdministracionEmpresaRepository _repository;
    private readonly ILogService _log;
    private readonly string NOMBREARCHIVO = "AdministracionEmpresaController.cs";

    public AdministracionEmpresaController(
        IAdministracionEmpresaRepository repository,
        ILogService log)
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
            var resp = await _repository.GetEmpresas(logId.ToString());

            _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, $"Fin de método: Success={resp.Success} - Msg={resp.Mensaje}");

            return Ok(new { status = resp.Success, mensaje = resp.Mensaje, data = new { resp.Empresas } });
        }
        catch (Exception ex)
        {
            _log.Error(logId.ToString(), NOMBREARCHIVO, metodo, "Error en GetSemana", ex);
            return Ok(new { status = false, mensaje = ex.Message, data = "" });
        }
    }

    [HttpGet("paginacion")]
    public async Task<IActionResult> GetPaginacion(
        [FromHeader] int page,
        [FromHeader] int pageSize,
        [FromHeader] string? search)
    {
        long logId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string metodo = "GetPaginacion()";

        _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, $"Inicio paginación page={page}, pageSize={pageSize}, search={search}");

        try
        {
            var resp = await _repository.GetEmpresasPagination(logId.ToString(), page, pageSize, search);

            return Ok(new 
            {
                status = resp.Success,
                mensaje = resp.Mensaje,
                data = new { resp.Empresas, resp.Total }
            });
        }
        catch (Exception ex)
        {
            _log.Error(logId.ToString(), NOMBREARCHIVO, metodo, "Error en paginación", ex);
            return Ok(new { status = false, mensaje = ex.Message });
        }
    }

    [HttpPost("insert")]
    public async Task<IActionResult> Insert([FromBody] AdministracionEmpresa data)
    {
        long logId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string metodo = "Insert()";

        _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, $"Inicio inserción: {JsonConvert.SerializeObject(data)}");

        try
        {
            var resp = await _repository.GuardarEmpresa(logId.ToString(), data);
            return Ok(new { status = resp.Success, mensaje = resp.Mensaje });
        }
        catch (Exception ex)
        {
            _log.Error(logId.ToString(), NOMBREARCHIVO, metodo, "Error insertando empresa", ex);
            return Ok(new { status = false, mensaje = ex.Message });
        }
    }

    [HttpPut("update")]
    public async Task<IActionResult> Update([FromBody] AdministracionEmpresa data)
    {
        long logId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string metodo = "Update()";

        _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, $"Inicio actualización: {JsonConvert.SerializeObject(data)}");

        try
        {
            var resp = await _repository.ModificarEmpresa(logId.ToString(), data);
            return Ok(new { status = resp.Success, mensaje = resp.Mensaje });
        }
        catch (Exception ex)
        {
            _log.Error(logId.ToString(), NOMBREARCHIVO, metodo, "Error actualizando empresa", ex);
            return Ok(new { status = false, mensaje = ex.Message });
        }
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> Delete([FromHeader] int lEmpresaId)
    {
        long logId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string metodo = "Delete()";

        _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, $"Inicio eliminación ID={lEmpresaId}");

        try
        {
            var resp = await _repository.EliminarEmpresa(logId.ToString(), lEmpresaId);
            return Ok(new { status = resp.Success, mensaje = resp.Mensaje });
        }
        catch (Exception ex)
        {
            _log.Error(logId.ToString(), NOMBREARCHIVO, metodo, "Error eliminando empresa", ex);
            return Ok(new { status = false, mensaje = ex.Message });
        }
    }
}
