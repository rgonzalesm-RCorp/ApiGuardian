using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.DTO;

namespace CleanDapperApi.Api.Services;

public sealed class MonteSionArbolService : IMonteSionArbolService
{
    private const int LongitudMaximaRuta = 500;
    private const int LongitudMinimaBusqueda = 3;
    private readonly IMonteSionArbolRepository _repository;

    public MonteSionArbolService(IMonteSionArbolRepository repository) => _repository = repository;

    public async Task<(bool Success, string Mensaje, int Total)> ArmarAsync(int cicloId, string usuario)
    {
        if (cicloId <= 0)
            return (false, "El ciclo es obligatorio.", 0);

        var origen = await _repository.ObtenerContactosAsync(Guid.NewGuid().ToString("N"));
        if (!origen.Success)
            return (false, origen.Mensaje, 0);

        var nodos = CalcularNodos(origen.Contactos);
        if (nodos.Any(nodo => nodo.Ruta.Length > LongitudMaximaRuta))
            return (false, "La profundidad del árbol supera el límite de la ruta.", 0);

        return await _repository.ReemplazarArbolAsync(
            Guid.NewGuid().ToString("N"),
            cicloId,
            string.IsNullOrWhiteSpace(usuario) ? "sistema" : usuario,
            nodos
        );
    }

    public async Task<(bool Success, string Mensaje, IReadOnlyList<MonteSionArbolNodo> Data)> RaicesAsync(int cicloId)
    {
        if (cicloId <= 0)
            return (false, "El ciclo es obligatorio.", []);

        var resultado = await _repository.ObtenerRaicesAsync(Guid.NewGuid().ToString("N"), cicloId);
        return (resultado.Success, resultado.Mensaje, resultado.Nodos);
    }

    public async Task<(bool Success, string Mensaje, IReadOnlyList<MonteSionArbolNodo> Data)> HijosAsync(int cicloId, int padreId)
    {
        if (cicloId <= 0 || padreId <= 0)
            return (false, "El ciclo y la persona son obligatorios.", []);

        var resultado = await _repository.ObtenerHijosAsync(Guid.NewGuid().ToString("N"), cicloId, padreId);
        return (resultado.Success, resultado.Mensaje, resultado.Nodos);
    }

    public async Task<(bool Success, string Mensaje, MonteSionArbolConsulta? Data)> ConsultarPersonaAsync(int cicloId, int contactoId)
    {
        if (cicloId <= 0 || contactoId <= 0)
            return (false, "El ciclo y la persona son obligatorios.", null);

        var logId = Guid.NewGuid().ToString("N");
        var persona = await _repository.ObtenerPersonaAsync(logId, cicloId, contactoId);
        if (!persona.Success)
            return (false, persona.Mensaje, null);
        if (persona.Persona is null)
            return (false, persona.Mensaje, null);

        var idsPadres = persona.Persona.Ruta
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(int.Parse)
            .Where(id => id != contactoId)
            .ToList();

        var padres = await _repository.ObtenerPorIdsAsync(logId, cicloId, idsPadres);
        if (!padres.Success)
            return (false, padres.Mensaje, null);

        var hijos = await _repository.ObtenerHijosAsync(logId, cicloId, contactoId);
        if (!hijos.Success)
            return (false, hijos.Mensaje, null);

        return (true, "Consulta realizada correctamente.", new MonteSionArbolConsulta
        {
            Persona = persona.Persona,
            Padres = padres.Nodos,
            Hijos = hijos.Nodos,
        });
    }

    public async Task<(bool Success, string Mensaje, IReadOnlyList<MonteSionArbolNodo> Data)> BuscarAsync(int cicloId, string texto)
    {
        var textoLimpio = (texto ?? string.Empty).Trim();
        if (cicloId <= 0)
            return (false, "El ciclo es obligatorio.", []);
        if (textoLimpio.Length < LongitudMinimaBusqueda)
            return (false, $"Escriba al menos {LongitudMinimaBusqueda} caracteres para buscar.", []);

        var resultado = await _repository.BuscarAsync(Guid.NewGuid().ToString("N"), cicloId, textoLimpio);
        return (resultado.Success, resultado.Mensaje, resultado.Nodos);
    }

    // Calcula nivel y ruta de cada contacto siguiendo lpatrocinante_id hasta la raíz.
    // Si los datos tienen un ciclo de patrocinio, la cadena se corta al repetirse un id
    // para no quedar en un bucle infinito.
    public static IReadOnlyList<MonteSionArbolNodo> CalcularNodos(IReadOnlyList<MonteSionArbolOrigen> contactos)
    {
        var padrePorId = contactos
            .GroupBy(contacto => contacto.LContactoId)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.First().LPatrocinanteId);
        var nombrePorId = contactos
            .GroupBy(contacto => contacto.LContactoId)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.First().SNombreCompleto);

        var nodos = new List<MonteSionArbolNodo>(padrePorId.Count);
        foreach (var contacto in padrePorId.Keys)
        {
            var cadena = new List<int> { contacto };
            var vistos = new HashSet<int> { contacto };
            var actual = contacto;

            while (padrePorId.TryGetValue(actual, out var padre)
                   && padre is int padreId
                   && padrePorId.ContainsKey(padreId)
                   && vistos.Add(padreId))
            {
                cadena.Add(padreId);
                actual = padreId;
            }

            cadena.Reverse();
            var padreInmediato = cadena.Count > 1 ? cadena[^2] : (int?)null;

            nodos.Add(new MonteSionArbolNodo
            {
                LContactoId = contacto,
                LPadreContactoId = padreInmediato,
                SNombreCompleto = nombrePorId[contacto],
                Nivel = cadena.Count,
                Ruta = "/" + string.Join('/', cadena) + "/",
            });
        }

        return nodos;
    }
}
