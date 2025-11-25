public class ListaAdministracionContacto
{
    public long LContactoId { get; set; }
        public string SNombreCompleto { get; set; } = string.Empty;
        public string SCedulaIdentidad { get; set; } = string.Empty;
        public string LNit { get; set; } = string.Empty;
        public string STelefonoFijo { get; set; } = string.Empty;
        public string STelefonoMovil { get; set; } = string.Empty;
        public string STelefonoOficina { get; set; } = string.Empty;
        //public string SCorreoElectronico { get; set; } = string.Empty;
        public string SCiudad { get; set; } = string.Empty;
        public long LPatrocinanteId { get; set; }
        public string PatrocinanteNombre { get; set; } = string.Empty;
        public long NNivelId { get; set; }
        public string NivelNombre { get; set; } = string.Empty;
        public long LPaisId { get; set; }
        public string PaisNombre { get; set; } = string.Empty;
        //public int LBancoId { get; set; } 
        //public string Banco { get; set; } = string.Empty;
        public string Comentario { get; set; } = string.Empty;
        //public string Moneda { get; set; } = string.Empty;
        public string SCorreo { get; set; } = string.Empty;
        public string SDireccion { get; set; } = string.Empty;
        public DateTime FechaNacimiento { get; set; }
        public DateTime FechaRegistro { get; set; }
        public string SCodigo { get; set; } = string.Empty;

}

public class AdministracionContacto
{
    public string? Usuario { get; set; }
    public string? NombreCompleto { get; set; }
    public string? CedulaIdentidad { get; set; }
    public string? TelefonoFijo { get; set; }
    public string? TelefonoMovil { get; set; }
    public string? CorreoElectronico { get; set; }
    public string? Ciudad { get; set; }
    public long PaisId { get; set; }
    public long? PatrocinanteId { get; set; }
    public long? NivelId { get; set; }
    public string? Comentario { get; set; }
    public string? TelefonoOficina { get; set; }
    public string? Direccion { get; set; }
    public long? Nit { get; set; }
    public DateTime FechaRegistro { get; set; }
    public DateTime FechaNacimiento { get; set; }
    public int LContactoId { get; set; }
}
public class AdministracionContactoBaja
{
    public long TipoBajaId { get; set; }
    public string? MotivoBaja { get; set; }
    public string? Usuario { get; set; }
    public int LContactoId { get; set; }
}