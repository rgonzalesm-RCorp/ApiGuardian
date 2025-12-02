using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using Newtonsoft.Json;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministracionTipoContratoController : ControllerBase
{
    private readonly IAdministracionTipoContratoRepository _repository;
    private readonly ILogService _log;
    private readonly string NOMBREARCHIVO = "AdministracionTipoContratoController.cs";

    public AdministracionTipoContratoController(IAdministracionTipoContratoRepository repository, ILogService log)
    {
        _repository = repository;
        _log = log;
    }

    // ======================================
    [HttpGet]
    public async Task<IActionResult> GetTipoContrato()
    {
        long logId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string metodo = "GetTipoContrato()";

        try
        {
            var resp = await _repository.GetTipoContrato(logId.ToString());
            return Ok(new { status = resp.Success, mensaje = resp.Mensaje, data = new { resp.TipoContrato } });
        }
        catch (Exception ex)
        {
            _log.Error(logId.ToString(), NOMBREARCHIVO, metodo, "Error", ex);
            return Ok(new { status = false, mensaje = ex.Message });
        }
    }

    // ======================================
    [HttpGet("paginacion")]
    public async Task<IActionResult> GetPaginacion(
        [FromHeader] int page,
        [FromHeader] int pageSize,
        [FromHeader] string? search)
    {
        long logId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string metodo = "GetPaginacion()";

        try
        {
            var resp = await _repository.GetTipoContratoPagination(logId.ToString(), page, pageSize, search);
            return Ok(new { status = resp.Success, mensaje = resp.Mensaje, data = new { resp.TipoContrato, resp.Total } });
        }
        catch (Exception ex)
        {
            _log.Error(logId.ToString(), NOMBREARCHIVO, metodo, "Error", ex);
            return Ok(new { status = false, mensaje = ex.Message });
        }
    }

    // ======================================
    [HttpPost("insert")]
    public async Task<IActionResult> Insert(AdministracionTipoContratoABM data)
    {
        long logId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string metodo = "Insert()";

        try
        {
            var resp = await _repository.GuardarTipoContrato(logId.ToString(), data);
            return Ok(new { status = resp.Success, mensaje = resp.Mensaje });
        }
        catch (Exception ex)
        {
            _log.Error(logId.ToString(), NOMBREARCHIVO, metodo, "Error", ex);
            return Ok(new { status = false, mensaje = ex.Message });
        }
    }

    // ======================================
    [HttpPut("update")]
    public async Task<IActionResult> Update(AdministracionTipoContratoABM data)
    {
        long logId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string metodo = "Update()";

        try
        {
            var resp = await _repository.ModificarTipoContrato(logId.ToString(), data);
            return Ok(new { status = resp.Success, mensaje = resp.Mensaje });
        }
        catch (Exception ex)
        {
            _log.Error(logId.ToString(), NOMBREARCHIVO, metodo, "Error", ex);
            return Ok(new { status = false, mensaje = ex.Message });
        }
    }

    // ======================================
    [HttpDelete("delete")]
    public async Task<IActionResult> Delete([FromHeader] int lTipoContratoId)
    {
        long logId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string metodo = "Delete()";

        try
        {
            var resp = await _repository.EliminarTipoContrato(logId.ToString(), lTipoContratoId);
            return Ok(new { status = resp.Success, mensaje = resp.Mensaje });
        }
        catch (Exception ex)
        {
            _log.Error(logId.ToString(), NOMBREARCHIVO, metodo, "Error", ex);
            return Ok(new { status = false, mensaje = ex.Message });
        }
    }
}
