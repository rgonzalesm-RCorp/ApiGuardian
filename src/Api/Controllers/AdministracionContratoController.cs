using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Infrastructure.Services.Pdf;
using Newtonsoft.Json;
using QuestPDF.Fluent;

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
        [FromHeader(Name = "search")] string? search,
        [FromHeader(Name = "fechaInicio")] DateTime? fechaInicio,
        [FromHeader(Name = "fechaFin")] DateTime? fechaFin
    )
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "GetAll()";

        try
        {
            if (!fechaInicio.HasValue || !fechaFin.HasValue)
            {
                return Ok(new
                {
                    status = false,
                    mensaje = "La fecha de inicio y la fecha de fin son obligatorias.",
                    data = ""
                });
            }

            if (fechaInicio.Value.Date > fechaFin.Value.Date)
            {
                return Ok(new
                {
                    status = false,
                    mensaje = "La fecha de inicio no puede ser mayor que la fecha de fin.",
                    data = ""
                });
            }

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo,
                $"Inicio de metodo [page:{page}, pageSize:{pageSize}, search:{search}, fechaInicio:{fechaInicio:yyyy-MM-dd}, fechaFin:{fechaFin:yyyy-MM-dd}]");

            var inicio = fechaInicio.Value.Date;
            var fin = fechaFin.Value.Date;
            var responseContrato = await _repository.GetAllAdministracionContrato(
                logTransaccionId.ToString(),
                page,
                pageSize,
                search,
                inicio,
                fin
            );

            if (!responseContrato.Success)
            {
                return Ok(new
                {
                    status = false,
                    mensaje = responseContrato.Mensaje,
                    data = new
                    {
                        listaContrato = responseContrato.Data,
                        total = responseContrato.Total,
                        fileName = "",
                        fileBase64 = "",
                        contentType = "",
                        fileNameXls = "",
                        base64Xls = ""
                    }
                });
            }

            var responseReporte = await _repository.GetReporteAdministracionContrato(
                logTransaccionId.ToString(),
                search,
                inicio,
                fin
            );

            if (!responseReporte.Success)
            {
                return Ok(new
                {
                    status = false,
                    mensaje = responseReporte.Mensaje,
                    data = ""
                });
            }

            var contratosReporte = responseReporte.Data.ToList();
            var documentoPdf = new ReporteContratos(contratosReporte, inicio, fin);
            var base64Pdf = Convert.ToBase64String(documentoPdf.GeneratePdf());
            var reporteXls = new ContratosXls().Generar(contratosReporte, inicio, fin);
            var sufijoPeriodo = $"{inicio:yyyyMMdd}-{fin:yyyyMMdd}";

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo,
                $"Fin de metodo: {responseContrato.Success} - {responseContrato.Mensaje}");

            return Ok(new
            {
                status = responseContrato.Success,
                mensaje = responseContrato.Mensaje,
                data = new
                {
                    listaContrato = responseContrato.Data,
                    total = responseContrato.Total,
                    fileName = $"REPORTE DE CONTRATOS {sufijoPeriodo}.pdf",
                    fileBase64 = base64Pdf,
                    contentType = "application/pdf",
                    fileNameXls = $"REPORTE DE CONTRATOS {sufijoPeriodo}.xlsx",
                    base64Xls = reporteXls.Base64
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
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Inicio de metodo AdministracionContrato: {JsonConvert.SerializeObject(data, Formatting.Indented)}");

            var responseContrato = await _repository.InsertContrato(logTransaccionId.ToString(), data);

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
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Inicio de metodo AdministracionContrato: {JsonConvert.SerializeObject(data, Formatting.Indented)}");

            var responseContrato = await _repository.UpdateContrato(logTransaccionId.ToString(), data);

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

    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteContrato(
        [FromHeader(Name = "lContratoId")] int lContratoId
    )
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        const string nombreMetodo = "DeleteContrato()";

        try
        {
            if (lContratoId <= 0)
            {
                return Ok(new
                {
                    status = false,
                    mensaje = "El identificador del contrato es obligatorio.",
                    data = ""
                });
            }

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreMetodo,
                $"Inicio de metodo [lContratoId: {lContratoId}]");

            var responseContrato = await _repository.DeleteContrato(
                logTransaccionId.ToString(),
                lContratoId
            );

            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreMetodo,
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
            _log.Error(logTransaccionId.ToString(), NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);

            return Ok(new
            {
                status = false,
                mensaje = ex.Message,
                data = ""
            });
        }
    }
}
