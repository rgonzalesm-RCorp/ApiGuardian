using ApiGuardian.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class RedSerController : ControllerBase
{
    private static readonly JsonSerializerOptions RedSerJsonOptions = new() { MaxDepth = 256 };
    private readonly IRedSerService _service;

    public RedSerController(IRedSerService service) => _service = service;

    [HttpPost("armar")]
    public async Task<IActionResult> Armar(
        [FromHeader(Name = "Usuario")] string usuario,
        [FromHeader(Name = "Inicio")] DateTime inicio,
        [FromHeader(Name = "Fin")] DateTime fin
    )
    {
        var resultado = await _service.ArmarAsync(usuario, inicio, fin);
        return new JsonResult(
            new { status = resultado.Success, mensaje = resultado.Mensaje, data = resultado.Data },
            RedSerJsonOptions
        );
    }

    [HttpGet]
    [HttpGet("obtener")]
    public async Task<IActionResult> Obtener(
        [FromHeader(Name = "Inicio")] DateTime? inicio = null,
        [FromHeader(Name = "Fin")] DateTime? fin = null
    )
    {
        var resultado = await _service.ObtenerAsync(inicio, fin);
        return new JsonResult(
            new { status = resultado.Success, mensaje = resultado.Mensaje, data = resultado.Data },
            RedSerJsonOptions
        );
    }
}
