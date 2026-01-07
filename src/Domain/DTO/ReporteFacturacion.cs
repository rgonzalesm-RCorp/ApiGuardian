public class RptFacturacion
{
    public int LContactoId { get; set; }
    public string? SCodigo { get; set; }
    public string? SCedulaIdentidad { get; set; }
    public string? SNombreCompleto { get; set; }
    public string? Pais { get; set; }
    public string? Ciudad { get; set; }
    public int LEmpresaId { get; set; }
    public string? Empresa { get; set; }
    public decimal TotalComisionVtaGrupoResidual { get; set; }
    public decimal TotalComisionVtaPersonal { get; set; }
    public int LCicloId { get; set; }
    public decimal TotalComisionVtaGrupoResidualBs { get; set; }
    public decimal TotalComisionVtaPersonalBs { get; set; }
    public string? RazonSocial { get; set; }
    public string? Nit { get; set; }
    public string? NombreCiclo { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
}