public class AplicacionesItem
{
    public int LContactoId { get; set; }
    public string? SCodigo { get; set; }
    public string? SCedulaIdentidad { get; set; }
    public string? SNombreCompleto { get; set; }
    public decimal ComisionVP { get; set; }
    public decimal ComisionVG { get; set; }
    public decimal ComisionBR { get; set; }
    public decimal ComisionBL { get; set; }
    public decimal Retencion { get; set; }
    public decimal PorcentajeRetencion { get; set; }
    public decimal Descuento { get; set; }
}
public class RptAplicaciones
{
    public IEnumerable<AplicacionesItem> Aplicaciones { get; set; }
}