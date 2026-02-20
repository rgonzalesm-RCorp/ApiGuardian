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
            string complejosId = string.Join(",", pC_ConfigVtaPersonal.Complejos.Select(x => x.LComplejo_id));
            var verificarComplejos = await _configuracionProcesoComisionesRepository.VerificarComplejos(logTransaccionId.ToString(), complejosId, pC_ConfigVtaPersonal.LCiclo_id);
            if (verificarComplejos.Success)
            {
                if(verificarComplejos.Listado.Count() > 0)
                {
                    return Ok(new
                    {
                        status = false,
                        mensaje = "LOS SIGUIENTES COMPLEJOS YA SE ENCUENTRA EN OTRA CONFIGURACION DEL MISMO CICLO.",
                        swall = true,
                        data = verificarComplejos.Listado
                    });
                }
            }else
            {
                return Ok(new
                {
                    status = false,
                    mensaje = "No se logro realizar la verificacion de los complejos.",
                    swall = false,
                    data = ""
                });
            }
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, "Inicio de metodo");
            var reponseSaveConfiguracion = await _configuracionProcesoComisionesRepository.GuardarConfiguracionComisionVentaPersonal(logTransaccionId.ToString(), pC_ConfigVtaPersonal);
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Fin de metodo.");

            return Ok(new
            {
                status = reponseSaveConfiguracion.Success ? true : false,
                mensaje = reponseSaveConfiguracion.Mensaje,
                swall = false,
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
                swall = false,
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
    [HttpDelete("delete/vta/cnx")]
    public async Task<IActionResult> DeleteConfiguracionVentaPersona([FromHeader(Name = "usuario")]string usuario, [FromHeader(Name = "ConfigVtaPersonalId")]int ConfigVtaPersonalId)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "DeleteConfiguracionVentaPersona()";
        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, "Inicio de metodo");
            var reponseSaveConfiguracion = await _configuracionProcesoComisionesRepository.DeleteConfiguracionComisionVentaPersonal(logTransaccionId.ToString(), usuario, ConfigVtaPersonalId);
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

}
