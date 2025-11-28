using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using Org.BouncyCastle.Asn1.IsisMtt.X509;
using Newtonsoft.Json;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministracionContactoController : ControllerBase
{
    private readonly IAdministracionContactoRepository _repository;
    private readonly string NOMBREARCHIVO = "AdministracionContactoController.cs";
    private readonly ILogService _log;
    public AdministracionContactoController(IAdministracionContactoRepository repository, ILogService log)
    {
        _repository = repository;
        _log = log;
    }
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromHeader(Name = "page")] int page,
        [FromHeader(Name = "pageSize")] int pageSize,
        [FromHeader(Name = "search")] string? search
    )
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string NombreMetodo = "GetAllAdministracionContacto()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, NombreMetodo, $"Inicio de metodo [page:{page}, pageSize:{pageSize}, search:{search}]");

            var responseContacto = await _repository.GetAllAdministracionContacto(logTransaccionId.ToString(), page, pageSize, search);

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, NombreMetodo,
                $"Fin de metodo: {responseContacto.Success} - {responseContacto.Mensaje}");

            return Ok(new
            {
                status = responseContacto.Success,
                mensaje = responseContacto.Mensaje,
                data = new
                {
                    listaContacto = responseContacto.Data,
                    total = responseContacto.Total
                }
            });
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId.ToString(), NOMBREARCHIVO, NombreMetodo, "Fin de metodo", ex);

            return Ok(new
            {
                status = false,
                mensaje = ex.Message,
                data = ""
            });
        }
    }
    [HttpPost("insert")]
    public async Task<IActionResult> InsertContacto(AdministracionContacto data)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "InsertContacto()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Inicio de metodo AdministracionContacto : {JsonConvert.SerializeObject(data, Formatting.Indented)}");

            var responseContacto = await _repository.InsertContacto(logTransaccionId.ToString(), data);

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo,
                $"Fin de metodo: {responseContacto.Success} - {responseContacto.Mensaje}");

            return Ok(new
            {
                status = responseContacto.Success,
                mensaje = responseContacto.Mensaje,
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
    [HttpPut("update")]
    public async Task<IActionResult> UpdateContacto(AdministracionContacto data)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "UpdateContacto()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Inicio de metodo AdministracionContacto : {JsonConvert.SerializeObject(data, Formatting.Indented)}");

            var responseContacto = await _repository.UpdateContacto(logTransaccionId.ToString(), data);

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo,
                $"Fin de metodo: {responseContacto.Success} - {responseContacto.Mensaje}");

            return Ok(new
            {
                status = responseContacto.Success,
                mensaje = responseContacto.Mensaje,
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
    [HttpDelete("baja")]
    public async Task<IActionResult> BajaContacto(AdministracionContactoBaja data)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "BajaContacto()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Inicio de metodo AdministracionContacto : {JsonConvert.SerializeObject(data, Formatting.Indented)}");

            var responseContacto = await _repository.BajaContacto(logTransaccionId.ToString(), data);

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo,
                $"Fin de metodo: {responseContacto.Success} - {responseContacto.Mensaje}");

            return Ok(new
            {
                status = responseContacto.Success,
                mensaje = responseContacto.Mensaje,
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
