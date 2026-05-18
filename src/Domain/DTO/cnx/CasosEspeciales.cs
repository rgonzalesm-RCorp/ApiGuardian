public class UpgradeSolicitudDto
{
    public long UpgradeSolicitudId { get; set; }= 0;
    public int SolicitudId { get; set; }
    public string DocId { get; set; } = string.Empty;
    public string? DocIdVendedor { get; set; }

    public int EmpresaHoldId { get; set; }
    public int ProyectoHoldId { get; set; }
    public int VentaHoldId { get; set; }
    public string ProductoHoldId { get; set; } = string.Empty;

    public decimal MontoHoldId { get; set; }
    public decimal PagadoHoldId { get; set; }
    public decimal DeudaHoldId { get; set; }

    public int? EmpresaId { get; set; }
    public int? ProyectoId { get; set; }
    public int? VentaId { get; set; }
    public string ProductoId { get; set; } = string.Empty;

    public decimal? Monto { get; set; }
    public decimal? Deuda { get; set; }
    public int? Cuota { get; set; }
}