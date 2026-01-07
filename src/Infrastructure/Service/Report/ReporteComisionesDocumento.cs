using ApiGuardian.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Reportes.Estilos;

namespace ApiGuardian.Infrastructure.Services.Pdf
{
    public class ReporteComisionesDocumento : IDocument
    {
        private readonly ReporteComisionesDto _data;

        public ReporteComisionesDocumento(ReporteComisionesDto data)
        {
            _data = data;
        }
        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(20);

                //page.Header().Element(ComposeHeader);
                page.Header()
                    .ShowOnce()
                    .Element(c => ComposeHeader(c)); 
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
                    column.Item().Text("REPORTE DE COMISIONES")
                        .FontSize(15).Bold().FontColor(Colors.Blue.Medium)
                        .AlignCenter();

                    column.Item().Text($"{_data.Encabezado.Ciclo} ({_data.Encabezado.Inicio.ToString("dd/MM/yyyy")} - {_data.Encabezado.Fin.ToString("dd/MM/yyyy")})")
                        .FontSize(10).Bold().AlignCenter();
                    column.Item().Text("");

                    column.Item().Text($"{_data.Encabezado.NombreCompleto}")
                        .FontSize(10);

                    column.Item().Text($"MESES ACTIVIDAD: {_data.Encabezado.MesActividad}")
                        .FontSize(8);
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
                column.Item().Element(ComposeVentasGrupo);
                column.Item().Element(ComposeResumen);
            });
        }

        // SECCIÓN: VENTAS PERSONALES
        private void ComposeVentasPersonales(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().Text("VENTAS PERSONALES")
                    .FontSize(10).Bold().FontColor(Colors.Blue.Medium);

                column.Item().Element(c =>
                {
                    c.Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1); // Fecha
                            columns.RelativeColumn(4); // Venta
                            columns.RelativeColumn(1); // Tipo
                            columns.RelativeColumn(1); // CI
                            columns.RelativeColumn(1); // % / Comisión
                            columns.RelativeColumn(1); // % / Comisión
                        });

                        // Encabezado
                        table.Header(header =>
                        {
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("Fecha").FontSize(9).AlignCenter();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("Nro. Venta").FontSize(9).AlignCenter();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("Tipo").FontSize(9).AlignCenter();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("Inicial").FontSize(9).AlignRight();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("%").FontSize(9).AlignRight();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("Comisión").FontSize(9).AlignRight();
                        });

                        // Filas
                        foreach (var v in _data.VentasPersonales)
                        {
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text( v.Fecha?.Replace(" 00:00:00", "") ).FontSize(7);
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.NumeroVenta).FontSize(7);
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.Tipo?.ToLower()).FontSize(6).Bold().AlignCenter();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.CuotaInicial.ToString("0.00")).FontSize(7).AlignRight();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text($"{v.Porcentaje:0.##}%").FontSize(7).AlignRight();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text($"{v.Comision:0.00}").FontSize(7).AlignRight();
                        }
                        table.Footer(footer =>
                        {
                            decimal totalInicial = _data.VentasPersonales?.Sum(x => x.CuotaInicial) ?? 0;
                            decimal totalComision = _data.VentasPersonales?.Sum(x => x.Comision) ?? 0;

                            table.Cell().ColumnSpan(3).Element(EstiloReporte.HeaderCellStyle).Text("TOTAL:").FontSize(7).AlignRight().Bold();
                            table.Cell().Element(EstiloReporte.HeaderCellStyle).Text(totalInicial.ToString("N2")).FontSize(7).AlignRight();
                            table.Cell().Element(EstiloReporte.HeaderCellStyle).Text("").FontSize(7);
                            table.Cell().Element(EstiloReporte.HeaderCellStyle).Text(totalComision.ToString("N2")).FontSize(7).AlignRight();
                        });
                        
                    });
                });
            });
        }

        // SECCIÓN: VENTAS DEL GRUPO
        private void ComposeVentasGrupo(IContainer container)
        {
            if (_data.VentasGrupo.Count() <= 0 )return;
            container.Column(column =>
            {
                column.Item().Text("VENTAS DEL GRUPO")
                    .FontSize(10).Bold().FontColor(Colors.Blue.Medium);

                column.Item().Element(c =>
                {
                    c.Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(4); // Asesor
                            columns.RelativeColumn(1); // Tipo
                            columns.RelativeColumn(1); // Nivel
                            columns.RelativeColumn(1); // Producción
                            columns.RelativeColumn(1); // Comisión
                            columns.RelativeColumn(1); // Comisión
                            columns.RelativeColumn(1); // Comisión
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("Asesor").FontSize(9);
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("Basado en").FontSize(9).AlignCenter();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("Gen.").FontSize(9).AlignCenter();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("Tipo").FontSize(9).AlignCenter();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("Vta").FontSize(9).AlignRight();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("%").FontSize(9).AlignRight();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("Comision").FontSize(9).AlignRight();
                        });

                        foreach (var g in _data.VentasGrupo)
                        {
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(g.Asesor).FontSize(7);
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(g.BasadoEn?.ToLower()).FontSize(6).Bold().AlignCenter();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(g.Generacion).FontSize(7).AlignCenter();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(g.Tipo).FontSize(7).AlignCenter();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text($"{g.VentaPersonal:0.00}").FontSize(7).AlignRight();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text($"{g.PorcentajeComision.ToString("N2")}%").FontSize(7).AlignRight();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(g.Comision.ToString("N2")).FontSize(7).AlignRight();
                        }
                        table.Footer(footer =>
                        {
                            decimal totaVentas = _data.VentasGrupo?.Sum(x => x.VentaPersonal) ?? 0;
                            decimal totalComision = _data.VentasGrupo?.Sum(x => x.Comision) ?? 0;
                            table.Cell().ColumnSpan(4).Element(EstiloReporte.HeaderCellStyle).Text("TOTALES:").FontSize(7).AlignRight();
                            table.Cell().Element(EstiloReporte.HeaderCellStyle).Text(totaVentas.ToString("N2")).FontSize(7).AlignRight();
                            table.Cell().Element(EstiloReporte.HeaderCellStyle).Text("").FontSize(7).AlignRight();
                            table.Cell().Element(EstiloReporte.HeaderCellStyle).Text(totalComision.ToString("N2")).FontSize(7).AlignRight();
                        });
                    });
                });
            });
        }

        // SECCIÓN: RESUMEN
        private void ComposeResumen(IContainer container)
        {
            container.Column(column =>
            {
                // Columna izquierda
                column.Item().Row(row =>
                {
                   
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("PLAN DE CARRERA").FontSize(10).Bold().FontColor(Colors.Blue.Medium);
                        col.Item().Element(c =>
                        {
                            c.Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                });
                                table.Header(header =>
                                {
                                    header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("Nivel Obtenido en el Mes").FontSize(9).AlignLeft();
                                    header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("Cant. Prod. Vtas.").FontSize(9).AlignCenter();

                                });
                                foreach (var g in _data.BonoCarrera)
                                {
                                    table.Cell().Element(EstiloReporte.BodyCellStyle).Text(g.NivelCiclo).FontSize(7).AlignLeft();
                                    table.Cell().Element(EstiloReporte.BodyCellStyle).Text(g.CantidadVentas.ToString("N2")).FontSize(7).AlignCenter();

                                }
                            });    
                        });
                        col.Item().Text("");

                        col.Item().Text("BONO RESIDUAL").FontSize(10).Bold().FontColor(Colors.Blue.Medium);
                        col.Item().Element(c =>
                        {
                            c.Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                });
                                table.Header(header =>
                                {
                                    header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("Tipo").FontSize(9).AlignCenter();
                                    header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("%1G").FontSize(9).AlignCenter();
                                    header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("BxL").FontSize(9).AlignCenter();
                                    header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("Terrenos").FontSize(9).AlignRight();
                                    header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("Total").FontSize(9).AlignRight();
                                });
                                foreach (var g in _data.BonoRedisual)
                                {
                                    table.Cell().Element(EstiloReporte.BodyCellStyle).Text($"{g?.Tipo ?? 0}").FontSize(7).AlignCenter();
                                    table.Cell().Element(EstiloReporte.BodyCellStyle).Text($"{g?.PocentajeGeneracion ?? 0:0.##}%").FontSize(7).AlignCenter();
                                    table.Cell().Element(EstiloReporte.BodyCellStyle).Text($"{g?.Bxl ?? 0:0.00}").FontSize(7).AlignCenter();
                                    table.Cell().Element(EstiloReporte.BodyCellStyle).Text(g?.Terrenos.ToString("N2")).FontSize(7).AlignRight();
                                    table.Cell().Element(EstiloReporte.BodyCellStyle).Text(g?.Total.ToString("N2")).FontSize(7).AlignRight();
                                }
                            });    
                        });
                        
                        col.Item().Text("");

                        col.Item().Text("BONO LIDERAZGO").FontSize(10).Bold().FontColor(Colors.Blue.Medium);
                        col.Item().Element(c =>
                        {
                            c.Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(1);
                                });
                                table.Header(header =>
                                {
                                    header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("Cant. P.").FontSize(9).AlignLeft();
                                    header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("Total").FontSize(9).AlignRight();
                                });
                                foreach (var g in _data.BonoLiderazgo)
                                {
                                    table.Cell().Element(EstiloReporte.BodyCellStyle).Text($"{g?.Cantidad ?? 0}").FontSize(7).AlignLeft();
                                    table.Cell().Element(EstiloReporte.BodyCellStyle).Text(g?.Comision.ToString("N2")).FontSize(7).AlignRight();
                                }
                            });    
                        });
                        
                        col.Item().Text("");

                        col.Item().Text($"DESCUENTOS: {_data.Comisiones.Detalle}").FontSize(7);
                    });
                    row.ConstantItem(25);
                    // Columna derecha
                    row.RelativeItem().Column(col =>
                    {

                        col.Item().Element(c =>
                        {
                            c.Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(1);
                                });

                                    table.Cell().Element(EstiloReporte.BodyCellStyle).Text($"Total Vta. Personal: ").FontSize(7).AlignRight();
                                    table.Cell().Element(EstiloReporte.BodyCellStyle).Text( _data.Comisiones.ComisionVentaPersonal .ToString("N2")).FontSize(7).AlignRight();

                                    table.Cell().Element(EstiloReporte.BodyCellStyle).Text($"Total Vta. Grupo: ").FontSize(7).AlignRight();
                                    table.Cell().Element(EstiloReporte.BodyCellStyle).Text(_data.Comisiones.ComisionVentaGrupo.ToString("N2")).FontSize(7).AlignRight();

                                    table.Cell().Element(EstiloReporte.BodyCellStyle).Text($"Total Bono Residual: ").FontSize(7).AlignRight();
                                    table.Cell().Element(EstiloReporte.BodyCellStyle).Text(_data.Comisiones.ComisionResidual.ToString("N2")).FontSize(7).AlignRight();

                                    table.Cell().Element(EstiloReporte.BodyCellStyle).Text($"Total Bono Liderazgo: ").FontSize(7).AlignRight();
                                    table.Cell().Element(EstiloReporte.BodyCellStyle).Text(_data.Comisiones.ComisionLiderazgo.ToString("N2")).FontSize(7).AlignRight();

                                    table.Cell().Element(EstiloReporte.BodyCellStyle).Text($"Total Comisiones Mensual: ").FontSize(7).AlignRight().Bold();
                                    table.Cell().Element(TotalCellStyle).Text((
                                        _data.Comisiones.ComisionVentaPersonal + 
                                        _data.Comisiones.ComisionVentaGrupo+ 
                                        _data.Comisiones.ComisionResidual + 
                                        _data.Comisiones.ComisionLiderazgo
                                    ).ToString("N2")).FontSize(7).AlignRight().Bold();

                                    table.Cell().Element(EstiloReporte.BodyCellStyle).Text($"Retencion ({_data.Comisiones.PorcentajeRetencion.ToString("N2")}%): ").FontSize(7).AlignRight();
                                    table.Cell().Element(EstiloReporte.BodyCellStyle).Text($"-{_data.Comisiones.Retencion.ToString("N2")}").FontSize(7).AlignRight();

                                    table.Cell().Element(EstiloReporte.BodyCellStyle).Text($"Total Final: ").FontSize(7).AlignRight().Bold();
                                    table.Cell().Element(TotalCellStyle).Text(
                                        (_data.Comisiones.ComisionVentaPersonal + _data.Comisiones.ComisionVentaGrupo+  _data.Comisiones.ComisionResidual +  _data.Comisiones.ComisionLiderazgo - _data.Comisiones.Retencion).ToString("N2")
                                    ).FontSize(7).AlignRight().Bold();

                                    table.Cell().Element(EstiloReporte.BodyCellStyle).Text($"Descuento Lote: ").FontSize(7).AlignRight();
                                    table.Cell().Element(EstiloReporte.BodyCellStyle).Text($"-{_data.Comisiones.DescuentoLote.ToString("N2")}").FontSize(7).AlignRight();

                                    table.Cell().Element(EstiloReporte.BodyCellStyle).Text($"Tota a Pagar: ").FontSize(7).AlignRight().Bold();
                                    table.Cell().Element(TotalCellStyle).Text(
                                        (_data.Comisiones.ComisionVentaPersonal + _data.Comisiones.ComisionVentaGrupo+  _data.Comisiones.ComisionResidual +  _data.Comisiones.ComisionLiderazgo - _data.Comisiones.Retencion - _data.Comisiones.DescuentoLote).ToString("N2")
                                    ).FontSize(7).AlignRight().Bold();

                            }); 
                        });
                    });
               
                });

            });
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
