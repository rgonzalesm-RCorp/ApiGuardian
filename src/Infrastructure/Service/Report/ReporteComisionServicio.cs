using ApiGuardian.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Reportes.Estilos;

namespace ApiGuardian.Infrastructure.Services.Pdf
{
    public class ReporteComisionServicio : IDocument
    {
        private readonly List<RptComisionServicio> _data;
        private readonly int _empresaId = 0;

        public ReporteComisionServicio(List<RptComisionServicio> data, int empresaId)
        {
            _data = data.ToList();
            _empresaId = empresaId;
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
                    column.Item().Row(row =>
                    {
                        row.Spacing(20);

                        // COLUMNA IZQUIERDA
                        row.RelativeItem(3).Column(colLeft =>
                        {
                            colLeft.Spacing(3);

                            colLeft.Item().Text(text =>
                            {
                                text.Span("NIT: ").FontSize(6).Bold();
                                text.Span(_data[0].SNit).FontSize(6);
                            });

                            colLeft.Item().Text(text =>
                            {
                                text.Span("CICLO: ").FontSize(6).Bold();
                                text.Span(_data[0].Ciclo).FontSize(6);
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
                                text.Span("EMPRESA: ").FontSize(6).Bold();
                                text.Span( _empresaId == -1? "TODAS": _data[0].Empresa).FontSize(6);
                            });

                            colRight.Item().Text(text =>
                            {
                                text.Span("FECHA : ").FontSize(6).Bold();
                                text.Span($"{_data[0].FechaInicio.ToString("dd/MM/yyyy")} - {_data[0].FechaFin.ToString("dd/MM/yyyy")}").FontSize(6);
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
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(5f);

                            if(_empresaId == -1)
                            {
                                columns.RelativeColumn(2.5f);
                            }
                            columns.RelativeColumn(1.5F);
                            columns.RelativeColumn(1.5F);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1f);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1.7f);
                        });

                        // Encabezado
                        table.Header(header =>
                        {
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("COD.").FontSize(5).AlignLeft();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("ASESOR").FontSize(5).AlignLeft();
                            if(_empresaId == -1)
                            {
                                header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("EMPRESA").FontSize(5).AlignLeft();
                            }
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("COMISION $").FontSize(5).AlignRight();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("SERVICIO $").FontSize(5).AlignRight();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("TOTAL COM. $").FontSize(4).AlignRight();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("13%").FontSize(5).AlignRight();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("87%").FontSize(5).AlignRight();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("RET. %").FontSize(5).AlignCenter();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("RET. $").FontSize(5).AlignRight();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("TOTAL $").FontSize(5).AlignRight();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("TOTAL PAGAR $").FontSize(5).AlignRight();
                        });

                        // Filas
                        foreach (var v in _data)
                        {
                            decimal trece = (v.Comision + v.Servicio )*Convert.ToDecimal( 0.13);
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.SCodigo).FontSize(6).AlignLeft();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.SNombreCompleto).FontSize(6).AlignLeft();
                            if(_empresaId == -1)
                            {
                                string nombre = v.Empresa;
                                nombre = nombre.Replace("S.R.L.", "");
                                nombre = nombre.Replace("S.R.L", "");
                                nombre = nombre.Replace("INMOBILIARIA", "");
                                nombre = nombre.TrimEnd().TrimStart();
                                table.Cell().Element(EstiloReporte.BodyCellStyle).Text(nombre).FontSize(6).AlignLeft();
                            }
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.Comision.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.Servicio.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text((v.Comision + v.Servicio).ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(
                                ((v.PorcentajeRetencion <= 0 )? ((v.Comision + v.Servicio)* Convert.ToDecimal(0.13)) : 0).ToString("N2")
                            ).FontSize(6).AlignRight();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(
                                ((v.PorcentajeRetencion <= 0 )? ((v.Comision + v.Servicio)* Convert.ToDecimal(0.87)) : 0).ToString("N2")
                            ).FontSize(6).AlignRight();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.PorcentajeRetencion.ToString("N2")).FontSize(6).AlignCenter();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.MontoRetencion.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text((v.Comision + v.Servicio).ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(
                                (v.Comision + v.Servicio - v.MontoRetencion).ToString("N2")
                            ).FontSize(6).AlignRight();


                        }
                        table.Footer(footer =>
                        {
                            decimal totalComision = _data?.Sum(x => x.Comision) ?? 0;
                            decimal totalServicio = _data?.Sum(x => x.Servicio) ?? 0; 
                            decimal TtotalComision = totalComision + totalServicio ;   
                            decimal totalTrece = _data?.Sum(x => x.PorcentajeRetencion <= 0 ?  ((x.Comision + x.Servicio)* Convert.ToDecimal(0.13)) : 0) ?? 0; 
                            decimal totalOchoSiete = _data?.Sum(x => x.PorcentajeRetencion <= 0 ?  ((x.Comision + x.Servicio)* Convert.ToDecimal(0.87)) : 0) ?? 0; 

                            decimal totalRetencion = _data?.Sum(x => x.MontoRetencion) ?? 0; 
                            decimal total = totalComision + totalServicio ; 
                            decimal totalPagar = totalComision + totalServicio - totalRetencion ; 
                            int colSpam = _empresaId == -1 ? 3:2;
                             
                            table.Cell().ColumnSpan(Convert.ToUInt32(colSpam)).Element(EstiloReporte.HeaderCellStyle).Text("TOTAL:").FontSize(6).AlignRight().Bold();

                            table.Cell().Element(EstiloReporte.HeaderCellStyle).Text(totalComision.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(EstiloReporte.HeaderCellStyle).Text(totalServicio.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(EstiloReporte.HeaderCellStyle).Text(TtotalComision.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(EstiloReporte.HeaderCellStyle).Text(totalTrece.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(EstiloReporte.HeaderCellStyle).Text(totalOchoSiete.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(EstiloReporte.HeaderCellStyle).Text("").FontSize(6).AlignRight();
                            table.Cell().Element(EstiloReporte.HeaderCellStyle).Text(totalRetencion.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(EstiloReporte.HeaderCellStyle).Text(total.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(EstiloReporte.HeaderCellStyle).Text(totalPagar.ToString("N2")).FontSize(6).AlignRight();

                        });
                        
                    });
                });
            });
        }
    }
}
