using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministracionCuentaBancoController : ControllerBase
{
    private readonly IAdministracionCuentaBancoRepository _repository;

    public AdministracionCuentaBancoController(IAdministracionCuentaBancoRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("id")]
    public async Task<IActionResult> GetCuentaBanco([FromHeader(Name = "lContactoId")] int lContactoId)
    {
        var respose = await _repository.GetCuentaBanco(lContactoId);
        return Ok(new
        {
            status = respose.Success ? true : false,
            mensaje = respose.Mensaje,
            data = new
            {
                dataCuentaBanco = respose.Data,
            }
        });
    }
    [HttpGet]
    public async Task<IActionResult> GetAllCuentaBanco(
        [FromHeader(Name = "page")] int page,
        [FromHeader(Name = "pageSize")] int pageSize,
        [FromHeader(Name = "search")] string? search)
    {
        var respose = await _repository.GetAllCuentaBanco(page, pageSize, search);
        return Ok(new
        {
            status = respose.Success ? true : false,
            mensaje = respose.Mensaje,
            data = new
            {
                listaCuentaBanco = respose.Data,
                totalRegistro = respose.Total
            }
        });
    }
    [HttpPut("update")]
    public async Task<IActionResult> UpdateCuentaBanco(DataCuentaBanco data)
    {
        var respose = await _repository.UpdateCuentaBanco(data);
        return Ok(new
        {
            status = respose.Success ? true : false,
            mensaje = respose.Mensaje,
            data = ""
        });
    }

}
