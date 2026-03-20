public class BrConfiguracion
{
    public int BrConfiguracionId { get; set; }
    public int LCicloId { get; set; }
    public int TipoProductoId { get; set; }
    public string? Usuario { get; set; }

    public List<BrConfiguracionDetalle> Detalles { get; set; } = new();
}
public class BrConfiguracionDetalle
{
    public int BrConfiguracionDetalleId { get; set; }
    public int BrNivelesId { get; set; }
    public decimal Porcentaje { get; set; }
}

public class DetailsBrConfiguracion
{
    public int BrConfiguracionId { get; set; }
    public int LCicloId { get; set; }
    public int TipoProductoId { get; set; }
    public int NivelId { get; set; }
    public int BrConfiguracionDetalleId { get; set; }
    public string? Ciclo { get; set; }
    public string? TipoProducto { get; set; }
    public string? NombreNivel { get; set; }
    public int Nivel { get; set; }
    public decimal PorcentajeComision { get; set; }
}
public class BrNiveles
{
    public int NivelId { get; set; }
    public string? NombreNivel { get; set; }
    public int Nivel { get; set; }
}
public class BrTipoProducto
{
    public int TipoProductoId { get; set; }
    public string? TipoProducto { get; set; }
}
