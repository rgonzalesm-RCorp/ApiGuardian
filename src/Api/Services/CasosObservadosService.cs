using ApiGuardian.Application.Interfaces;

namespace CleanDapperApi.Api.Services;

public sealed class CasosObservadosService : ICasosObservadosService
{
    private readonly ICasosObservadosRepository _repository;
    private readonly IControlProcesoRepository _controlProcesoRepository;

    public CasosObservadosService(
        ICasosObservadosRepository repository,
        IControlProcesoRepository controlProcesoRepository
    )
    {
        _repository = repository;
        _controlProcesoRepository = controlProcesoRepository;
    }

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerAsync(
        string usuario,
        int cicloId,
        DateTime? inicio,
        DateTime? fin
    )
    {
        if (cicloId <= 0 || !inicio.HasValue || !fin.HasValue)
            return (false, "El ciclo, la fecha de inicio y la fecha de fin son obligatorios.", "");
        if (inicio.Value.Date > fin.Value.Date)
            return (false, "La fecha de inicio no puede ser mayor que la fecha de fin.", "");
        var id = Id();
        try
        {
            var r = await _repository.GetCasosObservados(
                id,
                usuario,
                cicloId,
                inicio.Value.Date,
                fin.Value.Date
            );
            var paso = await _controlProcesoRepository.GetSiguientePaso(
                id,
                usuario,
                ProcesosDiccionario.COMISIONES,
                cicloId
            );
            return (
                r.Success,
                r.Mensaje,
                new
                {
                    casosObservados = r.Data,
                    resumen = r.Resumen,
                    controlPasos = new
                    {
                        ejecutado = !string.Equals(
                            PasosDiccionario.CASOS_OBSERVADOS,
                            paso.Data.nombre,
                            StringComparison.OrdinalIgnoreCase
                        ),
                        data = paso.Data,
                    },
                }
            );
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje, object Data)> ProcesarAsync(
        string usuario,
        int cicloId
    )
    {
        var id = Id();
        bool iniciado = false;
        try
        {
            var siguiente = await _controlProcesoRepository.GetSiguientePaso(
                id,
                usuario,
                ProcesosDiccionario.COMISIONES,
                cicloId
            );
            if (
                !string.Equals(
                    PasosDiccionario.CASOS_OBSERVADOS,
                    siguiente.Data.nombre,
                    StringComparison.OrdinalIgnoreCase
                )
            )
                return (
                    false,
                    "El paso Casos Observados no se encuentra habilitado para este ciclo.",
                    ""
                );
            var inicio = await _controlProcesoRepository.IniciarPaso(
                id,
                usuario,
                ProcesosDiccionario.COMISIONES,
                cicloId,
                PasosDiccionario.CASOS_OBSERVADOS
            );
            if (!inicio.Success || !(inicio.Data?.status ?? false))
                return (false, inicio.Data?.mensaje ?? inicio.Mensaje, "");
            iniciado = true;
            var r = await _repository.ProcesarCasosObservados(id, usuario, cicloId);
            if (!r.Success)
                return await CancelarAsync(id, usuario, cicloId, r.Mensaje);
            var fin = await _controlProcesoRepository.FinalizarPaso(
                id,
                usuario,
                ProcesosDiccionario.COMISIONES,
                cicloId,
                PasosDiccionario.CASOS_OBSERVADOS
            );
            if (!fin.Success || !(fin.Data?.status ?? false))
                return await CancelarAsync(id, usuario, cicloId, fin.Data?.mensaje ?? fin.Mensaje);
            iniciado = false;
            return (true, r.Mensaje, "");
        }
        catch (Exception ex)
        {
            if (iniciado)
                await _controlProcesoRepository.CancelarPaso(
                    id,
                    usuario,
                    ProcesosDiccionario.COMISIONES,
                    cicloId,
                    PasosDiccionario.CASOS_OBSERVADOS
                );
            return (false, ex.Message, "");
        }
    }

    private async Task<(bool Success, string Mensaje, object Data)> CancelarAsync(
        string id,
        string usuario,
        int cicloId,
        string mensaje
    )
    {
        await _controlProcesoRepository.CancelarPaso(
            id,
            usuario,
            ProcesosDiccionario.COMISIONES,
            cicloId,
            PasosDiccionario.CASOS_OBSERVADOS
        );
        return (false, mensaje, "");
    }

    private static string Id() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
}
