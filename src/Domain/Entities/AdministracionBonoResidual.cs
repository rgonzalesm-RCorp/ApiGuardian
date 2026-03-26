public class ItemAdministracionBonoResidual
{
    public string Usuario { get; set; } = string.Empty;
    public int LBonoResidualId { get; set; }
    public int LCicloId { get; set; }
    public int LContactoId { get; set; }
    public int LTipoBono { get; set; } = 1;
    public int DMontoLote { get; set; } = 1;
    public int LMoraG1 { get; set; } = 0;
    public int LPorcentajeMoraG1 { get; set; } = 0;
    public int LTerrenoG1 { get; set; } = 0;
    public int LMisTerrenosConMora { get; set; } = 0;
    public int LTotalTerrenosSinMora { get; set; } = 0;
    public decimal DTotalBono { get; set; }
    public int DTotalPagadosLicencia { get; set; } = 0;
    public decimal DTotalBonoLicencia { get; set; } = 0;
    public int LNroSemana { get; set; } = 0;
    public int LSemanaId { get; set; } = 0;

}