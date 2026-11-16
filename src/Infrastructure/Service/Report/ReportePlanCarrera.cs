using ApiGuardian.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ApiGuardian.Infrastructure.Services.Pdf;

public class ReportePlanCarrera(List<ItemPlanCarrera> data, string moneda, decimal tipoCambioOficial) : IDocument
{
    private readonly List<ItemPlanCarrera> _data = data;
    private readonly string _moneda = moneda;
    private readonly decimal _tipoCambioOficial = tipoCambioOficial;
    private bool MostrarEquivalenteBs => _moneda == "SUS";
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container) => container.Page(page =>
    {
        page.Size(PageSizes.A3.Landscape()); page.Margin(12);
        page.Header().Text($"BONO MONTE SION - {_data[0].Ciclo?.ToUpper()}").Bold().FontSize(10).FontColor(Colors.Green.Darken2).AlignCenter();
        page.Content().PaddingTop(8).Table(tabla =>
        {
            tabla.ColumnsDefinition(c =>
            {
                c.ConstantColumn(16); c.RelativeColumn(1); c.RelativeColumn(1.2f); c.RelativeColumn(3.4f); c.RelativeColumn(1); c.RelativeColumn(.8f);
                if (MostrarEquivalenteBs) c.RelativeColumn(.8f);
                c.RelativeColumn(1.1f); c.RelativeColumn(1); c.RelativeColumn(1); c.RelativeColumn(1); c.RelativeColumn(1.2f); c.RelativeColumn(3); c.RelativeColumn(1.2f);
            });
            var encabezados = new List<string> { "#", "CODIGO DE CLIENTE", "NRO DE CUENTA", "EMPRENDEDOR INDEPENDIENTE", "DOC. DE IDENTIDAD", $"IMPORTE {_moneda}" };
            if (MostrarEquivalenteBs) encabezados.Add("MONTO BS");
            encabezados.AddRange(["FECHA DE SOLIC. DE PAGO", "FORMA DE PAGO", "MONEDA DE DESTINO", "ENTIDAD DESTINO", "SUCURSAL DESTINO", "GLOSA", "EMPRESA QUE ASUME"]);
            tabla.Header(h => { foreach (var texto in encabezados) h.Cell().Element(Encabezado).Text(texto).FontSize(5).AlignCenter(); });
            foreach (var item in _data)
            {
                Celda(tabla, item.Nro.ToString(), true); Celda(tabla, item.Codigo); Celda(tabla, item.Cuenta); Celda(tabla, item.Nombre); Celda(tabla, item.Carnet); Celda(tabla, item.Monto.ToString("N2"), true);
                if (MostrarEquivalenteBs) Celda(tabla, (item.Monto * _tipoCambioOficial).ToString("N2"), true);
                Celda(tabla, DateTime.Today.ToString("dd/MM/yyyy")); Celda(tabla, string.Empty); Celda(tabla, string.Empty); Celda(tabla, string.Empty); Celda(tabla, item.Ciudad); Celda(tabla, Glosa(item)); Celda(tabla, string.Empty);
            }
            tabla.Footer(f =>
            {
                f.Cell().ColumnSpan(5).Element(Encabezado).Text("TOTAL:").FontSize(5).AlignRight(); f.Cell().Element(Encabezado).Text(_data.Sum(x => x.Monto).ToString("N2")).FontSize(5).AlignRight();
                if (MostrarEquivalenteBs) f.Cell().Element(Encabezado).Text((_data.Sum(x => x.Monto) * _tipoCambioOficial).ToString("N2")).FontSize(5).AlignRight();
                f.Cell().ColumnSpan(7).Element(Encabezado).Text(string.Empty);
            });
        });
    });

    private static IContainer Encabezado(IContainer c) => c.Background(Colors.Green.Medium).Border(0.5f).BorderColor(Colors.Black).Padding(2).DefaultTextStyle(x => x.FontColor(Colors.White).Bold());
    private static string Glosa(ItemPlanCarrera i) => i.SubieronNivel ? $"BONO MONTE SION {i.Ciclo?.ToUpper()} - ASCENSO AL RANGO {i.NivelAlcanzadoCiclo?.ToUpper()}" : $"BONO MONTE SION {i.Ciclo?.ToUpper()}";
    private static void Celda(TableDescriptor t, string? v, bool derecha = false) { var c = t.Cell().Border(0.5f).BorderColor(Colors.Black).Padding(2); if (derecha) c.AlignRight().Text(v ?? string.Empty).FontSize(5); else c.Text(v ?? string.Empty).FontSize(5); }
}
