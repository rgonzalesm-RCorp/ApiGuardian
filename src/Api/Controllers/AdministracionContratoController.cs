using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using Newtonsoft.Json;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministracionContratoController : ControllerBase
{
    private readonly IAdministracionContratoRepository _repository;
    private readonly string NOMBREARCHIVO = "AdministracionContratoController.cs";
    private readonly ILogService _log;
    public AdministracionContratoController(IAdministracionContratoRepository repository, ILogService log )
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
        string nombreArchivo = "GetAll()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Inicio de metodo [page:{page}, pageSize:{pageSize}, search:{search}]");

            var responseContrato = await _repository.GetAllAdministracionContrato(page, pageSize, search);

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo,
                $"Fin de metodo: {responseContrato.Success} - {responseContrato.Mensaje}");

            return Ok(new
            {
                status = responseContrato.Success,
                mensaje = responseContrato.Mensaje,
                data = new
                {
                    listaContrato = responseContrato.Data,
                    total = responseContrato.Total
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
    [HttpPost("insert")]
    public async Task<IActionResult> InsertContrato(AdministracionContrato data)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "InsertContrato()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Inicio de metodo AdministracionContrato: {JsonConvert.SerializeObject(data)}");

            var responseContrato = await _repository.InsertContrato(data);

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo,
                $"Fin de metodo: {responseContrato.Success} - {responseContrato.Mensaje}");

            return Ok(new
            {
                status = responseContrato.Success,
                mensaje = responseContrato.Mensaje,
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
    public async Task<IActionResult> UpdateContrato(AdministracionContrato data)
    {
         long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "UpdateContrato()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Inicio de metodo AdministracionContrato: {JsonConvert.SerializeObject(data)}");

            var responseContrato = await _repository.UpdateContrato(data);

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo,
                $"Fin de metodo: {responseContrato.Success} - {responseContrato.Mensaje}");

            return Ok(new
            {
                status = responseContrato.Success,
                mensaje = responseContrato.Mensaje,
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
