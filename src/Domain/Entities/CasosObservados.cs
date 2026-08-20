public class ItemCasoObservado
{
    public int CasoObservadoId { get; set; }
    public int LCicloId { get; set; }
    public string TipoCaso { get; set; } = string.Empty;
    public int? LContratoId { get; set; }
    public string NroVenta { get; set; } = string.Empty;
    public DateTime? FechaVenta { get; set; }
    public int? ClienteId { get; set; }
    public string Cliente { get; set; } = string.Empty;
    public string ClienteDocId { get; set; } = string.Empty;
    public string ClienteBaja { get; set; } = string.Empty;
    public DateTime? ClienteFechaRegistro { get; set; }
    public string ClienteCodigo { get; set; } = string.Empty;
    public int? ClientePatrocinadorId { get; set; }
    public int? PatrocinadorId { get; set; }
    public string Patrocinador { get; set; } = string.Empty;
    public string PatrocinadorDocId { get; set; } = string.Empty;
    public string PatrocinadorBaja { get; set; } = string.Empty;
    public int? VendedorId { get; set; }
    public string Vendedor { get; set; } = string.Empty;
    public string VendedorDocId { get; set; } = string.Empty;
    public string VendedorBaja { get; set; } = string.Empty;
    public DateTime? VendedorFechaRegistro { get; set; }
    public string VendedorCodigo { get; set; } = string.Empty;
    public int? ContratoAsesorId { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string Estado { get; set; } = "PENDIENTE";
}

public class CasosObservadosResumen
{
    public int TotalCasos { get; set; }
    public int CasosPendientes { get; set; }
    public int CasosRevisados { get; set; }
    public int DoblePatrocinio { get; set; }
    public int VendedoresDadosBaja { get; set; }
    public int ClientesDadosBaja { get; set; }
    public int VendedoresSinVentasUnAnio { get; set; }
    public int ClientesSinComprasUnAnio { get; set; }
}
