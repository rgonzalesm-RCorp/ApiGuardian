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
    public List<AdministracionBonoResidualDetalle> Detalle { get; set; } = new List<AdministracionBonoResidualDetalle>();

}
public class AdministracionBonoResidualDetalle
{
    public long LbonoresidualDetalleId { get; set; }
    public int LbonoresidualId { get; set; }
    public int LempresaId { get; set; }
    public int LcomplejoId { get; set; }
    public int Nivel { get; set; }
    public string Producto { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public decimal PorcentajeComision { get; set; }
    public decimal Comision { get; set; }
}

public class ItemBonoCompleto
{
    public int Id { get; set; }
    public int Nivel { get; set; }
    public int LContactoId { get; set; }
    public int LContactoIdHijo  { get; set; }
    public string DocumentoHijo  { get; set; } = string.Empty;
    public int LComplejoId { get; set; }
    public decimal TotalBono { get; set; }
    public decimal TotalPago { get; set; }
    public int Cantidad  { get; set; }
    public int LCicloId  { get; set; }

}
public class ItemRedEmpresaComplejo
{
    public int LRedEmpresaComplejoId { get; set; }
    public int LCicloId  { get; set; }
    public int LContactoId { get; set; }
    public int LComplejoId { get; set; }
    public decimal DMonto { get; set; }
    public string Usuario { get; set; } = "";
}