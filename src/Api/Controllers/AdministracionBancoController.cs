using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using Newtonsoft.Json;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministracionBancoController : ControllerBase
{
    private readonly ILogService _log;
    private readonly IAdministracionBancoRepository _repository;
    private readonly string NOMBREARCHIVO = "AdministracionBancoController.cs";
    public AdministracionBancoController(IAdministracionBancoRepository repository, ILogService log)
    {
        _repository = repository;
        _log = log;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllCuentaBanco()
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "GetAllCuentaBanco()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, "Inicio de metodo");

            var responseBanco = await _repository.GetAllBanco();

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo,
                $"Fin de metodo: {responseBanco.Success} - {responseBanco.Mensaje}");

            return Ok(new
            {
                status = responseBanco.Success,
                mensaje = responseBanco.Mensaje,
                data = new
                {
                    listaBanco = responseBanco.Data
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
    [HttpGet("moneda")]
    public async Task<IActionResult> GetAllMoneda()
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "GetAllMoneda()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, "Inicio de metodo");

            var responseMoneda = await _repository.GetAllMoneda();

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo,
                $"Fin de metodo: {responseMoneda.Success} - {responseMoneda.Mensaje}");

            return Ok(new
            {
                status = responseMoneda.Success,
                mensaje = responseMoneda.Mensaje,
                data = new
                {
                    listaMoneda = responseMoneda.Data
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
    [HttpPut("update")]
    public async Task<IActionResult> UpdateBanco(AdministracionBanco data)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "UpdateBanco()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Inicio de metodo AdministracionBanco: {JsonConvert.SerializeObject(data)}");

            var responseBanco = await _repository.UpdateBanco(data);

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo,
                $"Fin de metodo: {responseBanco.Success} - {responseBanco.Mensaje}");

            return Ok(new
            {
                status = responseBanco.Success,
                mensaje = responseBanco.Mensaje,
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
    [HttpPost("insert")]
    public async Task<IActionResult> InsertBanco(AdministracionBanco data)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "InsertBanco()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Inicio de metodo AdministracionBanco: {JsonConvert.SerializeObject(data)}");

            var responseBanco = await _repository.InsertBanco(data);

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo,
                $"Fin de metodo: {responseBanco.Success} - {responseBanco.Mensaje}");

            return Ok(new
            {
                status = responseBanco.Success,
                mensaje = responseBanco.Mensaje,
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
    public async Task<IActionResult> DeleteBanco(
        [FromHeader(Name = "lBancoId")]  int lBancoId ,
        [FromHeader(Name = "usuario")]  string? usuario)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "DeleteBanco()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Inicio de metodo [lBancoId: {lBancoId}, usuario: {usuario}]");

            var responseBanco = await _repository.DeleteBanco(lBancoId, usuario);

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo,
                $"Fin de metodo: {responseBanco.Success} - {responseBanco.Mensaje}");

            return Ok(new
            {
                status = responseBanco.Success,
                mensaje = responseBanco.Mensaje,
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
