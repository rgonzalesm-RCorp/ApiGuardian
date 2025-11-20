using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministracionObservacionComisionController : ControllerBase
{
    private readonly IAdministracionObservacionComisionRepository _repository;

    public AdministracionObservacionComisionController(IAdministracionObservacionComisionRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAdministracionCObservacionComision(
        [FromHeader(Name = "page")] int page,
        [FromHeader(Name = "pageSize")] int pageSize,
        [FromHeader(Name = "search")] string? search,
        [FromHeader(Name = "lCicloId")] int lCicloId
    )
    {
        var respose = await _repository.GetAllAdministracionCObservacionComisionAsync(page, pageSize, search, lCicloId);
        return Ok(new
        {
            status = respose.Success ? true : false,
            mensaje = respose.Mensaje,
            data = new
            {
                data = respose.Data,
                total = respose.Total
            }
        });
    }
    [HttpPost("register")]
    public async Task<IActionResult> InsertAdministracionObservacionComision(AdministracionObservacionComision data)
    {
        var responseInsert = await _repository.InsertAdministracionObservacionComision(data);
        return Ok(new
        {
            status = responseInsert.succes ? true : false,
            mensaje = responseInsert.mensaje,
            data = ""
        });
    }
    [HttpPut("update")]
    public async Task<IActionResult> UpdateAdministracionObservacionComision(AdministracionObservacionComision data)
    {
        var responseInsert = await _repository.UpdateAdministracionObservacionComision(data);
        return Ok(new
        {
            status = responseInsert.succes ? true : false,
            mensaje = responseInsert.mensaje,
            data = ""
        });
    }
    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteAdministracionObservacionComision(
        [FromHeader(Name = "lObservacionId")] int lObservacionId,
        [FromHeader(Name = "usuario")] string? usuario)
    {
        var responseInsert = await _repository.DeleteAdministracionObservacionComision(lObservacionId, usuario);
        return Ok(new
        {
            status = responseInsert.succes ? true : false,
            mensaje = responseInsert.mensaje,
            data = ""
        });
    }
}
