using ApiGuardian.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ApiGuardian.Infrastructure.Services.Pdf
{
    public class ReporteComisionServicio : IDocument
    {
        private readonly List<RptComisionServicio> _data;

        public ReporteComisionServicio(List<RptComisionServicio> data)
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
                    
                    column.Item().Text("REPORTE EMPRESA CONSOLIDADO COMISION - SERVICIO")
                        .FontSize(9).Bold().FontColor(Colors.Blue.Medium)
                        .AlignCenter();
                    column.Item().Text("");



                    
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
                            columns.RelativeColumn(4.5f);
                            columns.RelativeColumn(1F);
                            columns.RelativeColumn(2F);
                            columns.RelativeColumn(1f);
                            columns.RelativeColumn(1f);
                            columns.RelativeColumn(1f);
                            columns.RelativeColumn(1f);
                            columns.RelativeColumn(1f);
                            columns.RelativeColumn(1f);
                            columns.RelativeColumn(1f);
                        });

                        // Encabezado
                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCellStyle).Text("COD.").FontSize(6).AlignLeft();
                            header.Cell().Element(HeaderCellStyle).Text("ASESOR").FontSize(6).AlignLeft();
                            header.Cell().Element(HeaderCellStyle).Text("COMISION $").FontSize(6).AlignLeft();
                            header.Cell().Element(HeaderCellStyle).Text("SERVICIO $").FontSize(6).AlignLeft();
                            header.Cell().Element(HeaderCellStyle).Text("TOTAL COMISION $").FontSize(6).AlignLeft();
                            header.Cell().Element(HeaderCellStyle).Text("13%").FontSize(6).AlignLeft();
                            header.Cell().Element(HeaderCellStyle).Text("87%").FontSize(6).AlignRight();
                            header.Cell().Element(HeaderCellStyle).Text("RET. %").FontSize(6).AlignRight();
                            header.Cell().Element(HeaderCellStyle).Text("RET. $").FontSize(6).AlignRight();
                            header.Cell().Element(HeaderCellStyle).Text("TOTAL $").FontSize(6).AlignRight();
                            header.Cell().Element(HeaderCellStyle).Text("TOTAL PAGAR $").FontSize(6).AlignRight();
                        });

                        // Filas
                        foreach (var v in _data)
                        {
                            decimal trece = (v.Comision + v.Servicio )*Convert.ToDecimal( 0.13);
                            table.Cell().Element(BodyCellStyle).Text(v.SCodigo).FontSize(6).AlignLeft();
                            table.Cell().Element(BodyCellStyle).Text(v.SNombreCompleto).FontSize(6).AlignLeft();
                            table.Cell().Element(BodyCellStyle).Text(v.Comision.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(BodyCellStyle).Text(v.Servicio.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(BodyCellStyle).Text((v.Comision + v.Servicio).ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(BodyCellStyle).Text(
                                ((v.PorcentajeRetencion <= 0 )? ((v.Comision + v.Servicio)* Convert.ToDecimal(0.13)) : 0).ToString("N2")
                            ).FontSize(6).AlignRight();
                            table.Cell().Element(BodyCellStyle).Text(
                                ((v.PorcentajeRetencion <= 0 )? ((v.Comision + v.Servicio)* Convert.ToDecimal(0.87)) : 0).ToString("N2")
                            ).FontSize(6).AlignRight();
                            table.Cell().Element(BodyCellStyle).Text(v.PorcentajeRetencion.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(BodyCellStyle).Text(v.MontoRetencion.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(BodyCellStyle).Text((v.Comision + v.Servicio).ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(BodyCellStyle).Text(
                                (v.Comision + v.Servicio - v.MontoRetencion).ToString("N2")
                            ).FontSize(6).AlignRight();


                        }
                        table.Footer(footer =>
                        {
                            decimal importe =  0;
                            decimal retencion =  0;
                            decimal liquido = 0;
                            decimal descuento = 0;
                            decimal prorrateo = 0;

                            table.Cell().ColumnSpan(2).Element(HeaderCellStyle).Text("TOTAL:").FontSize(6).AlignRight().Bold();
                            table.Cell().Element(HeaderCellStyle).Text(importe.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(HeaderCellStyle).Text(retencion.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(HeaderCellStyle).Text(liquido.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(HeaderCellStyle).Text(descuento.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(HeaderCellStyle).Text(prorrateo.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(HeaderCellStyle).Text("").FontSize(6).AlignRight();
                            table.Cell().Element(HeaderCellStyle).Text(prorrateo.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(HeaderCellStyle).Text(prorrateo.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(HeaderCellStyle).Text(prorrateo.ToString("N2")).FontSize(6).AlignRight();

                        });
                        
                    });
                });
            });
        }

 
        // ESTILOS DE CELDA
        private static IContainer HeaderCellStyle(IContainer container)
        {
            return container
                .DefaultTextStyle(x => x.SemiBold())
                .Background(Colors.Grey.Lighten3)
                .PaddingVertical(4)
                .PaddingHorizontal(3)
                .AlignMiddle()
                .BorderColor(Colors.Grey.Lighten1);
        }

        private static IContainer BodyCellStyle(IContainer container)
        {
            return container
                .PaddingVertical(1)
                .PaddingHorizontal(3)
                //.BorderBottom(0.5f)
                .BorderColor(Colors.Grey.Lighten3);
        }
        private static IContainer TotalCellStyle(IContainer container)
        {
            return container
                .PaddingVertical(1)
                .PaddingHorizontal(3)
                //.BorderBottom(0.5f)
                .Border(0.3f)
                
                .BorderColor(Colors.Black);
        }
    }
}
