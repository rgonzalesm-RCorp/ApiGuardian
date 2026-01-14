using ApiGuardian.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Reportes.Estilos;

namespace ApiGuardian.Infrastructure.Services.Pdf
{
    public class ReporteProrrateo : IDocument
    {
        private readonly List<RptProrrateo> _data;

        public ReporteProrrateo(List<RptProrrateo> data)
        {
            _data = data.ToList();
        }
        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape()); 
                page.Margin(20);

                //page.Header().Element(ComposeHeader);
                page.Header().Row(row =>
                {
                    // Encabezado principal (izquierda)
                    row.RelativeItem().Element(ComposeHeader);

                    // Paginador (derecha)
                    row.ConstantItem(120).AlignRight().Text(text =>
                    {
                        text.Span("Página ").FontSize(7);
                        text.CurrentPageNumber().FontSize(7);
                        text.Span(" / ").FontSize(7);
                        text.TotalPages().FontSize(7);
                    });
                });
                page.Content().Element(ComposeContent);
            });
        }

        // HEADER
        private void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    
                    column.Item().Text("REPORTE DE PRORRATEO")
                        .FontSize(9).Bold().FontColor(Colors.Blue.Medium)
                        .AlignCenter();
                    column.Item().Text("");
                    column.Item().Text(_data[0].Ciclo)
                        .FontSize(7).Bold().FontColor(Colors.Blue.Medium)
                        .AlignCenter();


                    
                });
            });
        }

        // CONTENT
        private void ComposeContent(IContainer container)
        {
            container.PaddingVertical(10).Column(column =>
            {
                column.Spacing(15);

                column.Item().Element(ComposeDetalleFacturacion);
            });
        }

        // SECCIÓN: DETALLE APLICACIONES
        private void ComposeDetalleFacturacion(IContainer container)
        {
            container.Column(column =>
            {

                column.Item().Element(c =>
                {
                    c.Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(4.5f);
                            columns.RelativeColumn(1F);
                            columns.RelativeColumn(2F);
                            columns.RelativeColumn(1f);
                            columns.RelativeColumn(1f);
                            columns.RelativeColumn(1f);
                            columns.RelativeColumn(1f);
                            columns.RelativeColumn(1f);
                        });

                        // Encabezado
                        table.Header(header =>
                        {
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("Cod. Cliente").FontSize(6).AlignLeft();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("Nro. Cuenta").FontSize(6).AlignLeft();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("Asesor").FontSize(6).AlignLeft();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("Carnet").FontSize(6).AlignLeft();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("Empresa").FontSize(6).AlignLeft();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("Importe $").FontSize(6).AlignRight();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("Retencion $").FontSize(6).AlignRight();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("Liquido $").FontSize(6).AlignRight();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("Desc. $").FontSize(6).AlignRight();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("Prorrateo $").FontSize(6).AlignRight();
                        });

                        // Filas
                        foreach (var v in _data)
                        {
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.LCodigoBanco).FontSize(6).AlignLeft();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.LCuentaBanco).FontSize(6).AlignLeft();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.SNombreCompleto).FontSize(6).AlignLeft();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.SCedulaIdentidad).FontSize(6).AlignLeft();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.SEmpresa).FontSize(6).AlignLeft();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.Importe.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.Retencion.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.Liquido.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.Descuento.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.Prorrateo.ToString("N2")).FontSize(6).AlignRight();

                        }
                        table.Footer(footer =>
                        {
                            decimal importe = _data?.Sum(x => x.Importe) ?? 0;
                            decimal retencion = _data?.Sum(x => x.Retencion) ?? 0;
                            decimal liquido = _data?.Sum(x => x.Liquido) ?? 0;
                            decimal descuento = _data?.Sum(x => x.Descuento) ?? 0;
                            decimal prorrateo = _data?.Sum(x => x.Prorrateo) ?? 0;

                            table.Cell().ColumnSpan(5).Element(EstiloReporte.HeaderCellStyle).Text("TOTAL:").FontSize(6).AlignRight().Bold();
                            table.Cell().Element(EstiloReporte.HeaderCellStyle).Text(importe.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(EstiloReporte.HeaderCellStyle).Text(retencion.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(EstiloReporte.HeaderCellStyle).Text(liquido.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(EstiloReporte.HeaderCellStyle).Text(descuento.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(EstiloReporte.HeaderCellStyle).Text(prorrateo.ToString("N2")).FontSize(6).AlignRight();
                        });
                        
                    });
                });
            });
        }
    }
}
