using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Infrastructure.Services.Pdf;
using QuestPDF.Fluent;
using ApiGuardian.Models;
using ApiGuardian.Application.Interfaces;

namespace ApiGuardian.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportesController : ControllerBase
    {
        private readonly ILogService _log;
        private readonly IReportesRepository _repo;
        private readonly IAdministracionDescuentoComisionRepository _comision;
        private readonly string NOMBREARCHIVO = "UtilsController.cs";

        public ReportesController(IReportesRepository repo, ILogService log, IAdministracionDescuentoComisionRepository comision)
        {
            _repo = repo;
            _log = log;
            _comision = comision;
        }
        [HttpGet("comisiones")]
        public async Task<IActionResult> ReporteComisiones(
            [FromHeader] int lCicloId,
            [FromHeader] int lContactoId,
            [FromHeader] string? usuario
        )
        {
            long logId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string metodo = "ReporteComisiones()";

            _log.Info(
                logId.ToString(),
                NOMBREARCHIVO,
                metodo,
                $"Inicio ReporteComisiones - lCicloId={lCicloId}, lContactoId={lContactoId}, usuario={usuario}"
            );

            try
            {
                // 🔹 Llamadas al repositorio (debes reemplazar los valores quemados)
                var reporteComision = await _repo.GetReporteComision(logId.ToString(), lCicloId, lContactoId);
                var comision = await _comision.GetComision(logId.ToString(), lContactoId, lCicloId, 1);

                // 🔹 Unimos las comisiones
                reporteComision.Data.Comisiones = comision.Data;

                // 🔹 Generación del PDF
                var documento = new ReporteComisionesDocumento(reporteComision.Data);
                var pdfBytes = documento.GeneratePdf();

                string base64Pdf = Convert.ToBase64String(pdfBytes);

                _log.Info(
                    logId.ToString(),
                    NOMBREARCHIVO,
                    metodo,
                    "PDF generado correctamente."
                );

                return Ok(new
                {
                    status = true,
                    mensaje = "Reporte generado correctamente.",
                    data = new
                    {
                        FileName = $"REPORTE DE COMISIONES {reporteComision.Data.Encabezado.NombreCompleto} - {reporteComision.Data.Encabezado.Ciclo}.pdf",
                        FileBase64 = base64Pdf,
                        ContentType = "application/pdf",
                        tieneComicion = reporteComision.Data.VentasPersonales.Count() <= 0 ? false :true,
                    }
                    
                });
            }
            catch (Exception ex)
            {
                _log.Error(
                    logId.ToString(),
                    NOMBREARCHIVO,
                    metodo,
                    "Error al generar reporte de comisiones",
                    ex
                );

                return Ok(new
                {
                    status = false,
                    mensaje = ex.Message,
                    data =""
                });
            }
        }

    }
}
