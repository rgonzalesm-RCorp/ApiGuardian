namespace ApiGuardian.Domain.Entities;

public sealed class SolicitudGuardarAsesoresFacturacion
{
    public string Usuario { get; set; } = string.Empty;
    public int LCicloId { get; set; }
    public List<int> ContactosIds { get; set; } = [];
}

public sealed class ResultadoGuardarAsesoresFacturacion
{
    public int Insertados { get; set; }
    public int Omitidos { get; set; }
    public List<string> Errores { get; set; } = [];
}
