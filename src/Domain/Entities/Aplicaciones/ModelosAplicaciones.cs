namespace ApiGuardian.Domain.Entities;

public class SolicitudEjecucionAplicaciones
{
    public int LCicloId { get; set; }
}

public class RespuestaVistaPreviaAplicaciones
{
    public int LCicloId { get; set; }
    public bool VistaPrevia { get; set; }
    public bool AplicacionesComisionadoExiste { get; set; }
    public bool ExistenComisionesPorEmpresa { get; set; }
    public bool RequiereRegistrarComisionados { get; set; }
    public bool ErrorGrave { get; set; }
    public string ErrorGraveMensaje { get; set; } = string.Empty;
    public int TotalComisionadosGuardian { get; set; }
    public int TotalPendientes { get; set; }
    public decimal TotalPendienteAplicar { get; set; }
    public List<string> Notas { get; set; } = new();
    public List<ResultadoComisionadoAplicaciones> Comisionados { get; set; } = new();
}

public class RespuestaEjecucionAplicaciones : RespuestaVistaPreviaAplicaciones
{
    public int TotalProcesados { get; set; }
    public int TotalErrores { get; set; }
}

public class ResultadoComisionadoAplicaciones
{
    public string Carnet { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public decimal TotalAplicar { get; set; }
    public decimal SaldoInicial { get; set; }
    public decimal SaldoFinal { get; set; }
    public bool Procesado { get; set; }
    public bool ErrorGrave { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public List<OperacionAplicacion> Operaciones { get; set; } = new();
}

public class OperacionAplicacion
{
    public string Paso { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public int? EmpresaId { get; set; }
    public int? VentaId { get; set; }
    public string ProductoId { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public string Observacion { get; set; } = string.Empty;
    public int? ReciboId { get; set; }
    public int? FacturaId { get; set; }
    public string TipoPago { get; set; } = string.Empty;
    public string TiempoPago { get; set; } = string.Empty;
}
