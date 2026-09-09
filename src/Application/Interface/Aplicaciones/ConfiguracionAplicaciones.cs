namespace ApiGuardian.Application.Interfaces;

public class ConfiguracionAplicaciones
{
    public decimal MontoMinimoParaPagoACuenta { get; set; } = 20m;
    public int LimiteErroresFacturacion { get; set; } = 3;
    // Por seguridad, la facturación externa se debe habilitar explícitamente por ambiente.
    public bool HabilitarPasarelaFacturacion { get; set; } = false;
    public bool RequerirCoincidenciaCantidadComisionados { get; set; } = true;
    public int TiempoEsperaComandoSegundos { get; set; } = 180;
    public int TiempoEsperaPagoSegundos { get; set; } = 180;
    public List<int> EmpresasSinFacturacion { get; set; } = [32, 33];
    public ConfiguracionPagosBolivianosAplicaciones PagosBolivianos { get; set; } = new();
    public ConfiguracionFacturacionAplicaciones Facturacion { get; set; } = new();
}

public class ConfiguracionPagosBolivianosAplicaciones
{
    // Permite revertir únicamente esta funcionalidad desde configuración, sin alterar pagos históricos.
    public bool Habilitado { get; set; } = true;
    public decimal TipoCambio { get; set; } = 6.96m;
    public List<int> Empresas { get; set; } = [32, 33];
}

public class ConfiguracionFacturacionAplicaciones
{
    public string PuntoFinal { get; set; } = string.Empty;
    // Si queda vacío, se obtiene a partir de PuntoFinal según el WSDL de Sion.
    public string AccionSoap { get; set; } = string.Empty;
    public string Usuario { get; set; } = "Comisiones";
    public string Contrasena { get; set; } = string.Empty;
    public string CodigoAgente { get; set; } = "-13";
    public string LlaveConexion { get; set; } = string.Empty;
    public int TiempoEsperaSegundos { get; set; } = 120;
}
