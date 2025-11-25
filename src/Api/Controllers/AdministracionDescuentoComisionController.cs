using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministracionDescuentoComisionController : ControllerBase
{
    private readonly IAdministracionDescuentoComisionRepository _repository;

    public AdministracionDescuentoComisionController(IAdministracionDescuentoComisionRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAdministracionCObservacionComision(
        [FromHeader(Name = "lContactoId")] int lContactoId,
        [FromHeader(Name = "lCicloId")] int lCicloId,
        [FromHeader(Name = "lSemanaId")] int lSemanaId
    )
    {
        var respose = await _repository.GetComision(lContactoId, lCicloId, lSemanaId);
        var responseDetalle = await _repository.GetDetalleDescuentoCiclo(lCicloId, lContactoId);
        return Ok(new
        {
            status = respose.Success ? true : false,
            mensaje = respose.Mensaje,
            data = new
            {
                comision = respose.Data,
                detalleDescuento = responseDetalle.Data
            }
        });
    }
    [HttpDelete("delete")]
    public async Task<IActionResult> EliminarDescuento(
        [FromHeader(Name = "lDescuentoDetalleId")] int LDescuentoDetalleId,
        [FromHeader(Name = "lContactoId")] int LContactoId,
        [FromHeader(Name = "lCicloId")] int LCicloId,
        [FromHeader(Name = "usuario")] string? Usuario
        //int LContactoId, int LCicloId
    )
    {
        var respose = await _repository.EliminarDescuento(LDescuentoDetalleId, LContactoId, LCicloId, Usuario);
 
        return Ok(new
        {
            status = respose.Success ? true : false,
            mensaje = respose.Mensaje,
            data = ""
        });
    }
    [HttpPost("insert")]
    public async Task<IActionResult> EliminarDescuento( DataDescuento DataDescuento)
    {
        var respose = await _repository.InsertarDescuento(DataDescuento);
 
        return Ok(new
        {
            status = respose.Success ? true : false,
            mensaje = respose.Mensaje,
            data = ""
        });
    }
}
