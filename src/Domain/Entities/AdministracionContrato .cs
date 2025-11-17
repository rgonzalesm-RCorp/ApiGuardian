public class AdministracionContrato
{
    public int LcontratoId { get; set; }
    public DateTime FechaRegistro { get; set; }
    public DateTime Fecha { get; set; }
    public string? NroVenta { get; set; }
    public string? Propietario { get; set; }
    public string? Complejo { get; set; }
    public string? NroMnzo { get; set; }
    public string? NroLote { get; set; }
    public decimal Precio { get; set; }
    public decimal CuotaInicial { get; set; }
    public int Estado { get; set; }
    public string? TipoContrato { get; set; }
    public string? Asesor { get; set; }
}
