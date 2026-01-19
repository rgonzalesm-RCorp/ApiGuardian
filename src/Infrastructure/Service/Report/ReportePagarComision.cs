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

        public ReportePagarComision(List<RptPagarComision> data, List<RptProrrateo> prorrateo, List<EmpresaHeaderPagarComision> headerEmpresa)
        {
            _data = data.ToList();
            _prorrateo = prorrateo;
            _headerEmpresa = headerEmpresa;
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
                            foreach (var item in _headerEmpresa)
                            {
                                columns.RelativeColumn(1.5f);
                            }
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
                            foreach (var item in _headerEmpresa)
                            {
                                string nombre = item.SEmpresa;
                                nombre = nombre.Replace("S.R.L.", "");
                                nombre = nombre.Replace("S.R.L", "");
                                nombre = nombre.Replace("INMOBILIARIA", "");
                                header.Cell().Element(EstiloReporte.HeaderCellStyle).Text(nombre.Trim()).FontSize(5).AlignRight();
                            }
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
                                Prorrateo = g.Sum(x => x.Prorrateo)
                            })
                            .ToList();

                        var retencionPorContacto = _prorrateo
                            .GroupBy(x => x.LContactoId)
                            .ToDictionary(
                                g => g.Key,
                                g => g.Sum(x => x.Retencion)
                            );

                        var prorrateoLookup = new Dictionary<(int LContactoId, int EmpresaId), decimal>();

                        foreach (var contacto in grupos.GroupBy(x => x.LContactoId))
                        {
                            var lContactoId = contacto.Key;
                            var retencionTotal = retencionPorContacto.GetValueOrDefault(lContactoId);

                            var empresa21 = contacto.FirstOrDefault(x => x.EmpresaId == 21);
                            var empresa2  = contacto.FirstOrDefault(x => x.EmpresaId == 2);

                            foreach (var item in contacto)
                            {
                                prorrateoLookup[(item.LContactoId, item.EmpresaId)] = item.Prorrateo;
                            }

                            if (retencionTotal > 0 && empresa21 != null)
                            {
                                var montoEmpresa21 = empresa21.Prorrateo;

                                prorrateoLookup[(lContactoId, 21)] = 0m;

                                if (empresa2 != null)
                                {
                                    prorrateoLookup[(lContactoId, 2)] =
                                        prorrateoLookup[(lContactoId, 2)] + montoEmpresa21;
                                }
                                else
                                {
                                    prorrateoLookup[(lContactoId, 2)] = montoEmpresa21;
                                }
                            }
                        }


                        foreach (var v in _data)
                        {
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.TipoCuenta).FontSize(6).AlignLeft();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.CodigoBanco).FontSize(6).AlignLeft();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.CuentaBanco).FontSize(6).AlignLeft();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.Ciudad).FontSize(6).AlignLeft();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.NombreCompleto).FontSize(6).AlignLeft();
                            table.Cell().Element(EstiloReporte.BodyCellStyle).Text(v.CedulaIdentidad).FontSize(6).AlignCenter();

                            decimal montoTotal = 0;

                            foreach (var item in _headerEmpresa)
                            {
                                if (prorrateoLookup.TryGetValue((v.LContactold, item.EmpresaId), out var monto))
                                {
                                    montoTotal += monto;
                                    table.Cell().Element(EstiloReporte.BodyCellStyle)
                                        .Text(monto.ToString("N2"))
                                        .FontSize(6)
                                        .AlignRight();
                                }
                                else
                                {
                                    table.Cell().Element(EstiloReporte.BodyCellStyle)
                                        .Text(montoCero.ToString("N2"))
                                        .FontSize(6)
                                        .AlignRight();
                                }
                            }
                            table.Cell().Element(EstiloReporte.BodyCellStyle)
                                //.Text((v.Personal + v.Liderazgo + v.Grupo + v.Residual - v.Descuento - v.Retencion).ToString("N2"))
                                .Text(montoTotal.ToString("N2"))
                                .FontSize(6)
                                .AlignRight();
                        }
                        table.Footer(footer =>
                        {
                            decimal totalGeneral = 0;

                            // ===== TOTAL GENERAL
                            table.Cell().ColumnSpan(6).Element(EstiloReporte.HeaderCellStyle).Text("TOTAL:").FontSize(6).AlignRight().Bold();

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
                            table.Cell().Element(EstiloReporte.HeaderCellStyle).Text(totalGeneral.ToString("N2")).FontSize(6).AlignRight().Bold();
                        });
                        
                    });
                });
            });
        }
    }
}
