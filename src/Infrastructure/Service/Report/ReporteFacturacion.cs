using ApiGuardian.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Reportes.Estilos;

namespace ApiGuardian.Infrastructure.Services.Pdf
{
    public class ReporteFacturacion : IDocument
    {
        private readonly List<RptFacturacion> _data;
        private readonly byte[] _logo;
        private readonly Utils Ut = new Utils();
        private readonly List<ItemAdministracionDetalleFactura> _detalleFactura;
        public ReporteFacturacion(List<RptFacturacion> data, byte[] logo, List<ItemAdministracionDetalleFactura> detalleFactura)
        {
            _data = data;
            _logo = logo;
            _detalleFactura = detalleFactura;
        }
        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
        
        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape()); 
                page.Margin(50);

                //page.Header().Element(ComposeHeader);
                page.Header().Row(row =>
                {
                    // Encabezado principal (izquierda)
                    row.RelativeItem().Element(ComposeHeaderFacturacion);

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
    private void ComposeHeaderFacturacion(IContainer container)
    {
        container.PaddingBottom(10).Row(row =>
        {
            row.Spacing(8);

            row.ConstantItem(90)   // 👈 ancho fijo
                .AlignMiddle()
                .AlignRight()
                .Image(_logo).FitArea();

            row.RelativeItem(10).Column(col =>
            {
                col.Spacing(2);
                col.Item().Text("DATOS PARA FACTURACION").FontSize(7).AlignLeft().Bold();

                col.Item()
                .DefaultTextStyle(x => x.FontSize(6))
                .Text(t =>
                {
                    t.Span("CICLO: ").Bold();
                    t.Span(_data[0].NombreCiclo);
                });

                col.Item()
                .DefaultTextStyle(x => x.FontSize(6))
                .Text(t =>
                {
                    t.Span("EMPRENDEDOR: ").Bold();
                    t.Span(_data[0].SNombreCompleto);
                });

                col.Item()
                .DefaultTextStyle(x => x.FontSize(6))
                .Text(t =>
                {
                    t.Span("C - P: ").Bold();
                    t.Span("CBBA - BOL");
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
                column.Item().Text("FACTURADOR POR COMISIONES").FontSize(8).Bold();
                column.Item().Element(c =>
                {
                    c.Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2.5f);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(2.5f);
                            columns.RelativeColumn(1.5F);
                            columns.RelativeColumn(1.5F);
                            columns.RelativeColumn(4.5f);


                        });

                        // Encabezado
                        table.Header(header =>
                        {
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("RAZON SOCIAL").FontSize(6).AlignLeft();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("NIT").FontSize(6).AlignLeft();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("DETALLE DE LA FACTURA").FontSize(6).AlignLeft();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("MONTO SUS").FontSize(6).AlignRight();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("MONTO BS").FontSize(6).AlignRight();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("LITERAL").FontSize(6).AlignLeft();
                        });

                        // Filas
                        foreach (var v in _data)
                        {
                            //if (v.TotalComisionVtaPersonal <= 0) return;
                            if (v.TotalComisionVtaPersonal > 0)
                            {
                                table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.RazonSocial).FontSize(6).AlignLeft();
                                table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.Nit).FontSize(6).AlignLeft();
                                table.Cell().Element(EstiloReporte.BodyCellStyle).Text(_detalleFactura.Where(x=> x.LTipoComisionId == 1).ToList()[0].SDetalle).FontSize(6).AlignLeft();
                                table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.TotalComisionVtaPersonal.ToString("N2")).FontSize(6).AlignRight();
                                table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.TotalComisionVtaPersonalBs.ToString("N2")).FontSize(6).AlignRight();
                                table.Cell().Element(EstiloReporte.BodyCellStyle).Text(Ut.NumeroALiteral(v.TotalComisionVtaPersonalBs)).FontSize(6).AlignLeft();
                            }

                            

                        }
                        table.Footer(footer =>
                        {
                            decimal totalVP = _data?.Sum(x => x.TotalComisionVtaPersonal) ?? 0;
                            decimal totalVPBS = _data?.Sum(x => x.TotalComisionVtaPersonalBs) ?? 0;

                            table.Cell().ColumnSpan(3).Element(EstiloReporte.HeaderCellStyle).Text("TOTAL:").FontSize(6).AlignRight().Bold();
                            table.Cell().Element(EstiloReporte.HeaderCellStyle).Text(totalVP.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(EstiloReporte.HeaderCellStyle).Text(totalVPBS.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(EstiloReporte.HeaderCellStyle).Text("").FontSize(6).AlignRight();

                        });
                        
                    });
                });
                column.Item().Text("");
                column.Item().Text("FACTURADOR POR SERVICIOS").FontSize(8).Bold();
                column.Item().Element(c =>
                {
                    c.Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2.5f);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(2.5f);
                            columns.RelativeColumn(1.5F);
                            columns.RelativeColumn(1.5F);
                            columns.RelativeColumn(4.5f);
                        });

                        // Encabezado
                        table.Header(header =>
                        {
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("RAZON SOCIAL").FontSize(6).AlignLeft();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("NIT").FontSize(6).AlignLeft();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("DETALLE DE LA FACTURA").FontSize(6).AlignLeft();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("MONTO SUS").FontSize(6).AlignRight();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("MONTO BS").FontSize(6).AlignRight();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("LITERAL").FontSize(6).AlignLeft();
                        });
                        
                        // Filas
                        foreach (var v in _data)
                        {
                            if (v.TotalComisionVtaGrupoResidual > 0)
                            {
                                table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.RazonSocial).FontSize(6).AlignLeft();
                                table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.Nit).FontSize(6).AlignLeft();
                                table.Cell().Element(EstiloReporte.BodyCellStyle).Text(_detalleFactura.Where(x=> x.LTipoComisionId == 2).ToList()[0].SDetalle).FontSize(6).AlignLeft();
                                table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.TotalComisionVtaGrupoResidual.ToString("N2")).FontSize(6).AlignRight();
                                table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.TotalComisionVtaGrupoResidualBs.ToString("N2")).FontSize(6).AlignRight();
                                table.Cell().Element(EstiloReporte.BodyCellStyle).Text(Ut.NumeroALiteral(v.TotalComisionVtaGrupoResidualBs)).FontSize(6).AlignLeft();
                            }
                        }
                        table.Footer(footer =>
                        {
                            decimal totalVP = _data?.Sum(x => x.TotalComisionVtaGrupoResidual) ?? 0;
                            decimal totalVPBS = _data?.Sum(x => x.TotalComisionVtaGrupoResidualBs) ?? 0;

                            table.Cell().ColumnSpan(3).Element(EstiloReporte.HeaderCellStyle).Text("TOTAL:").FontSize(6).AlignRight().Bold();
                            table.Cell().Element(EstiloReporte.HeaderCellStyle).Text(totalVP.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(EstiloReporte.HeaderCellStyle).Text(totalVPBS.ToString("N2")).FontSize(6).AlignRight();
                            table.Cell().Element(EstiloReporte.HeaderCellStyle).Text("").FontSize(6).AlignRight();

                        });
                        
                    });
                });
            });
        }
    }
}
