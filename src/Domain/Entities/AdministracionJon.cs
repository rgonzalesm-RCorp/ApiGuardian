public class ItemProcesoJon
{
    public string Proceso { get; set; } = string.Empty;
    public int Estado { get; set; }
    public DateTime Inicio { get; set; }
    public DateTime Fin { get; set; }
}

public class VentaPersonalComisionDto
{
    public long lcontrato_id { get; set; }
    public long lcontacta_id { get; set; }
    public string scedulaidentidad { get; set; } = string.Empty;
    public string snombrecompleto { get; set; } = string.Empty;
    public string proyecto { get; set; } = string.Empty;
    public string snroventa { get; set; } = string.Empty;
    public decimal dprecio { get; set; }
    public decimal PorcentajeInicial { get; set; }
    public decimal inicial { get; set; }
    public decimal dporcentajecomision { get; set; }
    public decimal dcomision { get; set; }
    public int lciclo_id { get; set; }
    public int lsemana_id { get; set; }
    public int lnrosemana { get; set; }
}

