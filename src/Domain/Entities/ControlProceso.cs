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
    public string mensajes { get; set; } = "";
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
    public bool esObligatoria { get; set; }
}

public class ItemControlProcesoResumen
{
    public int ProcesoId { get; set; }
    public string Proceso { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public int LCicloId { get; set; }
    public int ProcesoCicloId { get; set; }
    public bool ExisteCiclo { get; set; }
    public bool PuedeResetear { get; set; }
    public string EstadoCiclo { get; set; } = "NO_INICIADO";
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public string SiguientePaso { get; set; } = "";
    public int SiguientePasoOrden { get; set; }
    public List<ItemControlProcesoPasoDetalle> Pasos { get; set; } = new();
    public List<ItemControlProcesoHistorial> Historial { get; set; } = new();
}

public class ItemControlProcesoPasoDetalle
{
    public int PasoId { get; set; }
    public string NombreInterno { get; set; } = "";
    public string Nombre { get; set; } = "";
    public int Orden { get; set; }
    public bool EsObligatorio { get; set; }
    public string Estado { get; set; } = "PENDIENTE";
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public bool EsSiguientePaso { get; set; }
    public bool Ejecutado { get; set; }
}

public class ItemControlProcesoHistorial
{
    public int ProcesoCicloId { get; set; }
    public int NumeroCiclo { get; set; }
    public string Estado { get; set; } = "";
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public bool EsActual { get; set; }
}

public class ControlProcesoConfiguracion
{
    public int ProcesoId { get; set; }
    public string Nombre { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public int Estado { get; set; } = 1;
    public DateTime? FechaCreacion { get; set; }
    public List<ControlProcesoPasoConfiguracion> Pasos { get; set; } = new();
}

public class ControlProcesoPasoConfiguracion
{
    public int PasoId { get; set; }
    public int ProcesoId { get; set; }
    public string Referencia { get; set; } = "";
    public string Nombre { get; set; } = "";
    public int Orden { get; set; }
    public bool EsObligatorio { get; set; } = true;
    public int Estado { get; set; } = 1;
    public List<string> DependenciasReferencia { get; set; } = new();
    public List<ControlProcesoDependenciaConfiguracion> Dependencias { get; set; } = new();
}

public class ControlProcesoDependenciaConfiguracion
{
    public int DependenciaId { get; set; }
    public int PasoId { get; set; }
    public int PasoRequeridoId { get; set; }
    public string PasoRequeridoNombre { get; set; } = "";
    public string PasoRequeridoReferencia { get; set; } = "";
}


public static class PasosDiccionario
{
    public const string OBTENER_VENTAS = "OBTENER VENTAS";
    public const string ADICIONAR_VENTAS = "ADICIONAR VENTAS";
    public const string COMISION_DIRECTA = "COMISION DIRECTA";
    public const string RED_COMPRIMIDA = "RED COMPRIMIDA";
    public const string RED_COMPLETA = "RED COMPLETA";
    public const string COMISION_GRUPO = "COMISION GRUPO";
    public const string OBTENER_CARTERA = "OBTENER CARTERA";
    public const string OBTENER_CUOTAS = "OBTENER CUOTAS";
    public const string OBTENER_EXCEDENTE = "OBTENER EXCEDENTE";
    public const string COMISION_RESIDUAL = "COMISION RESIDUAL";
    public const string COMISION_LIDERAZGO = "COMISION LIDERAZGO";
    public const string COMISION_VENTA_RESIDUAL = "COMISION VENTA RESIDUAL";
    public const string BONO_PAR = "BONO PAR";
    public const string VENTAS_ESPECIALES = "VENTAS ESPECIALES";

    public static bool EsBonoPar(string? paso)
    {
        return string.Equals(paso, BONO_PAR, StringComparison.OrdinalIgnoreCase)
            || string.Equals(paso, COMISION_LIDERAZGO, StringComparison.OrdinalIgnoreCase);
    }

    public static string ObtenerNombreVisual(string? paso)
    {
        if (EsBonoPar(paso))
        {
            return BONO_PAR;
        }

        return paso ?? string.Empty;
    }
};
public static class ProcesosDiccionario
{
    public const string COMISIONES = "COMISIONES";
}
public static class TiposContratosDiccionario
{
    public static class TiposContratosDiccionarioGrd
    {
        public const int CREDITO = 1;
        public const int CONTADO = 2;
        public const int INTERCAMBIO = 3;
        public const int FTV = 4;
        public const int AMORTIZACION = 5;
        public const int UPGRADE = 6;
        public const int RECUPERACION = 7;
        public const int RECOMPRA = 8;
    }
    public static class TiposContratosDiccionarioCnx
    {
        public const int CREDITO = 0;
        public const int CONTADO = 0;
        public const int INTERCAMBIO = 2;
        public const int AMORTIZACION = 0;
        public const int UPGRADE = 7;
        public const int RECUPERACION = 5;
        public const int RECOMPRA = 6;
    }

    public class TipoComision
    {
        public int Cnx { get; set; }
        public int Grd { get; set; }
    }
    private static readonly List<TipoComision> Contratos = new()
    {
         new TipoComision { Cnx = 2, Grd = 3 },
         new TipoComision { Cnx = 5, Grd = 7 },
         new TipoComision { Cnx = 6, Grd = 8 },
         new TipoComision { Cnx = 7, Grd = 6 },
    };

    public static int ObtenerGrd(int cnx, bool esContado, bool esCredito)
    {
        if (esContado && cnx <= 1)
        {
            return 2;
        }else if (esCredito && cnx <= 1)
        {
            return 1;
        }else{

            var contrato = Contratos.FirstOrDefault(c => c.Cnx == cnx);
            return contrato?.Grd ?? 0;
        }
    }
}