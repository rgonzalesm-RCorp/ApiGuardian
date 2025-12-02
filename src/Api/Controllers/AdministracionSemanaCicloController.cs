using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using Newtonsoft.Json;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministracionSemanaCicloController : ControllerBase
{
    private readonly IAdministracionSemanaCicloRepository _repository;
    private readonly ILogService _log;
    private readonly string NOMBREARCHIVO = "AdministracionSemanaCicloController.cs";

    public AdministracionSemanaCicloController(
        IAdministracionSemanaCicloRepository repository,
        ILogService log)
    {
        _repository = repository;
        _log = log;
    }

    [HttpGet("paginacion")]
    public async Task<IActionResult> GetPaginacion([FromHeader] int page, [FromHeader] int pageSize, [FromHeader] string? search)
    {
        long logId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string metodo = "GetPaginacion()";

        _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, $"Inicio paginación page={page}, pageSize={pageSize}");

        try
        {
            var resp = await _repository.GetSemanaCicloPagination(logId.ToString(), page, pageSize, search);
            return Ok(new { status = resp.Success, mensaje = resp.Mensaje, data = new { resp.Semanas, resp.Total } });
        }
        catch (Exception ex)
        {
            _log.Error(logId.ToString(), NOMBREARCHIVO, metodo, "Error paginando", ex);
            return Ok(new { status = false, mensaje = ex.Message });
        }
    }

    [HttpPost("insert")]
    public async Task<IActionResult> Insert([FromBody] AdministracionSemanaCicloABM data)
    {
        long logId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string metodo = "Insert()";

        _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, "Inicio inserción " + JsonConvert.SerializeObject(data));

        try
        {
            var resp = await _repository.GuardarSemanaCiclo(logId.ToString(), data);
            return Ok(new { status = resp.Success, mensaje = resp.Mensaje });
        }
        catch (Exception ex)
        {
            _log.Error(logId.ToString(), NOMBREARCHIVO, metodo, "Error insertando", ex);
            return Ok(new { status = false, mensaje = ex.Message });
        }
    }

    [HttpPut("update")]
    public async Task<IActionResult> Update([FromBody] AdministracionSemanaCicloABM data)
    {
        long logId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string metodo = "Update()";

        _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, "Inicio actualización " + JsonConvert.SerializeObject(data));

        try
        {
            var resp = await _repository.ModificarSemanaCiclo(logId.ToString(), data);
            return Ok(new { status = resp.Success, mensaje = resp.Mensaje });
        }
        catch (Exception ex)
        {
            _log.Error(logId.ToString(), NOMBREARCHIVO, metodo, "Error actualizando", ex);
            return Ok(new { status = false, mensaje = ex.Message });
        }
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> Delete([FromHeader] int lSemanaId)
    {
        long logId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string metodo = "Delete()";

        _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, $"Inicio eliminación ID={lSemanaId}");

        try
        {
            var resp = await _repository.EliminarSemanaCiclo(logId.ToString(), lSemanaId);
            return Ok(new { status = resp.Success, mensaje = resp.Mensaje });
        }
        catch (Exception ex)
        {
            _log.Error(logId.ToString(), NOMBREARCHIVO, metodo, "Error eliminando", ex);
            return Ok(new { status = false, mensaje = ex.Message });
        }
    }
}
