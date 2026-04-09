public class ItemContactoActivo
{
    public int LContactoId { get; set; }
    public int LVendedorId { get; set; }
}

public class ItemContactoRed
{
    public int LContratoId { get; set; }
    public int LContactoId { get; set; }
    public int LPatrocinadorId { get; set; }
    public int Nivel { get; set; }
    public string Usuario { get; set; } = "";
    public int LCicloId { get; set; }
}

public class ItemCuotasRed
{
    public string DocId { get; set; } = "";
    public string Cliente { get; set; } = "";
    public string ScedulaIdentidad { get; set; } = "";
    public int LContactoId { get; set; }
    public int LPatrocinanteId { get; set; }
}