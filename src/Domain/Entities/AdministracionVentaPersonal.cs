public class AdministracionVentaPersonal
{
    public int lventapersonal_id { get; set; }
    public string susuarioadd { get; set; } = string.Empty;
    public DateTime dtfechaadd { get; set; } = DateTime.Now;
    public string susuariomod { get; set; } = string.Empty;
    public DateTime dtfechamod { get; set; } = DateTime.Now;
    public DateTime dtfechacalculo { get; set; } = DateTime.Now;
    public int lciclo_id { get; set; }
    public long lcontacto_id { get; set; }
    public decimal dpreciolote { get; set; }
    public decimal dporcentajecomision { get; set; }
    public decimal dcomision { get; set; }
    public long lcontrato_id { get; set; }
    public decimal ddescuentoatencion { get; set; } = 0;
    public decimal ddescuentotramite { get; set; } = 0;
    public decimal ddescuentoreferido { get; set; } = 0;
    public int latencion_id { get; set; } = 0;
    public int ltramite_id { get; set; } = 0;
    public int lreferido_id { get; set; } =0;
    public decimal ddescuentolote { get; set; } = 0;
    public string snotadescuentolote { get; set; } = string.Empty;
    public bool cventapagada { get; set; } = false;
    public DateTime dtfechapago { get; set; } = DateTime.Now;
    public int lnrosemana { get; set; }
    public decimal dporcentajeretencion { get; set; } = 0;
    public decimal dmontoRetencion { get; set; } = 0;
    public bool cpresentafactura { get; set; } = false;
    public decimal dtotaapagar { get; set; } = 0;
    public int lsemana_id { get; set; }
}