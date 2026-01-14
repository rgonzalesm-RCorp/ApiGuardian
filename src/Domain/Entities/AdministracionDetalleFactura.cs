public class AdministracionDetalleFactura
{
    public int LDetalleFacturaId { get; set; }
    public int LTipoComisionId { get; set; }
    public string SDetalle { get; set; } = string.Empty;
    public string? Usuario { get; set; }
}

public class ItemAdministracionDetalleFactura
{
    public int LDetalleFacturaId { get; set; }
    public int LTipoComisionId { get; set; }
    public string? TipoComision { get; set; }
    public string SDetalle { get; set; } = string.Empty;
    public string? Usuario { get; set; }
}
public class AdministracionTipoComision
{
    public int TipoComisionId { get; set; }
    public string? Descripcion { get; set; }
}
