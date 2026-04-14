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

public class ItemControlProcesoPrincipal
{
    public bool status { get; set; }
    public string mensaje { get; set; } = "";
    public bool next { get; set; }
}

public class ItemControlProcesoNext
{
    public bool status { get; set; }
    public string mensajes { get; set; } = "";
    public bool next { get; set; }
    public int id { get; set; }
    public string nombre { get; set; } = "";
    public int orden { get; set; }
    public bool esOblogatoria { get; set; }
}


public static class PasosDiccionario
{
    public const string OBTENER_VENTAS = "OBTENER VENTAS";
    public const string ADICIONAR_VENTAS = "ADICIONAR VENTAS";
    public const string COMISION_DIRECTA = "COMISION DIRECTA";
    public const string COMISION_GRUPO = "COMISION GRUPO";
    public const string OBTENER_CARTERA = "OBTENER CARTERA";
    public const string OBTENER_CUOTAS = "OBTENER CUOTAS";
    public const string OBTENER_EXCEDENTE = "OBTENER EXCEDENTE";
    public const string COMISION_RESIDUAL = "COMISION RESIDUAL";
    public const string COMISION_LIDERAZGO = "COMISION LIDERAZGO";
};
public static class ProcesosDiccionario
{
    public const string COMISIONES = "COMISIONES";
}

