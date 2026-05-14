public class VentaResidual
{
    public int IdCuotproduc { get; set; } = 0;
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
    public int NroCuotaPagables { get; set; }
    
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
    public decimal Porcentaje { get; set; }
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
    public int NroCuotaPagables { get; set; }
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
    public decimal Porcentaje { get; set; }
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
    public decimal TotalComision { get; set; }
    public int TotalCuotasComisionables { get; set; }
    public int TotalCuotasContabilizar { get; set; }
}
public class ProductosDetalleCuotas
{
    private string _usuarioAdd = string.Empty;
    private string _fechaAdd = string.Empty;
    private string _pagado = string.Empty;
    private string _habilitado = string.Empty;
    public int? IdProductoDetalle { get; set; }
    public string UsuarioAdd
    {
        get => _usuarioAdd;
        set => _usuarioAdd = value?.TrimEnd() ?? string.Empty;
    }
    public string FechaAdd
    {
        get => _fechaAdd;
        set => _fechaAdd = value?.TrimEnd() ?? string.Empty;
    }
    public int? FkIdProductoPagar { get; set; }
    public int? LcontratoId { get; set; }
    public int? CantCuotas { get; set; }
    public int? ExcCuotas { get; set; } = 0;
    public string Pagado
    {
        get => _pagado;
        set => _pagado = value?.TrimEnd() ?? string.Empty;
    }
    public string Habilitado
    {
        get => _habilitado;
        set => _habilitado = value?.TrimEnd() ?? string.Empty;
    }
    public int? LcicloId { get; set; }
}
public class ProductosPagarMensualUpdate
{
    public int IdProductoPagar { get; set; }
    public int LContactoId { get; set; }
    public int? LContratoId { get; set; }
    public string SNroVenta { get; set; } = string.Empty;
    public int CantidadNroCuotas { get; set; }
    public int? CuotasPagadas { get; set; }
    public int? CuotasTotalesAPagar { get; set; }
    public bool ActivoMes { get; set; }
    public decimal MontoPagarMes { get; set; }
    public decimal TotalComision { get; set; }
    public int TotalCuotasContabilizar { get; set; }
    public List<ProductosDetalleCuotas> _ProductosDetalleCuotas {get; set;} = new List<ProductosDetalleCuotas>();
}
