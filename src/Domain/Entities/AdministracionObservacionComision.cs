public class ListaAdministracionObservacionComision
{
    public int ObservacionId { get; set; }
    public DateTime Fecha { get; set; }
    public int LContactoId { get; set; }
    public string? SNombreCompleto { get; set; }
    public int LCicloId { get; set; }
    public string? Ciclo { get; set; }
    public string? SObservacion { get; set; }
}
public class AdministracionObservacionComision
{
    public string? usuario { get; set; }
    public int LContactoId { get; set; }
    public int lObservacionId { get; set; }
    public int LCicloId { get; set; }
    public string? SObservacion { get; set; }

}
