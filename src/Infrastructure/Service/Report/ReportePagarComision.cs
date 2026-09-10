using ApiGuardian.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Reportes.Estilos;

namespace ApiGuardian.Infrastructure.Services.Pdf
{
    public class ReportePagarComision : IDocument
    {
        private readonly List<RptPagarComision> _data;
        private readonly List<RptProrrateo> _prorrateo;
        private readonly List<EmpresaHeaderPagarComision> _headerEmpresa;
        private readonly List<RedistribucionPagoComision> _redistribucionesPorRetencion;

        public ReportePagarComision(List<RptPagarComision> data, List<RptProrrateo> prorrateo, List<EmpresaHeaderPagarComision> headerEmpresa, List<RedistribucionPagoComision> redistribucionesPorRetencion)
        {
            _data = data.ToList();
            _prorrateo = prorrateo;
            _headerEmpresa = headerEmpresa;
            _redistribucionesPorRetencion = redistribucionesPorRetencion;
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
                    column.Item().Text(_data[0].Ciclo).AlignCenter().FontSize(7);
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
                            foreach (var item in _headerEmpresa)
                            {
                                columns.RelativeColumn(1.5f);
                            }
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1.5f);
                        });

                        // Encabezado
                        table.Header(header =>
                        {
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("Tipo Cuenta.").FontSize(5).AlignLeft();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("Cod. Banco").FontSize(5).AlignLeft();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("Cta. Banco").FontSize(5).AlignLeft();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("Ciudad").FontSize(5).AlignLeft();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("Asesor").FontSize(5).AlignLeft();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("Cedula Identidad").FontSize(5).AlignCenter();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("Comisión Inicial").FontSize(5).AlignRight();
                            foreach (var item in _headerEmpresa)
                            {
                                string nombre = item.SEmpresa;
                                nombre = nombre.Replace("S.R.L.", "");
                                nombre = nombre.Replace("S.R.L", "");
                                nombre = nombre.Replace("INMOBILIARIA", "");
                                header.Cell().Element(EstiloReporte.HeaderCellStyle).Text(nombre.Trim()).FontSize(5).AlignRight();
                            }
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("Total Descuento").FontSize(5).AlignRight();
                            header.Cell().Element(EstiloReporte.HeaderCellStyle).Text("Total Pagar").FontSize(5).AlignRight();
                        });

                        // Filas
                        decimal montoCero = 0;
                        
                        var grupos = _prorrateo
                            .GroupBy(x => new { x.LContactoId, x.EmpresaId })
                            .Select(g => new
                            {
                                g.Key.LContactoId,
                                g.Key.EmpresaId,
                                Prorrateo = g.Sum(x => x.Prorrateo),
                                Retencion = g.Sum(x => x.Retencion)
                            })
                            .ToList();

                        var prorrateoLookup = new Dictionary<(int LContactoId, int EmpresaId), decimal>();

                        foreach (var contacto in grupos.GroupBy(x => x.LContactoId))
                        {
                            var lContactoId = contacto.Key;
                            foreach (var item in contacto)
                            {
                                prorrateoLookup[(item.LContactoId, item.EmpresaId)] = item.Prorrateo;
                            }

                            foreach (var regla in _redistribucionesPorRetencion)
                            {
                                if (regla.EmpresaOrigenId <= 0 || regla.EmpresaAsumeId <= 0)
                                    continue;
                                var empresaOrigen = contacto.FirstOrDefault(item => item.EmpresaId == regla.EmpresaOrigenId);
                                var retencionOrigen = contacto
                                    .Where(item => item.EmpresaId == regla.EmpresaOrigenId)
                                    .Sum(item => item.Retencion);
                                if (retencionOrigen > 0 && empresaOrigen != null)
                                {
                                    var montoOrigen = prorrateoLookup.GetValueOrDefault((lContactoId, regla.EmpresaOrigenId));
                                    prorrateoLookup[(lContactoId, regla.EmpresaOrigenId)] = 0m;
                                    prorrateoLookup[(lContactoId, regla.EmpresaAsumeId)] =
                                        prorrateoLookup.GetValueOrDefault((lContactoId, regla.EmpresaAsumeId)) + montoOrigen;
                                }
                            }
                        }


                        foreach (var v in _data)
                        {
                            table.Cell().Element(EstiloReporte.PagarComisionBodyCellStyle).Text(v.TipoCuenta).AlignLeft();
                            table.Cell().Element(EstiloReporte.PagarComisionBodyCellStyle).Text(v.CodigoBanco).AlignLeft();
                            table.Cell().Element(EstiloReporte.PagarComisionBodyCellStyle).Text(v.CuentaBanco).AlignLeft();
                            table.Cell().Element(EstiloReporte.PagarComisionBodyCellStyle).Text(v.Ciudad).AlignLeft();
                            table.Cell().Element(EstiloReporte.PagarComisionBodyCellStyle).Text(v.NombreCompleto).AlignLeft();
                            table.Cell().Element(EstiloReporte.PagarComisionBodyCellStyle).Text(v.CedulaIdentidad).AlignCenter();
                            table.Cell().Element(EstiloReporte.PagarComisionBodyCellStyle).Text(v.ComisionDespuesRetencion.ToString("N2")).AlignRight();

                            decimal montoTotal = 0;

                            foreach (var item in _headerEmpresa)
                            {
                                if (prorrateoLookup.TryGetValue((v.LContactold, item.EmpresaId), out var monto))
                                {
                                    montoTotal += monto;
                                    table.Cell().Element(EstiloReporte.PagarComisionBodyCellStyle)
                                        .Text(monto.ToString("N2"))
                                        .FontSize(5)
                                        .AlignRight();
                                }
                                else
                                {
                                    table.Cell().Element(EstiloReporte.PagarComisionBodyCellStyle)
                                        .Text(montoCero.ToString("N2"))
                                        .FontSize(5)
                                        .AlignRight();
                                }
                            }
                            table.Cell().Element(EstiloReporte.PagarComisionBodyCellStyle)
                                .Text(v.TotalDescuento.ToString("N2"))
                                .FontSize(5)
                                .AlignRight();
                            table.Cell().Element(EstiloReporte.PagarComisionBodyCellStyle)
                                .Text(montoTotal.ToString("N2"))
                                .FontSize(5)
                                .AlignRight();
                        }
                        table.Footer(footer =>
                        {
                            decimal totalGeneral = 0;
                            decimal totalDescuento = 0;

                            // ===== TOTAL GENERAL
                            table.Cell().ColumnSpan(6).Element(EstiloReporte.HeaderCellStyle).Text("TOTAL:").FontSize(6).AlignRight().Bold();
                            table.Cell().Element(EstiloReporte.HeaderCellStyle).Text(_data.Sum(item => item.ComisionDespuesRetencion).ToString("N2")).FontSize(6).AlignRight().Bold();

                            // ===== TOTALES POR EMPRESA (DINÁMICO)
                            foreach (var item in _headerEmpresa)
                            {
                                decimal totalEmpresa = 0;

                                foreach (var v in _data)
                                {
                                    if (prorrateoLookup.TryGetValue((v.LContactold, item.EmpresaId), out var monto))
                                    {
                                        totalEmpresa += monto;
                                    }
                                }
                                totalGeneral += totalEmpresa;

                                table.Cell().Element(EstiloReporte.HeaderCellStyle).Text(totalEmpresa.ToString("N2")).FontSize(6).AlignRight().Bold();
                            }
                            totalDescuento = _data.Sum(item => item.TotalDescuento);
                            table.Cell().Element(EstiloReporte.HeaderCellStyle).Text(totalDescuento.ToString("N2")).FontSize(6).AlignRight().Bold();
                            table.Cell().Element(EstiloReporte.HeaderCellStyle).Text(totalGeneral.ToString("N2")).FontSize(6).AlignRight().Bold();
                        });
                        
                    });
                });
            });
        }
    }
}
