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
