using ApiGuardian.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ApiGuardian.Infrastructure.Services.Pdf;

public class ReporteAscensoRango(List<ItemAscensoRango> data) : IDocument
{
    private readonly List<ItemAscensoRango> _data = data;
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container) => container.Page(page =>
    {
        page.Size(PageSizes.A3.Landscape()); page.Margin(12);
        page.Header().Text($"LISTADO DE NUEVOS ASCENSOS - MONTE SION {_data[0].Mes?.ToUpper()}").Bold().FontSize(10).FontColor(Colors.Red.Medium).AlignCenter();
        page.Content().PaddingTop(8).Table(tabla =>
        {
            tabla.ColumnsDefinition(c => { c.ConstantColumn(18); c.RelativeColumn(1); c.RelativeColumn(3.5f); c.RelativeColumn(1); c.RelativeColumn(1.2f); c.RelativeColumn(1.4f); c.RelativeColumn(1); c.RelativeColumn(1.8f); c.RelativeColumn(2.6f); c.RelativeColumn(3.5f); });
            var encabezados = new[] { "#", "MES", "FREELANCE", "CEDULA DE IDENTIDAD", "CELULAR", "CIUDAD DE RESIDENCIA", "PAIS", "NIVEL ALCANZADO", "ADICIONAL", "RANGOS TOTALES ASCENDIDOS" };
            tabla.Header(header => { for (var i = 0; i < encabezados.Length; i++) header.Cell().Element(c => Encabezado(c, i == 7)).Text(encabezados[i]).FontSize(5).AlignCenter(); });
            foreach (var item in _data)
            {
                Celda(tabla, item.Nro.ToString(), true); Celda(tabla, item.Mes, true); Celda(tabla, item.Nombre); Celda(tabla, item.CI); Celda(tabla, item.Telefono); Celda(tabla, item.Ciudad); Celda(tabla, item.Pais, true); Celda(tabla, item.NivelAlcanzado, true); Celda(tabla, item.Adicional, true); Celda(tabla, item.RangosTotalesAscendidos, true);
            }
        });
    });

    private static IContainer Encabezado(IContainer c, bool amarillo) => c.Background(amarillo ? Colors.Yellow.Medium : Colors.Green.Medium).Border(.5f).BorderColor(Colors.Black).Padding(2).DefaultTextStyle(x => x.FontColor(amarillo ? Colors.Black : Colors.White).Bold());
    private static void Celda(TableDescriptor tabla, string? valor, bool centro = false)
    {
        var cell = tabla.Cell().Border(.5f).BorderColor(Colors.Black).Padding(2);
        if (centro) cell.AlignCenter().Text(valor ?? string.Empty).FontSize(5); else cell.Text(valor ?? string.Empty).FontSize(5);
    }
}
