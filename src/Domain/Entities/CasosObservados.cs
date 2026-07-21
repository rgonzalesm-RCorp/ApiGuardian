public class ItemCasoObservado
{
    public int CasoObservadoId { get; set; }
    public int LCicloId { get; set; }
    public int VentaId { get; set; }
    public string Cliente { get; set; } = string.Empty;
    public string Vendedor { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
    public string Estado { get; set; } = "PENDIENTE";
}

public class CasosObservadosResumen
{
    public int TotalCasos { get; set; }
    public int CasosPendientes { get; set; }
    public int CasosRevisados { get; set; }
}
