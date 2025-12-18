using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Infrastructure.Services.Pdf;
using QuestPDF.Fluent;
using ApiGuardian.Models;
using ApiGuardian.Application.Interfaces;
using System.ComponentModel;

namespace ApiGuardian.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportesController : ControllerBase
    {
        private readonly ILogService _log;
        private readonly IReportesRepository _repo;
        private readonly IAdministracionDescuentoComisionRepository _comision;
        private readonly string NOMBREARCHIVO = "UtilsController.cs";

        public ReportesController(IReportesRepository repo, ILogService log, IAdministracionDescuentoComisionRepository comision)
        {
            _repo = repo;
            _log = log;
            _comision = comision;
        }
        [HttpGet("comisiones")]
        public async Task<IActionResult> ReporteComisiones(
            [FromHeader] int lCicloId,
            [FromHeader] int lContactoId,
            [FromHeader] string? usuario
        )
        {
            long logId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string metodo = "ReporteComisiones()";

            _log.Info(logId.ToString(),NOMBREARCHIVO,metodo,$"Inicio ReporteComisiones - lCicloId={lCicloId}, lContactoId={lContactoId}, usuario={usuario}"
            );

            try
            {
                var reporteComision = await _repo.GetReporteComision(logId.ToString(), lCicloId, lContactoId);
                var comision = await _comision.GetComision(logId.ToString(), lContactoId, lCicloId, 1);

                reporteComision.Data.Comisiones = comision.Data;

                var documento = new ReporteComisionesDocumento(reporteComision.Data);


                byte[] pdfBytes = documento.GeneratePdf();
                string base64Pdf = Convert.ToBase64String(pdfBytes);

                _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, "PDF generado correctamente.");

                return Ok(new
                {
                    status = true,
                    mensaje = "Reporte generado correctamente.",
                    data = new
                    {
                        FileName = $"REPORTE DE COMISIONES {reporteComision.Data.Encabezado.NombreCompleto} - {reporteComision.Data.Encabezado.Ciclo}.pdf",
                        FileBase64 = base64Pdf,
                        ContentType = "application/pdf",
                        tieneComicion = reporteComision.Data.VentasPersonales.Count() <= 0 ? false :true,
                    }
                    
                });
            }
            catch (Exception ex)
            {
                _log.Error(
                    logId.ToString(),
                    NOMBREARCHIVO,
                    metodo,
                    "Error al generar reporte de comisiones",
                    ex
                );

                return Ok(new
                {
                    status = false,
                    mensaje = ex.Message,
                    data =""
                });
            }
        }
            [HttpGet("aplicaciones")]
        public async Task<IActionResult> ReporteAplicaciones(
            [FromHeader] int lCicloId,
            [FromHeader] int lContactoId,
            [FromHeader] string? usuario
        )
        {
            long logId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string metodo = "ReporteAplicaciones()";

            _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, $"Inicio ReporteAplicaciones - lCicloId={lCicloId}, lContactoId={lContactoId}, usuario={usuario}");

            try
            {
                var aplicaciones = await _repo.GetReporteAplicacines(logId.ToString(), lCicloId, lContactoId);

                if(aplicaciones.Data.Aplicaciones == null || aplicaciones.Data.Aplicaciones.Count()<= 0)
                {
                    return Ok(new
                    {
                        status = false,
                        mensaje = "No existe aplicaciones para el ciclo o asesor seleccionado",
                        data = new
                        {
                            FileName = "",
                            FileBase64 = "",
                            ContentType = "",
                            
                        }
                    });
                }
                var documento = new ReporteAplicacionesDocumento(aplicaciones.Data);


                byte[] pdfBytes = documento.GeneratePdf();
                string base64Pdf = Convert.ToBase64String(pdfBytes);

                _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, "PDF generado correctamente.");

                return Ok(new
                {
                    status = true,
                    mensaje = "Reporte generado correctamente.",
                    data = new
                    {
                        FileName = $"REPORTE DE APLICACIONES.pdf",
                        FileBase64 = base64Pdf,
                        ContentType = "application/pdf",
                         
                    }
                });
            }
            catch (Exception ex)
            {
                _log.Error(
                    logId.ToString(),
                    NOMBREARCHIVO,
                    metodo,
                    "Error al generar reporte de comisiones",
                    ex
                );

                return Ok(new
                {
                    status = false,
                    mensaje = ex.Message,
                    data =""
                });
            }
        }
        [HttpGet("descuento/empresa")]
        public async Task<IActionResult> ReporteDescuentoEmpresa(
            [FromHeader] int lCicloId,
            [FromHeader] int empresaId,
            [FromHeader] string? usuario
        )
        {
            long logId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string metodo = "ReporteDescuentoEmpresa()";

            _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, $"Inicio ReporteDescuentoEmpresa - lCicloId={lCicloId}, empresaId={empresaId}, usuario={usuario}");

            try
            {
                var descuentoEmpresa = await _repo.GetReporteDecuentoEmpresa(logId.ToString(), lCicloId, empresaId);

                if(descuentoEmpresa.Data == null || descuentoEmpresa.Data.Count()<= 0)
                {
                    return Ok(new
                    {
                        status = false,
                        mensaje = "No existe descuentos para el ciclo seleccionado",
                        data = new
                        {
                            FileName = "",
                            FileBase64 = "",
                            ContentType = "",
                            
                        }
                    });
                }
                var ListaDescuentoEmpresa = descuentoEmpresa.Data.ToList();
                var documento = new ReporteDescuentoEmpresa(ListaDescuentoEmpresa);


                byte[] pdfBytes = documento.GeneratePdf();
                string base64Pdf = Convert.ToBase64String(pdfBytes);

                _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, "PDF generado correctamente.");
                DescuentoEmpresaXls _ins = new DescuentoEmpresaXls();
                var responseXls = await _ins.GetDescuentoEmpresaXls(ListaDescuentoEmpresa); 
                
                return Ok(new
                {
                    status = true,
                    mensaje = "Reporte generado correctamente.",
                    data = new
                    {
                        FileName = $"REPORTE DE DESCUENTO POR EMPRESA - {ListaDescuentoEmpresa[0].Empresa}.pdf",
                        FileBase64 = base64Pdf,
                        ContentType = "application/pdf",
                        FileNameXls = $"REPORTE DE PRORRATEO  {ListaDescuentoEmpresa[0].Empresa}.xlsx",
                        base64Xls = responseXls.base64
                    }
                });
            }
            catch (Exception ex)
            {
                _log.Error(
                    logId.ToString(),
                    NOMBREARCHIVO,
                    metodo,
                    "Error al generar reporte de comisiones",
                    ex
                );

                return Ok(new
                {
                    status = false,
                    mensaje = ex.Message,
                    data =""
                });
            }
        }
        [HttpGet("facturacion")]
        public async Task<IActionResult> ReporteFacturacion(
            [FromHeader] int lCicloId,
            [FromHeader] int lContactoId,
            [FromHeader] string? usuario
        )
        {
            long logId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string metodo = "ReporteFacturacion()";

            _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, $"Inicio ReporteFacturacion - lCicloId={lCicloId}, lContactoId={lContactoId}, usuario={usuario}");

            try
            {
                var facturacion = await _repo.GetReporteFacturacion(logId.ToString(), lCicloId, lContactoId);

                if(facturacion.Data == null || facturacion.Data.Count()<= 0)
                {
                    return Ok(new
                    {
                        status = false,
                        mensaje = "No existe datos para el ciclo y asesor seleccionado",
                        data = new
                        {
                            FileName = "",
                            FileBase64 = "",
                            ContentType = "",
                            
                        }
                    });
                }
                var listaFacturacion = facturacion.Data.ToList();
                var logoPath = Path.Combine(Directory.GetCurrentDirectory(),"Assets","Logokalomai.png");

                byte[] logoBytes = System.IO.File.ReadAllBytes(logoPath);
                var documento = new ReporteFacturacion(listaFacturacion, logoBytes);


                byte[] pdfBytes = documento.GeneratePdf();
                string base64Pdf = Convert.ToBase64String(pdfBytes);

                _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, "PDF generado correctamente.");

                return Ok(new
                {
                    status = true,
                    mensaje = "Reporte generado correctamente.",
                    data = new
                    {
                        FileName = $"REPORTE DE FACTURACION {listaFacturacion[0].NombreCiclo} - {listaFacturacion[0].SNombreCompleto}.pdf",
                        FileBase64 = base64Pdf,
                        ContentType = "application/pdf",
                         
                    }
                });
            }
            catch (Exception ex)
            {
                _log.Error(
                    logId.ToString(),
                    NOMBREARCHIVO,
                    metodo,
                    "Error al generar reporte de comisiones",
                    ex
                );

                return Ok(new
                {
                    status = false,
                    mensaje = ex.Message,
                    data =""
                });
            }
        }
        [HttpGet("prorrateo")]
        public async Task<IActionResult> ReporteProrrateo(
            [FromHeader] int lCicloId,
            [FromHeader] string? usuario
        )
        {
            long logId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string metodo = "ReporteFacturacion()";

            _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, $"Inicio ReporteFacturacion - lCicloId={lCicloId}, usuario={usuario}");

            try
            {
                var facturacion = await _repo.GetReporteProrrateo(logId.ToString(), lCicloId);

                if(facturacion.Data == null || facturacion.Data.Count()<= 0)
                {
                    return Ok(new
                    {
                        status = false,
                        mensaje = "No existe datos para el ciclo seleccionado",
                        data = new
                        {
                            FileName = "",
                            FileBase64 = "",
                            ContentType = "",
                            
                        }
                    });
                }
                var listaFacturacion = facturacion.Data.ToList();
                var documento = new ReporteProrrateo(listaFacturacion);


                byte[] pdfBytes = documento.GeneratePdf();
                string base64Pdf = Convert.ToBase64String(pdfBytes);

                _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, "PDF generado correctamente.");
                ProrrateoXls _ins = new ProrrateoXls();
                var responseXls = await _ins.GetProrrateoXls(listaFacturacion); 
                return Ok(new
                {
                    status = true,
                    mensaje = "Reporte generado correctamente.",
                    data = new
                    {
                        FileName = $"REPORTE DE PRORRATEO {listaFacturacion[0].Ciclo}.pdf",
                        FileBase64 = base64Pdf,
                        ContentType = "application/pdf",
                        FileNameXls = $"REPORTE DE PRORRATEO  {listaFacturacion[0].Ciclo}.xlsx",
                        base64Xls = responseXls.base64
                         
                    }
                });
            }
            catch (Exception ex)
            {
                _log.Error(
                    logId.ToString(),
                    NOMBREARCHIVO,
                    metodo,
                    "Error al generar reporte de comisiones",
                    ex
                );

                return Ok(new
                {
                    status = false,
                    mensaje = ex.Message,
                    data =""
                });
            }
        }
        [HttpGet("comision/servicio")]
        public async Task<IActionResult> ReporteComisionServicio(
            [FromHeader] int lCicloId,
            [FromHeader] int empresaId,
            [FromHeader] string? usuario
        )
        {
            long logId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string metodo = "ReporteComisionServicio()";

            _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, $"Inicio ReporteFacturacion - lCicloId={lCicloId}, usuario={usuario}, empresaId={empresaId}");

            try
            {
                var comisionServicio = await _repo.GetReporteComisionServicio(logId.ToString(), lCicloId, empresaId);

                if(comisionServicio.Data == null || comisionServicio.Data.Count()<= 0)
                {
                    return Ok(new
                    {
                        status = false,
                        mensaje = "No existe datos para el ciclo seleccionado",
                        data = new
                        {
                            FileName = "",
                            FileBase64 = "",
                            ContentType = "",
                            
                        }
                    });
                }
                var listaComisionServicio= comisionServicio.Data.ToList();
                var documento = new ReporteComisionServicio(listaComisionServicio);


                byte[] pdfBytes = documento.GeneratePdf();
                string base64Pdf = Convert.ToBase64String(pdfBytes);

                _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, "PDF generado correctamente.");
                ComisionServicioXls _ins = new ComisionServicioXls();
                var responseXls = await _ins.GetComisionServicioXls(listaComisionServicio);

                return Ok(new
                {
                    status = true,
                    mensaje = "Reporte generado correctamente.",
                    data = new
                    {
                        FileName = $"REPORTE DE COMISION - SERVICIO  {listaComisionServicio[0].Empresa}.pdf",
                        FileBase64 = base64Pdf,
                        ContentType = "application/pdf",
                        FileNameXls = $"REPORTE DE COMISION - SERVICIO  {listaComisionServicio[0].Empresa}.xlsx",
                        base64Xls = responseXls.base64
                    }
                });
            }
            catch (Exception ex)
            {
                _log.Error(
                    logId.ToString(),
                    NOMBREARCHIVO,
                    metodo,
                    "Error al generar reporte de comisiones",
                    ex
                );

                return Ok(new
                {
                    status = false,
                    mensaje = ex.Message,
                    data =""
                });
            }
        }

        [HttpGet("pagar/comision")]
        public async Task<IActionResult> ReportePagarComision(
            [FromHeader] int lCicloId,
            [FromHeader] string? usuario
        )
        {
            long logId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string metodo = "ReportePagarComision()";

            _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, $"Inicio ReporteFacturacion - lCicloId={lCicloId}, usuario={usuario}");

            try
            {
                var pagarComision = await _repo.GetReportePagarComision(logId.ToString(), lCicloId);
                var prorrateo = await _repo.GetReporteProrrateo(logId.ToString(), lCicloId);
                var listaProrrateo = prorrateo.Data.ToList();

               List<EmpresaHeaderPagarComision> headerEmpresa = listaProrrateo
                                                                .GroupBy(x => x.EmpresaId)
                                                                .Select(g => new EmpresaHeaderPagarComision
                                                                {
                                                                    EmpresaId = g.Key,
                                                                    SEmpresa = g.First().SEmpresa
                                                                })
                                                                .ToList();



                if(pagarComision.Data == null || pagarComision.Data.Count()<= 0)
                {
                    return Ok(new
                    {
                        status = false,
                        mensaje = "No existe datos para el ciclo seleccionado",
                        data = new
                        {
                            FileName = "",
                            FileBase64 = "",
                            ContentType = "",
                            
                        }
                    });
                }
                var listaPagarComision = pagarComision.Data.ToList();
                var documento = new ReportePagarComision(listaPagarComision, listaProrrateo, headerEmpresa);


                byte[] pdfBytes = documento.GeneratePdf();
                string base64Pdf = Convert.ToBase64String(pdfBytes);

                _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, "PDF generado correctamente.");

                PagarComisionxls _ins = new PagarComisionxls();
                var reponseXLS = await _ins.GetPagarComisionXls(listaPagarComision);

                return Ok(new
                {
                    status = true,
                    mensaje = "Reporte generado correctamente.",
                    data = new
                    {
                        FileName = $"REPORTE DE PAGAR COMISION - {listaPagarComision[0].Ciclo}.pdf",
                        FileNameXls = $"REPORTE DE PAGAR COMISION - {listaPagarComision[0].Ciclo}.xlsx",
                        FileBase64 = base64Pdf,
                        ContentType = "application/pdf",
                        base64Xls = reponseXLS.base64
                    }
                });
            }
            catch (Exception ex)
            {
                _log.Error(
                    logId.ToString(),
                    NOMBREARCHIVO,
                    metodo,
                    "Error al generar reporte de comisiones",
                    ex
                );

                return Ok(new
                {
                    status = false,
                    mensaje = ex.Message,
                    data =""
                });
            }
        }



    }
}
