public class ItemBonoPar
{
    public int LContctoGanadorId { get; set; }
    public string SNombreGanador { get; set; } = "";
    public string SCedulaIdentidadGanador { get; set; } = "";
    public bool EsHabilitado { get; set; }
    public int PersonaQueVendieron { get; set; }
    public decimal Bono { get; set; }
    public int CantidadVenta { get; set; }
    public string VendedoresId { get; set; } = "";
    public string LContratoId { get; set; } = "";
    public string SNroVenta { get; set; } = "";
    public decimal MontoVentas { get; set; }
    public decimal CuotasIniciales { get; set; }
    public int LCicloId { get; set; } = 0;
    public List<ItemBonoParDetalle> ListaDetalleBonoPar { get; set; } = new List<ItemBonoParDetalle>();
}

public class ItemBonoParDetalle
{
    public int LContactoGanadorId { get; set; }
    public int LContactoVendedorId { get; set; }
    public string SNombreVendedor { get; set; } = "";
    public string SCedulaIdentidadVendedor { get; set; } = "";
    public int LContactoClienteId { get; set; }
    public string SNombreCliente { get; set; } = "";
    public string SCedulaCliente { get; set; } = "";
    public int LContratoId { get; set; }
    public DateTime DtFecha { get; set; }
    public string SNroVenta { get; set; } = "";
    public decimal DPrecio { get; set; }
    public decimal DCuotaInicial { get; set; }
}
