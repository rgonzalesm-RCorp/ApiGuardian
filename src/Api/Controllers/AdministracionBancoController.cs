using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministracionBancoController : ControllerBase
{
    private readonly IAdministracionBancoRepository _repository;

    public AdministracionBancoController(IAdministracionBancoRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllCuentaBanco()
    {
        var respose = await _repository.GetAllBanco();
        return Ok(new
        {
            status = respose.Success ? true : false,
            mensaje = respose.Mensaje,
            data = new
            {
                listaBanco = respose.Data,
            }
        });
    }
    [HttpGet("moneda")]
    public async Task<IActionResult> GetAllMoneda()
    {
        var respose = await _repository.GetAllMoneda();
        return Ok(new
        {
            status = respose.Success ? true : false,
            mensaje = respose.Mensaje,
            data = new
            {
                listaMoneda = respose.Data,
            }
        });
    }
    [HttpPut("update")]
    public async Task<IActionResult> UpdateBanco(AdministracionBanco data)
    {
        var respose = await _repository.UpdateBanco(data);
        return Ok(new
        {
            status = respose.Success ? true : false,
            mensaje = respose.Mensaje,
            data = ""
        });
    }
    [HttpPost("insert")]
    public async Task<IActionResult> InsertBanco(AdministracionBanco data)
    {
        var respose = await _repository.InsertBanco(data);
        return Ok(new
        {
            status = respose.Success ? true : false,
            mensaje = respose.Mensaje,
            data = ""
        });
    }
    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteBanco(
        [FromHeader(Name = "lBancoId")]  int lBancoId ,
        [FromHeader(Name = "usuario")]  string? usuario)
    {
        var respose = await _repository.DeleteBanco(lBancoId, usuario);
        return Ok(new
        {
            status = respose.Success ? true : false,
            mensaje = respose.Mensaje,
            data = ""
        });
    }
}
