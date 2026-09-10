namespace ApiGuardian.Domain.Entities;

public sealed class SolicitudMigracionAplicacionesProrrateo
{
    public int Ciclo { get; set; }
    public string FechaInicio { get; set; } = string.Empty;
    public string Usuario { get; set; } = "MIGRACION_APLICACIONES";
}

public sealed class ResultadoMigracionAplicacionesProrrateo
{
    public int RegistrosOrigen { get; set; }
    public int RegistrosListos { get; set; }
    public int RegistrosInsertados { get; set; }
    public int RegistrosDuplicados { get; set; }
    public int ProrrateosOrigen { get; set; }
    public int ProrrateosInsertados { get; set; }
    public List<string> Errores { get; set; } = [];
}
