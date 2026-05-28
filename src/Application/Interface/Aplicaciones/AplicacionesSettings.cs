namespace ApiGuardian.Application.Interfaces;

public class AplicacionesSettings
{
    public decimal MinimumAmountForPaymentOnAccount { get; set; } = 20m;
    public int InvoiceFailureLimit { get; set; } = 3;
    public bool EnableInvoiceGateway { get; set; } = true;
    public bool RequireCommissionCountMatch { get; set; } = true;
    public int CommandTimeoutSeconds { get; set; } = 180;
    public int PaymentCommandTimeoutSeconds { get; set; } = 180;
    public AplicacionesFacturacionSettings Facturacion { get; set; } = new();
}

public class AplicacionesFacturacionSettings
{
    public string Endpoint { get; set; } = string.Empty;
    public string SoapAction { get; set; } = "urn:gruposion.com.bo#wsGenerarFacturaRecibo";
    public string Login { get; set; } = "Comisiones";
    public string Password { get; set; } = string.Empty;
    public string AgentCode { get; set; } = "-13";
    public string ConnectionKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 120;
}
