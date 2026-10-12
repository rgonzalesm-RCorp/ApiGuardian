namespace ApiGuardian.Domain.Entities;

public sealed class RedSerContacto
{
    public int LContactoId { get; set; }
    public int? LPatrocinanteId { get; set; }
    public string SNombreCompleto { get; set; } = string.Empty;
    public string SCedulaIdentidad { get; set; } = string.Empty;
}

public class RedSerNodo
{
    public int LContactoId { get; set; }
    public int? LPadreContactoId { get; set; }
    public string SNombreCompleto { get; set; } = string.Empty;
    public string SCedulaIdentidad { get; set; } = string.Empty;
    public int Nivel { get; set; }
    public bool EsEstructural { get; set; }
    public List<RedSerNodo> Hijos { get; set; } = [];
}
