using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Services;

public sealed class RedesService : IRedesService
{
    private readonly IRedesRepository _repository;
    private readonly IAdministracionHabilitacionComisionRepository _habilitacionRepository;
    private readonly IControlProcesoRepository _controlProcesoRepository;
    private readonly HashSet<int> _noComprimir;

    public RedesService(
        IRedesRepository repository,
        IAdministracionHabilitacionComisionRepository habilitacionRepository,
        IControlProcesoRepository controlProcesoRepository,
        IConfiguration configuration
    )
    {
        _repository = repository;
        _habilitacionRepository = habilitacionRepository;
        _controlProcesoRepository = controlProcesoRepository;
        _noComprimir =
            configuration
                .GetSection("HabilidacionesParaNoComprimirRed")
                .Get<int[]>()
                ?.Where(id => id > 0)
                .ToHashSet()
            ?? new HashSet<int>();
    }

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerRedComprimidaAsync(
        string usuario,
        int cicloId,
        string inicio,
        string fin,
        bool controlPaso = true
    )
    {
        var id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        const string paso = PasosDiccionario.RED_COMPRIMIDA;
        bool pasoIniciado = false;
        try
        {
            var ini = DateTime.Now;
            if (controlPaso)
            {
                var siguiente = await _controlProcesoRepository.GetSiguientePaso(
                    id,
                    usuario,
                    ProcesosDiccionario.COMISIONES,
                    cicloId
                );
                if (paso != siguiente.Data.nombre)
                    return (
                        false,
                        "Esta paso ya se encuentra ejecutado para este ciclo, si quieres volver a a procesar debes reinicar el proceso para el ciclo",
                        ""
                    );
                var inicioPaso = await _controlProcesoRepository.IniciarPaso(
                    id,
                    usuario,
                    ProcesosDiccionario.COMISIONES,
                    cicloId,
                    paso
                );
                if (!inicioPaso.Success || !(inicioPaso.Data?.status ?? false))
                    return (false, inicioPaso.Data?.mensaje ?? inicioPaso.Mensaje, "");
                pasoIniciado = true;
            }
            var habilitaciones = await _habilitacionRepository.GetHabilitaciones(
                id,
                usuario,
                cicloId
            );
            var activos = await _repository.GetObetenerContactoVentasMes(id, usuario, inicio, fin);
            var red = await _repository.GetRedCotactoAll(id, usuario);
            var personas = habilitaciones.Data.ToList();
            var activosPorId = activos
                .ListadoContactosActivos.Select(x => new ItemContactoActivo
                {
                    LContactoId = x.LContactoId > 0 ? x.LContactoId : x.LVendedorId,
                    LVendedorId = x.LVendedorId > 0 ? x.LVendedorId : x.LContactoId,
                })
                .Where(x => x.LVendedorId > 0)
                .GroupBy(x => x.LVendedorId)
                .ToDictionary(x => x.Key, x => x.First());
            foreach (var h in personas)
                if (h.LContactoId > 0 && !activosPorId.ContainsKey(h.LContactoId))
                    activosPorId[h.LContactoId] = new ItemContactoActivo
                    {
                        LContactoId = h.LContactoId,
                        LVendedorId = h.LContactoId,
                    };
            var redPorContacto = red
                .ListadoContactosCuotas.Where(x => x.Hijo > 0)
                .GroupBy(x => x.Hijo)
                .ToDictionary(x => x.Key, x => x.First());
            var noEncontradas = _noComprimir
                .Where(x => !redPorContacto.ContainsKey(x))
                .OrderBy(x => x)
                .ToList();
            foreach (var contacto in _noComprimir.Where(redPorContacto.ContainsKey))
                if (!activosPorId.ContainsKey(contacto))
                    activosPorId[contacto] = new ItemContactoActivo
                    {
                        LContactoId = contacto,
                        LVendedorId = contacto,
                    };
            var activosLista = activosPorId.Values.ToList();
            var activoIds = activosPorId.Keys.ToHashSet();
            var lista = new List<ItemContactoRed>();
            foreach (var item in activosLista)
            {
                int contacto = item.LVendedorId,
                    nivel = 1;
                var visitados = new HashSet<int> { contacto };
                while (nivel <= 7 && redPorContacto.TryGetValue(contacto, out var patrocinador))
                {
                    int padre = patrocinador.Padre;
                    if (padre <= 0 || !visitados.Add(padre))
                        break;
                    if (activoIds.Contains(padre))
                        lista.Add(
                            new ItemContactoRed
                            {
                                LContactoId = item.LVendedorId,
                                LPatrocinadorId = padre,
                                Nivel = nivel++,
                                LCicloId = cicloId,
                                LContratoId = 0,
                                Usuario = usuario,
                            }
                        );
                    contacto = padre;
                }
            }
            var guardado = await _repository.GuardarRedComprimida(id, usuario, lista);
            if (!guardado.Success)
                return controlPaso
                    ? await CancelarAsync(id, usuario, cicloId, paso, guardado.Mensaje)
                    : (false, guardado.Mensaje, "");
            if (controlPaso)
            {
                var finPaso = await _controlProcesoRepository.FinalizarPaso(
                    id,
                    usuario,
                    ProcesosDiccionario.COMISIONES,
                    cicloId,
                    paso
                );
                if (!finPaso.Success || !(finPaso.Data?.status ?? false))
                    return (false, finPaso.Data?.mensaje ?? finPaso.Mensaje, "");
            }
            pasoIniciado = false;
            return (
                guardado.Success,
                guardado.Mensaje,
                new
                {
                    ini,
                    fin = DateTime.Now,
                    Nivel = activosLista,
                    RedComprimida = lista,
                    personasHabilitadas = personas,
                    habilidacionesParaNoComprimirRed = _noComprimir.OrderBy(x => x),
                    habilidacionesConfiguradasNoEncontradas = noEncontradas,
                    guardado.Success,
                    guardado.Mensaje,
                }
            );
        }
        catch (Exception ex)
        {
            if (controlPaso && pasoIniciado)
                await _controlProcesoRepository.CancelarPaso(
                    id,
                    usuario,
                    ProcesosDiccionario.COMISIONES,
                    cicloId,
                    paso
                );
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerRedCuotasAsync(
        string usuario,
        int cicloId
    )
    {
        var id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        bool pasoIniciado = false;
        try
        {
            var ini = DateTime.Now;
            var siguiente = await _controlProcesoRepository.GetSiguientePaso(
                id,
                usuario,
                ProcesosDiccionario.COMISIONES,
                cicloId
            );
            if (PasosDiccionario.RED_COMPLETA != siguiente.Data.nombre)
                return (
                    false,
                    "Esta paso ya se encuentra ejecutado para este ciclo, si quieres volver a a procesar debes reinicar el proceso para el ciclo",
                    ""
                );
            var inicio = await _controlProcesoRepository.IniciarPaso(
                id,
                usuario,
                ProcesosDiccionario.COMISIONES,
                cicloId,
                PasosDiccionario.RED_COMPLETA
            );
            if (!inicio.Success || !(inicio.Data?.status ?? false))
                return (false, inicio.Data?.mensaje ?? inicio.Mensaje, "");
            pasoIniciado = true;
            var red = await _repository.GetRedCotactoAll(id, usuario);
            var diccionario = red.ListadoContactosCuotas.ToDictionary(x => x.Hijo, x => x.Padre);
            var lista = new List<ItemRedSieteNiveles>();
            foreach (var item in red.ListadoContactosCuotas)
            {
                var fila = new ItemRedSieteNiveles { Hijo = item.Hijo };
                int actual = item.Hijo;
                for (int nivel = 1; nivel <= 7; nivel++)
                {
                    if (!diccionario.TryGetValue(actual, out int padre))
                        break;
                    switch (nivel)
                    {
                        case 1:
                            fila.PadreN1 = padre;
                            break;
                        case 2:
                            fila.PadreN2 = padre;
                            break;
                        case 3:
                            fila.PadreN3 = padre;
                            break;
                        case 4:
                            fila.PadreN4 = padre;
                            break;
                        case 5:
                            fila.PadreN5 = padre;
                            break;
                        case 6:
                            fila.PadreN6 = padre;
                            break;
                        case 7:
                            fila.PadreN7 = padre;
                            break;
                    }
                    actual = padre;
                }
                lista.Add(fila);
            }
            var guardado = await _repository.GuardarRedContactoTemporal(id, usuario, lista);
            if (!guardado.Success)
                return await CancelarAsync(
                    id,
                    usuario,
                    cicloId,
                    PasosDiccionario.RED_COMPLETA,
                    guardado.Mensaje
                );
            var fin = await _controlProcesoRepository.FinalizarPaso(
                id,
                usuario,
                ProcesosDiccionario.COMISIONES,
                cicloId,
                PasosDiccionario.RED_COMPLETA
            );
            if (!fin.Success || !(fin.Data?.status ?? false))
                return (false, fin.Data?.mensaje ?? fin.Mensaje, "");
            pasoIniciado = false;
            return (
                guardado.Success,
                guardado.Mensaje,
                new
                {
                    ClientesCuotas = lista.Count,
                    ini,
                    fin = DateTime.Now,
                }
            );
        }
        catch (Exception ex)
        {
            if (pasoIniciado)
                await _controlProcesoRepository.CancelarPaso(
                    id,
                    usuario,
                    ProcesosDiccionario.COMISIONES,
                    cicloId,
                    PasosDiccionario.RED_COMPLETA
                );
            return (false, ex.Message, "");
        }
    }

    private async Task<(bool Success, string Mensaje, object Data)> CancelarAsync(
        string id,
        string usuario,
        int cicloId,
        string paso,
        string mensaje
    )
    {
        await _controlProcesoRepository.CancelarPaso(
            id,
            usuario,
            ProcesosDiccionario.COMISIONES,
            cicloId,
            paso
        );
        return (false, mensaje, "");
    }
}
