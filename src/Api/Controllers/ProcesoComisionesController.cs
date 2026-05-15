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

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProcesoComisionesController : ControllerBase
{
    private readonly ILogService _log;
    private readonly IVentasCnxRepository _ventasCnxRepository;
    private readonly MiCronJob _miCronJob;
    private readonly IProcesoComisionesRepository _procesoComisionesRepository;
    private readonly IAdministracionCicloRepository _administracionCicloRepository;
    private readonly IAdministracionContratoRepository _administracionContratoRepository;
    private readonly IAdministracionVentaPersonalRepository _administracionVentaPersonalRepository;
    private readonly IAdministracionVentaGrupoRepository _administracionVentaGrupoRepository;
    private readonly IControlProcesoRepository _controlProcesoRepository;
    private readonly IAdministracionSemanaCicloRepository _administracionSemanaCicloRepository;
    private readonly ICuotasVentaResidualRepository _cuotasVentaResidualRepository;
    private readonly string NOMBREARCHIVO = "UtilsController.cs";

    public ProcesoComisionesController(IVentasCnxRepository ventasCnxRepository, ILogService log
        , MiCronJob miCronJob, IProcesoComisionesRepository procesoComisionesRepository
        , IAdministracionCicloRepository administracionCicloRepository, IAdministracionContratoRepository administracionContratoRepository
        , IAdministracionVentaPersonalRepository administracionVentaPersonalRepository, IControlProcesoRepository controlProcesoRepository
        , IAdministracionVentaGrupoRepository administracionVentaGrupoRepository, IAdministracionSemanaCicloRepository administracionSemanaCicloRepository, ICuotasVentaResidualRepository cuotasVentaResidualRepository)
    {
        _ventasCnxRepository = ventasCnxRepository;
        _log = log;
        _procesoComisionesRepository = procesoComisionesRepository;
        _miCronJob = miCronJob;
        _administracionCicloRepository = administracionCicloRepository;
        _administracionContratoRepository = administracionContratoRepository;
        _administracionVentaPersonalRepository = administracionVentaPersonalRepository;
        _controlProcesoRepository = controlProcesoRepository;
        _administracionVentaGrupoRepository = administracionVentaGrupoRepository;
        _administracionSemanaCicloRepository = administracionSemanaCicloRepository;
        _cuotasVentaResidualRepository = cuotasVentaResidualRepository;
    }
    [HttpGet("vta/cnx")]
    public async Task<IActionResult> GetVentaCnx([FromHeader(Name = "lCicloId")] int lCicloId)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string nombreArchivo = "GetVentaCnx()";
        try
        {
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, "Inicio de metodo");
            var responseCiclo = await _administracionCicloRepository.GetCiclos(logTransaccionId.ToString());
            string inicio = "";
            string fin = "";
            if (responseCiclo.Success)
            {
                if(responseCiclo.Ciclos.Count() > 0)
                {
                    AdministracionCicloABM ciclo = responseCiclo.Ciclos.Where(c => c.LCicloId == lCicloId).FirstOrDefault()?? new AdministracionCicloABM();
                    inicio = ciclo?.DtFechaInicio ?? "";
                    fin = ciclo?.DtFechaFin ?? "";
                }else
                {
                    return Ok(new
                    {
                        status = false,
                        mensaje = "No se pudo obtener los ciclos",
                        data = ""
                    });
                }
            }
            else
            {
                return Ok(new
                {
                    status = false,
                    mensaje = "No se pudo obtener los ciclos",
                    data = ""
                });
            }
            var responseVtaCnx = await _ventasCnxRepository.GetVentaCnx(logTransaccionId.ToString(), inicio, fin);
            var responseContratofecha = await _administracionContratoRepository.GetContratoFecha(logTransaccionId.ToString(), inicio, fin);
            _log.Info(logTransaccionId.ToString(), NOMBREARCHIVO, nombreArchivo, $"Fin de metodo.");

            var responseSiguientePaso = await _controlProcesoRepository.GetSiguientePaso(logTransaccionId.ToString(), "system", ProcesosDiccionario.COMISIONES, lCicloId);


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
        

        var t = _miCronJob.ProcesoPrincipal("logTransaccionId.ToString()", null, "JOB", "", "", false, "EJEMPLO", "SYSTEM", 0);
        
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
    public async Task<IActionResult> GetVentaPersonal([FromHeader(Name = "lCicloId")] int lCicloId, [FromHeader(Name = "Inicio")] string Inicio, [FromHeader(Name = "Fin")] string Fin, [FromHeader(Name = "Usuario")] string Usuario)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var responseVentaPersonal = await _procesoComisionesRepository.GetCalculoVentaPersonal(logTransaccionId.ToString(), Usuario, Inicio, Fin, lCicloId);

        var responseSiguientePaso = await _controlProcesoRepository.GetSiguientePaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, lCicloId);

        ComisionVentadirectaXls Comi = new ComisionVentadirectaXls();
        var responseXls = await Comi.GetComicionVentaPersonalXls(responseVentaPersonal.Data.ToList());
        return Ok(new{
            status = responseVentaPersonal.Success,
            mensaje = responseVentaPersonal.Mensaje,
            data = new {
                ventaPersonal = responseVentaPersonal.Data, 
                ventaPersonalCalculado = responseVentaPersonal.ListaVtaPersonal,
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
        string paso = "GUARDAR_VTA";
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        try
        {
            
            var responseSiguientePaso = await _controlProcesoRepository.GetSiguientePaso(logTransaccionId.ToString(), Data.Usuario, ProcesosDiccionario.COMISIONES, Data.LCicloId);
            if (Data.Rezagada)
            {
                if (PasosDiccionario.ADICIONAR_VENTAS != responseSiguientePaso.Data.nombre)
                {
                    return Ok(new
                    {
                        status = false,
                        mensaje = "Esta paso ya se encuentra ejecutado para este ciclo, si quieres volver a a procesar debes reinicar el proceso para el ciclo",
                        data = ""
                    });
                }
            }
            else
            {
                if (PasosDiccionario.OBTENER_VENTAS != responseSiguientePaso.Data.nombre)
                {
                    return Ok(new
                    {
                        status = false,
                        mensaje = "Esta paso ya se encuentra ejecutado para este ciclo, si quieres volver a a procesar debes reinicar el proceso para el ciclo",
                        data = ""
                    });
                }
            }
            

            var responseControlProceso = await _controlProcesoRepository.GetControlProceso(logTransaccionId.ToString(), Data.Usuario, paso, Data.LCicloId );
            if(responseControlProceso.Success)
            {
                if (responseControlProceso.Data.ControlProcesoId <= 0)
                {
                    ItemControlProceso item = new ItemControlProceso{
                        Paso = paso,
                        lciclo_id = Data.LCicloId,
                        Inicio = DateTime.Now,
                    };
                    var responseGuardarControl = await _controlProcesoRepository.GuardarControlProceso(logTransaccionId.ToString(), Data.Usuario, item );
                
                }else
                {
                    if(responseControlProceso.Data.Fin == null)
                    {
                        return Ok(new
                        {
                            status = false,
                            mensaje = "Debe esperar a que termine el proceso.",
                            data = ""
                        });
                    }
                    else
                    {
                        ItemControlProceso item = new ItemControlProceso{
                            Paso = paso,
                            lciclo_id = Data.LCicloId,
                            Inicio = DateTime.Now,
                        };
                        var responseGuardarControl = await _controlProcesoRepository.GuardarControlProceso(logTransaccionId.ToString(), Data.Usuario, item );
                    }
                }
            }
            else
            {
                return Ok(new
                {
                    status = false,
                    mensaje = "Hubo un problema para obtener el control de proceso.",
                    data = ""
                });
            }
            var responseVentaPersonal = await _procesoComisionesRepository.GuardarVtaRezagadas(logTransaccionId.ToString(), Data.NoListaSeleccionado, "");
            var t = _miCronJob.ProcesoPrincipal(logTransaccionId.ToString(), Data.ListaSeleccionado, "", "", "", Data.Rezagada, paso, Data.Usuario, Data.LCicloId);
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

        var responseVentaPersonalComision = await _procesoComisionesRepository.GetCalculoVentaPersonal(logTransaccionId.ToString(), request.Usuario, request.Inicio, request.Fin, request.LCicloId);

        if (responseVentaPersonalComision.Data.Count() != request.ListaComision.Count)
        {
            return Ok(new
            {
                status = false,
                mensaje = "La cantidad de registro enviada no coincide con la cantidad obtenida de DB",
                data = ""
            });
        }
        List<AdministracionVentaPersonal> ListadoVtaPersonal = new List<AdministracionVentaPersonal>();
        foreach (var item in responseVentaPersonalComision.Data)
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
        
        var responseVtaPersonsal = await _administracionVentaPersonalRepository.InsertVentaPersonal(logTransaccionId.ToString(), ListadoVtaPersonal);
        var responseCalculoVentaResidual = await CalculoVentaResidual(request.LCicloId, request.Inicio, request.Fin, request.Usuario);

        await _controlProcesoRepository.EjecutarPaso(logTransaccionId.ToString(), request.Usuario, ProcesosDiccionario.COMISIONES, request.LCicloId,  PasosDiccionario.COMISION_DIRECTA);

        return Ok(new
        {
            status = responseVtaPersonsal.Success,
            mensaje = responseVtaPersonsal.Mensaje,
            data = ""
        });
    }
    private async Task<bool> CalculoVentaResidual( int lCicloId, string inicio, string fin, string usuario)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        try
        {
            var complejosMembresia = new HashSet<int> { 85, 29, 58, 95, 98, 101, 102 };

            var responseContrato = await _administracionContratoRepository.GetAdministracionContratoFechaVentaResidual(logTransaccionId.ToString(), inicio, fin);

            var listado = responseContrato.Data.Select(item =>
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
    public async Task<IActionResult> GetVentaGrupo([FromHeader(Name = "lCicloId")] int lCicloId, [FromHeader(Name = "Inicio")] string Inicio, [FromHeader(Name = "Fin")] string Fin, [FromHeader(Name = "Usuario")] string Usuario)
    {
        long logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var responseVentaGrupo = await _procesoComisionesRepository.GetCalculoVentaGrupo(logTransaccionId.ToString(), Usuario, Inicio, Fin, lCicloId);
        var responseSiguientePaso = await _controlProcesoRepository.GetSiguientePaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, lCicloId);

        ComisionVentaGrupoXls comi = new ComisionVentaGrupoXls();

        var responseXls = await comi.GetComicionVentaGrupoXls(responseVentaGrupo.Data.ToList());
        return Ok(new{
            status = responseVentaGrupo.Success,
            mensaje = responseVentaGrupo.Mensaje,
            data = new
            {
                listado = responseVentaGrupo.Data,
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

        var responseGetVentaGrupo = await _procesoComisionesRepository.GetCalculoVentaGrupo(logTransaccionId.ToString(), request.Usuario, request.Inicio, request.Fin, request.LCicloId);

        if (responseGetVentaGrupo.Data.Count() != request.ListaComision.Count)
        {
            return Ok(new
            {
                status = false,
                mensaje = "La cantidad de registro enviada no coincide con la cantidad obtenida de DB",
                data = ""
            });
        }
        var responseAdministracionSemanaciclo = await _administracionSemanaCicloRepository.GetSemanaCicloId(logTransaccionId.ToString(), request.LCicloId);
        List<ItemVentaGrupo> ListadoVentaGrupo = new List<ItemVentaGrupo>();
        foreach (var item in responseGetVentaGrupo.Data)
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
        
        var responseVtaPersonsal = await _administracionVentaGrupoRepository.InsertAdministracionVentaGrupo(logTransaccionId.ToString(), ListadoVentaGrupo);
        await _controlProcesoRepository.EjecutarPaso(logTransaccionId.ToString(), request.Usuario, ProcesosDiccionario.COMISIONES, request.LCicloId,  PasosDiccionario.COMISION_GRUPO);

        return Ok(new
        {
            status = responseVtaPersonsal.Success,
            mensaje = responseVtaPersonsal.Mensaje,
            data = ""
        });
    }

    public class RequestSaveVtaPersonal
    {
        public List<VentaPersonalComisionDto> ListaComision { get; set; } = new List<VentaPersonalComisionDto>();
        public int LCicloId { get; set; }
        public string Inicio { get; set; } = string.Empty; 
        public string Fin { get; set; } = string.Empty; 
        public string Usuario { get; set; } = string.Empty; 
    }
    public class RequestGuardarVentaGRD
    {
        public List<ItemVentaCnx> ListaSeleccionado { get; set; } = new List<ItemVentaCnx>();
        public List<ItemVentaCnx> NoListaSeleccionado { get; set; } = new List<ItemVentaCnx>();
        public string Usuario { get; set; } = string.Empty;
        public bool Rezagada { get; set; } = false;
        public int LCicloId { get; set; } = 0;
    }
    public class RequestGuardarVentaGrupo
    {
        public List<ItemComisionVentaGrupoDto> ListaComision { get; set; } = new List<ItemComisionVentaGrupoDto>();
        public int LCicloId { get; set; }
        public string Inicio { get; set; } = string.Empty; 
        public string Fin { get; set; } = string.Empty; 
        public string Usuario { get; set; } = string.Empty; 
    }
}
