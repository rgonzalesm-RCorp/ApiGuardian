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

public class RedContacto
{
    public int Hijo { get; set; }
    public int Padre { get; set; }
}

public class ItemRedSieteNiveles
{
    public int Id { get; set; } = 0;
    public int Hijo { get; set; }

    public int? PadreN1 { get; set; } = 0;
    public int? PadreN2 { get; set; } = 0;
    public int? PadreN3 { get; set; } = 0;
    public int? PadreN4 { get; set; } = 0;
    public int? PadreN5 { get; set; } = 0;
    public int? PadreN6 { get; set; } = 0;
    public int? PadreN7 { get; set; } = 0;
}