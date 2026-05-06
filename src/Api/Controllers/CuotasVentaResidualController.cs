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
public class CuotasVentaResidualController: ControllerBase
{
    private readonly ICuotasVentaResidualRepository _repo;
    private readonly ILogService _log;
    private const string NOMBREARCHIVO = "BrConfiguracionController.cs";

    public CuotasVentaResidualController(ICuotasVentaResidualRepository repo, ILogService log)
    {
        _repo = repo;
        _log = log;
    }

    [HttpGet("cuotas/venta/residual")]
    public async Task<IActionResult> GetDatos([FromHeader(Name = "Usuario")] string Usuario, [FromHeader(Name = "LCicloId")] int LCicloId ,[FromHeader(Name = "Inicio")]  string Inicio, [FromHeader(Name = "Fin")] string Fin )
    {
        var logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        _log.Info(logTransaccionId, NOMBREARCHIVO, "GetDatos", $"Inicio GetDatos() Usuario: {Usuario}");

        try
        {
            var ResponseCuotasVentaRecidual = await _repo.GetCuotasVentasResidual(logTransaccionId, Usuario, Inicio, Fin);
           
            return Ok(new 
            {
                status = ResponseCuotasVentaRecidual.Success,
                mensaje = ResponseCuotasVentaRecidual.Mensaje,
                data = new
                {
                    ResponseCuotasVentaRecidual.ListadoCuotasVentasResidual.ToList().Count
           

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

}