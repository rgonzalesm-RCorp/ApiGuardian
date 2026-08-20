using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Reportes.Estilos;

namespace ApiGuardian.Infrastructure.Services.Pdf;

public class ReporteContratos : IDocument
{
    private readonly IReadOnlyCollection<ListaAdministracionContrato> _contratos;
    private readonly DateTime _fechaInicio;
    private readonly DateTime _fechaFin;

    public ReporteContratos(
        IReadOnlyCollection<ListaAdministracionContrato> contratos,
        DateTime fechaInicio,
        DateTime fechaFin
    )
    {
        _contratos = contratos;
        _fechaInicio = fechaInicio;
        _fechaFin = fechaFin;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(20);
            page.DefaultTextStyle(style => style.FontSize(6));
            page.Header().Element(ComposeHeader);
            page.Content().PaddingTop(10).Element(ComposeContent);
            page.Footer().AlignCenter().Text(text =>
            {
                text.Span("Página ");
                text.CurrentPageNumber();
                text.Span(" de ");
                text.TotalPages();
            });
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(column =>
        {
            column.Item()
                .AlignCenter()
                .Text("REPORTE DE CONTRATOS")
                .FontSize(11)
                .Bold()
                .FontColor(Colors.Blue.Darken3);

            column.Item()
                .AlignCenter()
                .Text($"Periodo: {_fechaInicio:dd/MM/yyyy} - {_fechaFin:dd/MM/yyyy}")
                .FontSize(7);

            column.Item()
                .AlignCenter()
                .Text($"Total de contratos: {_contratos.Count}")
                .FontSize(7);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(0.7f);
                columns.RelativeColumn(1.1f);
                columns.RelativeColumn(1.2f);
                columns.RelativeColumn(2.7f);
                columns.RelativeColumn(1.8f);
                columns.RelativeColumn(1.1f);
                columns.RelativeColumn(1.1f);
                columns.RelativeColumn(1.1f);
                columns.RelativeColumn(1.1f);
                columns.RelativeColumn(1.4f);
                columns.RelativeColumn(1.5f);
                columns.RelativeColumn(2.5f);
            });

            table.Header(header =>
            {
                HeaderCell(header, "ID");
                HeaderCell(header, "Fecha");
                HeaderCell(header, "Nro. venta");
                HeaderCell(header, "Propietario");
                HeaderCell(header, "Complejo");
                HeaderCell(header, "MZ / Lote");
                HeaderCell(header, "Precio inicial", true);
                HeaderCell(header, "Cuota inicial", true);
                HeaderCell(header, "Precio", true);
                HeaderCell(header, "Estado");
                HeaderCell(header, "Tipo");
                HeaderCell(header, "Asesor");
            });

            foreach (var contrato in _contratos)
            {
                BodyCell(table, contrato.LcontratoId.ToString());
                BodyCell(table, contrato.Fecha.ToString("dd/MM/yyyy"));
                BodyCell(table, contrato.NroVenta);
                BodyCell(table, contrato.Propietario);
                BodyCell(table, contrato.Complejo);
                BodyCell(table, $"M-{contrato.NroMnzo} / L-{contrato.NroLote}");
                BodyCell(table, contrato.DPecioInicial.ToString("N2"), true);
                BodyCell(table, contrato.CuotaInicial.ToString("N2"), true);
                BodyCell(table, contrato.Precio.ToString("N2"), true);
                BodyCell(table, contrato.EstadoContrato);
                BodyCell(table, contrato.TipoContrato);
                BodyCell(table, contrato.Asesor);
            }

            table.Footer(footer =>
            {
                footer.Cell()
                    .ColumnSpan(6)
                    .Element(EstiloReporte.HeaderCellStyle)
                    .AlignRight()
                    .Text("TOTALES:")
                    .FontSize(6);
                FooterAmount(footer, _contratos.Sum(item => item.DPecioInicial));
                FooterAmount(footer, _contratos.Sum(item => item.CuotaInicial));
                FooterAmount(footer, _contratos.Sum(item => item.Precio));
                footer.Cell().ColumnSpan(3).Element(EstiloReporte.HeaderCellStyle).Text(string.Empty);
            });
        });
    }

    private static void HeaderCell(TableCellDescriptor table, string value, bool alignRight = false)
    {
        var cell = table.Cell().Element(EstiloReporte.HeaderCellStyle).Text(value).FontSize(5.5f);
        if (alignRight)
        {
            cell.AlignRight();
        }
    }

    private static void BodyCell(TableDescriptor table, string? value, bool alignRight = false)
    {
        var cell = table.Cell()
            .Element(EstiloReporte.BodyCellStyle)
            .Text(value ?? string.Empty)
            .FontSize(5.5f);

        if (alignRight)
        {
            cell.AlignRight();
        }
    }

    private static void FooterAmount(TableCellDescriptor table, decimal amount)
    {
        table.Cell()
            .Element(EstiloReporte.HeaderCellStyle)
            .AlignRight()
            .Text(amount.ToString("N2"))
            .FontSize(5.5f);
    }
}
