public class ListaAdministracionCicloFactura
{
    public int LCicloFacturaId { get; set; }
    public DateTime FechaRegistro { get; set; }
    public int LCicloId { get; set; }
    public string? Ciclo { get; set; }
    public int LContactoId { get; set; }
    public string? SNombreCompleto { get; set; }
    public int lSemanaId { get; set; }
    public string? semana { get; set; }

}

public class AdministracionCicloFactura
{
    public string? Usuario { get; set; }
    public int LCicloId { get; set; }
    public int LContactoId { get; set; }
    public int LSemanaId { get; set; }
    public int LCicloFactura { get; set; }
}