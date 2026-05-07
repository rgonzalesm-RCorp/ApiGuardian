public class VentaResidual
{
    private string _nroVenta = string.Empty;
    private string _empresa = string.Empty;
    private string _proyecto = string.Empty;
    private string _lotes = string.Empty;
    private string _nombreCliente = string.Empty;
    private string _ciCliente = string.Empty;
    private string _vendedor = string.Empty;
    private string _ciVendedor = string.Empty;
    private string _concepto1 = string.Empty;
    public string NroVenta
    {
        get => _nroVenta;
        set => _nroVenta = value?.TrimEnd() ?? string.Empty;
    }
    public string Empresa
    {
        get => _empresa;
        set => _empresa = value?.TrimEnd() ?? string.Empty;
    }
    public int IdVenta { get; set; }
    public DateTime Fecha { get; set; }
    public int IdAlmacen { get; set; }
    public string Proyecto
    {
        get => _proyecto;
        set => _proyecto = value?.TrimEnd() ?? string.Empty;
    }
    public string Lotes
    {
        get => _lotes;
        set => _lotes = value?.TrimEnd() ?? string.Empty;
    }
    public int IdRecibo { get; set; }
    public DateTime FechaRecibo { get; set; }
    public int NroCuota { get; set; }
    public decimal ImporteTotal { get; set; }
    public int IdCliente { get; set; }
    public string NombreCliente
    {
        get => _nombreCliente;
        set => _nombreCliente = value?.TrimEnd() ?? string.Empty;
    }
    public string CiCliente
    {
        get => _ciCliente;
        set => _ciCliente = value?.TrimEnd() ?? string.Empty;
    }
    public int IdVendedor { get; set; }
    public string Vendedor
    {
        get => _vendedor;
        set => _vendedor = value?.TrimEnd() ?? string.Empty;
    }
    public string CiVendedor
    {
        get => _ciVendedor;
        set => _ciVendedor = value?.TrimEnd() ?? string.Empty;
    }
    public string Concepto1
    {
        get => _concepto1;
        set => _concepto1 = value?.TrimEnd() ?? string.Empty;
    }
    public int LcicloId { get; set; }
}
public class ProductosPagarMensuales
{
    private string _snroventa = string.Empty;
    private string _ciclosHabilitados = string.Empty;
    public int IdProductoPagar { get; set; }
    public int? LcontratoId { get; set; }
    public int? LcomplejoId { get; set; }
    public string Snroventa
    {
        get => _snroventa;
        set => _snroventa = value?.TrimEnd() ?? string.Empty;
    }
    public int? LcontactoId { get; set; }
    public int? LasesorId { get; set; }
    public DateTime? Dtfecha { get; set; }
    public decimal? Precio { get; set; }
    public decimal? CuotaInicial { get; set; }
    public int? Porcentaje { get; set; }
    public decimal? Comision { get; set; }
    public int? CuotAccPen { get; set; }
    public int? CuotPagadas { get; set; }
    public decimal? Inicial10 { get; set; }
    public decimal? MontPagar { get; set; }
    public decimal? MensPagar { get; set; }
    public string CiclosHabilitados
    {
        get => _ciclosHabilitados;
        set => _ciclosHabilitados = value?.TrimEnd() ?? string.Empty;
    }
    public int? Terminado { get; set; } = 0;
}

public class ListadoComisionCuotaResidual
{
    public string NroVenta { get; set; } = string.Empty;
    public string Empresa { get; set; } = string.Empty;
    public int IdVenta { get; set; }
    public DateTime Fecha { get; set; }
    public int IdAlmacen { get; set; }
    public string Proyecto { get; set; } = string.Empty;
    public string Lotes { get; set; } = string.Empty;
    public int IdRecibo { get; set; }
    public DateTime FechaRecibo { get; set; }
    public int NroCuota { get; set; }
    public decimal ImporteTotal { get; set; }
    public int IdCliente { get; set; }
    public string NombreCliente { get; set; } = string.Empty;
    public string CiCliente { get; set; } = string.Empty;
    public int IdVendedor { get; set; }
    public string Vendedor { get; set; } = string.Empty;
    public string CiVendedor { get; set; } = string.Empty;
    public string Concepto1 { get; set; } = string.Empty;
    public int LcicloId { get; set; }
    public int IdProductoPagar { get; set; }
    public int? LcontratoId { get; set; }
    public int? LcomplejoId { get; set; }
    public decimal? Precio { get; set; }
    public decimal? CuotaInicial { get; set; }
    public int? Porcentaje { get; set; }
    public decimal? Comision { get; set; }
    public int? CuotAccPen { get; set; }
    public int? CuotPagadas { get; set; }
    public decimal? Inicial10 { get; set; }
    public decimal? MontPagar { get; set; }
    public decimal? MensPagar { get; set; }
    public string CiclosHabilitados { get; set; } = string.Empty;
    public int? Terminado { get; set; }
    public int? LasesorId { get; set; }
    public bool Recibe { get; set; }
}