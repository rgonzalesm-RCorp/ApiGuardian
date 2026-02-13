using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Microsoft.AspNetCore.DataProtection.Repositories;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfiguracionProcesoComisionesController : ControllerBase
{
    private readonly ILogService _log;
    private readonly IConfiguracionProcesoComisionesRepository _configuracionProcesoComisionesRepository;

    private readonly string NOMBREARCHIVO = "UtilsController.cs";

    public ConfiguracionProcesoComisionesController(ILogService log, IConfiguracionProcesoComisionesRepository configuracionProcesoComisionesRepository )
    {
        _log = log;
        _configuracionProcesoComisionesRepository = configuracionProcesoComisionesRepository;
    }
    [HttpPost("vta/cnx")]
    public async Task<IActionResult> GuardarConfiguracionVentaPersona([FromBody] PC_ConfigVtaPersonal pC_ConfigVtaPersonal)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "GetVentaCnx()";
        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, "Inicio de metodo");
            var reponseSaveConfiguracion = await _configuracionProcesoComisionesRepository.GuardarConfiguracionComisionVentaPersonal(logTransaccionId.ToString(), pC_ConfigVtaPersonal);
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Fin de metodo.");

            return Ok(new
            {
                status = reponseSaveConfiguracion.Success ? true : false,
                mensaje = reponseSaveConfiguracion.Mensaje,
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
    [HttpGet("get/vta/cnx")]
    public async Task<IActionResult> GetConfiguracionVentaPersona()
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "GetVentaCnx()";
        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, "Inicio de metodo");
            var reponseSaveConfiguracion = await _configuracionProcesoComisionesRepository.GETConfiguracionComisionVentaPersonal(logTransaccionId.ToString());
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Fin de metodo.");

            return Ok(new
            {
                status = reponseSaveConfiguracion.Success ? true : false,
                mensaje = reponseSaveConfiguracion.Mensaje,
                data = reponseSaveConfiguracion.pC_ConfigVtaPersonal
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
