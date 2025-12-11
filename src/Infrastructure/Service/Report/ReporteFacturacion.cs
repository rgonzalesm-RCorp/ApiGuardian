using ApiGuardian.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ApiGuardian.Infrastructure.Services.Pdf
{
    public class ReporteFacturacion : IDocument
    {
        private readonly List<RptFacturacion> _data;

        public ReporteFacturacion(List<RptFacturacion> data)
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
                    
                    column.Item().Text("REPORTE CONSOLIDADO FACTURACION")
                        .FontSize(9).Bold().FontColor(Colors.Blue.Medium)
                        .AlignCenter();
                    column.Item().Text("");

                    /*column.Item().Text(_data[0].Ciclo)
                        .FontSize(10).Bold().AlignCenter();
                    column.Item().Text("");

                    /*column.Item().Text($"{_data.Encabezado.NombreCompleto}")
                        .FontSize(10);

                    column.Item().Text($"MESES ACTIVIDAD: {_data.Encabezado.MesActividad}")
                        .FontSize(8);*/

                    column.Item().Row(row =>
                    {
                        row.Spacing(20);

                        // COLUMNA IZQUIERDA
                        row.RelativeItem(3).Column(colLeft =>
                        {
                            colLeft.Spacing(3);

                            colLeft.Item().Text(text =>
                            {
                                text.Span("CODIGO: ").FontSize(6).Bold();
                                text.Span(_data[0].SCodigo).FontSize(6);
                            });

                            colLeft.Item().Text(text =>
                            {
                                text.Span("DOCUMENTO IDENTIDAD: ").FontSize(6).Bold();
                                text.Span(_data[0].SCedulaIdentidad).FontSize(6);
                            });

                            colLeft.Item().Text(text =>
                            {
                                text.Span("CICLO: ").FontSize(6).Bold();
                                text.Span(_data[0].NombreCiclo).FontSize(6);
                            });
                        });

                        // ESPACIADO CENTRAL
                        row.ConstantItem(30);

                        // COLUMNA DERECHA
                        row.RelativeItem(7).Column(colRight =>
                        {
                            colRight.Spacing(3);

                            colRight.Item().Text(text =>
                            {
                                text.Span("NOMBRE COMPLETO: ").FontSize(6).Bold();
                                text.Span(_data[0].SNombreCompleto).FontSize(6);
                            });

                            colRight.Item().Text(text =>
                            {
                                text.Span("FECHA INICIO: ").FontSize(6).Bold();
                                text.Span(_data[0].FechaInicio.ToString("dd/MM/yyyy")).FontSize(6);
                            });

                            colRight.Item().Text(text =>
                            {
                                text.Span("FECHA FIN: ").FontSize(6).Bold();
                                text.Span(_data[0].FechaFin.ToString("dd/MM/yyyy")).FontSize(6);
                            });
                        });
                    });

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
                            columns.RelativeColumn(3.5f);
                            columns.RelativeColumn(2.5f);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1.5F);
                            columns.RelativeColumn(1.5F);
                            columns.RelativeColumn(1.5f);


                        });

                        // Encabezado
                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCellStyle).Text("EMPRESA").FontSize(6).AlignLeft();
                            header.Cell().Element(HeaderCellStyle).Text("NIT").FontSize(6).AlignLeft();
                            header.Cell().Element(HeaderCellStyle).Text("COMISION SUS").FontSize(6).AlignRight();
                            header.Cell().Element(HeaderCellStyle).Text("COMISION BS").FontSize(6).AlignRight();
                            header.Cell().Element(HeaderCellStyle).Text("SERVICIO SUS").FontSize(6).AlignRight();
                            header.Cell().Element(HeaderCellStyle).Text("SERVICIO BS").FontSize(6).AlignRight();
                        });

                        // Filas
                        foreach (var v in _data)
                        {
                            table.Cell().Element(BodyCellStyle).Text(v.RazonSocial).FontSize(6).AlignLeft();
                            table.Cell().Element(BodyCellStyle).Text(v.Nit).FontSize(6).AlignLeft();
                            table.Cell().Element(BodyCellStyle).Text(v.TotalComisionVtaPersonal.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(BodyCellStyle).Text(v.TotalComisionVtaPersonalBs.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(BodyCellStyle).Text(v.TotalComisionVtaGrupoResidual.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(BodyCellStyle).Text(v.TotalComisionVtaGrupoResidualBs.ToString("N2")).FontSize(6).AlignRight();

                        }
                        table.Footer(footer =>
                        {
                            decimal totalVP = _data?.Sum(x => x.TotalComisionVtaPersonal) ?? 0;
                            decimal totalVPBS = _data?.Sum(x => x.TotalComisionVtaPersonalBs) ?? 0;
                            decimal totalServicio = _data?.Sum(x => x.TotalComisionVtaGrupoResidual) ?? 0;
                            decimal totalServicioBs = _data?.Sum(x => x.TotalComisionVtaGrupoResidualBs) ?? 0;

                            table.Cell().ColumnSpan(2).Element(HeaderCellStyle).Text("TOTAL:").FontSize(6).AlignRight().Bold();
                            table.Cell().Element(HeaderCellStyle).Text(totalVP.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(HeaderCellStyle).Text(totalVPBS.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(HeaderCellStyle).Text(totalServicio.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(HeaderCellStyle).Text(totalServicioBs.ToString("N2")).FontSize(6).AlignRight();
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
