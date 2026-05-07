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
public class RedesController : ControllerBase
{
    private readonly IRedesRepository _repo;
    private readonly ILogService _log;
    private readonly IControlProcesoRepository _controlProcesoRepository;

    private const string NOMBREARCHIVO = "BrConfiguracionController.cs";

    public RedesController(IRedesRepository repo, ILogService log, IControlProcesoRepository controlProcesoRepository)
    {
        _repo = repo;
        _log = log;
        _controlProcesoRepository = controlProcesoRepository;
    }

    [HttpGet("armar/red/comprimida/mes")]
    public async Task<IActionResult> GetDatos([FromHeader(Name = "Usuario")] string Usuario, [FromHeader(Name = "LCicloId")] int LCicloId ,[FromHeader(Name = "Inicio")]  string Inicio, [FromHeader(Name = "Fin")] string Fin )
    {
        var logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        _log.Info(logTransaccionId, NOMBREARCHIVO, "GetDatos", $"Inicio GetDatos() Usuario: {Usuario}");

        try
        {
            var responseSiguientePaso = await _controlProcesoRepository.GetSiguientePaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, LCicloId);
            if (PasosDiccionario.RED_COMPRIMIDA != responseSiguientePaso.Data.nombre)
            {
                return Ok(new
                {
                    status = false,
                    mensaje = "Esta paso ya se encuentra ejecutado para este ciclo, si quieres volver a a procesar debes reinicar el proceso para el ciclo",
                    data = ""
                });
            }
            var ResponseContactoVentaMes = await _repo.GetObetenerContactoVentasMes(logTransaccionId, Usuario, Inicio, Fin);
            List<ItemContactoRed> Lista = new List<ItemContactoRed>();
            foreach (var item in ResponseContactoVentaMes.ListadoContactosActivos)
            {
                int LContactoId = item.LVendedorId;
                int LPatrocinadorId = 0;
                int counter = 1;
                while (counter <= 7)
                {
                    var responsePatrocinador = await _repo.GetObetenerPatrocinador(logTransaccionId, Usuario, LContactoId);
                    LPatrocinadorId = responsePatrocinador.PatrocinadorId;
                    if(LPatrocinadorId <= 0)
                        break;
                    int EstaActivo = ResponseContactoVentaMes.ListadoContactosActivos.Where(x => x.LVendedorId == LPatrocinadorId).Count();
                    if (EstaActivo > 0)
                    {
                        Console.WriteLine($"{item.LContactoId} - PatrocinadorId: {responsePatrocinador.PatrocinadorId}");
                        ItemContactoRed ObjRedComprimidad = new ItemContactoRed
                        {
                            LContactoId = item.LVendedorId,
                            LPatrocinadorId = LPatrocinadorId,
                            Nivel = counter,
                            LCicloId = LCicloId,
                            LContratoId = 0,
                            Usuario = Usuario
                        };
                        Lista.Add(ObjRedComprimidad);

                        counter++;
                    }
                    LContactoId = LPatrocinadorId;
                    
                }
            }
            var ResponseGuardarRedComprimida = await _repo.GuardarRedComprimida(logTransaccionId, Usuario, Lista);
            if (ResponseGuardarRedComprimida.Success)
            {
                await _controlProcesoRepository.EjecutarPaso(
                    logTransaccionId.ToString(),
                    Usuario,
                    ProcesosDiccionario.COMISIONES,
                    LCicloId,
                    responseSiguientePaso.Data.nombre
                );
            }
            return Ok(new 
            {
                status = ResponseContactoVentaMes.Success,
                mensaje = ResponseContactoVentaMes.Mensaje,
                data = new
                {
                    Nivel = ResponseContactoVentaMes.ListadoContactosActivos,
                    RedComprimida = Lista,
                    ResponseGuardarRedComprimida.Success,
                    ResponseGuardarRedComprimida.Mensaje

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
    [HttpGet("armar/red/cuotas")]
    public async Task<IActionResult> GetClientesCuotas([FromHeader(Name = "Usuario")] string Usuario, [FromHeader(Name = "LCicloId")] int LCicloId )
    {
        var logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        _log.Info(logTransaccionId, NOMBREARCHIVO, "GetClientesCuotas", $"Inicio GetClientesCuotas() Usuario: {Usuario}");

        try
        {
            var ResponseClientesCuotas = await _repo.GetObtnerClientesCuotas(logTransaccionId, Usuario);
            List<ItemContactoRed> Lista = new List<ItemContactoRed>();

            List<ItemCuotasRed> ListadoContactosCuotas = ResponseClientesCuotas.ListadoContactosCuotas.ToList();
            List<BrContacto> ListaContacto = ResponseClientesCuotas.ListaContacto.ToList();
            var cachePatrocinadores = new Dictionary<int, int>();

            foreach (var item in ListadoContactosCuotas)
            {
                int contactoId = item.LContactoId;

                for (int nivel = 1; nivel <= 7; nivel++)
                {
                    int patrocinadorId = ListaContacto.Where(x => x.LContactoId == contactoId)
                        .Select(x => x.LPatrocinanteId)
                        .FirstOrDefault();
                    if (patrocinadorId <= 0)
                        break;
                    Lista.Add(new ItemContactoRed
                    {
                        LContactoId = item.LContactoId,
                        LPatrocinadorId = patrocinadorId,
                        Nivel = nivel,
                        LCicloId = LCicloId,
                        LContratoId = 0,
                        Usuario = Usuario
                    });

                    contactoId = patrocinadorId;
                }
            }
            var responseGuardarRedCompletaCuotas = await _repo.GuardarRedCompletaCuotas(logTransaccionId, Usuario, Lista);

            return Ok(new 
            {
                status = ResponseClientesCuotas.Success,
                mensaje = ResponseClientesCuotas.Mensaje,
                data = new
                {
                    ClientesCuotas = ResponseClientesCuotas.ListadoContactosCuotas.Count()
                }
            });
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId, NOMBREARCHIVO, "GetClientesCuotas", "Error", ex);
            return Ok(new 
            {
                status = false,
                mensaje = ex.Message,
                data = ""
            });
        }
    }
}