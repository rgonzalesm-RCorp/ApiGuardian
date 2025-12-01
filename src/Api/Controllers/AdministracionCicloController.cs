using Microsoft.AspNetCore.Mvc;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using Newtonsoft.Json;

namespace CleanDapperApi.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdministracionCicloController : ControllerBase
    {
        private readonly IAdministracionCicloRepository _repo;
        private readonly ILogService _log;
        private readonly string NOMBREARCHIVO = "AdministracionCicloController.cs";

        public AdministracionCicloController(IAdministracionCicloRepository repo, ILogService log)
        {
            _repo = repo;
            _log = log;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            long logId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string metodo = "Get()";

            try
            {
                _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, "Inicio de metodo");

                var resp = await _repo.GetCiclos(logId.ToString());
                _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, "Fin de metodo");

                return Ok(new { status = resp.Success, mensaje = resp.Mensaje, data = resp.Ciclos });

            }
            catch (Exception ex)
            {
                _log.Error(logId.ToString(), NOMBREARCHIVO, metodo, "ERROR", ex);
                return Ok(new { status = false, mensaje = ex.Message });
            }
        }

        [HttpGet("paginacion")]
        public async Task<IActionResult> GetPagination(
            [FromHeader] int page,
            [FromHeader] int pageSize,
            [FromHeader] string? search)
        {
            long logId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string metodo = "GetPagination()";

            try
            {
                _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, "Inicio de metodo");

                var resp = await _repo.GetCiclosPagination(logId.ToString(), page, pageSize, search);
                _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, "Fin de metodo");

                return Ok(new { 
                    status = resp.Success, 
                    mensaje = resp.Mensaje, 
                    data = new{ ciclos = resp.Ciclos, total = resp.Total }
                });
            }
            catch (Exception ex)
            {
                _log.Error(logId.ToString(), NOMBREARCHIVO, metodo, "ERROR", ex);
                return Ok(new { status = false, mensaje = ex.Message });
            }
        }

        [HttpPost("insert")]
        public async Task<IActionResult> Insert(AdministracionCicloABM ciclo)
        {
            long logId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string metodo = "Insert()";

            try
            {
                _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, $"Inicio de metodo [ciclo: {JsonConvert.SerializeObject(ciclo, Formatting.Indented)}]");

                var resp = await _repo.GuardarCiclo(logId.ToString(), ciclo);
                _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, "Fin de metodo");

                return Ok(new { status = resp.Success, mensaje = resp.Mensaje });
                
            }
            catch (Exception ex)
            {
                _log.Error(logId.ToString(), NOMBREARCHIVO, metodo, "ERROR", ex);
                return Ok(new { status = false, mensaje = ex.Message });
            }
        }

        [HttpPut("update")]
        public async Task<IActionResult> Update(AdministracionCicloABM ciclo)
        {
            long logId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string metodo = "Update()";

            try
            {
                _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, $"Inicio de metodo [ciclo: {JsonConvert.SerializeObject(ciclo, Formatting.Indented)}]");

                var resp = await _repo.ModificarCiclo(logId.ToString(), ciclo);
                _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, "Fin de metodo");

                return Ok(new { status = resp.Success, mensaje = resp.Mensaje });
            }
            catch (Exception ex)
            {
                _log.Error(logId.ToString(), NOMBREARCHIVO, metodo, "ERROR", ex);
                return Ok(new { status = false, mensaje = ex.Message });
            }
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> Delete([FromHeader(Name = "LCicloId")] int LCicloId)
        {
            long logId = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string metodo = "Delete()";

            try
            {
                _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, $"Inicio de metodo [lcicloid: {LCicloId}]");

                var resp = await _repo.EliminarCiclo(logId.ToString(), LCicloId);
                _log.Info(logId.ToString(), NOMBREARCHIVO, metodo, "Fin de metodo");

                return Ok(new { status = resp.Success, mensaje = resp.Mensaje });
            }
            catch (Exception ex)
            {
                _log.Error(logId.ToString(), NOMBREARCHIVO, metodo, "ERROR", ex);
                return Ok(new { status = false, mensaje = ex.Message });
            }
        }
    }
}
