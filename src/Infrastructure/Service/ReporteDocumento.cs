using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ApiGuardian.Infrastructure.Services.Pdf
{
    public class ReporteDocumento : IDocument
    {
        private readonly string _titulo;
        private readonly string _contenido;

        public ReporteDocumento(string titulo, string contenido)
        {
            _titulo = titulo;
            _contenido = contenido;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(40);

                page.Header().Text(_titulo)
                    .FontSize(22).Bold().FontColor(Colors.Blue.Medium);

                page.Content().Text(_contenido)
                    .FontSize(14)
                    .LineHeight(1.4f);

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Página ");
                    x.CurrentPageNumber();
                });
            });
        }
    }
}
