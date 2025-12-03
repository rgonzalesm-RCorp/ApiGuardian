using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Microsoft.AspNetCore.DataProtection.Repositories;
using DinkToPdf;
using ApiGuardian.Infrastructure.Services.Pdf;


namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReporteTestController : ControllerBase
{
 
    private readonly string NOMBREARCHIVO = "UtilsController.cs";
    private readonly PdfService _pdfService;
    public ReporteTestController(PdfService pdfService)
        {
            _pdfService = pdfService;
        }
    [HttpGet]
    public async Task<IActionResult> GetAdministracionSemanaCiclo()
    {
         string html = @"
                <html>
                <head>
                    <style>
                        body { font-family: Arial, sans-serif; padding: 20px; }
                        h1 { color: #1976d2; }
                        table { width: 100%; border-collapse: collapse; margin-top: 20px; }
                        th, td { border: 1px solid #ccc; padding: 8px; }
                        th { background-color: #f5f5f5; }
                    </style>
                </head>
                <body>
                    <h1>Reporte de Ejemplo</h1>
                    <p>Generado el: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + @"</p>

                    <table>
                        <tr>
                            <th>Nombre</th>
                            <th>Monto</th>
                        </tr>
                        <tr>
                            <td>Rolando Medina</td>
                            <td>500 Bs</td>
                        </tr>
                        <tr>
                            <td>Pedro López</td>
                            <td>300 Bs</td>
                        </tr>
                    </table>
                </body>
                </html>";

            var pdfBytes = await _pdfService.HtmlToPdfAsync(html);

            return File(pdfBytes, "application/pdf", "reporte_ejemplo.pdf");
        /*return Ok(new
        {
            status = true,
            mensaje = "correcto",
            data = ""
        });*/
        
    }
}
