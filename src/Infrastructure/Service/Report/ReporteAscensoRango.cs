using ApiGuardian.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Reportes.Estilos;

namespace ApiGuardian.Infrastructure.Services.Pdf
{
    public class ReporteAscensoRango : IDocument
    {
        private readonly List<ItemAscensoRango> _data;
 

        public ReporteAscensoRango(List<ItemAscensoRango> data)
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
                    
                    column.Item().Text("REPORTE ASCENSO DE RANGO")
                        .FontSize(9).Bold().FontColor(Colors.Blue.Medium)
                        .AlignCenter();
                    column.Item().Text("");
                    column.Item().Text(_data[0].Mes.ToUpper()).AlignCenter().FontSize(7);
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
                            columns.RelativeColumn(4F);
                            columns.RelativeColumn(1.5F);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1f);
                            columns.RelativeColumn(2f);
                            columns.RelativeColumn(1f);
                            columns.RelativeColumn(2f);
                            columns.RelativeColumn(1f);
                            columns.RelativeColumn(1f);
                        });

                        // Encabezado
                        table.Header(header =>
                        {
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("#").FontSize(5).AlignCenter();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("MES").FontSize(5).AlignCenter();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("ASESOR").FontSize(5).AlignLeft();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("CI").FontSize(5).AlignLeft();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("TELEFONO").FontSize(5).AlignLeft();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("CIUDAD").FontSize(5).AlignLeft();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("PAIS").FontSize(5).AlignCenter();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("PTOS. DEL CICLO").FontSize(5).AlignRight();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("NIVEL ALCANZADO").FontSize(5).AlignCenter();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("INCENTIVO $").FontSize(5).AlignRight();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("INCENTIVO ESPECIE").FontSize(5).AlignLeft();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("VALOR ESPECIE").FontSize(5).AlignRight();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("ALCANZO VME").FontSize(5).AlignCenter();
                        });

                        // Filas
                        foreach (var v in _data)
                        {
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.Nro.ToString()).FontSize(5).AlignCenter();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.Mes.ToUpper()).FontSize(5).AlignCenter();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.Nombre).FontSize(5).AlignLeft();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.CI).FontSize(5).AlignLeft();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.Telefono).FontSize(5).AlignLeft();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.Ciudad).FontSize(5).AlignLeft();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.Pais).FontSize(5).AlignLeft();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.PuntosAlcanzado.ToString("N2")).FontSize(5).AlignRight();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.NivelAlcanzado).FontSize(4).AlignCenter();

                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.IncentivoDolares.ToString("N2")).FontSize(5).AlignRight();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.Incentivo).FontSize(4).AlignLeft();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.ValorEspecie.ToString("N2")).FontSize(5).AlignRight();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text("").FontSize(5).AlignCenter();
                        }
                        table.Footer(footer =>
                        {
                            decimal totalIncentivoDolares = _data?.Sum(x => x.IncentivoDolares) ?? 0;
                            decimal totalValorEspecie = _data?.Sum(x => x.ValorEspecie) ?? 0;

                            // ===== TOTAL GENERAL
                            table.Cell().ColumnSpan(9).Element(EstiloReporte.HeaderCellStyle).Text("TOTAL:").FontSize(5).AlignRight().Bold();
                            table.Cell().Element(EstiloReporte.HeaderCellStyle).Text(totalIncentivoDolares.ToString("N2")).FontSize(5).AlignRight().Bold();
                            table.Cell().Element(EstiloReporte.HeaderCellStyle).Text("").FontSize(5).AlignRight().Bold();
                            table.Cell().Element(EstiloReporte.HeaderCellStyle).Text(totalValorEspecie.ToString("N2")).FontSize(5).AlignRight().Bold();
                            table.Cell().Element(EstiloReporte.HeaderCellStyle).Text("").FontSize(5).AlignRight().Bold();
                             
                        });
                        
                    });
                });
            });
        }
    }
}
