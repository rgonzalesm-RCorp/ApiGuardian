namespace ApiGuardian.Domain.Entities;

public sealed class RetencionEmpresaItem
{
    public int LCicloId { get; set; }
    public int EmpresaId { get; set; }
    public string EmpresaNombre { get; set; } = string.Empty;
    public int ContactoId { get; set; }
    public string Carnet { get; set; } = string.Empty;
    public string SNombreCompleto { get; set; } = string.Empty;
    public decimal VPers { get; set; }
    public decimal VGrupo { get; set; }
    public decimal Residual { get; set; }
    public decimal MontoComision { get; set; }
    public int LPresentaFactura { get; set; }
    public decimal PorcentajeRetencion { get; set; }
    public decimal MontoRetencion { get; set; }
    public decimal TotalComision { get; set; }
}
