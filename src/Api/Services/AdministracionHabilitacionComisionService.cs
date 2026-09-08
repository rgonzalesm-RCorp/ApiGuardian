using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Services;

public sealed class AdministracionHabilitacionComisionService
    : IAdministracionHabilitacionComisionService
{
    private readonly IAdministracionHabilitacionComisionRepository _repository;
    private readonly IControlProcesoRepository _controlProcesoRepository;

    public AdministracionHabilitacionComisionService(
        IAdministracionHabilitacionComisionRepository repository,
        IControlProcesoRepository controlProcesoRepository
    )
    {
        _repository = repository;
        _controlProcesoRepository = controlProcesoRepository;
    }

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerAsync(
        string? logId,
        string usuario,
        int cicloId
    )
    {
        var id = ResolveLogId(logId);
        try
        {
            var response = await _repository.GetHabilitaciones(id, usuario, cicloId);
            var siguiente = await _controlProcesoRepository.GetSiguientePaso(
                id,
                usuario,
                ProcesosDiccionario.COMISIONES,
                cicloId
            );
            return (
                response.Success,
                response.Mensaje,
                new
                {
                    habilitaciones = response.Data,
                    controlPasos = new
                    {
                        ejecutado = PasoRegistroHabilitacionesEjecutado(siguiente.Data),
                        data = siguiente.Data,
                    },
                }
            );
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje)> GuardarAsync(
        string? logId,
        string usuario,
        int cicloId,
        List<ItemHabilitacionComision> listado
    )
    {
        var id = ResolveLogId(logId);
        bool pasoIniciado = false;
        try
        {
            var siguiente = await _controlProcesoRepository.GetSiguientePaso(
                id,
                usuario,
                ProcesosDiccionario.COMISIONES,
                cicloId
            );
            if (!PuedeGuardarHabilitaciones(siguiente.Data))
                return (
                    false,
                    "Debe completar los pasos previos antes de registrar habilitaciones."
                );

            bool debeEjecutarPaso = string.Equals(
                siguiente.Data?.nombre,
                PasosDiccionario.REGISTRO_HABILITACIONES,
                StringComparison.OrdinalIgnoreCase
            );
            if (debeEjecutarPaso)
            {
                var inicio = await _controlProcesoRepository.IniciarPaso(
                    id,
                    usuario,
                    ProcesosDiccionario.COMISIONES,
                    cicloId,
                    PasosDiccionario.REGISTRO_HABILITACIONES
                );
                if (!inicio.Success || !(inicio.Data?.status ?? false))
                    return (false, inicio.Data?.mensaje ?? inicio.Mensaje);
                pasoIniciado = true;
            }

            var response = await _repository.SaveHabilitaciones(id, usuario, cicloId, listado);
            if (!response.Success)
            {
                if (pasoIniciado)
                    await _controlProcesoRepository.CancelarPaso(
                        id,
                        usuario,
                        ProcesosDiccionario.COMISIONES,
                        cicloId,
                        PasosDiccionario.REGISTRO_HABILITACIONES
                    );
                return (response.Success, response.Mensaje);
            }

            if (debeEjecutarPaso)
            {
                var fin = await _controlProcesoRepository.FinalizarPaso(
                    id,
                    usuario,
                    ProcesosDiccionario.COMISIONES,
                    cicloId,
                    PasosDiccionario.REGISTRO_HABILITACIONES
                );
                if (!fin.Success || !(fin.Data?.status ?? false))
                    return (
                        false,
                        fin.Data?.mensaje
                            ?? fin.Data?.mensajes
                            ?? "Las habilitaciones fueron guardadas, pero no se pudo actualizar el paso del proceso."
                    );
                pasoIniciado = false;
            }
            return (response.Success, response.Mensaje);
        }
        catch (Exception ex)
        {
            if (pasoIniciado)
                await _controlProcesoRepository.CancelarPaso(
                    id,
                    usuario,
                    ProcesosDiccionario.COMISIONES,
                    cicloId,
                    PasosDiccionario.REGISTRO_HABILITACIONES
                );
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Mensaje)> ActualizarAsync(
        string? logId,
        string usuario,
        ItemHabilitacionComision data
    )
    {
        try
        {
            var r = await _repository.UpdateHabilitacion(ResolveLogId(logId), usuario, data);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Mensaje)> EliminarAsync(
        string? logId,
        string usuario,
        int habilitacionId
    )
    {
        try
        {
            var r = await _repository.DeleteHabilitacion(
                ResolveLogId(logId),
                usuario,
                habilitacionId
            );
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static string ResolveLogId(string? logId) =>
        string.IsNullOrWhiteSpace(logId)
            ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()
            : logId;

    private static bool PuedeGuardarHabilitaciones(ItemControlProcesoNext? siguiente) =>
        siguiente == null
        || string.IsNullOrWhiteSpace(siguiente.nombre)
        || !EsPasoPrevioRegistroHabilitaciones(siguiente.nombre);

    private static bool PasoRegistroHabilitacionesEjecutado(ItemControlProcesoNext? siguiente)
    {
        if (siguiente == null || string.IsNullOrWhiteSpace(siguiente.nombre))
            return true;
        if (
            string.Equals(
                siguiente.nombre,
                PasosDiccionario.REGISTRO_HABILITACIONES,
                StringComparison.OrdinalIgnoreCase
            )
        )
            return false;
        return !EsPasoPrevioRegistroHabilitaciones(siguiente.nombre);
    }

    private static bool EsPasoPrevioRegistroHabilitaciones(string? paso) =>
        !string.IsNullOrWhiteSpace(paso)
        && (
            string.Equals(paso, PasosDiccionario.OBTENER_VENTAS, StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                paso,
                PasosDiccionario.CASOS_OBSERVADOS,
                StringComparison.OrdinalIgnoreCase
            )
            || string.Equals(
                paso,
                PasosDiccionario.ADICIONAR_VENTAS,
                StringComparison.OrdinalIgnoreCase
            )
            || string.Equals(
                paso,
                PasosDiccionario.VENTAS_ESPECIALES,
                StringComparison.OrdinalIgnoreCase
            )
            || string.Equals(
                paso,
                PasosDiccionario.COMISION_DIRECTA,
                StringComparison.OrdinalIgnoreCase
            )
        );
}
