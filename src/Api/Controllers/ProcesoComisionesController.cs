using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Microsoft.AspNetCore.DataProtection.Repositories;
using DocumentFormat.OpenXml.Wordprocessing;
using Org.BouncyCastle.Ocsp;
using DocumentFormat.OpenXml.Office2019.Excel.RichData2;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProcesoComisionesController : ControllerBase
{
    private readonly ILogService _log;
    private readonly IVentasCnxRepository _ventasCnxRepository;
    private readonly IProcesoComisionesRepository _procesoComisionesRepository;
    private readonly IAdministracionCicloRepository _administracionCicloRepository;
    private readonly IAdministracionContratoRepository _administracionContratoRepository;
    private readonly IAdministracionVentaPersonalRepository _administracionVentaPersonalRepository;
    private readonly IAdministracionVentaGrupoRepository _administracionVentaGrupoRepository;
    private readonly IAdministracionHabilitacionComisionRepository _habilitacionRepository;
    private readonly IControlProcesoRepository _controlProcesoRepository;
    private readonly IAdministracionSemanaCicloRepository _administracionSemanaCicloRepository;
    private readonly ICuotasVentaResidualRepository _cuotasVentaResidualRepository;
    private readonly ICasosEspecialesRepository _casosEspecialesRepository;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly string NOMBREARCHIVO = "UtilsController.cs";

    public ProcesoComisionesController(IVentasCnxRepository ventasCnxRepository, ILogService log
        , IProcesoComisionesRepository procesoComisionesRepository
        , IAdministracionCicloRepository administracionCicloRepository, IAdministracionContratoRepository administracionContratoRepository
        , IAdministracionVentaPersonalRepository administracionVentaPersonalRepository, IControlProcesoRepository controlProcesoRepository
        , IAdministracionVentaGrupoRepository administracionVentaGrupoRepository, IAdministracionSemanaCicloRepository administracionSemanaCicloRepository
        , ICuotasVentaResidualRepository cuotasVentaResidualRepository, ICasosEspecialesRepository casosEspecialesRepository
        , IAdministracionHabilitacionComisionRepository habilitacionRepository
        , IServiceScopeFactory serviceScopeFactory)
    {
        _ventasCnxRepository = ventasCnxRepository;
        _log = log;
        _procesoComisionesRepository = procesoComisionesRepository;
        _administracionCicloRepository = administracionCicloRepository;
        _administracionContratoRepository = administracionContratoRepository;
        _administracionVentaPersonalRepository = administracionVentaPersonalRepository;
        _controlProcesoRepository = controlProcesoRepository;
        _administracionVentaGrupoRepository = administracionVentaGrupoRepository;
        _habilitacionRepository = habilitacionRepository;
        _administracionSemanaCicloRepository = administracionSemanaCicloRepository;
        _cuotasVentaResidualRepository = cuotasVentaResidualRepository;
        _casosEspecialesRepository = casosEspecialesRepository;
        _serviceScopeFactory = serviceScopeFactory;
    }

    private async Task<(bool Success, string Mensaje, string Inicio, string Fin)> ObtenerFechasCiclo(string logTransaccionId, int LCicloId)
    {
        var responseCiclo = await _administracionCicloRepository.GetCiclo(logTransaccionId, LCicloId);

        if (!responseCiclo.Success || responseCiclo.Data.LCicloId <= 0)
        {
            return (false, $"No se encontró el ciclo {LCicloId}.", string.Empty, string.Empty);
        }

        string inicio = responseCiclo.Data.DtFechaInicio ?? string.Empty;
        string fin = responseCiclo.Data.DtFechaFin ?? string.Empty;

        if (string.IsNullOrWhiteSpace(inicio) || string.IsNullOrWhiteSpace(fin))
        {
            return (false, $"El ciclo {LCicloId} no tiene fechas configuradas.", string.Empty, string.Empty);
        }

        return (true, responseCiclo.Mensaje, inicio, fin);
    }

    [HttpGet("vta/cnx")]
    public async Task<IActionResult> GetVentaCnx([FromHeader(Name = "lCicloId")] int lCicloId)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "GetVentaCnx()";
        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, "Inicio de metodo");
            var cicloFechas = await ObtenerFechasCiclo(logTransaccionId.ToString(), lCicloId);
            if (!cicloFechas.Success)
            {
                return Ok(new
                {
                    status = false,
                    mensaje = cicloFechas.Mensaje,
                    data = ""
                });
            }
            string inicio = cicloFechas.Inicio;
            string fin = cicloFechas.Fin;
            var responseVtaCnx = await _ventasCnxRepository.GetVentaCnx(logTransaccionId.ToString(), inicio, fin);
            var responseContratofecha = await _administracionContratoRepository.GetContratoFecha(logTransaccionId.ToString(), inicio, fin);
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Fin de metodo.");

            var responseSiguientePaso = await _controlProcesoRepository.GetSiguientePaso(logTransaccionId.ToString(), "system", ProcesosDiccionario.COMISIONES, lCicloId);

            foreach(var item in responseVtaCnx.Data)
            {
                decimal InicialCalculado = Math.Ceiling(item.DPrecio *  0.10m);
                if(item.SCuotaInicial > InicialCalculado )
                {
                    item.SCuotaInicial = InicialCalculado;
                }
            }
            return Ok(new
            {
                status = responseVtaCnx.Success ? true : false,
                mensaje = responseVtaCnx.Mensaje,
                data = new {
                    VtaCnx = responseVtaCnx.Data,
                    VtaGrd = responseContratofecha.Data,
                    controlPasos = new {
                            ejecutado = PasosDiccionario.OBTENER_VENTAS == responseSiguientePaso.Data.nombre ? false : true,
                            data = responseSiguientePaso.Data
                        }
                }
            });
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, "Fin de metodo", ex);
            return Ok(new
            {
                status = false,
                mensaje = ex.Message,
                data = ""
            });
        }
        
    }
    [HttpPost("ejemplo")]
    public async Task<IActionResult> Ejecutar()
    {
        

        //var t = _miCronJob.ProcesoPrincipal("logTransaccionId.ToString()", null, "JOB", "", "", false, "EJEMPLO", "SYSTEM", 0);
        
        return Ok(new
        {
            ex = true
        });
    }
    [HttpGet("vta/rezagadas")]
    public async Task<IActionResult> GetVtaRezagadas([FromHeader(Name = "lCicloId")] int lCicloId, [FromHeader(Name = "Usuario")] string Usuario)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        

        var responseVtaRezagadas = await _procesoComisionesRepository.GetVtaRezada(logTransaccionId.ToString(), Usuario);

        var responseSiguientePaso = await _controlProcesoRepository.GetSiguientePaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, lCicloId);
        
        return Ok(new
        {
            status = responseVtaRezagadas.Success,
            mensaje = responseVtaRezagadas.Mensaje,
            data = new
            {
                responseVtaRezagadas.Data, 
                controlPasos = new {
                            ejecutado = PasosDiccionario.ADICIONAR_VENTAS == responseSiguientePaso.Data.nombre ? false : true,
                            data = responseSiguientePaso.Data
                        }
            }
            
        });
    }
    [HttpGet("venta/personal")]
    public async Task<IActionResult> GetVentaPersonal([FromHeader(Name = "lCicloId")] int lCicloId, [FromHeader(Name = "Usuario")] string Usuario)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var cicloFechas = await ObtenerFechasCiclo(logTransaccionId.ToString(), lCicloId);
        if (!cicloFechas.Success)
        {
            return Ok(new
            {
                status = false,
                mensaje = cicloFechas.Mensaje,
                data = ""
            });
        }

        string Inicio = cicloFechas.Inicio;
        string Fin = cicloFechas.Fin;

        var responseVentaPersonal = await _procesoComisionesRepository.GetCalculoVentaPersonal(logTransaccionId.ToString(), Usuario, Inicio, Fin, lCicloId);
        var responseHabilitaciones = await _habilitacionRepository.GetHabilitaciones(logTransaccionId.ToString(), Usuario, lCicloId);

        var responseSiguientePaso = await _controlProcesoRepository.GetSiguientePaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, lCicloId);

        if (!responseHabilitaciones.Success)
        {
            return Ok(new
            {
                status = false,
                mensaje = responseHabilitaciones.Mensaje,
                data = ""
            });
        }

        var contactosBloqueados = HabilitacionComisionHelper.GetContactosBloqueadosParaComision(responseHabilitaciones.Data);
        var listadoVentaPersonal = responseVentaPersonal.Data.Where(item => !contactosBloqueados.Contains(Convert.ToInt32(item.lcontacta_id))).ToList();
        var listadoVentaPersonalCalculado = responseVentaPersonal.ListaVtaPersonal.Where(item => !contactosBloqueados.Contains(Convert.ToInt32(item.lcontacta_id))).ToList();

        List<VentaPersonalComisionDto> CantidadUpgrade = listadoVentaPersonal.Where(x => x.TipoContratoId == TiposContratosDiccionario.TiposContratosDiccionarioGrd.UPGRADE).ToList();
        if (CantidadUpgrade.Count > 0)
        {
            //RECALCULAMOS LAS COMISIONES DE LAS VENTAS UPGRADE CON LOS DATOS DE UPGRADE_SOLICITUD
            foreach (var item in listadoVentaPersonal)
            {
                if (item.TipoContratoId == TiposContratosDiccionario.TiposContratosDiccionarioGrd.UPGRADE 
                || item.TipoContratoId == TiposContratosDiccionario.TiposContratosDiccionarioGrd.RECOMPRA
                || item.TipoContratoId == TiposContratosDiccionario.TiposContratosDiccionarioGrd.RECUPERACION
                ||item.TipoContratoId == TiposContratosDiccionario.TiposContratosDiccionarioGrd.CASOSESPECIALES)
                { 
                    item.dcomision = item.inicial * 67 / 100;
                    item.PorcentajeInicial = item.inicial * 100 / item.dprecio;
                    item.dporcentajecomision = 67;
                }
            }
        }

        ComisionVentadirectaXls Comi = new ComisionVentadirectaXls();
        var responseXls = await Comi.GetComicionVentaPersonalXls(listadoVentaPersonal);
        return Ok(new{
            status = responseVentaPersonal.Success,
            mensaje = responseVentaPersonal.Mensaje,
            data = new {
                ventaPersonal = listadoVentaPersonal,//.Where(x => x.TipoContratoId == TiposContratosDiccionario.TiposContratosDiccionarioGrd.UPGRADE).ToList(),
                ventaPersonalCalculado = listadoVentaPersonalCalculado,
                base64Xls = responseXls.base64,
                controlPasos = new {
                            ejecutado = PasosDiccionario.COMISION_DIRECTA == responseSiguientePaso.Data.nombre ? false : true,
                            data = responseSiguientePaso.Data
                        }
            }
        });
    }

    [HttpPost("save/vta/proceso")]
    public async Task<IActionResult> SaveVenta(RequestGuardarVentaGRD Data)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        try
        {
            var cicloFechas = await ObtenerFechasCiclo(logTransaccionId.ToString(), Data.LCicloId);
            if (!cicloFechas.Success)
            {
                return Ok(new
                {
                    status = false,
                    mensaje = cicloFechas.Mensaje,
                    data = ""
                });
            }

            
            var responseSiguientePaso = await _controlProcesoRepository.GetSiguientePaso(logTransaccionId.ToString(), Data.Usuario, ProcesosDiccionario.COMISIONES, Data.LCicloId);
            var pasoEsperado = (Data.Rezagada, Data.EsEspecial) switch
            {
                (true, _) => PasosDiccionario.ADICIONAR_VENTAS,
                (false, true) => PasosDiccionario.VENTAS_ESPECIALES,
                _ => PasosDiccionario.OBTENER_VENTAS
            };

            if (pasoEsperado != responseSiguientePaso.Data.nombre)
            {
                return Ok(new
                {
                    status = false,
                    mensaje = "Este paso ya se encuentra ejecutado para este ciclo, si quieres volver a procesar debes reiniciar el proceso para el ciclo",
                    data = ""
                });
            }

            var responseInicioPaso = await _controlProcesoRepository.IniciarPaso(
                logTransaccionId.ToString(),
                Data.Usuario,
                ProcesosDiccionario.COMISIONES,
                Data.LCicloId,
                pasoEsperado
            );

            if (!responseInicioPaso.Success || !(responseInicioPaso.Data?.status ?? false))
            {
                return Ok(new
                {
                    status = false,
                    mensaje = responseInicioPaso.Data?.mensaje ?? responseInicioPaso.Mensaje,
                    data = ""
                });
            }
            

            if (Data.Rezagada)
            {
                var responseVentaPersonal = await _procesoComisionesRepository.GuardarVtaRezagadas(logTransaccionId.ToString(), Data.NoListaSeleccionado, "");
            }
            if (Data.EsEspecial)
            {
                var ResponseVentaEspecial = await _casosEspecialesRepository.GetUpgradeSolicitudPorVentasCnx(logTransaccionId.ToString(), Data.Usuario, string.Join(",", Data.ListaSeleccionado.Where(x => x.TipoComisionable == TiposContratosDiccionario.TiposContratosDiccionarioCnx.UPGRADE).Select(x => x.IdVenta)));
                var responseSaveVentaEspecial = await _casosEspecialesRepository.SaveUpgradeSolicitud(logTransaccionId.ToString(), Data.Usuario, Data.LCicloId, ResponseVentaEspecial.Lista.ToList());
            }
            RequestProcesoPrincipal dat = new RequestProcesoPrincipal
            {
                Tipo = "API",
                Rezagada = Data.Rezagada,
                Usuario = Data.Usuario,
                LCicloId = Data.LCicloId,
                Paso = Data.Rezagada ? PasosDiccionario.ADICIONAR_VENTAS : Data.EsEspecial? PasosDiccionario.VENTAS_ESPECIALES : PasosDiccionario.OBTENER_VENTAS
            };

            var requestProceso = dat;
            var ventasSeleccionadas = Data.ListaSeleccionado?.ToList() ?? new List<ItemVentaCnx>();

            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var cronJob = scope.ServiceProvider.GetRequiredService<MiCronJob>();
                    await cronJob.ProcesoPrincipal(logTransaccionId.ToString(), requestProceso, ventasSeleccionadas);
                }
                catch (Exception ex)
                {
                    _log.Error(logTransaccionId.ToString(), NOMBREARCHIVO, "SaveVenta()", "Error en procesamiento en segundo plano", ex);
                }
            });

            return Ok(new
            {
                status = true,
                mensaje = "Se esta registrando las ventas en segundo plano, por favor espere que termine para realizar el calculo de comisiones",
                data = ""
            });
        }
        catch (System.Exception)
        {
            return Ok(new
            {
                status = false,
                mensaje = "Hubo un problema con el registro de ventas, por favor contactese con el administracion del sistema.",
                data = ""
            });
        }
        
    }
    
    [HttpPost("save/vta/personal")]
    public async Task<IActionResult> SaveVtaPersonal(RequestSaveVtaPersonal request)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        bool pasoIniciado = false;

        try
        {
            var cicloFechas = await ObtenerFechasCiclo(logTransaccionId.ToString(), request.LCicloId);
            if (!cicloFechas.Success)
            {
                return Ok(new
                {
                    status = false,
                    mensaje = cicloFechas.Mensaje,
                    data = ""
                });
            }

            string Inicio = cicloFechas.Inicio;
            string Fin = cicloFechas.Fin;
            var responseSiguientePaso = await _controlProcesoRepository.GetSiguientePaso(logTransaccionId.ToString(), request.Usuario, ProcesosDiccionario.COMISIONES, request.LCicloId);
            if (PasosDiccionario.COMISION_DIRECTA != responseSiguientePaso.Data.nombre)
            {
                return Ok(new
                {
                    status = false,
                    mensaje = "Esta paso ya se encuentra ejecutado para este ciclo, si quieres volver a a procesar debes reinicar el proceso para el ciclo",
                    data = ""
                });
            }

            var responseInicioPaso = await _controlProcesoRepository.IniciarPaso(
                logTransaccionId.ToString(),
                request.Usuario,
                ProcesosDiccionario.COMISIONES,
                request.LCicloId,
                PasosDiccionario.COMISION_DIRECTA
            );

            if (!responseInicioPaso.Success || !(responseInicioPaso.Data?.status ?? false))
            {
                return Ok(new
                {
                    status = false,
                    mensaje = responseInicioPaso.Data?.mensaje ?? responseInicioPaso.Mensaje,
                    data = ""
                });
            }

            pasoIniciado = true;

            var responseVentaPersonalComision = await _procesoComisionesRepository.GetCalculoVentaPersonal(logTransaccionId.ToString(), request.Usuario, Inicio, Fin, request.LCicloId);
            var responseHabilitaciones = await _habilitacionRepository.GetHabilitaciones(logTransaccionId.ToString(), request.Usuario, request.LCicloId);

            if (!responseHabilitaciones.Success)
            {
                await _controlProcesoRepository.CancelarPaso(logTransaccionId.ToString(), request.Usuario, ProcesosDiccionario.COMISIONES, request.LCicloId, PasosDiccionario.COMISION_DIRECTA);
                pasoIniciado = false;

                return Ok(new
                {
                    status = false,
                    mensaje = responseHabilitaciones.Mensaje,
                    data = ""
                });
            }

            var contactosBloqueados = HabilitacionComisionHelper.GetContactosBloqueadosParaComision(responseHabilitaciones.Data);
            var listadoVentaPersonalComision = responseVentaPersonalComision.Data.Where(item => !contactosBloqueados.Contains(Convert.ToInt32(item.lcontacta_id))).ToList();

            List<VentaPersonalComisionDto> CantidadUpgrade = listadoVentaPersonalComision.Where(x => x.TipoContratoId == TiposContratosDiccionario.TiposContratosDiccionarioGrd.UPGRADE).ToList();
            if (CantidadUpgrade.Count > 0)
            {

                //RECALCULAMOS LAS COMISIONES DE LAS VENTAS UPGRADE CON LOS DATOS DE UPGRADE_SOLICITUD
                foreach (var item in listadoVentaPersonalComision)
                {
                    if (item.TipoContratoId == TiposContratosDiccionario.TiposContratosDiccionarioGrd.UPGRADE 
                    || item.TipoContratoId == TiposContratosDiccionario.TiposContratosDiccionarioGrd.RECOMPRA
                    || item.TipoContratoId == TiposContratosDiccionario.TiposContratosDiccionarioGrd.RECUPERACION
                    ||item.TipoContratoId == TiposContratosDiccionario.TiposContratosDiccionarioGrd.CASOSESPECIALES)
                    { 
                        item.dcomision = item.inicial * 67 / 100;
                        item.PorcentajeInicial = item.inicial * 100 / item.dprecio;
                        item.dporcentajecomision = 67;
                    }
                }
            }

            if (listadoVentaPersonalComision.Count != request.ListaComision.Count)
            {
                await _controlProcesoRepository.CancelarPaso(logTransaccionId.ToString(), request.Usuario, ProcesosDiccionario.COMISIONES, request.LCicloId, PasosDiccionario.COMISION_DIRECTA);
                pasoIniciado = false;

                return Ok(new
                {
                    status = false,
                    mensaje = "La cantidad de registro enviada no coincide con la cantidad obtenida de DB",
                    data = ""
                });
            }
            List<AdministracionVentaPersonal> ListadoVtaPersonal = new List<AdministracionVentaPersonal>();
            foreach (var item in listadoVentaPersonalComision)
            {
                AdministracionVentaPersonal row = new AdministracionVentaPersonal
                {
                    susuarioadd = request.Usuario,
                    susuariomod = request.Usuario,
                    lciclo_id = request.LCicloId,
                    lcontacto_id = item.lcontacta_id,
                    dpreciolote = item.inicial,
                    dporcentajecomision = item.dporcentajecomision,
                    dcomision = item.dcomision,
                    lcontrato_id = item.lcontrato_id,
                    lnrosemana = 1,
                    lsemana_id = 124,
                };
                ListadoVtaPersonal.Add(row);
            }

            (bool Success, string Mensaje) responseVtaPersonsal = ListadoVtaPersonal.Count > 0
                ? await _administracionVentaPersonalRepository.InsertVentaPersonal(logTransaccionId.ToString(), ListadoVtaPersonal)
                : (true, "No existen ventas personales que generen comisión para guardar.");
            var responseCalculoVentaResidual = await CalculoVentaResidual(request.LCicloId, Inicio, Fin, request.Usuario);

            if (!responseVtaPersonsal.Success || !responseCalculoVentaResidual)
            {
                await _controlProcesoRepository.CancelarPaso(logTransaccionId.ToString(), request.Usuario, ProcesosDiccionario.COMISIONES, request.LCicloId, PasosDiccionario.COMISION_DIRECTA);
                pasoIniciado = false;

                return Ok(new
                {
                    status = false,
                    mensaje = responseVtaPersonsal.Success ? "No se pudo calcular la venta residual asociada al paso." : responseVtaPersonsal.Mensaje,
                    data = ""
                });
            }

            var responseFinPaso = await _controlProcesoRepository.FinalizarPaso(
                logTransaccionId.ToString(),
                request.Usuario,
                ProcesosDiccionario.COMISIONES,
                request.LCicloId,
                PasosDiccionario.COMISION_DIRECTA
            );

            if (!responseFinPaso.Success || !(responseFinPaso.Data?.status ?? false))
            {
                return Ok(new
                {
                    status = false,
                    mensaje = responseFinPaso.Data?.mensaje ?? responseFinPaso.Mensaje,
                    data = ""
                });
            }

            pasoIniciado = false;

            return Ok(new
            {
                status = responseVtaPersonsal.Success,
                mensaje = responseVtaPersonsal.Mensaje,
                data = ""
            });
        }
        catch (Exception ex)
        {
            if (pasoIniciado)
            {
                await _controlProcesoRepository.CancelarPaso(logTransaccionId.ToString(), request.Usuario, ProcesosDiccionario.COMISIONES, request.LCicloId, PasosDiccionario.COMISION_DIRECTA);
            }

            return Ok(new
            {
                status = false,
                mensaje = ex.Message,
                data = ""
            });
        }
    }
    private async Task<bool> CalculoVentaResidual( int lCicloId, string inicio, string fin, string usuario)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        try
        {
            var complejosMembresia = new HashSet<int> { 85, 29, 58, 95, 98, 101, 102 };

            var responseContrato = await _administracionContratoRepository.GetAdministracionContratoFechaVentaResidual(logTransaccionId.ToString(), inicio, fin);
            var responseHabilitaciones = await _habilitacionRepository.GetHabilitaciones(logTransaccionId.ToString(), usuario, lCicloId);

            if (!responseHabilitaciones.Success)
            {
                return false;
            }

            var contactosBloqueados = HabilitacionComisionHelper.GetContactosBloqueadosParaComision(responseHabilitaciones.Data);

            // Las ventas especiales no deben generar base para el bono residual y
            // una habilitación con GeneraComisiones = false no debe generar bonos.
            var listado = responseContrato.Data
                .Where(item =>
                    !HabilitacionComisionHelper.TiposContratoEspeciales.Contains(item.LTipoContratoId)
                    && !contactosBloqueados.Contains(item.LAsesorId))
                .Select(item =>
            {
                bool esMembresia = complejosMembresia.Contains(item.LComplejoId);

                var calculo = GetTotalVentaResidual(item.Precio, item.CuotaInicial, esMembresia);

                return new ProductosPagarMensuales
                {
                    IdProductoPagar = 0,
                    LcontratoId = item.LcontratoId,
                    LcomplejoId = item.LComplejoId,
                    Snroventa = item.NroVenta ?? "",
                    LcontactoId = item.LcontratoId,
                    LasesorId = item.LAsesorId,
                    Dtfecha = item.Fecha,
                    Precio = item.Precio,
                    CuotaInicial = item.CuotaInicial,
                    Porcentaje = item.PorcentajeInicial,
                    Comision = calculo.ComisionDirecta,
                    CuotAccPen = calculo.Cuotas,
                    CuotPagadas = 0,
                    Inicial10 = calculo.InicialAl10,
                    MontPagar = calculo.DiferenciaComision,
                    MensPagar = calculo.ComisionMensual,
                    Terminado = 0
                };
            }).ToList();
            listado = listado.Where(x => x.MontPagar > 0 &&  x.Porcentaje < 100).ToList();
            var responser = await _cuotasVentaResidualRepository.InsertProductosPagarMensuales(logTransaccionId.ToString(), usuario, listado);
            return responser.Success;
        }
        catch (Exception ex)
        {
            return false;
        }
    }
    private class ObjTotalVentaResidual
    {
        public decimal ComisionMensual { get; set; }
        public decimal DiferenciaComision { get; set; }
        public decimal NuevaComision { get; set; }
        public decimal InicialAl10 { get; set; }
        public int Cuotas { get; set; }
        public decimal ComisionDirecta { get; set; }
    }
    private static ObjTotalVentaResidual GetTotalVentaResidual(decimal vendidoEn, decimal cuotaInicial, bool esMembresia)
    {
        decimal inicialAl10 = vendidoEn * 10 / 100;

        decimal comisionDirecta;
        decimal nuevaComision;
        int cantidadMeses;

        if (esMembresia)
        {
            comisionDirecta = cuotaInicial * 40 / 100;
            nuevaComision = inicialAl10;
            cantidadMeses = 12;
        }
        else
        {
            comisionDirecta = cuotaInicial * 30 / 100;
            nuevaComision = inicialAl10 * 30 / 100;
            cantidadMeses = 6;
        }

        decimal diferenciaComision = nuevaComision - comisionDirecta;
        decimal comisionMensual = diferenciaComision / cantidadMeses;

        return new ObjTotalVentaResidual
        {
            ComisionMensual = comisionMensual,
            DiferenciaComision = diferenciaComision,
            NuevaComision = nuevaComision,
            InicialAl10 = inicialAl10,
            Cuotas = cantidadMeses,
            ComisionDirecta = comisionDirecta
        };
    }
    
    [HttpGet("venta/grupo")]
    public async Task<IActionResult> GetVentaGrupo([FromHeader(Name = "lCicloId")] int lCicloId, [FromHeader(Name = "Usuario")] string Usuario)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var cicloFechas = await ObtenerFechasCiclo(logTransaccionId.ToString(), lCicloId);
        if (!cicloFechas.Success)
        {
            return Ok(new
            {
                status = false,
                mensaje = cicloFechas.Mensaje,
                data = ""
            });
        }

        string Inicio = cicloFechas.Inicio;
        string Fin = cicloFechas.Fin;

        var responseVentaGrupo = await _procesoComisionesRepository.GetCalculoVentaGrupo(logTransaccionId.ToString(), Usuario, Inicio, Fin, lCicloId);
        var responseHabilitaciones = await _habilitacionRepository.GetHabilitaciones(logTransaccionId.ToString(), Usuario, lCicloId);
        var responseSiguientePaso = await _controlProcesoRepository.GetSiguientePaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, lCicloId);

        if (!responseHabilitaciones.Success)
        {
            return Ok(new
            {
                status = false,
                mensaje = responseHabilitaciones.Mensaje,
                data = ""
            });
        }

        var personasHabilitadas = responseHabilitaciones.Data.ToList();
        var contactosBloqueados = HabilitacionComisionHelper.GetContactosBloqueadosParaComision(personasHabilitadas);
        var habilitadosSet = HabilitacionComisionHelper.GetContactosHabilitadosQueGeneranComision(personasHabilitadas);
        var listadoVentaGrupo = responseVentaGrupo.Data
            .Where(item => !contactosBloqueados.Contains(item.LGanadorId))
            .ToList();

        foreach (var item in listadoVentaGrupo)
        {
            item.EsHabilitado = habilitadosSet.Contains(item.LGanadorId);
        }

        ComisionVentaGrupoXls comi = new ComisionVentaGrupoXls();

        var responseXls = await comi.GetComicionVentaGrupoXls(listadoVentaGrupo);
        return Ok(new{
            status = responseVentaGrupo.Success,
            mensaje = responseVentaGrupo.Mensaje,
            data = new
            {
                listado = listadoVentaGrupo,
                personasHabilitadas,
                base64Xls = responseXls.base64,
                controlPasos = new {
                                    ejecutado = PasosDiccionario.COMISION_GRUPO == responseSiguientePaso.Data.nombre ? false : true,
                                    data = responseSiguientePaso.Data
                                }
            }
        });
    }

    [HttpPost("save/vta/grupo")]
    public async Task<IActionResult> SaveVtaGrupo(RequestGuardarVentaGrupo request)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        bool pasoIniciado = false;

        try
        {
            var cicloFechas = await ObtenerFechasCiclo(logTransaccionId.ToString(), request.LCicloId);
            if (!cicloFechas.Success)
            {
                return Ok(new
                {
                    status = false,
                    mensaje = cicloFechas.Mensaje,
                    data = ""
                });
            }

            string Inicio = cicloFechas.Inicio;
            string Fin = cicloFechas.Fin;
            var responseSiguientePaso = await _controlProcesoRepository.GetSiguientePaso(logTransaccionId.ToString(), request.Usuario, ProcesosDiccionario.COMISIONES, request.LCicloId);
            if (PasosDiccionario.COMISION_GRUPO != responseSiguientePaso.Data.nombre)
            {
                return Ok(new
                {
                    status = false,
                    mensaje = "Esta paso ya se encuentra ejecutado para este ciclo, si quieres volver a a procesar debes reinicar el proceso para el ciclo",
                    data = ""
                });
            }

            var responseInicioPaso = await _controlProcesoRepository.IniciarPaso(
                logTransaccionId.ToString(),
                request.Usuario,
                ProcesosDiccionario.COMISIONES,
                request.LCicloId,
                PasosDiccionario.COMISION_GRUPO
            );

            if (!responseInicioPaso.Success || !(responseInicioPaso.Data?.status ?? false))
            {
                return Ok(new
                {
                    status = false,
                    mensaje = responseInicioPaso.Data?.mensaje ?? responseInicioPaso.Mensaje,
                    data = ""
                });
            }

            pasoIniciado = true;

            var responseGetVentaGrupo = await _procesoComisionesRepository.GetCalculoVentaGrupo(logTransaccionId.ToString(), request.Usuario, Inicio, Fin, request.LCicloId);
            var responseHabilitaciones = await _habilitacionRepository.GetHabilitaciones(logTransaccionId.ToString(), request.Usuario, request.LCicloId);

            if (!responseHabilitaciones.Success)
            {
                await _controlProcesoRepository.CancelarPaso(logTransaccionId.ToString(), request.Usuario, ProcesosDiccionario.COMISIONES, request.LCicloId, PasosDiccionario.COMISION_GRUPO);
                pasoIniciado = false;

                return Ok(new
                {
                    status = false,
                    mensaje = responseHabilitaciones.Mensaje,
                    data = ""
                });
            }

            var contactosBloqueados = HabilitacionComisionHelper.GetContactosBloqueadosParaComision(responseHabilitaciones.Data);
            var listadoVentaGrupoCalculado = responseGetVentaGrupo.Data
                .Where(item => !contactosBloqueados.Contains(item.LGanadorId))
                .ToList();

            if (listadoVentaGrupoCalculado.Count != request.ListaComision.Count)
            {
                await _controlProcesoRepository.CancelarPaso(logTransaccionId.ToString(), request.Usuario, ProcesosDiccionario.COMISIONES, request.LCicloId, PasosDiccionario.COMISION_GRUPO);
                pasoIniciado = false;

                return Ok(new
                {
                    status = false,
                    mensaje = "La cantidad de registro enviada no coincide con la cantidad obtenida de DB",
                    data = ""
                });
            }
            var responseAdministracionSemanaciclo = await _administracionSemanaCicloRepository.GetSemanaCicloId(logTransaccionId.ToString(), request.LCicloId);
            List<ItemVentaGrupo> ListadoVentaGrupo = new List<ItemVentaGrupo>();
            foreach (var item in listadoVentaGrupoCalculado)
            {
                ItemVentaGrupo row = new ItemVentaGrupo
                {
                    usuario = request.Usuario,
                    lciclo_id = request.LCicloId,
                    lcontacto_id = item.LGanadorId,
                    lgeneracion = item.Nivel,
                    lasesor_id = item.LVendedorId,
                    dporcentajecomision = item.Porcentaje,
                    dcomision = item.Comision,
                    dventapersonal = item.DCuotaInicial,
                    dventapersonalinicial = item.DCuotaInicial,
                    lcontrato_id = item.LContratoId,
                    lnrosemana = responseAdministracionSemanaciclo.Semanas.ToList()[0].LNroSemana,
                    lsemana_id = responseAdministracionSemanaciclo.Semanas.ToList()[0].LSemanaId,
                };
                ListadoVentaGrupo.Add(row);
            }

            (bool Success, string Mensaje) responseVtaPersonsal = ListadoVentaGrupo.Count > 0
                ? await _administracionVentaGrupoRepository.InsertAdministracionVentaGrupo(logTransaccionId.ToString(), ListadoVentaGrupo)
                : (true, "No existen comisiones de grupo habilitadas para guardar.");

            if (!responseVtaPersonsal.Success)
            {
                await _controlProcesoRepository.CancelarPaso(logTransaccionId.ToString(), request.Usuario, ProcesosDiccionario.COMISIONES, request.LCicloId, PasosDiccionario.COMISION_GRUPO);
                pasoIniciado = false;

                return Ok(new
                {
                    status = false,
                    mensaje = responseVtaPersonsal.Mensaje,
                    data = ""
                });
            }

            var responseFinPaso = await _controlProcesoRepository.FinalizarPaso(
                logTransaccionId.ToString(),
                request.Usuario,
                ProcesosDiccionario.COMISIONES,
                request.LCicloId,
                PasosDiccionario.COMISION_GRUPO
            );

            if (!responseFinPaso.Success || !(responseFinPaso.Data?.status ?? false))
            {
                return Ok(new
                {
                    status = false,
                    mensaje = responseFinPaso.Data?.mensaje ?? responseFinPaso.Mensaje,
                    data = ""
                });
            }

            pasoIniciado = false;

            return Ok(new
            {
                status = responseVtaPersonsal.Success,
                mensaje = responseVtaPersonsal.Mensaje,
                data = ""
            });
        }
        catch (Exception ex)
        {
            if (pasoIniciado)
            {
                await _controlProcesoRepository.CancelarPaso(logTransaccionId.ToString(), request.Usuario, ProcesosDiccionario.COMISIONES, request.LCicloId, PasosDiccionario.COMISION_GRUPO);
            }

            return Ok(new
            {
                status = false,
                mensaje = ex.Message,
                data = ""
            });
        }
    }


}
