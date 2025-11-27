using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using Newtonsoft.Json;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministracionCicloFacturaController : ControllerBase
{
    private readonly IAdministracionCicloFacturaRepository _repository;
    private readonly ILogService _log;
    private readonly string NOMBREARCHIVO = "AdministracionCicloFacturaController.cs";
    public AdministracionCicloFacturaController(IAdministracionCicloFacturaRepository repository, ILogService log)
    {
        _repository = repository;
        _log = log;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAdministracionCiclofactura(
        [FromHeader(Name = "page")] int page,
        [FromHeader(Name = "pageSize")] int pageSize,
        [FromHeader(Name = "lCicloId")] int lCicloId
    )
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "GetAllAdministracionCiclofactura()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Inicio de metodo [page:{page}, pageSize:{pageSize}, lCicloId:{lCicloId}]");

            var responseCicloFactura = await _repository.GetAllAdministracionCiclofactura(page, pageSize, lCicloId);

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo,
                $"Fin de metodo: {responseCicloFactura.Success} - {responseCicloFactura.Mensaje}");

            return Ok(new
            {
                status = responseCicloFactura.Success,
                mensaje = responseCicloFactura.Mensaje,
                data = new
                {
                    dataList = responseCicloFactura.Data,
                    total = responseCicloFactura.Total
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
    public async Task<IActionResult> InsertAdministracionCiclofactura(AdministracionCicloFactura data)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "InsertAdministracionCiclofactura()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Inicio de metodo AdministracionCicloFactura: {JsonConvert.SerializeObject(data)}");

            var responseCicloFactura = await _repository.InsertAdministracionCiclofactura(data);

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo,
                $"Fin de metodo: {responseCicloFactura.success} - {responseCicloFactura.mensaje}");

            return Ok(new
            {
                status = responseCicloFactura.success,
                mensaje = responseCicloFactura.mensaje,
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
    public async Task<IActionResult> DeleteAdministracionCiclofactura(
        [FromHeader(Name = "lciclofactura")] int lciclofactura,
        [FromHeader(Name = "usuario")] string? usuario)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "DeleteAdministracionCiclofactura()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Inicio de metodo [lciclofactura:{lciclofactura}, usuario:{usuario}]");

            var responseCicloFactura = await _repository.DeleteAdministracionCiclofactura(lciclofactura, usuario);

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo,
                $"Fin de metodo: {responseCicloFactura.succes} - {responseCicloFactura.mensaje}");

            return Ok(new
            {
                status = responseCicloFactura.succes,
                mensaje = responseCicloFactura.mensaje,
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
