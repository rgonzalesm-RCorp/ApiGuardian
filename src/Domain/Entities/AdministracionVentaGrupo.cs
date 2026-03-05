public class ItemVentaGrupo
{
    public string usuario { get; set; } = string.Empty;
    public int lventagrupo_id { get; set; } = 0;
    public DateTime dtfechacalculo { get; set; } = DateTime.Now;
    public int lciclo_id { get; set; }
    public int lcontacto_id { get; set; }
    public int lgeneracion { get; set; }
    public int lasesor_id { get; set; }
    public decimal dporcentajecomision { get; set; }
    public decimal dcomision { get; set; }
    public decimal dventapersonal { get; set; }
    public decimal dventapersonalinicial { get; set; }
    public int lcontrato_id { get; set; }
    public int lnrosemana { get; set; }
    public int lsemana_id { get; set; }
}