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

    [HttpGet("count")]
    public async Task<IActionResult> GetCountAdministracionObservacionComision(
        [FromHeader(Name = "search")] string? search,
        [FromHeader(Name = "lCicloId")] int lCicloId
    )
    {
        return Ok(await _repository.GetCountObservacionComicion(search, lCicloId));
    }
    [HttpPost("register")]
    public async Task<IActionResult> InsertAdministracionObservacionComision(AdministracionObservacionComision data)
    {
        var responseInsert = await _repository.InsertAdministracionObservacionComision(data);
        return Ok(new
        {
            status = responseInsert.succes ? 0 : 1,
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
            status = responseInsert.succes ? 0 : 1,
            mensaje = responseInsert.mensaje,
            data = ""
        });
    }
    /*
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
