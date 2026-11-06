namespace ApiGuardian.Domain.DTO;

// Contacto tal como viene de administracioncontacto, antes de calcular la ruta.
public sealed class MonteSionArbolOrigen
{
    public int LContactoId { get; set; }
    public int? LPatrocinanteId { get; set; }
    public string SNombreCompleto { get; set; } = string.Empty;
}

// Fila de montesion_arbol.
public sealed class MonteSionArbolNodo
{
    public int LContactoId { get; set; }
    public int? LPadreContactoId { get; set; }
    public string SNombreCompleto { get; set; } = string.Empty;
    public int Nivel { get; set; }
    public string Ruta { get; set; } = string.Empty;
    public int CantidadHijos { get; set; }
}

// Respuesta de consulta de una persona: ella misma, sus padres y sus hijos directos.
public sealed class MonteSionArbolConsulta
{
    public MonteSionArbolNodo Persona { get; set; } = new();
    public IReadOnlyList<MonteSionArbolNodo> Padres { get; set; } = [];
    public IReadOnlyList<MonteSionArbolNodo> Hijos { get; set; } = [];
}
