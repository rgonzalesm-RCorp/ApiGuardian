public class ConfiguracionUtils
{
    public int cantidadContactos { get; set; }
    public int cantidadContratos { get; set; }
}
public class AdministracionCiclo
{
    public int LCicloId { get; set; }
    public string? NombreCiclo { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public int CVerEnWeb { get; set; }
}
public class AdministracionSemanaCiclo
{
    public int LCicloId { get; set; }
    public string? NombreSemana { get; set; }
    public int IdSemana { get; set; }
    public int LSemanaId { get; set; }
    public string? Descripcion { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
}
