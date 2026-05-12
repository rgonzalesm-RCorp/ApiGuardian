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
            DateTime ini = DateTime.Now;
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
            var ResponseContactoAll = await _repo.GetRedCotactoAll(logTransaccionId, Usuario);

            List<ItemContactoRed> Lista = new List<ItemContactoRed>();
            foreach (var item in ResponseContactoVentaMes.ListadoContactosActivos)
            {
                int LContactoId = item.LVendedorId;
                int LPatrocinadorId = 0;
                int counter = 1;
                while (counter <= 7)
                {
                    //var responsePatrocinador = await _repo.GetObetenerPatrocinador(logTransaccionId, Usuario, LContactoId);
                    var responsePatrocinador = ResponseContactoAll.ListadoContactosCuotas.Where(x => x.Hijo == LContactoId).FirstOrDefault();
                    //LPatrocinadorId = responsePatrocinador.PatrocinadorId;
                    LPatrocinadorId = responsePatrocinador.Padre;
                    if(LPatrocinadorId <= 0)
                        break;
                    int EstaActivo = ResponseContactoVentaMes.ListadoContactosActivos.Where(x => x.LVendedorId == LPatrocinadorId).Count();
                    if (EstaActivo > 0)
                    {
                        Console.WriteLine($"{item.LContactoId} - PatrocinadorId: {responsePatrocinador.Padre}");
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
            DateTime fin = DateTime.Now;

            return Ok(new 
            {
                status = ResponseContactoVentaMes.Success,
                mensaje = ResponseContactoVentaMes.Mensaje,
                data = new
                {
                    ini,
                    fin,
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
            DateTime ini = DateTime.Now;
            var ResponseContactoAll = await _repo.GetRedCotactoAll(logTransaccionId, Usuario);
            var diccionario = ResponseContactoAll.ListadoContactosCuotas.ToDictionary(x => x.Hijo , x => x.Padre);
            List<ItemRedSieteNiveles> Lista = new List<ItemRedSieteNiveles>();

            foreach (var item in ResponseContactoAll.ListadoContactosCuotas)
            {
                ItemRedSieteNiveles Red = new ItemRedSieteNiveles
                {
                    Hijo = item.Hijo
                };
                int actual = item.Hijo;
                for (int nivel = 1; nivel <= 7; nivel++)
                {
                    if (!diccionario.TryGetValue(actual, out int padre))
                        break;
                    switch (nivel)
                    {
                        case 1: Red.PadreN1 = padre; break;
                        case 2: Red.PadreN2 = padre; break;
                        case 3: Red.PadreN3 = padre; break;
                        case 4: Red.PadreN4 = padre; break;
                        case 5: Red.PadreN5 = padre; break;
                        case 6: Red.PadreN6 = padre; break;
                        case 7: Red.PadreN7 = padre; break;
                    }
                    actual = padre;
                    
                }
                Lista.Add(Red);
    
            }
            var ResponseSave = await _repo.GuardarRedContactoTemporal(logTransaccionId, Usuario, Lista);
            DateTime fin = DateTime.Now;
            
            return Ok(new 
            {
                status = ResponseSave.Success,
                mensaje = ResponseSave.Mensaje,
                data = new
                {
                    ClientesCuotas = Lista.Count,
                    ini,
                    fin
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