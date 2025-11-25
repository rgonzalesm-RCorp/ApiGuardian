public class DataComision
{
    public decimal ComisionVentaPersonal { get; set; }
    public decimal ComisionVentaGrupo { get; set; }
    public decimal ComisionResidual { get; set; }
    public decimal ComisionLiderazgo { get; set; }
    public decimal Retencion { get; set; }
    public decimal DescuentoLote { get; set; }
    public decimal PorcentajeRetencion { get; set; }
    public decimal TotalComision { get; set; }
    public decimal TotalFinal { get; set; }
    public int LDescuentoId { get; set; }

}
public class ListaAdministracionDescuentoCiclo
{
    public string? SDetalles { get; set; }
    public int LDescuentoCicloDetalleId { get; set; }
    public int LTipoDescuentoId { get; set; }
    public string? STipoDescuento { get; set; }
    public int LComplejoId { get; set; }
    public string? SComplejo { get; set; }
    public string? SMzna { get; set; }
    public string? SLote { get; set; }
    public string? SUv { get; set; }
    public decimal DMonto { get; set; }
    public string? SObservacion { get; set; }
    public int LContactoId { get; set; }
}
public class DataDescuento
{
    public int LDescuentoCicloId { get; set; }
    public int LCicloId { get; set; }
    public int LContactoId { get; set; }
    public int LTipoDescuentoId { get; set; }
    public int LComplejoId { get; set; }
    public string? Mz { get; set; }
    public string? Lote { get; set; }
    public string? Uv { get; set; }
    public decimal Monto { get; set; }
    public string? Descripcion { get; set; }
    public string? Usuario { get; set; }

}