using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using Newtonsoft.Json;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministracionCuentaBancoController : ControllerBase
{
    private readonly IAdministracionCuentaBancoRepository _repository;
    private readonly string NOMBREARCHIVO = "AdministracionCuentaBancoController.cs";
    private readonly ILogService _log;
    public AdministracionCuentaBancoController(IAdministracionCuentaBancoRepository repository, ILogService log)
    {
        _repository = repository;
        _log = log;
    }

    [HttpGet("id")]
    public async Task<IActionResult> GetCuentaBanco([FromHeader(Name = "lContactoId")] int lContactoId)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "GetCuentaBanco()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Inicio de metodo [lContactoId:{lContactoId}]");

            var responseCuentaBanco = await _repository.GetCuentaBanco(logTransaccionId.ToString(), lContactoId);

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo,
                $"Fin de metodo: {responseCuentaBanco.Success} - {responseCuentaBanco.Mensaje}");

            return Ok(new
            {
                status = responseCuentaBanco.Success,
                mensaje = responseCuentaBanco.Mensaje,
                data = new
                {
                    dataCuentaBanco = responseCuentaBanco.Data
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
    [HttpGet]
    public async Task<IActionResult> GetAllCuentaBanco(
        [FromHeader(Name = "page")] int page,
        [FromHeader(Name = "pageSize")] int pageSize,
        [FromHeader(Name = "search")] string? search)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "GetAllCuentaBanco()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Inicio de metodo [page:{page}, pageSize:{pageSize}, search:{search}]");

            var responseCuentaBanco = await _repository.GetAllCuentaBanco(logTransaccionId.ToString(), page, pageSize, search);

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo,
                $"Fin de metodo: {responseCuentaBanco.Success} - {responseCuentaBanco.Mensaje}");

            return Ok(new
            {
                status = responseCuentaBanco.Success,
                mensaje = responseCuentaBanco.Mensaje,
                data = new
                {
                    listaCuentaBanco = responseCuentaBanco.Data,
                    totalRegistro = responseCuentaBanco.Total
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
    public async Task<IActionResult> UpdateCuentaBanco(DataCuentaBanco data)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "UpdateCuentaBanco()";

        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Inicio de metodo DataCuentaBanco: {JsonConvert.SerializeObject(data, Formatting.Indented)}");

            var responseCuentaBanco = await _repository.UpdateCuentaBanco(logTransaccionId.ToString(), data);

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo,
                $"Fin de metodo: {responseCuentaBanco.Success} - {responseCuentaBanco.Mensaje}");

            return Ok(new
            {
                status = responseCuentaBanco.Success,
                mensaje = responseCuentaBanco.Mensaje,
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
