using ApiGuardian.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ApiGuardian.Infrastructure.Services.Pdf
{
    public class ReporteDescuentoEmpresa : IDocument
    {
        private readonly List<RptDescuentoEmpresa> _data;

        public ReporteDescuentoEmpresa(List<RptDescuentoEmpresa> data)
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
                /*page.Header()
                    .ShowOnce()
                    .Element(c => ComposeHeader(c)); */
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
                    column.Item().Text("REPORTE DE DESCUENTOS POR EMPRESA")
                        .FontSize(15).Bold().FontColor(Colors.Blue.Medium)
                        .AlignCenter();
                    column.Item().Text("");

                    column.Item().Text(_data[0].Ciclo)
                        .FontSize(10).Bold().AlignCenter();
                    column.Item().Text("");

                    /*column.Item().Text($"{_data.Encabezado.NombreCompleto}")
                        .FontSize(10);

                    column.Item().Text($"MESES ACTIVIDAD: {_data.Encabezado.MesActividad}")
                        .FontSize(8);*/
                });
            });
        }

        // CONTENT
        private void ComposeContent(IContainer container)
        {
            container.PaddingVertical(10).Column(column =>
            {
                column.Spacing(15);

                column.Item().Element(ComposeVentasPersonales);
            });
        }

        // SECCIÓN: DETALLE APLICACIONES
        private void ComposeVentasPersonales(IContainer container)
        {
            container.Column(column =>
            {

                column.Item().Element(c =>
                {
                    c.Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3.5f);
                            columns.RelativeColumn(2.5f);
                            columns.RelativeColumn(2.5f);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(3);

                        });

                        // Encabezado
                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCellStyle).Text("ASESOR").FontSize(6).AlignLeft();
                            header.Cell().Element(HeaderCellStyle).Text("COMPLEJO").FontSize(6).AlignLeft();
                            header.Cell().Element(HeaderCellStyle).Text("EMPRESA").FontSize(6).AlignLeft();
                            header.Cell().Element(HeaderCellStyle).Text("MZ").FontSize(6).AlignCenter();
                            header.Cell().Element(HeaderCellStyle).Text("LT").FontSize(6).AlignCenter();
                            header.Cell().Element(HeaderCellStyle).Text("UV").FontSize(6).AlignCenter();
                            header.Cell().Element(HeaderCellStyle).Text("TIPO").FontSize(6).AlignCenter();
                            header.Cell().Element(HeaderCellStyle).Text("MONTO").FontSize(6).AlignRight();
                            header.Cell().Element(HeaderCellStyle).Text("OBSERVACION").FontSize(6).AlignLeft();
                        });

                        // Filas
                        foreach (var v in _data)
                        {
                            table.Cell().Element(BodyCellStyle).Text( v.Asesor ).FontSize(6).AlignLeft();
                            table.Cell().Element(BodyCellStyle).Text(v.Complejo).FontSize(6).AlignLeft();
                            table.Cell().Element(BodyCellStyle).Text(v.Empresa).FontSize(6).AlignLeft();
                            table.Cell().Element(BodyCellStyle).Text(v.Mz).FontSize(6).AlignCenter();
                            table.Cell().Element(BodyCellStyle).Text(v.Lote).FontSize(6).AlignCenter();
                            table.Cell().Element(BodyCellStyle).Text(v.Uv).FontSize(6).AlignCenter();
                            table.Cell().Element(BodyCellStyle).Text(v.Tipo).FontSize(6).AlignCenter();
                            table.Cell().Element(BodyCellStyle).Text(v.Monto.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(BodyCellStyle).Text(v.Observacion).FontSize(6).AlignLeft();

                        }
                        table.Footer(footer =>
                        {
                            decimal totalVP = _data?.Sum(x => x.Monto) ?? 0;

                            table.Cell().ColumnSpan(7).Element(HeaderCellStyle).Text("TOTAL:").FontSize(6).AlignRight().Bold();
                            table.Cell().Element(HeaderCellStyle).Text(totalVP.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(HeaderCellStyle).Text("").FontSize(6).AlignRight();
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
                .PaddingVertical(2)
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
