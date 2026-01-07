using ApiGuardian.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Reportes.Estilos;

namespace ApiGuardian.Infrastructure.Services.Pdf
{
    public class ReportePlanCarrera : IDocument
    {
        private readonly List<ItemPlanCarrera> _data;
 

        public ReportePlanCarrera(List<ItemPlanCarrera> data)
        {
            _data = data;
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
                    
                    column.Item().Text("REPORTE POR VENDEDOR PLAN DE CARRERA")
                        .FontSize(9).Bold().FontColor(Colors.Blue.Medium)
                        .AlignCenter();
                    column.Item().Text("");
                    column.Item().Text(_data[0].Ciclo.ToUpper()).AlignCenter().FontSize(7);
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
                            columns.RelativeColumn(0.5f);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1.5F);
                            columns.RelativeColumn(1.5F);
                            columns.RelativeColumn(1f);
                            columns.RelativeColumn(4f);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1f);
                            columns.RelativeColumn(1f);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1f);
                        });

                        // Encabezado
                        table.Header(header =>
                        {
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("#").FontSize(5).AlignCenter();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("MES").FontSize(5).AlignCenter();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("TIPO").FontSize(5).AlignCenter();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("CTA. BANCO").FontSize(5).AlignCenter();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("COD. BANCO").FontSize(5).AlignCenter();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("ASESOR").FontSize(5).AlignLeft();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("CI").FontSize(5).AlignLeft();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("CIUDAD").FontSize(5).AlignLeft();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("PRODUCCION").FontSize(5).AlignRight();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("MONTO").FontSize(5).AlignRight();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("NIVEL CICLO").FontSize(5).AlignCenter();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("NIVEL CONSOLIDADO").FontSize(5).AlignCenter();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("NIVELES ESCALADOS").FontSize(5).AlignCenter();
                        });

                        // Filas
                        foreach (var v in _data)
                        {
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.Nro.ToString()).FontSize(5).AlignCenter();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.Ciclo.ToUpper()).FontSize(5).AlignCenter();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.Tipo).FontSize(5).AlignCenter();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.Cuenta).FontSize(5).AlignCenter();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.CodigoBanco).FontSize(5).AlignCenter();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.Nombre).FontSize(5).AlignLeft();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.Carnet).FontSize(5).AlignLeft();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.Ciudad).FontSize(5).AlignLeft();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.PuntosR.ToString()).FontSize(5).AlignRight();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.Monto.ToString("N2")).FontSize(5).AlignRight();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.NivelAlcanzadoCiclo.ToUpper()).FontSize(5).AlignCenter();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.NivelConsolidado.ToUpper()).FontSize(5).AlignCenter();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.Escalados.ToString()).FontSize(5).AlignCenter();
                        }
                        table.Footer(footer =>
                        {
                            decimal totalProduccion = _data?.Sum(x => x.PuntosR) ?? 0;
                            decimal totalMonto = _data?.Sum(x => x.Monto) ?? 0;

                            // ===== TOTAL GENERAL
                            table.Cell().ColumnSpan(8).Element(EstiloReporte.HeaderCellStyle).Text("TOTAL:").FontSize(5).AlignRight().Bold();
                            table.Cell().Element(EstiloReporte.HeaderCellStyle).Text(totalProduccion.ToString("N2")).FontSize(5).AlignRight().Bold();
                            table.Cell().Element(EstiloReporte.HeaderCellStyle).Text(totalMonto.ToString("N2")).FontSize(5).AlignRight().Bold();
                            table.Cell().ColumnSpan(3).Element(EstiloReporte.HeaderCellStyle).Text("").FontSize(5).AlignRight().Bold();
                        });
                        
                    });
                });
            });
        }
    }
}
