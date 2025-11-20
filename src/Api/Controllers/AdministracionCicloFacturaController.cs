using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministracionCicloFacturaController : ControllerBase
{
    private readonly IAdministracionCicloFacturaRepository _repository;

    public AdministracionCicloFacturaController(IAdministracionCicloFacturaRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAdministracionCiclofactura(
        [FromHeader(Name = "page")] int page,
        [FromHeader(Name = "pageSize")] int pageSize,
        [FromHeader(Name = "lCicloId")] int lCicloId
    )
    {
        var respose = await _repository.GetAllAdministracionCiclofactura(page,pageSize, lCicloId);
        return Ok(new
        {
            status = respose.Success ? true : false,
            mensaje = respose.Mensaje,
            data = new
            {
                dataList = respose.Data,
                total = respose.Total
            }
        });
    }
    [HttpPost("register")]
    public async Task<IActionResult> InsertAdministracionCiclofactura(AdministracionCicloFactura data)
    {
        var responseInsert = await _repository.InsertAdministracionCiclofactura(data);
        return Ok(new
        {
            status = responseInsert.success ? true : false,
            mensaje = responseInsert.mensaje,
            data = ""
        });
    }
        
    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteAdministracionCiclofactura(
        [FromHeader(Name = "lciclofactura")] int lciclofactura,
        [FromHeader(Name = "usuario")] string? usuario)
    {
        var responseInsert = await _repository.DeleteAdministracionCiclofactura(lciclofactura, usuario);
        return Ok(new
        {
            status = responseInsert.succes ? true : false,
            mensaje = responseInsert.mensaje,
            data = ""
        });
    }
}
