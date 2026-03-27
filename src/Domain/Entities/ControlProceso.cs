public class ItemControlProceso
{
    public int ControlProcesoId { get; set; }

    public string Paso { get; set; } = string.Empty;
    public int lciclo_id { get; set; }
    public DateTime? Inicio { get; set; }
    public DateTime? Fin { get; set; } = null;

    public int Estado { get; set; } = 1;

    public DateTime? FechaAdd { get; set; } = DateTime.Now;
    public string UsuarioAdd { get; set; } = string.Empty;

    public DateTime? FechaMod { get; set; } = DateTime.Now;
    public string UsuarioMod { get; set; } = string.Empty;
}