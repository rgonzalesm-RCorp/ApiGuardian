public class ItemHabilitacionComision
{
    public int LHabilitacionId { get; set; }
    public int LContactoId { get; set; }
    public int LCicloId { get; set; }
    public decimal MontoVenta { get; set; }
    public string Observacion { get; set; } = string.Empty;
    public bool GeneraComisiones { get; set; } = true;
    public int Estado { get; set; } = 1;
    public string UsuarioCreacion { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public string UsuarioModificacion { get; set; } = string.Empty;
    public DateTime? FechaModificacion { get; set; }
    public string NombreAsesor { get; set; } = string.Empty;
    public string DocumentoAsesor { get; set; } = string.Empty;
}
