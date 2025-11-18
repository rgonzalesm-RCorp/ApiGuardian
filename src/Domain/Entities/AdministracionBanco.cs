public class ListaAdministracionBanco
{
    public int LBancoId { get; set; }
    public string? SNombre { get; set; }
    public string? SCodigo { get; set; }
    public int Estado { get; set; }
    public int LMonedaId { get; set; }
    public DateTime FechaAdd { get; set; }
    public string? UsuarioAdd { get; set; }
    public DateTime? FechaMod { get; set; }
    public string? UsuarioMod { get; set; }
    public string? Moneda { get; set; }
}
public class AdministracionBanco
{
    public int LBancoId { get; set; }
    public string? SNombre { get; set; }
    public string? SCodigo { get; set; }
    public int LMonedaId { get; set; }
    public int Estado { get; set; }
    public string? Usuario { get; set; }
}
public class ListaMoneda
{
    public int LMonedaId { get; set; }
    public string? SNombre { get; set; }
}
