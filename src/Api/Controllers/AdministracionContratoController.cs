using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministracionContratoController : ControllerBase
{
    private readonly IAdministracionContratoRepository _repository;

    public AdministracionContratoController(IAdministracionContratoRepository repository)
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
        var respose = await _repository.GetAllAdministracionContrato(page, pageSize, search);
        return Ok(new
        {
            status = respose.Success ? true : false,
            mensaje = respose.Mensaje,
            data = new
            {
                listaContrato = respose.Data,
                total = respose.Total
            }
        });
    }
    [HttpPost("insert")]
    public async Task<IActionResult> InsertBanco(AdministracionContrato data)
    {
        var respose = await _repository.InsertContrato(data);
        return Ok(new
        {
            status = respose.Success ? true : false,
            mensaje = respose.Mensaje,
            data = ""
        });
    }
    [HttpPut("update")]
    public async Task<IActionResult> UpdateContrato(AdministracionContrato data)
    {
        var respose = await _repository.UpdateContrato(data);
        return Ok(new
        {
            status = respose.Success ? true : false,
            mensaje = respose.Mensaje,
            data = ""
        });
    }

}
