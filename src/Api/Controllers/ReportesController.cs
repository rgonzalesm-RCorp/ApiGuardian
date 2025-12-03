using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Infrastructure.Services.Pdf;
using QuestPDF.Fluent;

namespace ApiGuardian.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportesController : ControllerBase
    {
        [HttpGet("pdf")]
        public IActionResult GenerarPdf()
        {
            // Crear un documento
            var documento = new ReporteDocumento(
                "Reporte Básico con QuestPDF",
                "Este es un PDF generado desde QuestPDF.\n\n" +
                "Puede incluir texto, tablas, imágenes y diseño profesional."
            );

            // Generar PDF en bytes
            var pdfBytes = documento.GeneratePdf();

            return File(pdfBytes, "application/pdf", "reporte.pdf");
        }
    }
}
