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
public class AdministracionComplejo
{
    public int LComplejoId { get; set; }
    public string? SCodigo { get; set; }
    public string? SNombre { get; set; }
}
public class BasePaisDepartamento
{
    public int LDepartamentoId { get; set; }
    public int LPaisId { get; set; }
    public string? SNombre { get; set; }
}
public class AdministracionTipoContrato
{
    public int LTipoContratoId { get; set; }
    public string? SNombre { get; set; }
}
public class AdministracionEstadoContrato
{
    public int LEstadoContratoId { get; set; }
    public string? SNombre { get; set; }
}

