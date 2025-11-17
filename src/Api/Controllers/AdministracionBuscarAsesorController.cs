using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministracionBuscarAsesorController : ControllerBase
{
    private readonly IAdministracionBuscarAsesorRepository _repository;

    public AdministracionBuscarAsesorController(IAdministracionBuscarAsesorRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAdministracionCiclofactura([FromHeader(Name = "lContactoId")] int lContactoId)
    {
        var respose = await _repository.GetAsesoreSieteNiveles(lContactoId);
        return Ok(new
        {
            status = respose.Success ? true : false,
            mensaje = respose.Mensaje,
            data = new
            {
                dataFijos = respose.DataFijos,
                dataActivos = respose.DataActivos
            }
        });
    }

}
