using ApiGuardian.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ApiGuardian.Infrastructure.Services.Pdf;

public class ReportePlanCarrera(List<ItemPlanCarrera> data) : IDocument
{
    private const decimal TipoCambio = 6.96m;
    private readonly List<ItemPlanCarrera> _data = data;
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container) => container.Page(page =>
    {
        page.Size(PageSizes.A3.Landscape());
        page.Margin(12);
        page.Header().Text($"BONO MONTE SION - {_data[0].Ciclo?.ToUpper()}").Bold().FontSize(10).FontColor(Colors.Green.Darken2).AlignCenter();
        page.Content().PaddingTop(8).Table(tabla =>
        {
            tabla.ColumnsDefinition(columnas =>
            {
                columnas.ConstantColumn(16); columnas.RelativeColumn(1); columnas.RelativeColumn(1.2f); columnas.RelativeColumn(3.4f);
                columnas.RelativeColumn(1); columnas.RelativeColumn(.8f); columnas.RelativeColumn(.8f); columnas.RelativeColumn(1.1f);
                columnas.RelativeColumn(1); columnas.RelativeColumn(1); columnas.RelativeColumn(1); columnas.RelativeColumn(1.2f); columnas.RelativeColumn(3); columnas.RelativeColumn(1.2f);
            });
            var encabezados = new[] { "#", "CODIGO DE CLIENTE", "NRO DE CUENTA", "EMPRENDEDOR INDEPENDIENTE", "DOC. DE IDENTIDAD", "IMPORTE $US", "MONTO BS", "FECHA DE SOLIC. DE PAGO", "FORMA DE PAGO", "MONEDA DE DESTINO", "ENTIDAD DESTINO", "SUCURSAL DESTINO", "GLOSA", "EMPRESA QUE ASUME" };
            tabla.Header(header => { foreach (var texto in encabezados) header.Cell().Element(Encabezado).Text(texto).FontSize(5).AlignCenter(); });
            foreach (var item in _data)
            {
                Celda(tabla, item.Nro.ToString(), true); Celda(tabla, item.Codigo); Celda(tabla, item.Cuenta); Celda(tabla, item.Nombre);
                Celda(tabla, item.Carnet); Celda(tabla, item.Monto.ToString("N2"), true); Celda(tabla, (item.Monto * TipoCambio).ToString("N2"), true); Celda(tabla, DateTime.Today.ToString("dd/MM/yyyy"));
                Celda(tabla, string.Empty); Celda(tabla, string.Empty); Celda(tabla, string.Empty); Celda(tabla, item.Ciudad); Celda(tabla, Glosa(item)); Celda(tabla, string.Empty);
            }
            tabla.Footer(footer => { footer.Cell().ColumnSpan(5).Element(Encabezado).Text("TOTAL:").FontSize(5).AlignRight(); footer.Cell().Element(Encabezado).Text(_data.Sum(x => x.Monto).ToString("N2")).FontSize(5).AlignRight(); footer.Cell().Element(Encabezado).Text((_data.Sum(x => x.Monto) * TipoCambio).ToString("N2")).FontSize(5).AlignRight(); footer.Cell().ColumnSpan(7).Element(Encabezado).Text(string.Empty); });
        });
    });

    private static IContainer Encabezado(IContainer c) => c.Background(Colors.Green.Medium).Border(0.5f).BorderColor(Colors.Black).Padding(2).DefaultTextStyle(x => x.FontColor(Colors.White).Bold());
    private static string Glosa(ItemPlanCarrera item) => item.SubieronNivel
        ? $"BONO MONTE SION {item.Ciclo?.ToUpper()} - ASCENSO AL RANGO {item.NivelAlcanzadoCiclo?.ToUpper()}"
        : $"BONO MONTE SION {item.Ciclo?.ToUpper()}";

    private static void Celda(TableDescriptor tabla, string? valor, bool derecha = false)
    {
        var cell = tabla.Cell().Border(0.5f).BorderColor(Colors.Black).Padding(2);
        if (derecha) cell.AlignRight().Text(valor ?? string.Empty).FontSize(5); else cell.Text(valor ?? string.Empty).FontSize(5);
    }
}
