using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using Org.BouncyCastle.Asn1.IsisMtt.X509;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministracionContactoController : ControllerBase
{
    private readonly IAdministracionContactoRepository _repository;

    public AdministracionContactoController(IAdministracionContactoRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromHeader(Name = "page")] int page,
        [FromHeader(Name = "pageSize")] int pageSize,
        [FromHeader(Name = "search")] string? search
    )
    {
        var respose = await _repository.GetAllAdministracionContacto(page, pageSize, search);
        return Ok(new
        {
            status = respose.Success ? true : false,
            mensaje = respose.Mensaje,
            data = new
            {
                listaContacto = respose.Data,
                total = respose.Total
            }
        });
    }
    [HttpPost("insert")]
    public async Task<IActionResult> InsertContacto(AdministracionContacto data)
    {
        var respose = await _repository.InsertContacto(data);
        return Ok(new
        {
            status = respose.Success ? true : false,
            mensaje = respose.Mensaje,
            data = ""
        });
    }
    [HttpPut("update")]
    public async Task<IActionResult> UpdateContacto(AdministracionContacto data)
    {
        var respose = await _repository.UpdateContacto(data);
        return Ok(new
        {
            status = respose.Success ? true : false,
            mensaje = respose.Mensaje,
            data = ""
        });
    }
    [HttpDelete("baja")]
    public async Task<IActionResult> BajaContacto(AdministracionContactoBaja data)
    {
        var respose = await _repository.BajaContacto(data);
        return Ok(new
        {
            status = respose.Success ? true : false,
            mensaje = respose.Mensaje,
            data = ""
        });
    }
}
