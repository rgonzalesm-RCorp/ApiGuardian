namespace ApiGuardian.Application.Interfaces;

public class ConfiguracionAplicaciones
{
    public decimal MontoMinimoParaPagoACuenta { get; set; } = 20m;
    public int LimiteErroresFacturacion { get; set; } = 3;
    public bool HabilitarPasarelaFacturacion { get; set; } = true;
    public bool RequerirCoincidenciaCantidadComisionados { get; set; } = true;
    public int TiempoEsperaComandoSegundos { get; set; } = 180;
    public int TiempoEsperaPagoSegundos { get; set; } = 180;
    public ConfiguracionFacturacionAplicaciones Facturacion { get; set; } = new();
}

public class ConfiguracionFacturacionAplicaciones
{
    public string PuntoFinal { get; set; } = string.Empty;
    public string AccionSoap { get; set; } = "urn:gruposion.com.bo#wsGenerarFacturaRecibo";
    public string Usuario { get; set; } = "Comisiones";
    public string Contrasena { get; set; } = string.Empty;
    public string CodigoAgente { get; set; } = "-13";
    public string LlaveConexion { get; set; } = string.Empty;
    public int TiempoEsperaSegundos { get; set; } = 120;
}
