public class CuentaBanco
{
    public int LContactoId { get; set; }
    public int CTieneCuenta { get; set; }
    public string? LCuentaBanco { get; set; }
    public string? LCodigoBanco { get; set; }
    public int CBaja { get; set; }
    public string? SNombreCompleto { get; set; }
    public DateTime FechaRegistro { get; set; }
    public string? SCedulaIdentidad { get; set; }
    public string? LNit { get; set; }
}
public class DataCuentaBanco
{
    public int LContactoId { get; set; }
    public int CTieneCuenta { get; set; }
    public int LCuentaBanco { get; set; }
    public int LCodigoBanco { get; set; }
    public int CBaja { get; set; }
    public string? SNombreCompleto { get; set; }
    public string? FechaRegistro { get; set; }
    public string? SCedulaIdentidad { get; set; }
    public string? LNit { get; set; }
    public string? usuario { get; set; }
}
