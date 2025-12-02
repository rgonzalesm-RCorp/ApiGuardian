using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using Newtonsoft.Json;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministracionTipoContactoController : ControllerBase
{
    private readonly ILogService _log;
    private readonly IAdministracionTipoContactoRepository _repository;
    private readonly string NOMBREARCHIVO = "AdministracionTipoContactoController.cs";

    public AdministracionTipoContactoController(IAdministracionTipoContactoRepository repository, ILogService log)
    {
        _repository = repository;
        _log = log;
    }

    // ======================================
    // GET LISTADO
    // ======================================
    [HttpGet]
    public async Task<IActionResult> GetTipoContacto()
    {
        long logId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreMetodo = "GetTipoContacto()";

        try
        {
            _log.Info(logId.ToString(), NOMBREARCHIVO, nombreMetodo, "Inicio de método");

            var resp = await _repository.GetTipoContacto(logId.ToString());

            _log.Info(logId.ToString(), NOMBREARCHIVO, nombreMetodo,
                $"Fin método: {resp.Success} - {resp.Mensaje}");

            return Ok(new
            {
                status = resp.Success,
                mensaje = resp.Mensaje,
                data = new { resp.TipoContacto }
            });
        }
        catch (Exception ex)
        {
            _log.Error(logId.ToString(), NOMBREARCHIVO, nombreMetodo, "Error", ex);
            return Ok(new { status = false, mensaje = ex.Message });
        }
    }
    [HttpGet("paginacion")]
    public async Task<IActionResult> GetTipoContactoPagination(
        [FromHeader(Name = "page")] int page,
        [FromHeader(Name = "pageSize")] int pageSize,
        [FromHeader(Name = "search")] string? search
    )
    {
        long logId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string metodo = "GetTipoContactoPagination()";

        try
        {
            _log.Info(logId.ToString(), NOMBREARCHIVO, metodo,
                $"Inicio método [page: {page}, pageSize: {pageSize}, search: {search}]");

            var resp = await _repository.GetTipoContactoPagination(logId.ToString(), page, pageSize, search);

            _log.Info(logId.ToString(), NOMBREARCHIVO, metodo,
                $"Fin método: {resp.Success} - {resp.Mensaje}");

            return Ok(new
            {
                status = resp.Success,
                mensaje = resp.Mensaje,
                data = new { resp.TipoContacto, resp.Total }
            });
        }
        catch (Exception ex)
        {
            _log.Error(logId.ToString(), NOMBREARCHIVO, metodo, "Error", ex);
            return Ok(new { status = false, mensaje = ex.Message });
        }
    }

    // ======================================
    // INSERT
    // ======================================
    [HttpPost("insert")]
    public async Task<IActionResult> Guardar(AdministracionTipoContacto data)
    {
        long logId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string metodo = "Guardar()";

        try
        {
            _log.Info(logId.ToString(), NOMBREARCHIVO, metodo,
                $"Inicio método [data: {JsonConvert.SerializeObject(data, Formatting.Indented)}]");

            var resp = await _repository.GuardarTipoContacto(logId.ToString(), data);

            _log.Info(logId.ToString(), NOMBREARCHIVO, metodo,
                $"Fin método: {resp.Success} - {resp.Mensaje}");

            return Ok(new { status = resp.Success, mensaje = resp.Mensaje });
        }
        catch (Exception ex)
        {
            _log.Error(logId.ToString(), NOMBREARCHIVO, metodo, "Error", ex);
            return Ok(new { status = false, mensaje = ex.Message });
        }
    }

    // ======================================
    // UPDATE
    // ======================================
    [HttpPut("update")]
    public async Task<IActionResult> Modificar(AdministracionTipoContacto data)
    {
        long logId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string metodo = "Modificar()";

        try
        {
            _log.Info(logId.ToString(), NOMBREARCHIVO, metodo,
                $"Inicio método [data: {JsonConvert.SerializeObject(data, Formatting.Indented)}]");

            var resp = await _repository.ModificarTipoContacto(logId.ToString(), data);

            _log.Info(logId.ToString(), NOMBREARCHIVO, metodo,
                $"Fin método: {resp.Success} - {resp.Mensaje}");

            return Ok(new { status = resp.Success, mensaje = resp.Mensaje });
        }
        catch (Exception ex)
        {
            _log.Error(logId.ToString(), NOMBREARCHIVO, metodo, "Error", ex);
            return Ok(new { status = false, mensaje = ex.Message });
        }
    }

    // ======================================
    // DELETE
    // ======================================
    [HttpDelete("delete")]
    public async Task<IActionResult> Eliminar(
        [FromHeader(Name = "lTipoContactoId")] int LTipoContactoId
    )
    {
        long logId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string metodo = "Eliminar()";

        try
        {
            _log.Info(logId.ToString(), NOMBREARCHIVO, metodo,
                $"Inicio método [LTipoContactoId: {LTipoContactoId}]");

            var resp = await _repository.EliminarTipoContacto(logId.ToString(), LTipoContactoId);

            _log.Info(logId.ToString(), NOMBREARCHIVO, metodo,
                $"Fin método: {resp.Success} - {resp.Mensaje}");

            return Ok(new { status = resp.Success, mensaje = resp.Mensaje });
        }
        catch (Exception ex)
        {
            _log.Error(logId.ToString(), NOMBREARCHIVO, metodo, "Error", ex);
            return Ok(new { status = false, mensaje = ex.Message });
        }
    }
}
