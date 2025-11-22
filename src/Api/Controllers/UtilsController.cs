using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UtilsController : ControllerBase
{
    private readonly IUtilsRepository _utilsRepository;

    public UtilsController(IUtilsRepository utilsRepository)
    {
        _utilsRepository = utilsRepository;
    }
    [HttpGet("administracion/ciclo")]
    public async Task<IActionResult> GetAdministracionCiclo()
    {
        var responseCiclos = await _utilsRepository.GetCiclos();
        return Ok(new
        {
            status = responseCiclos.Success ? true : false,
            mensaje = responseCiclos.Mensaje,
            data = responseCiclos.Ciclos
        });
    }
    [HttpGet("administracion/semana/ciclo")]
    public async Task<IActionResult> GetAdministracionSemanaCiclo([FromHeader(Name = "lCicloId")] int lCicloId)
    {
        var responseCiclosSemana = await _utilsRepository.GetSemanaCiclosAsync(lCicloId);
        return Ok(new
        {
            status = responseCiclosSemana.Success ? true : false,
            mensaje = responseCiclosSemana.Mensaje,
            data = responseCiclosSemana.Semanas
        });
    }

    [HttpGet("administracion/complejo")]
    public async Task<IActionResult> GetAdministracionComplejo()
    {
        var responseCiclos = await _utilsRepository.GetComplejo();
        return Ok(new
        {
            status = responseCiclos.Success ? true : false,
            mensaje = responseCiclos.Mensaje,
            data = new {
                responseCiclos.Complejo
            }
        });
    }
    [HttpGet("administracion/departamento")]
    public async Task<IActionResult> GetDepartamento()
    {
        var responseCiclos = await _utilsRepository.GetDepartamento();
        return Ok(new
        {
            status = responseCiclos.Success ? true : false,
            mensaje = responseCiclos.Mensaje,
            data = new {
                responseCiclos.Departamento
            }
        });
    }
    [HttpGet("administracion/tipo/contrato")]
    public async Task<IActionResult> GetTipoContrato()
    {
        var responseCiclos = await _utilsRepository.GetTipoContrato();
        return Ok(new
        {
            status = responseCiclos.Success ? true : false,
            mensaje = responseCiclos.Mensaje,
            data = new {
                responseCiclos.TipoContrato
            }
        });
    }
    [HttpGet("administracion/estado/contrato")]
    public async Task<IActionResult> GetEstadoContrato()
    {
        var responseCiclos = await _utilsRepository.GetEstadoContrato();
        return Ok(new
        {
            status = responseCiclos.Success ? true : false,
            mensaje = responseCiclos.Mensaje,
            data = new {
                responseCiclos.EstadoContrato
            }
        });
    }
}
