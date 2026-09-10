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
    public decimal BonoPar { get; set; }
    public decimal Residual { get; set; }
    public decimal Grupo { get; set; }
    public decimal Descuento { get; set; }
    public decimal Retencion { get; set; }
    public decimal ComisionDespuesRetencion { get; set; }
    public decimal TotalDescuento { get; set; }
    public string? Ciclo { get; set; }
}
public class EmpresaHeaderPagarComision
{
    public string? SEmpresa { get; set; }
    public int EmpresaId { get; set; }
}

public sealed class DescuentoAplicacionesProrrateo
{
    public int ContactoId { get; set; }
    public string Documento { get; set; } = string.Empty;
    public int EmpresaId { get; set; }
    public decimal Monto { get; set; }
}

public sealed class PagoComisionOpciones
{
    public List<RedistribucionPagoComision> RedistribucionesPorRetencion { get; set; } = [];
}

public sealed class RedistribucionPagoComision
{
    public int EmpresaOrigenId { get; set; }
    public int EmpresaAsumeId { get; set; }
}
