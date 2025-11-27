using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Microsoft.AspNetCore.DataProtection.Repositories;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UtilsController : ControllerBase
{
    private readonly ILogService _log;
    private readonly IUtilsRepository _utilsRepository;
    private readonly string NOMBREARCHIVO = "UtilsController.cs";

    public UtilsController(IUtilsRepository utilsRepository, ILogService log)
    {
        _utilsRepository = utilsRepository;
        _log = log;
    }
    [HttpGet("administracion/ciclo")]
    public async Task<IActionResult> GetAdministracionCiclo()
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "GetAdministracionCiclo()";
        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, "Inicio de metodo");
            var responseCiclos = await _utilsRepository.GetCiclos();
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Fin de metodo: {responseCiclos.Success} - {responseCiclos.Mensaje}");
            return Ok(new
            {
                status = responseCiclos.Success ? true : false,
                mensaje = responseCiclos.Mensaje,
                data = responseCiclos.Ciclos
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
    [HttpGet("administracion/semana/ciclo")]
    public async Task<IActionResult> GetAdministracionSemanaCiclo([FromHeader(Name = "lCicloId")] int lCicloId)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "GetAdministracionSemanaCiclo()";
        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, "Inicio de metodo");
            var responseCiclosSemana = await _utilsRepository.GetSemanaCiclosAsync(lCicloId);
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Fin de metodo: {JsonConvert.SerializeObject(responseCiclosSemana)}");

            return Ok(new
            {
                status = responseCiclosSemana.Success ? true : false,
                mensaje = responseCiclosSemana.Mensaje,
                data = responseCiclosSemana.Semanas
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

    [HttpGet("administracion/complejo")]
    public async Task<IActionResult> GetAdministracionComplejo()
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "GetAdministracionComplejo()";
        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, "Inicio de metodo");

            var responseComplejo = await _utilsRepository.GetComplejo();
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Fin de metodo: { responseComplejo.Success } - {responseComplejo.Mensaje}");

            return Ok(new
            {
                status = responseComplejo.Success ? true : false,
                mensaje = responseComplejo.Mensaje,
                data = new {
                    responseComplejo.Complejo
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
    [HttpGet("administracion/departamento")]
    public async Task<IActionResult> GetDepartamento([FromHeader(Name = "lPaisId")] int lPaisId = 2)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "GetDepartamento()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, "Inicio de metodo");

            var responseDepartamento = await _utilsRepository.GetDepartamento(lPaisId);

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, 
                $"Fin de metodo: {responseDepartamento.Success} - {responseDepartamento.Mensaje}");

            return Ok(new
            {
                status = responseDepartamento.Success,
                mensaje = responseDepartamento.Mensaje,
                data = new
                {
                    responseDepartamento.Departamento
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
    [HttpGet("administracion/tipo/contrato")]
    public async Task<IActionResult> GetTipoContrato()
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "GetTipoContrato()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, "Inicio de metodo");

            var responseTipoContrato = await _utilsRepository.GetTipoContrato();

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo,
                $"Fin de metodo: {responseTipoContrato.Success} - {responseTipoContrato.Mensaje}");

            return Ok(new
            {
                status = responseTipoContrato.Success,
                mensaje = responseTipoContrato.Mensaje,
                data = new
                {
                    responseTipoContrato.TipoContrato
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
    [HttpGet("administracion/estado/contrato")]
    public async Task<IActionResult> GetEstadoContrato()
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "GetEstadoContrato()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, "Inicio de metodo");

            var responseEstadoContrato = await _utilsRepository.GetEstadoContrato();

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo,
                $"Fin de metodo: {responseEstadoContrato.Success} - {responseEstadoContrato.Mensaje}");

            return Ok(new
            {
                status = responseEstadoContrato.Success,
                mensaje = responseEstadoContrato.Mensaje,
                data = new
                {
                    responseEstadoContrato.EstadoContrato
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
    [HttpGet("administracion/nivel")]
    public async Task<IActionResult> GetNivel()
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "GetNivel()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, "Inicio de metodo");

            var responseNivel = await _utilsRepository.GetNivel();

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo,
                $"Fin de metodo: {responseNivel.Success} - {responseNivel.Mensaje}");

            return Ok(new
            {
                status = responseNivel.Success,
                mensaje = responseNivel.Mensaje,
                data = new
                {
                    responseNivel.Nivel
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
    [HttpGet("administracion/tipo/baja")]
    public async Task<IActionResult> GetTipoBaja()
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "GetTipoBaja()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, "Inicio de metodo");

            var responseTipoBaja = await _utilsRepository.GetTipoBaja();

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo,
                $"Fin de metodo: {responseTipoBaja.Success} - {responseTipoBaja.Mensaje}");

            return Ok(new
            {
                status = responseTipoBaja.Success,
                mensaje = responseTipoBaja.Mensaje,
                data = new
                {
                    responseTipoBaja.TipoBaja
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
    [HttpGet("administracion/pais")]
    public async Task<IActionResult> GetPais()
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "GetPais()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, "Inicio de metodo");

            var responsePais = await _utilsRepository.GetPais();

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo,
                $"Fin de metodo: {responsePais.Success} - {responsePais.Mensaje}");

            return Ok(new
            {
                status = responsePais.Success,
                mensaje = responsePais.Mensaje,
                data = new
                {
                    responsePais.Pais
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
    [HttpGet("tipo/descuento")]
    public async Task<IActionResult> GetTipoDescuento()
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "GetTipoDescuento()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, "Inicio de metodo");

            var responseTipoDescuento = await _utilsRepository.GetTipoDescuento();

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo,
                $"Fin de metodo: {responseTipoDescuento.Success} - {responseTipoDescuento.Mensaje}");

            return Ok(new
            {
                status = responseTipoDescuento.Success,
                mensaje = responseTipoDescuento.Mensaje,
                data = new
                {
                    responseTipoDescuento.TipoDescuento
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
}
