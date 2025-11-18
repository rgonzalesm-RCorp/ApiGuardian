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
    public int LBancoId { get; set; }
    public string? Banco { get; set; }
    public string? Comentario { get; set; }
    public string? Moneda { get; set; }
}
public class DataCuentaBanco
{
    public int LContactoId { get; set; }
    public int CTieneCuenta { get; set; }
    public long LCuentaBanco { get; set; }
    public long LCodigoBanco { get; set; }
    public int CBaja { get; set; }
    public string? SNombreCompleto { get; set; }
    public string? FechaRegistro { get; set; }
    public string? SCedulaIdentidad { get; set; }
    public string? LNit { get; set; }
    public string? usuario { get; set; }
    public int LBancoId { get; set; }

}
