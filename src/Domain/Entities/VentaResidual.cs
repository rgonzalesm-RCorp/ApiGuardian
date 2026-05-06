public class VentaResidual
{
    public string NroVenta { get; set; } = "";
    public string Empresa { get; set; } = "";
    public int IdVenta { get; set; }
    public DateTime Fecha { get; set; }
    public int IdAlmacen { get; set; }
    public string Proyecto { get; set; } = "";
    public string Lotes { get; set; } = "";
    public int IdRecibo { get; set; }
    public DateTime FechaRecibo { get; set; }
    public int NroCuota { get; set; }
    public decimal ImporteTotal { get; set; }
    public int IdCliente { get; set; }
    public string NombreCliente { get; set; } = "";
    public string CiCliente { get; set; } = "";
    public int IdVendedor { get; set; }
    public string Vendedor { get; set; } = "";
    public string CiVendedor { get; set; } = "";
    public string Concepto1 { get; set; } = "";
    public int LcicloId { get; set; }
}