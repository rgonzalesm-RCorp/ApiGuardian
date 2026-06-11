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
public class CasosEspecialesController : ControllerBase
{
    private readonly ILogService _log;
    private readonly ICasosEspecialesRepository _casosEspecialesRepository;
    private readonly IControlProcesoRepository _controlProcesoRepository;
    private readonly IAdministracionContratoRepository _administracionContratoRepository;
    private const string NOMBREARCHIVO = "CasosEspecialesController.cs";

    public CasosEspecialesController(ILogService log, ICasosEspecialesRepository casosEspecialesRepository, IControlProcesoRepository controlProcesoRepository, IAdministracionContratoRepository administracionContratoRepository)
    {
        _log = log;
        _casosEspecialesRepository = casosEspecialesRepository;
        _controlProcesoRepository = controlProcesoRepository;
        _administracionContratoRepository = administracionContratoRepository;
    }

    [HttpGet("casos/especiales")]
    public async Task<IActionResult> Get([FromHeader(Name = "Usuario")] string Usuario, [FromHeader(Name = "LCicloId")] int LCicloId ,[FromHeader(Name = "Inicio")]  string Inicio, [FromHeader(Name = "Fin")] string Fin )
    {
        var logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        _log.Info(logTransaccionId, NOMBREARCHIVO, "GetDatos", $"Inicio GetDatos() Usuario: {Usuario}");

        try
        {
            DateTime ini = DateTime.Now;
            var responseSiguientePaso = await _controlProcesoRepository.GetSiguientePaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, LCicloId);
            /*if (PasosDiccionario.VENTAS_ESPECIALES != responseSiguientePaso.Data.nombre)
            {
                return Ok(new
                {
                    status = false,
                    mensaje = "Esta paso ya se encuentra ejecutado para este ciclo, si quieres volver a a procesar debes reinicar el proceso para el ciclo",
                    data = ""
                });
            }*/
            var ResponseVentasEspeciales = await _casosEspecialesRepository.GetVentasCasosEspeciales(logTransaccionId, Usuario, Inicio, Fin);
            var responseContratofecha = await _administracionContratoRepository.GetContratoFecha(logTransaccionId.ToString(), Inicio, Fin);
            
            var ResponseVentasUpGrade = await _casosEspecialesRepository.GetUpgradeSolicitudPorVentasCnx(logTransaccionId.ToString(), Usuario, string.Join(",", ResponseVentasEspeciales.VentasCasosEspeciales.Where(x => x.TipoComisionable == TiposContratosDiccionario.TiposContratosDiccionarioCnx.UPGRADE).Select(x => x.IdVenta)));
            //ResponseVentasEspeciales.VentasCasosEspeciales = ResponseVentasEspeciales.VentasCasosEspeciales.Where(x => x.TipoComisionable == TiposContratosDiccionario.TiposContratosDiccionarioCnx.RECOMPRA).OrderBy(x => x.IdVenta + "-"+ x.SLote).ToList();
            foreach (var item in ResponseVentasEspeciales.VentasCasosEspeciales)
            {
                if (item.TipoComisionable == TiposContratosDiccionario.TiposContratosDiccionarioCnx.UPGRADE)
                {
                    //item.dporcentajecomision = 67;
                    UpgradeSolicitudDto? upgradeSolicitud = ResponseVentasUpGrade.Lista.Where(x => x.VentaId  == item.IdVenta).FirstOrDefault();
                    if (upgradeSolicitud != null)
                    {
                        decimal DiferenciaUpgrade = item.DPrecio - upgradeSolicitud.MontoHold;
                        decimal TresPorCientoDiferencia = DiferenciaUpgrade * 3 / 100;
                        decimal MontoATomar = item.SCuotaInicial < TresPorCientoDiferencia ? item.SCuotaInicial : TresPorCientoDiferencia;
                        item.SCuotaInicial = MontoATomar;
                    }
                }
                if (item.TipoComisionable == TiposContratosDiccionario.TiposContratosDiccionarioCnx.RECOMPRA)
                {
                    item.SCuotaInicial = (item.DPrecio == item.SCuotaInicial || item.SCuotaInicial == 0) ? (item.DPrecio * 3 / 100) : item.SCuotaInicial;
                }
                if (item.TipoComisionable == TiposContratosDiccionario.TiposContratosDiccionarioCnx.RECUPERACION)
                {
                   
                }
                if (item.TipoComisionable == TiposContratosDiccionario.TiposContratosDiccionarioCnx.CASOSESPECIALES)
                {
                    decimal PorcentajeAumentar = 49.25M;
                    decimal Diferencia = item.DPrecio - item.SCuotaInicial;
                    decimal MontoATomarComoInicial = Diferencia * 3 / 100;
                    item.SCuotaInicial = MontoATomarComoInicial + (MontoATomarComoInicial *  PorcentajeAumentar / 100);
                }
                
            } 
            var xls = new CasosEspecialesXls();
            var responseXls = await xls.GetCasosEspecialesXls(ResponseVentasEspeciales.VentasCasosEspeciales.ToList());

    

            return Ok(new 
            {
                status = ResponseVentasEspeciales.Success,
                mensaje = ResponseVentasEspeciales.Mensaje,
                data = new
                {
                    VentasCasosEspeciales = ResponseVentasEspeciales.VentasCasosEspeciales,
                    VtaGrd = responseContratofecha.Data,
                    base64Xls = responseXls.base64
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
