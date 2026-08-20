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
    private readonly IAdministracionHabilitacionComisionRepository _habilitacionRepository;
    private readonly ILogService _log;
    private readonly IControlProcesoRepository _controlProcesoRepository;
    private readonly HashSet<int> _habilidacionesParaNoComprimirRed;

    private const string NOMBREARCHIVO = "RedesController.cs";

    public RedesController(
        IRedesRepository repo,
        IAdministracionHabilitacionComisionRepository habilitacionRepository,
        ILogService log,
        IControlProcesoRepository controlProcesoRepository,
        IConfiguration configuration
    )
    {
        _repo = repo;
        _habilitacionRepository = habilitacionRepository;
        _log = log;
        _controlProcesoRepository = controlProcesoRepository;
        _habilidacionesParaNoComprimirRed = configuration
            .GetSection("HabilidacionesParaNoComprimirRed")
            .Get<int[]>()?
            .Where(id => id > 0)
            .ToHashSet() ?? new HashSet<int>();
    }

    [HttpGet("armar/red/comprimida/mes")]
    public async Task<IActionResult> GetDatos([FromHeader(Name = "Usuario")] string Usuario, [FromHeader(Name = "LCicloId")] int LCicloId ,[FromHeader(Name = "Inicio")]  string Inicio, [FromHeader(Name = "Fin")] string Fin )
    {
        var logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        _log.Info(logTransaccionId, NOMBREARCHIVO, "GetDatos", $"Inicio GetDatos() Usuario: {Usuario}");
        bool pasoIniciado = false;

        try
        {
            DateTime ini = DateTime.Now;
            /*var responseSiguientePaso = await _controlProcesoRepository.GetSiguientePaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, LCicloId);
            if (PasosDiccionario.RED_COMPRIMIDA != responseSiguientePaso.Data.nombre)
            {
                return Ok(new
                {
                    status = false,
                    mensaje = "Esta paso ya se encuentra ejecutado para este ciclo, si quieres volver a a procesar debes reinicar el proceso para el ciclo",
                    data = ""
                });
            }

            var responseInicioPaso = await _controlProcesoRepository.IniciarPaso(
                logTransaccionId,
                Usuario,
                ProcesosDiccionario.COMISIONES,
                LCicloId,
                PasosDiccionario.RED_COMPRIMIDA
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

            pasoIniciado = true;*/

            var responseHabilitaciones = await _habilitacionRepository.GetHabilitaciones(logTransaccionId, Usuario, LCicloId);
            /*if (!responseHabilitaciones.Success)
            {
                await _controlProcesoRepository.CancelarPaso(
                    logTransaccionId,
                    Usuario,
                    ProcesosDiccionario.COMISIONES,
                    LCicloId,
                    PasosDiccionario.RED_COMPRIMIDA
                );

                pasoIniciado = false;

                return Ok(new
                {
                    status = false,
                    mensaje = responseHabilitaciones.Mensaje,
                    data = ""
                });
            }*/

            var ResponseContactoVentaMes = await _repo.GetObetenerContactoVentasMes(logTransaccionId, Usuario, Inicio, Fin);
            var ResponseContactoAll = await _repo.GetRedCotactoAll(logTransaccionId, Usuario);
            var personasHabilitadas = responseHabilitaciones.Data.ToList();

            var contactosActivosPorId = ResponseContactoVentaMes.ListadoContactosActivos
                .Select(item => new ItemContactoActivo
                {
                    LContactoId = item.LContactoId > 0 ? item.LContactoId : item.LVendedorId,
                    LVendedorId = item.LVendedorId > 0 ? item.LVendedorId : item.LContactoId
                })
                .Where(item => item.LVendedorId > 0)
                .GroupBy(item => item.LVendedorId)
                .ToDictionary(grupo => grupo.Key, grupo => grupo.First());

            foreach (var habilitado in personasHabilitadas)
            {
                if (habilitado.LContactoId <= 0 || contactosActivosPorId.ContainsKey(habilitado.LContactoId))
                {
                    continue;
                }

                contactosActivosPorId[habilitado.LContactoId] = new ItemContactoActivo
                {
                    LContactoId = habilitado.LContactoId,
                    LVendedorId = habilitado.LContactoId
                };
            }

            var redPorContacto = ResponseContactoAll.ListadoContactosCuotas
                .Where(item => item.Hijo > 0)
                .GroupBy(item => item.Hijo)
                .ToDictionary(grupo => grupo.Key, grupo => grupo.First());

            var habilidacionesConfiguradasNoEncontradas = _habilidacionesParaNoComprimirRed
                .Where(id => !redPorContacto.ContainsKey(id))
                .OrderBy(id => id)
                .ToList();

            foreach (var contactoId in _habilidacionesParaNoComprimirRed.Where(redPorContacto.ContainsKey))
            {
                if (contactosActivosPorId.ContainsKey(contactoId))
                {
                    continue;
                }

                contactosActivosPorId[contactoId] = new ItemContactoActivo
                {
                    LContactoId = contactoId,
                    LVendedorId = contactoId
                };
            }

            var contactosActivos = contactosActivosPorId.Values.ToList();
            var contactosActivosIds = contactosActivosPorId.Keys.ToHashSet();

            if (habilidacionesConfiguradasNoEncontradas.Count > 0)
            {
                _log.Info(
                    logTransaccionId,
                    NOMBREARCHIVO,
                    "GetDatos",
                    $"Contactos configurados en HabilidacionesParaNoComprimirRed no encontrados: {string.Join(",", habilidacionesConfiguradasNoEncontradas)}"
                );
            }

            List<ItemContactoRed> Lista = new List<ItemContactoRed>();
            foreach (var item in contactosActivos)
            {
                int LContactoId = item.LVendedorId;
                int LPatrocinadorId = 0;
                int counter = 1;
                var contactosVisitados = new HashSet<int> { LContactoId };
                while (counter <= 7)
                {
                    if (!redPorContacto.TryGetValue(LContactoId, out var responsePatrocinador))
                    {
                        break;
                    }

                    LPatrocinadorId = responsePatrocinador.Padre;
                    if (LPatrocinadorId <= 0 || !contactosVisitados.Add(LPatrocinadorId))
                    {
                        break;
                    }

                    if (contactosActivosIds.Contains(LPatrocinadorId))
                    {
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

            /*if (!ResponseGuardarRedComprimida.Success)
            {
                await _controlProcesoRepository.CancelarPaso(
                    logTransaccionId,
                    Usuario,
                    ProcesosDiccionario.COMISIONES,
                    LCicloId,
                    PasosDiccionario.RED_COMPRIMIDA
                );

                pasoIniciado = false;

                return Ok(new
                {
                    status = false,
                    mensaje = ResponseGuardarRedComprimida.Mensaje,
                    data = ""
                });
            }

            var responseFinPaso = await _controlProcesoRepository.FinalizarPaso(
                logTransaccionId,
                Usuario,
                ProcesosDiccionario.COMISIONES,
                LCicloId,
                PasosDiccionario.RED_COMPRIMIDA
            );

            if (!responseFinPaso.Success || !(responseFinPaso.Data?.status ?? false))
            {
                return Ok(new
                {
                    status = false,
                    mensaje = responseFinPaso.Data?.mensaje ?? responseFinPaso.Mensaje,
                    data = ""
                });
            }*/

            pasoIniciado = false;
            DateTime fin = DateTime.Now;

            return Ok(new 
            {
                status = ResponseGuardarRedComprimida.Success,
                mensaje = ResponseGuardarRedComprimida.Mensaje,
                data = new
                {
                    ini,
                    fin,
                    Nivel = contactosActivos,
                    RedComprimida = Lista,
                    personasHabilitadas,
                    habilidacionesParaNoComprimirRed = _habilidacionesParaNoComprimirRed.OrderBy(id => id),
                    habilidacionesConfiguradasNoEncontradas,
                    ResponseGuardarRedComprimida.Success,
                    ResponseGuardarRedComprimida.Mensaje

                }
            });
        }
        catch (Exception ex)
        {
            if (pasoIniciado)
            {
                await _controlProcesoRepository.CancelarPaso(logTransaccionId, Usuario, ProcesosDiccionario.COMISIONES, LCicloId, PasosDiccionario.RED_COMPRIMIDA);
            }

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
        bool pasoIniciado = false;

        try
        {
            DateTime ini = DateTime.Now;
            var responseSiguientePaso = await _controlProcesoRepository.GetSiguientePaso(logTransaccionId.ToString(), Usuario, ProcesosDiccionario.COMISIONES, LCicloId);
            if (PasosDiccionario.RED_COMPLETA != responseSiguientePaso.Data.nombre)
            {
                return Ok(new
                {
                    status = false,
                    mensaje = "Esta paso ya se encuentra ejecutado para este ciclo, si quieres volver a a procesar debes reinicar el proceso para el ciclo",
                    data = ""
                });
            }

            var responseInicioPaso = await _controlProcesoRepository.IniciarPaso(
                logTransaccionId,
                Usuario,
                ProcesosDiccionario.COMISIONES,
                LCicloId,
                PasosDiccionario.RED_COMPLETA
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

            if (!ResponseSave.Success)
            {
                await _controlProcesoRepository.CancelarPaso(
                    logTransaccionId,
                    Usuario,
                    ProcesosDiccionario.COMISIONES,
                    LCicloId,
                    PasosDiccionario.RED_COMPLETA
                );

                pasoIniciado = false;

                return Ok(new
                {
                    status = false,
                    mensaje = ResponseSave.Mensaje,
                    data = ""
                });
            }

            var responseFinPaso = await _controlProcesoRepository.FinalizarPaso(
                logTransaccionId,
                Usuario,
                ProcesosDiccionario.COMISIONES,
                LCicloId,
                PasosDiccionario.RED_COMPLETA
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
            if (pasoIniciado)
            {
                await _controlProcesoRepository.CancelarPaso(logTransaccionId, Usuario, ProcesosDiccionario.COMISIONES, LCicloId, PasosDiccionario.RED_COMPLETA);
            }

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
