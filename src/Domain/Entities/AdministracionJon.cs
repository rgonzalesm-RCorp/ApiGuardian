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
    public DateTime fechaVenta { get; set; } = DateTime.Now;
    public DateTime fechaCalculo { get; set; } = DateTime.Now;
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

public class ItemComisionVentaGrupoDto
{
    public string NombreVendedor { get; set; } = "";

    public int LVendedorId { get; set; }

    public int LGanadorId { get; set; }

    public string nombreGanador { get; set; } = "";

    public string SNroVenta { get; set; } ="";

    public int LContratoId { get; set; }

    public decimal DCuotaInicial { get; set; }

    public DateTime DtFecha { get; set; }

    public int Nivel { get; set; }

    public decimal Porcentaje { get; set; }

    public decimal Comision { get; set; }

    public bool EsCero { get; set; }
}

