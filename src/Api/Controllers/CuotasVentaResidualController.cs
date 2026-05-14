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
    private async Task<(decimal Comision, int CuotasComisionables, int CuotasContabilizar)> GetComision(int Tope, int CantidadComisionPagadas, int CuotasPagadasCiclo, int CuotasPagablesCiclo, decimal MesComision)
    {
        
        int diferencia = Math.Max(0, CuotasPagadasCiclo - CuotasPagablesCiclo);// Cuotas que se contabilizan pero NO generan comisión
        int cuotasContabilizar = 0;
        if (CantidadComisionPagadas + CuotasPagablesCiclo > Tope)
        {
            cuotasContabilizar = Tope - CantidadComisionPagadas;
        }
        else
        {
            cuotasContabilizar = CuotasPagadasCiclo;
        }
        int totalContabilizadas = CantidadComisionPagadas + diferencia;// Ya contabilizadas anteriormente + diferencia actual

        int restanteTope = Tope - totalContabilizadas;// Espacio restante hasta el tope

        if (restanteTope <= 0)
        {
            return (0, 0, cuotasContabilizar);
        }
        int cuotasComisionables = Math.Min(restanteTope, CuotasPagablesCiclo);// Cuotas que sí pueden generar comisión        
        decimal comision = cuotasComisionables * MesComision;// Comisión total

        return (comision, cuotasComisionables, cuotasContabilizar);
    }

    [HttpGet("cuotas/venta/residual")]
    public async Task<IActionResult> GetDatos([FromHeader(Name = "Usuario")] string Usuario, [FromHeader(Name = "LCicloId")] int LCicloId ,[FromHeader(Name = "Inicio")]  string Inicio, [FromHeader(Name = "Fin")] string Fin )
    {
        var logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        _log.Info(logTransaccionId, NOMBREARCHIVO, "GetDatos", $"Inicio GetDatos() Usuario: {Usuario}");

        try
        {
            var ResponseCuotasVentaRecidual = await _repo.GetCuotasVentasResidual(logTransaccionId, Usuario, Inicio, Fin, LCicloId);
            var ResponseProductosPagarMensuales = await _repo.GetProductosPagarMensuales(logTransaccionId, Usuario);
            var ResponseComisionVentaPersonas = await _ventaPersonal.GetVentaPersonal(logTransaccionId, Usuario, LCicloId);

             var responseSiguientePaso = await _controlProcesoRepository.GetSiguientePaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, LCicloId);


            var ListadoCuotasVentasResidual = ResponseCuotasVentaRecidual.ListadoCuotasVentasResidual;
            var ListadoProductosPagarMensual = ResponseProductosPagarMensuales.ListadoProductosPagarMensuales;
            var ListadoVentaPersonal = ResponseComisionVentaPersonas.ListadoAdministracionVentaPersonal;

            var asesoresVentaPersonal = ListadoVentaPersonal.Select(x => x.lcontacto_id).ToHashSet();

            var tareas = ListadoCuotasVentasResidual.Join(
                ListadoProductosPagarMensual,
                venta => venta.NroVenta.Trim(),
                producto => producto.Snroventa.Trim(),
                async (venta, producto) =>
                {
                    var resultadoComision = await GetComision(
                        (int)producto.CuotAccPen,
                        (int)producto.CuotPagadas,
                        venta.NroCuota,
                        venta.NroCuotaPagables,
                        Convert.ToDecimal(producto.MensPagar)
                    );

                    return new ListadoComisionCuotaResidual
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
                        NroCuotaPagables = venta.NroCuotaPagables,
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

                        Recibe = asesoresVentaPersonal.Contains((long)producto.LasesorId),

                        TotalComision = resultadoComision.Comision,
                        TotalCuotasComisionables = resultadoComision.CuotasComisionables,
                        TotalCuotasContabilizar = resultadoComision.CuotasContabilizar
                    };
                }
            );

            var ListadoComisionCuotaResidual = (await Task.WhenAll(tareas)).ToList();
 


           foreach (var item in ListadoComisionCuotaResidual)
           {
                item.Recibe = ListadoVentaPersonal.Where(a => a.lcontacto_id == item.LasesorId).ToList().Count > 0 ? true : false;
           }
            ComisionVentaResidualXls xls = new ComisionVentaResidualXls();
            var ResponseXLS = await xls.GetComisionVentaResidualXlS(ListadoComisionCuotaResidual);
            return Ok(new 
            {
                status = ResponseCuotasVentaRecidual.Success,
                mensaje = ResponseCuotasVentaRecidual.Mensaje,
                data = new
                {
                    ListadoComisionCuotaResidual,
                    base64Xls = ResponseXLS.base64,
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
    [HttpPost("cuotas/venta/residual")]
    public async Task<IActionResult> Guardar([FromHeader(Name = "Usuario")] string Usuario, [FromHeader(Name = "LCicloId")] int LCicloId, [FromHeader(Name = "Inicio")] string Inicio, [FromHeader(Name = "Fin")] string Fin)
    {
        string logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        const string nombreMetodo = "Guardar()";

        _log.Info(logTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio {nombreMetodo} Usuario: {Usuario}");

        try
        {
            var responseSiguientePaso = await _controlProcesoRepository.GetSiguientePaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, LCicloId);
            if (PasosDiccionario.COMISION_VENTA_RESIDUAL != responseSiguientePaso.Data.nombre)
            {
                return Ok(new
                {
                    status = false,
                    mensaje = "Esta paso ya se encuentra ejecutado para este ciclo, si quieres volver a a procesar debes reinicar el proceso para el ciclo",
                    data = ""
                });
            }
            var responseCuotasResidual = await _repo.GetCuotasVentasResidual(logTransaccionId, Usuario, Inicio, Fin, LCicloId);

            if (!responseCuotasResidual.Success)
            {
                return Ok(new
                {
                    status = false,
                    mensaje = responseCuotasResidual.Mensaje,
                    data = ""
                });
            }

            var responseProductosPagar = await _repo.GetProductosPagarMensuales(logTransaccionId, Usuario);

            var responseVentaPersonal = await _ventaPersonal.GetVentaPersonal(logTransaccionId, Usuario, LCicloId);

            await _repo.SaveCuotasVentasProductosPagarMensual(logTransaccionId, Usuario, responseCuotasResidual.ListadoCuotasVentasResidual.ToList());


            var listadoCuotasVentasResidual = responseCuotasResidual.ListadoCuotasVentasResidual.ToList();
            var listadoProductosPagarMensual = responseProductosPagar.ListadoProductosPagarMensuales.ToList();
            var listadoVentaPersonal = responseVentaPersonal.ListadoAdministracionVentaPersonal.ToList();

            var asesoresQueReciben = listadoVentaPersonal.Select(x => x.lcontacto_id).ToHashSet();

            var tareasComision = listadoCuotasVentasResidual
                .Join(
                    listadoProductosPagarMensual,
                    venta => venta.NroVenta.Trim(),
                    producto => producto.Snroventa.Trim(),
                    async (venta, producto) =>
                    {
                        var resultadoComision = await GetComision(
                            (int)producto.CuotAccPen,
                            (int)producto.CuotPagadas,
                            venta.NroCuota,
                            venta.NroCuotaPagables,
                            (decimal)producto.MensPagar
                        );

                        return new ListadoComisionCuotaResidual
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

                            Recibe = producto.LasesorId.HasValue 
                                && asesoresQueReciben.Contains(producto.LasesorId.Value),

                            TotalComision = resultadoComision.Comision,
                            TotalCuotasComisionables = resultadoComision.CuotasComisionables,
                            TotalCuotasContabilizar = resultadoComision.CuotasContabilizar
                        };
                    }
                );

            var listadoComisionCuotaResidual = (await Task.WhenAll(tareasComision)).ToList();

            var fechaActual = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            var listaProductoPagarMensualUpdate = listadoComisionCuotaResidual
            .GroupBy(x => new
            {
                x.IdProductoPagar,
                x.LcontratoId,
                x.NroVenta,
                x.Recibe,
                x.CuotPagadas,
                x.CuotAccPen,
                x.LasesorId,
                x.MensPagar,
                x.TotalComision,
                x.TotalCuotasContabilizar
            })
            .Select(g => new ProductosPagarMensualUpdate
            {
                IdProductoPagar = g.Key.IdProductoPagar,
                SNroVenta = g.Key.NroVenta,
                CantidadNroCuotas = g.Sum(x => x.NroCuota),
                ActivoMes = g.Key.Recibe,
                CuotasPagadas = g.Key.CuotPagadas,
                CuotasTotalesAPagar = g.Key.CuotAccPen,
                LContratoId = g.Key.LcontratoId,
                LContactoId = Convert.ToInt32(g.Key.LasesorId),
                MontoPagarMes = Convert.ToDecimal(g.Key.MensPagar),
                TotalComision = g.Key.TotalComision,
                TotalCuotasContabilizar = g.Key.TotalCuotasContabilizar,

                _ProductosDetalleCuotas = g.Select(x => new ProductosDetalleCuotas
                {
                    IdProductoDetalle = 0,
                    UsuarioAdd = Usuario,
                    FechaAdd = fechaActual,
                    FkIdProductoPagar = x.IdProductoPagar,
                    LcontratoId = x.LcontratoId,
                    CantCuotas = x.NroCuota,
                    ExcCuotas = 0,
                    Pagado = "1",
                    Habilitado = g.Key.Recibe ? "1" : "0",
                    LcicloId = LCicloId
                }).ToList()
            })
            .ToList();
            var ResponseControlProductoCuotas = await _repo.SaveControlProductos(logTransaccionId, Usuario, listaProductoPagarMensualUpdate);
            await _controlProcesoRepository.EjecutarPaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, LCicloId,  PasosDiccionario.COMISION_VENTA_RESIDUAL);

            return Ok(new
            {
                status = true,
                mensaje = "Proceso ejecutado correctamente.",
                data = listaProductoPagarMensualUpdate
            });
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId, NOMBREARCHIVO, nombreMetodo, "Error", ex);

            return Ok(new
            {
                status = false,
                mensaje = ex.Message,
                data = ""
            });
        }
    }
    
}