using ApiGuardian.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ApiGuardian.Infrastructure.Services.Pdf
{
    public class ReportePagarComision : IDocument
    {
        private readonly List<RptPagarComision> _data;

        public ReportePagarComision(List<RptPagarComision> data)
        {
            _data = data.ToList();
        }
        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                //page.Size(PageSizes.A4.Landscape()); 
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
                            columns.RelativeColumn(1f);
                            columns.RelativeColumn(1.5F);
                            columns.RelativeColumn(1.5F);
                            columns.RelativeColumn(5f);
                            columns.RelativeColumn(1f);
                            columns.RelativeColumn(1.5f);
                        });

                        // Encabezado
                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCellStyle).Text("Tipo Cuenta.").FontSize(5).AlignLeft();
                            header.Cell().Element(HeaderCellStyle).Text("Cod. Banco").FontSize(5).AlignLeft();
                            header.Cell().Element(HeaderCellStyle).Text("Cta. Banco").FontSize(5).AlignLeft();
                            header.Cell().Element(HeaderCellStyle).Text("Ciudad").FontSize(5).AlignLeft();
                            header.Cell().Element(HeaderCellStyle).Text("Asesor").FontSize(5).AlignLeft();
                            header.Cell().Element(HeaderCellStyle).Text("Cedula Identidad").FontSize(5).AlignCenter();
                            header.Cell().Element(HeaderCellStyle).Text("Total Pagar").FontSize(5).AlignRight();
                        });

                        // Filas
                        foreach (var v in _data)
                        {
                            table.Cell().Element(BodyCellStyle).Text(v.TipoCuenta).FontSize(6).AlignLeft();
                            table.Cell().Element(BodyCellStyle).Text(v.CodigoBanco).FontSize(6).AlignLeft();
                            table.Cell().Element(BodyCellStyle).Text(v.CuentaBanco).FontSize(6).AlignLeft();
                            table.Cell().Element(BodyCellStyle).Text(v.Ciudad).FontSize(6).AlignLeft();
                            table.Cell().Element(BodyCellStyle).Text(v.NombreCompleto).FontSize(6).AlignLeft();
                            table.Cell().Element(BodyCellStyle).Text(v.CedulaIdentidad).FontSize(6).AlignCenter();
                            table.Cell().Element(BodyCellStyle).Text((
                                v.Personal + v.Liderazgo + v.Grupo + v.Residual - v.Descuento - v.Retencion
                            ).ToString("N2")).FontSize(6).AlignRight();
                        }
                        table.Footer(footer =>
                        {
                            decimal totalPersonal = _data?.Sum(x => x.Personal) ?? 0;
                            decimal totalLiderazgo = _data?.Sum(x => x.Liderazgo) ?? 0;
                            decimal totalGrupo = _data?.Sum(x => x.Grupo) ?? 0;
                            decimal totalResidual = _data?.Sum(x => x.Residual) ?? 0;
                            decimal totalDescuento = _data?.Sum(x => x.Descuento) ?? 0;
                            decimal totalRetencion = _data?.Sum(x => x.Retencion) ?? 0;


                            table.Cell().ColumnSpan(6).Element(HeaderCellStyle).Text("TOTAL:").FontSize(6).AlignRight().Bold();
                            table.Cell().Element(HeaderCellStyle).Text((
                                totalPersonal + totalLiderazgo + totalGrupo + totalResidual - totalDescuento - totalRetencion
                            ).ToString("N2")).FontSize(6).AlignRight();

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
