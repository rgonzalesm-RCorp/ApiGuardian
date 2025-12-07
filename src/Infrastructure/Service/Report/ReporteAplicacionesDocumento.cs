using ApiGuardian.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ApiGuardian.Infrastructure.Services.Pdf
{
    public class ReporteAplicacionesDocumento : IDocument
    {
        private readonly RptAplicaciones _data;

        public ReporteAplicacionesDocumento(RptAplicaciones data)
        {
            _data = data;
        }
        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(20);

                page.Header().Element(ComposeHeader);
                /*page.Header()
                    .ShowOnce()
                    .Element(c => ComposeHeader(c)); */
                page.Content().Element(ComposeContent);

                page.Footer().AlignRight().Text(text =>
                {
                    text.Span("Página ").FontSize(8);
                    text.CurrentPageNumber().FontSize(8);
                    text.Span(" / ").FontSize(8);
                    text.TotalPages().FontSize(8);
                });
            });
        }

        // HEADER
        private void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("REPORTE DE APLICACIONES")
                        .FontSize(15).Bold().FontColor(Colors.Blue.Medium)
                        .AlignCenter();

                    /*column.Item().Text($"{_data.Encabezado.Ciclo} ({_data.Encabezado.Inicio.ToString("dd/MM/yyyy")} - {_data.Encabezado.Fin.ToString("dd/MM/yyyy")})")
                        .FontSize(10).Bold().AlignCenter();
                    column.Item().Text("");

                    column.Item().Text($"{_data.Encabezado.NombreCompleto}")
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
                //column.Item().Element(ComposeVentasGrupo);
                //column.Item().Element(ComposeResumen);
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
                            columns.RelativeColumn(1); // Fecha
                            columns.RelativeColumn(1.3f); // Venta
                            columns.RelativeColumn(4.5f); // Tipo
                            columns.RelativeColumn(1.3f); // CI
                            columns.RelativeColumn(1.3f); // % / Comisión
                            columns.RelativeColumn(1.3f); // % / Comisión
                            columns.RelativeColumn(1.3f); // % / Comisión
                            columns.RelativeColumn(1.3f); // % / Comisión
                            columns.RelativeColumn(1.3f); // % / Comisión
                            columns.RelativeColumn(1.3f); // % / Comisión
                        });

                        // Encabezado
                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCellStyle).Text("Codigo").FontSize(6).AlignCenter();
                            header.Cell().Element(HeaderCellStyle).Text("CI").FontSize(6).AlignCenter();
                            header.Cell().Element(HeaderCellStyle).Text("Asesor").FontSize(6).AlignLeft();
                            header.Cell().Element(HeaderCellStyle).Text("Vta. Directa").FontSize(6).AlignRight();
                            header.Cell().Element(HeaderCellStyle).Text("Honorarios").FontSize(6).AlignRight();
                            header.Cell().Element(HeaderCellStyle).Text("Total Comisiones").FontSize(6).AlignRight();
                            header.Cell().Element(HeaderCellStyle).Text("%").FontSize(6).AlignCenter();
                            header.Cell().Element(HeaderCellStyle).Text("Retencion").FontSize(6).AlignRight();
                            header.Cell().Element(HeaderCellStyle).Text("Desc.").FontSize(6).AlignRight();
                            header.Cell().Element(HeaderCellStyle).Text("Total Pagar").FontSize(6).AlignRight();
                        });

                        // Filas
                        foreach (var v in _data.Aplicaciones)
                        {
                            table.Cell().Element(BodyCellStyle).Text( v.SCodigo ).FontSize(6).AlignCenter();
                            table.Cell().Element(BodyCellStyle).Text(v.SCedulaIdentidad).FontSize(6).AlignLeft();
                            table.Cell().Element(BodyCellStyle).Text(v.SNombreCompleto).FontSize(6).AlignLeft();
                            table.Cell().Element(BodyCellStyle).Text(v.ComisionVP.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(BodyCellStyle).Text(
                                (v.ComisionVG + v.ComisionBR + v.ComisionBL).ToString("N2")
                            ).FontSize(6).AlignRight();
                            table.Cell().Element(BodyCellStyle).Text(
                                (v.ComisionVG + v.ComisionBR + v.ComisionBL + v.ComisionVP).ToString("N2")
                            ).FontSize(6).AlignRight();
                            table.Cell().Element(BodyCellStyle).Text(v.PorcentajeRetencion.ToString("N2")).FontSize(6).AlignCenter();
                            table.Cell().Element(BodyCellStyle).Text(v.Retencion.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(BodyCellStyle).Text(v.Descuento.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(BodyCellStyle).Text((v.ComisionVG + v.ComisionBR + v.ComisionBL + v.ComisionVP - v.Retencion - v.Descuento).ToString("N2")).FontSize(6).AlignRight().Bold();
                        }
                        table.Footer(footer =>
                        {
                            decimal totalVP = _data.Aplicaciones?.Sum(x => x.ComisionVP) ?? 0;
                            decimal totalVG = _data.Aplicaciones?.Sum(x => x.ComisionVG) ?? 0;
                            decimal totalBR = _data.Aplicaciones?.Sum(x => x.ComisionBR) ?? 0;
                            decimal totalBL = _data.Aplicaciones?.Sum(x => x.ComisionBL) ?? 0;
                            decimal totalRetencion = _data.Aplicaciones?.Sum(x => x.Retencion) ?? 0;
                            decimal totalDescuento = _data.Aplicaciones?.Sum(x => x.Descuento) ?? 0;
                             

                            table.Cell().ColumnSpan(3).Element(HeaderCellStyle).Text("TOTAL:").FontSize(6).AlignRight().Bold();
                            table.Cell().Element(HeaderCellStyle).Text(totalVP.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(HeaderCellStyle).Text(
                                (totalVG + totalBR + totalBL).ToString("N2")
                            ).FontSize(6).AlignRight();
                            table.Cell().Element(HeaderCellStyle).Text(
                                (totalVG + totalBR + totalBL + totalVP).ToString("N2")
                            ).FontSize(6).AlignRight();
                            table.Cell().Element(HeaderCellStyle).Text("").FontSize(6).AlignRight();
                            table.Cell().Element(HeaderCellStyle).Text(totalRetencion.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(HeaderCellStyle).Text(totalDescuento.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(HeaderCellStyle).Text(
                                (totalVG + totalBR + totalBL + totalVP - totalDescuento - totalRetencion).ToString("N2")
                            ).FontSize(6).AlignRight();
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
