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
    public int LTipoContratoId { get; set; }
    public int LAsesorId { get; set; }
    public int LPropietarioId { get; set; }
    public int LComplejoId { get; set; }
    public decimal DPecioInicial { get; set; }
    public string? EstadoContrato { get; set; }
}
