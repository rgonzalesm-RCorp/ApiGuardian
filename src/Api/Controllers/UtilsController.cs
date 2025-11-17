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

    [HttpGet("configuracion")]
    public async Task<IActionResult> GetConfiguraciones([FromHeader(Name = "search")] string? search)
    {
        var cantidadContactos = await _utilsRepository.GetCountContacto(search);
        return Ok(cantidadContactos);
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

    /*[HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Product product)
    {
        await _productRepository.AddAsync(product);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Product product)
    {
        product.Id = id;
        await _productRepository.UpdateAsync(product);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _productRepository.DeleteAsync(id);
        return NoContent();
    }*/
}
