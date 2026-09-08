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
using ApiGuardian.Infrastructure.Services;
using DocumentFormat.OpenXml.Bibliography;
namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BonoResidualController : ControllerBase
{
    private readonly IBonoResidualService _bonoResidualService;
    public BonoResidualController(IBonoResidualService bonoResidualService)
    {
        _bonoResidualService = bonoResidualService;
    }

    [HttpGet("get/cartera")]
    public async Task<IActionResult> GetCartera([FromHeader(Name = "Usuario")] string Usuario, [FromHeader(Name = "LCicloId")] int LCicloId)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var resultado = await _bonoResidualService.ObtenerCarteraAsync(Usuario, LCicloId, logTransaccionId.ToString());
        return Ok(new { status = resultado.Success, mensaje = resultado.Mensaje, data = resultado.Data });

    }
    [HttpPost("save/cartera")]
    public async Task<IActionResult> GuardarCartera([FromHeader(Name = "Usuario")] string Usuario, [FromHeader(Name = "LCicloId")] int LCicloId)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var resultado = await _bonoResidualService.GuardarCarteraAsync(Usuario, LCicloId, logTransaccionId.ToString());
        return Ok(new
        {
            status = resultado.Success,
            mensaje = resultado.Mensaje,
            data = resultado.Success
                ? (object)new { ini = resultado.Inicio, fin = resultado.Fin }
                : ""
        });

    }
    [HttpGet("get/cuota")]
    public async Task<IActionResult> GetCuota([FromHeader(Name = "Usuario")] string Usuario, [FromHeader(Name = "LCicloId")] int LCicloId)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var resultado = await _bonoResidualService.ObtenerCuotaAsync(Usuario, LCicloId, logTransaccionId.ToString());
        return Ok(new { status = resultado.Success, mensaje = resultado.Mensaje, data = resultado.Data });

    }
    [HttpPost("save/cuota")]
    public async Task<IActionResult> GuardarCuota([FromHeader(Name = "Usuario")] string Usuario, [FromHeader(Name = "LCicloId")] int LCicloId)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var resultado = await _bonoResidualService.GuardarCuotaAsync(Usuario, LCicloId, logTransaccionId.ToString());
        return Ok(new { status = resultado.Success, mensaje = resultado.Mensaje, data = resultado.Data });

    }
    [HttpGet("get/excedente")]
    public async Task<IActionResult> GetExcedente([FromHeader(Name = "Usuario")] string Usuario, [FromHeader(Name = "LCicloId")] int LCicloId)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var resultado = await _bonoResidualService.ObtenerExcedenteAsync(Usuario, LCicloId, logTransaccionId.ToString());
        return Ok(new { status = resultado.Success, mensaje = resultado.Mensaje, data = resultado.Data });

    }
    [HttpPost("save/excedente")]
    public async Task<IActionResult> GuardarExcedente([FromHeader(Name = "Usuario")] string Usuario, [FromHeader(Name = "LCicloId")] int LCicloId)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var resultado = await _bonoResidualService.GuardarExcedenteAsync(Usuario, LCicloId, logTransaccionId.ToString());
        return Ok(new
        {
            status = resultado.Success,
            mensaje = resultado.Mensaje,
            data = resultado.Success ? (object)new { listaExcedente = resultado.ListaExcedente } : ""
        });
    }

    [HttpGet("get/calculo/residual")]
    public async Task<IActionResult> GetBonoResidual([FromHeader(Name = "Usuario")] string Usuario, [FromHeader(Name = "LCicloId")] int LCicloId)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var resultado = await _bonoResidualService.ObtenerBonoResidualAsync(Usuario, LCicloId, logTransaccionId.ToString());
        return Ok(new { status = resultado.Success, mensaje = resultado.Mensaje, data = resultado.Data });

    }
    [HttpPost("save/calculo/residual")]
    public async Task<IActionResult> GuardarBonoResidual([FromHeader(Name = "Usuario")] string Usuario, [FromHeader(Name = "LCicloId")] int LCicloId)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var resultadoServicio = await _bonoResidualService.ProcesarBonoResidualAsync(Usuario, LCicloId, logTransaccionId.ToString());
        return Ok(new { status = resultadoServicio.Success, mensaje = resultadoServicio.Mensaje, data = resultadoServicio.Data });
    }


    [HttpGet("get/bono/par")]
    public async Task<IActionResult> ObtenerBonoPar([FromHeader(Name = "Usuario")] string Usuario, [FromHeader(Name = "LCicloId")] int LCicloId)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var resultado = await _bonoResidualService.ObtenerBonoParAsync(Usuario, LCicloId, logTransaccionId.ToString());
        return Ok(new { status = resultado.Success, mensaje = resultado.Mensaje, data = resultado.Data });

    }
    [HttpPost("save/bono/par")]
    public async Task<IActionResult> GuardarBonoPar([FromHeader(Name = "Usuario")] string Usuario, [FromHeader(Name = "LCicloId")] int LCicloId)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var resultado = await _bonoResidualService.GuardarBonoParAsync(Usuario, LCicloId, logTransaccionId.ToString());
        return Ok(new { status = resultado.Success, mensaje = resultado.Mensaje, data = resultado.Data });
    }

}
