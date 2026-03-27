public class PC_ConfigVtaPersonal
{
    public int PC_ConfigVtaPersonalId { get; set; }
    public int LCiclo_id { get; set; }
    public int Estado { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string Ciclo { get; set; } = string.Empty;
    public List<PC_ConfigVtaPersonalComplejo> Complejos { get; set; } = new List<PC_ConfigVtaPersonalComplejo>();
    public List<PC_ConfigVtaPersonalInicial> Inicials { get; set; } = new List<PC_ConfigVtaPersonalInicial>();
}
public class PC_ConfigVtaPersonalComplejo
{
    public int PC_ConfigVtaPersonalComplejoId { get; set; }
    public int PC_ConfigVtaPersonalId { get; set; }
    public int LComplejo_id { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string Complejo { get; set; } = string.Empty;
    
}
public class PC_ConfigVtaPersonalInicial
{
    public int PC_ConfigVtaPersonalInicialId { get; set; }
    public int? PC_ConfigVtaPersonalId { get; set; }
    public decimal? Inicial_desde { get; set; }
    public decimal? Inicial_hasta { get; set; }
    public decimal? Comision { get; set; }
    public string Usuario { get; set; } = string.Empty;
}
public class PC_VerificarListaComplejos
{
    public int LCicloId { get; set; }
    public int LComplejoId { get; set; }
    public string Complejo { get; set; } = string.Empty;
}
