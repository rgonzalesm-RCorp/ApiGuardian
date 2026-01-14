using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using Newtonsoft.Json;

namespace CleanDapperApi.Api.Controllers;
[ApiController]
[Route("api/[controller]")]
public class AdministracionDetalleFacturaController : ControllerBase
{
    private readonly IAdministracionDetalleFacturaRepository _repo;
    private readonly ILogService _log;
    private const string NOMBREARCHIVO = "AdministracionDetalleFacturaController.cs";

    public AdministracionDetalleFacturaController(
        IAdministracionDetalleFacturaRepository repo,
        ILogService log)
    {
        _repo = repo;
        _log = log;
    }

    [HttpGet("paginacion")]
    public async Task<IActionResult> GetPaginacion(
        [FromHeader] int page,
        [FromHeader] int pageSize)
    {
        long logId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        const string metodo = "GetPaginacion()";

        _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, "Inicio");

        try
        {
            var resp = await _repo.GetDetalleFacturaPagination(logId.ToString(), page, pageSize);
            _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, $"Fin de método: Success={resp.Success} - Msg={resp.Mensaje}");

            return Ok(new { status = resp.Success, mensaje = resp.Mensaje, data = new { resp.Data, resp.Total } });
        }
        catch (Exception ex)
        {
            _log.Error(logId.ToString(), NOMBREARCHIVO, metodo, "Error", ex);
            return Ok(new { status = false, mensaje = ex.Message });
        }
    }

    [HttpPost("insert")]
    public async Task<IActionResult> Insert([FromBody] AdministracionDetalleFactura data)
    {
        long logId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        const string metodo = "Insert()";

        _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, JsonConvert.SerializeObject(data));

        try
        {
            var resp = await _repo.GuardarDetalleFactura(logId.ToString(), data);
            _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, $"Fin de método: Success={resp.Success} - Msg={resp.Mensaje}");

            return Ok(new { status = resp.Success, mensaje = resp.Mensaje });
        }
        catch (Exception ex)
        {
            _log.Error(logId.ToString(), NOMBREARCHIVO, metodo, "Error insert", ex);
            return Ok(new { status = false, mensaje = ex.Message });
        }
    }

    [HttpPut("update")]
    public async Task<IActionResult> Update([FromBody] AdministracionDetalleFactura data)
    {
        long logId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        const string metodo = "Update()";

        _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, JsonConvert.SerializeObject(data));

        try
        {
            var resp = await _repo.ModificarDetalleFactura(logId.ToString(), data);
            _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, $"Fin de método: Success={resp.Success} - Msg={resp.Mensaje}");

            return Ok(new { status = resp.Success, mensaje = resp.Mensaje });
        }
        catch (Exception ex)
        {
            _log.Error(logId.ToString(), NOMBREARCHIVO, metodo, "Error update", ex);
            return Ok(new { status = false, mensaje = ex.Message });
        }
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> Delete([FromHeader] int lDetalleFacturaId)
    {
        long logId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        const string metodo = "Delete()";

        _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, $"ID={lDetalleFacturaId}");

        try
        {
            var resp = await _repo.EliminarDetalleFactura(logId.ToString(), lDetalleFacturaId);
            _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, $"Fin de método: Success={resp.Success} - Msg={resp.Mensaje}");

            return Ok(new { status = resp.Success, mensaje = resp.Mensaje });
        }
        catch (Exception ex)
        {
            _log.Error(logId.ToString(), NOMBREARCHIVO, metodo, "Error delete", ex);
            return Ok(new { status = false, mensaje = ex.Message });
        }
    }
    [HttpGet("tipo/comision")]
    public async Task<IActionResult> GetTipoComision()
    {
        long logId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        const string metodo = "Delete()";

        _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, $"Inicio");

        try
        {
            var resp = await _repo.GetTipoComision(logId.ToString());
            _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, $"Fin de método: Success={resp.Success} - Msg={resp.Mensaje}");

            return Ok(new { status = resp.Success, mensaje = resp.Mensaje
                , data = new {tipoComision= resp.Data} 
            });
        }
        catch (Exception ex)
        {
            _log.Error(logId.ToString(), NOMBREARCHIVO, metodo, "Error delete", ex);
            return Ok(new { status = false, mensaje = ex.Message });
        }
    }
}
