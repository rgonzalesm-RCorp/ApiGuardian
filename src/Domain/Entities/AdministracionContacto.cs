public class AdministracionContacto
{
    public long LContactoId { get; set; }
        public string SNombreCompleto { get; set; } = string.Empty;
        public string SCedulaIdentidad { get; set; } = string.Empty;
        public string LNit { get; set; } = string.Empty;
        public string STelefonoFijo { get; set; } = string.Empty;
        public string STelefonoMovil { get; set; } = string.Empty;
        public string STelefonoOficina { get; set; } = string.Empty;
        public string SCorreoElectronico { get; set; } = string.Empty;
        public string SCiudad { get; set; } = string.Empty;
        public long LPatrocinanteId { get; set; }
        public string PatrocinanteNombre { get; set; } = string.Empty;
        public long NNivelId { get; set; }
        public string NivelNombre { get; set; } = string.Empty;
        public long LPaisId { get; set; }
        public string PaisNombre { get; set; } = string.Empty;
        public int LBancoId { get; set; } 
        public string Banco { get; set; } = string.Empty;
        public string Comentario { get; set; } = string.Empty;
        public string Moneda { get; set; } = string.Empty;
}
