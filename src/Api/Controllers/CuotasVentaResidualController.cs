using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Microsoft.AspNetCore.DataProtection.Repositories;
using DocumentFormat.OpenXml.Wordprocessing;
using Org.BouncyCastle.Ocsp;
using DocumentFormat.OpenXml.Office2019.Excel.RichData2;
using DocumentFormat.OpenXml.Drawing.Charts;

namespace CleanDapperApi.Api.Controllers;
[ApiController]
[Route("api/[controller]")]
public class CuotasVentaResidualController: ControllerBase
{
    private readonly ICuotasVentaResidualRepository _repo;
    private readonly IAdministracionVentaPersonalRepository _ventaPersonal;
    private readonly ILogService _log;
    private readonly IControlProcesoRepository _controlProcesoRepository;

    private const string NOMBREARCHIVO = "BrConfiguracionController.cs";

    public CuotasVentaResidualController(ICuotasVentaResidualRepository repo, ILogService log, IAdministracionVentaPersonalRepository ventaPersonal, IControlProcesoRepository controlProcesoRepository)
    {
        _repo = repo;
        _log = log;
        _ventaPersonal = ventaPersonal;
        _controlProcesoRepository = controlProcesoRepository;
    }

    [HttpGet("cuotas/venta/residual")]
    public async Task<IActionResult> GetDatos([FromHeader(Name = "Usuario")] string Usuario, [FromHeader(Name = "LCicloId")] int LCicloId ,[FromHeader(Name = "Inicio")]  string Inicio, [FromHeader(Name = "Fin")] string Fin )
    {
        var logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        _log.Info(logTransaccionId, NOMBREARCHIVO, "GetDatos", $"Inicio GetDatos() Usuario: {Usuario}");

        try
        {
            var ResponseCuotasVentaRecidual = await _repo.GetCuotasVentasResidual(logTransaccionId, Usuario, Inicio, Fin);
            var ResponseProductosPagarMensuales = await _repo.GetProductosPagarMensuales(logTransaccionId, Usuario);
            var ResponseComisionVentaPersonas = await _ventaPersonal.GetVentaPersonal(logTransaccionId, Usuario, LCicloId);

             var responseSiguientePaso = await _controlProcesoRepository.GetSiguientePaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, LCicloId);


            var ListadoCuotasVentasResidual = ResponseCuotasVentaRecidual.ListadoCuotasVentasResidual;
            var ListadoProductosPagarMensual = ResponseProductosPagarMensuales.ListadoProductosPagarMensuales;
            var ListadoVentaPersonal = ResponseComisionVentaPersonas.ListadoAdministracionVentaPersonal;

            var ListadoComisionCuotaResidual = ListadoCuotasVentasResidual.Join(
                ListadoProductosPagarMensual,
                venta => venta.NroVenta.Trim(),
                producto => producto.Snroventa.Trim(),
                (venta, producto) => new ListadoComisionCuotaResidual
                {
                    NroVenta = venta.NroVenta,
                    Empresa = venta.Empresa,
                    IdVenta = venta.IdVenta,
                    Fecha = venta.Fecha,
                    IdAlmacen = venta.IdAlmacen,
                    Proyecto = venta.Proyecto,
                    Lotes = venta.Lotes,
                    IdRecibo = venta.IdRecibo,
                    FechaRecibo = venta.FechaRecibo,
                    NroCuota = venta.NroCuota,
                    ImporteTotal = venta.ImporteTotal,
                    IdCliente = venta.IdCliente,
                    NombreCliente = venta.NombreCliente,
                    CiCliente = venta.CiCliente,
                    IdVendedor = venta.IdVendedor,
                    Vendedor = venta.Vendedor,
                    CiVendedor = venta.CiVendedor,
                    Concepto1 = venta.Concepto1,
                    LcicloId = venta.LcicloId,

                    IdProductoPagar = producto.IdProductoPagar,
                    LcontratoId = producto.LcontratoId,
                    LcomplejoId = producto.LcomplejoId,
                    Precio = producto.Precio,
                    CuotaInicial = producto.CuotaInicial,
                    Porcentaje = producto.Porcentaje,
                    Comision = producto.Comision,
                    CuotAccPen = producto.CuotAccPen,
                    CuotPagadas = producto.CuotPagadas,
                    Inicial10 = producto.Inicial10,
                    MontPagar = producto.MontPagar,
                    MensPagar = producto.MensPagar,
                    CiclosHabilitados = producto.CiclosHabilitados,
                    Terminado = producto.Terminado,
                    LasesorId = producto.LasesorId,

                    Recibe = ListadoVentaPersonal
                        .Any(a => a.lcontacto_id == producto.LasesorId)
                }
            ).ToList();
           foreach (var item in ListadoComisionCuotaResidual)
           {
                item.Recibe = ListadoVentaPersonal.Where(a => a.lcontacto_id == item.LasesorId).ToList().Count > 0 ? true : false;
           }
            return Ok(new 
            {
                status = ResponseCuotasVentaRecidual.Success,
                mensaje = ResponseCuotasVentaRecidual.Mensaje,
                data = new
                {
                    ResponseCuotasVentaRecidual.ListadoCuotasVentasResidual.ToList().Count,
                    ListadoComisionCuotaResidual,
                    controlPasos = new {
                                    ejecutado = PasosDiccionario.COMISION_VENTA_RESIDUAL == responseSiguientePaso.Data.nombre ? false : true,
                                    data = responseSiguientePaso.Data
                                }
                }
            });
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId, NOMBREARCHIVO, "Get", "Error", ex);
            return Ok(new 
            {
                status = false,
                mensaje = ex.Message,
                data = ""
            });
        }
    }

}