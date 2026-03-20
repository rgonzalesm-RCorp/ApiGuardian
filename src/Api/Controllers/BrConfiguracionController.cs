using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Microsoft.AspNetCore.DataProtection.Repositories;
using DocumentFormat.OpenXml.Wordprocessing;
using Org.BouncyCastle.Ocsp;
using DocumentFormat.OpenXml.Office2019.Excel.RichData2;
using DocumentFormat.OpenXml.Drawing.Charts;

namespace CleanDapperApi.Api.Controllers;
[ApiController]
[Route("api/[controller]")]
public class BrConfiguracionController : ControllerBase
{
    private readonly IBrConfiguracionRepository _repo;
    private readonly ILogService _log;
    private const string NOMBREARCHIVO = "BrConfiguracionController.cs";

    public BrConfiguracionController(IBrConfiguracionRepository repo, ILogService log)
    {
        _repo = repo;
        _log = log;
    }

    [HttpGet("get/datos")]
    public async Task<IActionResult> GetDatos()
    {
        var logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

        try
        {
            var responseNivel = await _repo.GetNivel(logTransaccionId, "");
            var responseTipoProducto = await _repo.GetTipoProducto(logTransaccionId, "");


            
            
            return Ok(new 
            {
                status = responseNivel.Success,
                mensaje = responseTipoProducto.Mensaje,
                data = new
                {
                    Nivel = responseNivel.Data,
                    TipoProducto = responseTipoProducto.Data
                }
            });
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId, NOMBREARCHIVO, "Get", "Error", ex);
            return Ok(new 
            {
                status = false,
                mensaje = ex.Message,
                data = ""
            });
        }
    }

    [HttpGet("get/configuracion")]
    public async Task<IActionResult> Get()
    {
        var logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

        try
        {
            var responseConfiguracion = await _repo.GetConfiguracion(logTransaccionId, "");


            var resumen = responseConfiguracion.Data.GroupBy(x => new {x.BrConfiguracionId, x.LCicloId, x.Ciclo, x.TipoProducto, x.TipoProductoId})
            .Select(g => new
            {
                g.Key.BrConfiguracionId
                , g.Key.LCicloId
                , g.Key.Ciclo
                , g.Key.TipoProducto
                , g.Key.TipoProductoId
                , Details =  responseConfiguracion.Data.Where(x => x.BrConfiguracionId == g.Key.BrConfiguracionId).Select(
                    h => new
                    {
                        h.BrConfiguracionDetalleId
                        , h.NombreNivel
                        , h.Nivel
                        , h.PorcentajeComision
                    }
                ).ToList()
            })
            .ToList();
            
            return Ok(new 
            {
                status = responseConfiguracion.Success,
                mensaje = responseConfiguracion.Mensaje,
                data = new
                {
                    lista = resumen
                }
            });
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId, NOMBREARCHIVO, "Get", "Error", ex);
            return Ok(new 
            {
                status = false,
                mensaje = ex.Message,
                data = ""
            });
        }
    }

    [HttpPost("save/configuracion")]
    public async Task<IActionResult> Save([FromBody] BrConfiguracion data)
    {
        var logId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

        try
        {
            var resp = await _repo.GuardarConfiguracion(logId, "", data);
            return Ok(new { status = resp.Success, mensaje = resp.Mensaje });
        }
        catch (Exception ex)
        {
            _log.Error(logId, NOMBREARCHIVO, "Save", "Error", ex);
            return Ok(new { status = false, mensaje = ex.Message });
        }
    }
}