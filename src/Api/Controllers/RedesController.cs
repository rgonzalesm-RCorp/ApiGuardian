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
    private const string NOMBREARCHIVO = "BrConfiguracionController.cs";

    public RedesController(IRedesRepository repo, ILogService log)
    {
        _repo = repo;
        _log = log;
    }

    [HttpGet("get/datos")]
    public async Task<IActionResult> GetDatos([FromHeader(Name = "Usuario")] string Usuario, [FromHeader(Name = "LCicloId")] int LCicloId ,[FromHeader(Name = "Inicio")]  string Inicio, [FromHeader(Name = "Fin")] string Fin )
    {
        var logTransaccionId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        _log.Info(logTransaccionId, NOMBREARCHIVO, "GetDatos", $"Inicio GetDatos() Usuario: {Usuario}");

        try
        {
            var ResponseContactoVentaMes = await _repo.GetObetenerContactoVentasMes(logTransaccionId, Usuario, Inicio, Fin);
            List<ItemContactoRedComprimida> Lista = new List<ItemContactoRedComprimida>();
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
                        ItemContactoRedComprimida ObjRedComprimidad = new ItemContactoRedComprimida
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

}