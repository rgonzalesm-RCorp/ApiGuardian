using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace ApiGuardian.Infrastructure.Services.Pdf
{
    public class PdfService
    {
        public async Task<byte[]> HtmlToPdfAsync(string html)
        {
            // Abrir navegador
            await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
            });

            // Crear página
            await using var page = await browser.NewPageAsync();

            // Cargar HTML
            await page.SetContentAsync(html);

            // Opciones PDF compatibles con PuppeteerSharp 9.x+
            var options = new PdfOptions
            {
                Format = PaperFormat.A4,
                PrintBackground = true,
                MarginOptions = new MarginOptions  // <-- ESTA ES LA CORRECTA
                {
                    Top = "20px",
                    Bottom = "20px",
                    Left = "20px",
                    Right = "20px"
                }
            };

            return await page.PdfDataAsync(options);
        }
    }
}
