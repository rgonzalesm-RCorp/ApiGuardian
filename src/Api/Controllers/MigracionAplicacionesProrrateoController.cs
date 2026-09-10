using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace CleanDapperApi.Api.Controllers;

[ApiController]
[Route("api/MigracionAplicacionesProrrateo")]
public sealed class MigracionAplicacionesProrrateoController : ControllerBase
{
    private readonly IMigracionAplicacionesProrrateoRepository _repository;
    private readonly IControlProcesoRepository _controlProcesoRepository;
    private readonly IAdministracionCicloRepository _cicloRepository;

    public MigracionAplicacionesProrrateoController(
        IMigracionAplicacionesProrrateoRepository repository,
        IControlProcesoRepository controlProcesoRepository,
        IAdministracionCicloRepository cicloRepository
    )
    {
        _repository = repository;
        _controlProcesoRepository = controlProcesoRepository;
        _cicloRepository = cicloRepository;
    }

    [HttpPost("vista-previa")]
    public async Task<IActionResult> VistaPrevia([FromBody] SolicitudMigracionAplicacionesProrrateo solicitud)
    {
        var fechaInicio = await ObtenerFechaInicioCicloAsync(solicitud);
        if (!fechaInicio.Exito)
            return Ok(new { estado = false, mensaje = fechaInicio.Mensaje, datos = new ResultadoMigracionAplicacionesProrrateo() });

        solicitud.FechaInicio = fechaInicio.FechaInicio;
        var r = await _repository.VistaPreviaAsync(solicitud);
        return Ok(new { estado = r.Exito, mensaje = r.Mensaje, datos = r.Datos });
    }

    [HttpPost("ejecutar")]
    public async Task<IActionResult> Ejecutar([FromBody] SolicitudMigracionAplicacionesProrrateo solicitud)
    {
        var fechaInicio = await ObtenerFechaInicioCicloAsync(solicitud);
        if (!fechaInicio.Exito)
            return Ok(new { estado = false, mensaje = fechaInicio.Mensaje, datos = new ResultadoMigracionAplicacionesProrrateo() });

        solicitud.FechaInicio = fechaInicio.FechaInicio;
        var logId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        var siguiente = await _controlProcesoRepository.GetSiguientePaso(
            logId, solicitud.Usuario, ProcesosDiccionario.COMISIONES, solicitud.Ciclo);
        if (!siguiente.Success || siguiente.Data.nombre != PasosDiccionario.MIGRACION_APLICACIONES)
            return Ok(new { estado = false, mensaje = "El paso Migración aplicaciones no está habilitado para este ciclo.", datos = new ResultadoMigracionAplicacionesProrrateo() });

        var inicio = await _controlProcesoRepository.IniciarPaso(
            logId, solicitud.Usuario, ProcesosDiccionario.COMISIONES, solicitud.Ciclo, PasosDiccionario.MIGRACION_APLICACIONES);
        if (!inicio.Success || !(inicio.Data?.status ?? false))
            return Ok(new { estado = false, mensaje = inicio.Data?.mensaje ?? inicio.Mensaje, datos = new ResultadoMigracionAplicacionesProrrateo() });

        var r = await _repository.EjecutarAsync(solicitud);
        if (!r.Exito)
        {
            await _controlProcesoRepository.CancelarPaso(
                logId, solicitud.Usuario, ProcesosDiccionario.COMISIONES, solicitud.Ciclo, PasosDiccionario.MIGRACION_APLICACIONES);
            return Ok(new { estado = r.Exito, mensaje = r.Mensaje, datos = r.Datos });
        }

        var fin = await _controlProcesoRepository.FinalizarPaso(
            logId, solicitud.Usuario, ProcesosDiccionario.COMISIONES, solicitud.Ciclo, PasosDiccionario.MIGRACION_APLICACIONES);
        if (!fin.Success || !(fin.Data?.status ?? false))
            return Ok(new { estado = false, mensaje = fin.Data?.mensaje ?? fin.Mensaje, datos = r.Datos });
        return Ok(new { estado = r.Exito, mensaje = r.Mensaje, datos = r.Datos });
    }

    [HttpPost("ejecutar-sin-control-pasos")]
    public async Task<IActionResult> EjecutarSinControlPasos([FromBody] SolicitudMigracionAplicacionesProrrateo solicitud)
    {
        var fechaInicio = await ObtenerFechaInicioCicloAsync(solicitud);
        if (!fechaInicio.Exito)
            return Ok(new { estado = false, mensaje = fechaInicio.Mensaje, datos = new ResultadoMigracionAplicacionesProrrateo() });

        solicitud.FechaInicio = fechaInicio.FechaInicio;
        var r = await _repository.EjecutarDesdeCeroAsync(solicitud);
        return Ok(new { estado = r.Exito, mensaje = r.Mensaje, datos = r.Datos });
    }

    private async Task<(bool Exito, string Mensaje, string FechaInicio)> ObtenerFechaInicioCicloAsync(
        SolicitudMigracionAplicacionesProrrateo solicitud
    )
    {
        if (solicitud.Ciclo <= 0)
            return (false, "Debe seleccionar un ciclo válido.", string.Empty);

        var ciclo = await _cicloRepository.GetCiclo(
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(), solicitud.Ciclo);
        if (!ciclo.Success || string.IsNullOrWhiteSpace(ciclo.Data.DtFechaInicio))
            return (false, ciclo.Mensaje.Length > 0 ? ciclo.Mensaje : "El ciclo no tiene una fecha de inicio configurada.", string.Empty);

        return (true, string.Empty, ciclo.Data.DtFechaInicio);
    }
}
