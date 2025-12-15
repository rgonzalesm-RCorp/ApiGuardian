public class RptProrrateo
{
    public string? LCodigoBanco { get; set; }
    public string? LCuentaBanco { get; set; }
    public string? SNombreCompleto { get; set; }
    public string? SCedulaIdentidad { get; set; }
    public string? SEmpresa { get; set; }
    public string? Ciclo { get; set; }
    public decimal Importe { get; set; }
    public decimal Retencion { get; set; }
    public decimal Liquido { get; set; }
    public decimal Descuento { get; set; }
    public decimal Prorrateo { get; set; }
    public int EmpresaId { get; set; }
    public int LProrrateoId { get; set; }
    public int LContactoId { get; set; }
}