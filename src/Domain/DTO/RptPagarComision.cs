public class RptPagarComision
{
    public string? TipoCuenta { get; set; }
    public string? CodigoBanco { get; set; }
    public string? CuentaBanco { get; set; }
    public string? Ciudad { get; set; }
    public string? NombreCompleto { get; set; }
    public string? CedulaIdentidad { get; set; }
    public int LContactold { get; set; }
    public decimal Personal { get; set; }
    public decimal Liderazgo { get; set; }
    public decimal Residual { get; set; }
    public decimal Grupo { get; set; }
    public decimal Descuento { get; set; }
    public decimal Retencion { get; set; }
    public string? Ciclo { get; set; }
}
public class ProrrateoHorizontalDto
{
    public int LContactoId { get; set; }
    public Dictionary<int, decimal> ValoresPorEmpresa { get; set; } = new();
}
public class EmpresaHeaderPagarComision
{
    public string? SEmpresa { get; set; }
    public int EmpresaId { get; set; }
}