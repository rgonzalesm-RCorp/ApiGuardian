using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Services;

public sealed class RedSerService : IRedSerService
{
    private const int KalomaiId = 0;
    private const string Kalomai = "Kalomai LLC";
    private const int RenzoId = 2;
    private const int EvelioId = 3;
    private const int AnaId = 334;
    private const int IverKennyId = 6474;
    private const string GrupoSionInversionCi = "307412021";
    private readonly IRedSerRepository _repository;

    public RedSerService(IRedSerRepository repository) => _repository = repository;

    public async Task<(bool Success, string Mensaje, object Data)> ArmarAsync(
        string usuario,
        DateTime inicio,
        DateTime fin
    )
    {
        if (!EsSeptiembre(inicio, fin))
            return (false, "RedSer solo puede armarse con un período dentro de septiembre.", "");

        var logId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        var vendedores = await _repository.ObtenerVendedoresAsync(logId, inicio, fin);
        if (!vendedores.Success)
            return (false, vendedores.Mensaje, "");

        var contactos = await _repository.ObtenerContactosAsync(logId);
        if (!contactos.Success)
            return (false, contactos.Mensaje, "");

        try
        {
            var nodos = ConstruirNodos(vendedores.Vendedores, contactos.Contactos);
            var guardado = await _repository.GuardarAsync(logId, usuario, inicio, fin, nodos);
            if (!guardado.Success)
                return (false, guardado.Mensaje, "");

            return (
                true,
                guardado.Mensaje,
                new
                {
                    Inicio = inicio.Date,
                    Fin = fin.Date,
                    VendedoresDelMes = vendedores.Vendedores.Count,
                    Nodos = nodos.Count,
                    Red = CrearArbol(nodos),
                }
            );
        }
        catch (InvalidOperationException ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerAsync(DateTime? inicio, DateTime? fin)
    {
        if (inicio.HasValue != fin.HasValue)
            return (false, "Inicio y Fin deben enviarse juntos.", "");

        if (!inicio.HasValue)
        {
            var ultima = await _repository.ObtenerUltimaAsync(
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()
            );
            if (!ultima.Success || ultima.Nodos.Count == 0)
                return (false, ultima.Mensaje, "");

            return (
                true,
                ultima.Mensaje,
                new
                {
                    Inicio = ultima.Inicio,
                    Fin = ultima.Fin,
                    Nodos = ultima.Nodos.Count,
                    Red = CrearArbol(ultima.Nodos),
                }
            );
        }

        if (!EsSeptiembre(inicio.Value, fin!.Value))
            return (false, "RedSer solo está disponible para períodos dentro de septiembre.", "");

        var resultado = await _repository.ObtenerAsync(
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(),
            inicio.Value,
            fin.Value
        );
        if (!resultado.Success || resultado.Nodos.Count == 0)
            return (false, resultado.Mensaje, "");

        return (
            true,
            resultado.Mensaje,
            new { Inicio = inicio.Value.Date, Fin = fin.Value.Date, Nodos = resultado.Nodos.Count, Red = CrearArbol(resultado.Nodos) }
        );
    }

    private static List<RedSerNodo> ConstruirNodos(
        IReadOnlyList<int> vendedores,
        IReadOnlyList<RedSerContacto> contactos
    )
    {
        var porId = contactos.GroupBy(c => c.LContactoId).ToDictionary(g => g.Key, g => g.First());
        var renzo = ObtenerContacto(RenzoId, porId);
        var evelio = ObtenerContacto(EvelioId, porId);
        var ana = ObtenerContacto(AnaId, porId);
        if (!porId.ContainsKey(IverKennyId))
            throw new InvalidOperationException($"No se encontró el contacto obligatorio con lcontacto_id {IverKennyId}.");

        var vendedoresIds = vendedores
            .Append(IverKennyId)
            .Where(porId.ContainsKey)
            .Where(id => !EsGrupoSionInversion(porId[id]))
            .ToHashSet();

        var nodos = new Dictionary<int, RedSerNodo>
        {
            [KalomaiId] = new() { LContactoId = KalomaiId, SNombreCompleto = Kalomai, SCedulaIdentidad = string.Empty, Nivel = 0, EsEstructural = true },
            [renzo.LContactoId] = NodoEstructural(renzo, KalomaiId, 1),
            [evelio.LContactoId] = NodoEstructural(evelio, renzo.LContactoId, 2),
            [ana.LContactoId] = NodoEstructural(ana, renzo.LContactoId, 2),
        };

        foreach (var vendedorId in vendedoresIds.OrderBy(x => x))
        {
            if (nodos.ContainsKey(vendedorId))
                continue;

            var padre = PerteneceA(vendedorId, ana.LContactoId, porId)
                ? BuscarPadreVendedor(vendedorId, ana.LContactoId, vendedoresIds, porId)
                : PerteneceA(vendedorId, evelio.LContactoId, porId)
                    ? BuscarPadreVendedor(vendedorId, evelio.LContactoId, vendedoresIds, porId)
                    : evelio.LContactoId;

            nodos[vendedorId] = new RedSerNodo
            {
                LContactoId = vendedorId,
                LPadreContactoId = padre,
                SNombreCompleto = porId[vendedorId].SNombreCompleto,
                SCedulaIdentidad = porId[vendedorId].SCedulaIdentidad,
            };
        }

        foreach (var nodo in nodos.Values.Where(n => n.LContactoId != KalomaiId))
            nodo.Nivel = CalcularNivel(nodo, nodos, new HashSet<int>());

        return nodos.Values.OrderBy(n => n.Nivel).ThenBy(n => n.SNombreCompleto).ToList();
    }

    private static RedSerNodo NodoEstructural(RedSerContacto contacto, int padreId, int nivel) => new()
    {
        LContactoId = contacto.LContactoId,
        LPadreContactoId = padreId,
        SNombreCompleto = contacto.SNombreCompleto,
        SCedulaIdentidad = contacto.SCedulaIdentidad,
        Nivel = nivel,
        EsEstructural = true,
    };

    private static RedSerContacto ObtenerContacto(
        int contactoId,
        IReadOnlyDictionary<int, RedSerContacto> contactosPorId
    )
    {
        if (contactosPorId.TryGetValue(contactoId, out var contacto))
            return contacto;
        throw new InvalidOperationException($"No se encontró el contacto estructural con lcontacto_id {contactoId}.");
    }

    private static bool EsGrupoSionInversion(RedSerContacto contacto) =>
        string.Equals(
            contacto.SCedulaIdentidad.Trim(),
            GrupoSionInversionCi,
            StringComparison.OrdinalIgnoreCase
        );

    private static bool PerteneceA(int contactoId, int posibleAncestro, IReadOnlyDictionary<int, RedSerContacto> porId)
    {
        var visitados = new HashSet<int>();
        var actual = contactoId;
        while (visitados.Add(actual) && porId.TryGetValue(actual, out var contacto))
        {
            if (actual == posibleAncestro)
                return true;
            if (contacto.LPatrocinanteId is not > 0)
                return false;
            actual = contacto.LPatrocinanteId.Value;
        }
        return false;
    }

    private static int BuscarPadreVendedor(
        int contactoId,
        int limite,
        ISet<int> vendedores,
        IReadOnlyDictionary<int, RedSerContacto> porId
    )
    {
        var visitados = new HashSet<int>();
        var actual = contactoId;
        while (visitados.Add(actual) && porId.TryGetValue(actual, out var contacto))
        {
            if (actual == limite)
                return limite;
            if (contacto.LPatrocinanteId is not > 0)
                break;
            actual = contacto.LPatrocinanteId.Value;
            if (vendedores.Contains(actual))
                return actual;
        }
        return limite;
    }

    private static int CalcularNivel(RedSerNodo nodo, IReadOnlyDictionary<int, RedSerNodo> nodos, ISet<int> visitados)
    {
        if (nodo.LContactoId == KalomaiId)
            return 0;
        if (!visitados.Add(nodo.LContactoId) || nodo.LPadreContactoId is not int padreId || !nodos.TryGetValue(padreId, out var padre))
            throw new InvalidOperationException("La jerarquía RedSer contiene una relación de patrocinio inválida.");
        return CalcularNivel(padre, nodos, visitados) + 1;
    }

    private static RedSerNodo CrearArbol(IReadOnlyCollection<RedSerNodo> nodos)
    {
        var copia = nodos.ToDictionary(
            n => n.LContactoId,
            n => new RedSerNodo
            {
                LContactoId = n.LContactoId,
                LPadreContactoId = n.LPadreContactoId,
                SNombreCompleto = n.SNombreCompleto,
                SCedulaIdentidad = n.SCedulaIdentidad,
                Nivel = n.Nivel,
                EsEstructural = n.EsEstructural,
            }
        );
        foreach (var nodo in copia.Values.Where(n => n.LPadreContactoId is int))
            if (copia.TryGetValue(nodo.LPadreContactoId!.Value, out var padre))
                padre.Hijos.Add(nodo);
        foreach (var nodo in copia.Values)
            nodo.Hijos = nodo.Hijos.OrderBy(h => h.SNombreCompleto).ToList();
        return copia[KalomaiId];
    }

    private static bool EsSeptiembre(DateTime inicio, DateTime fin) =>
        inicio.Date <= fin.Date && inicio.Month == 9 && fin.Month == 9 && inicio.Year == fin.Year;
}
