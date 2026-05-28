namespace ApiGuardian.Domain.Entities;

public class AplicacionesExecuteRequest
{
    public int LCicloId { get; set; }
}

public class AplicacionesPreviewResponse
{
    public int LCicloId { get; set; }
    public bool Preview { get; set; }
    public bool AplicacionesComisionadoExiste { get; set; }
    public bool CompanyCommissionsExist { get; set; }
    public bool RequiereRegistrarComisionados { get; set; }
    public bool ErrorGrave { get; set; }
    public string ErrorGraveMensaje { get; set; } = string.Empty;
    public int TotalComisionadosGuardian { get; set; }
    public int TotalPendientes { get; set; }
    public decimal TotalPendienteAplicar { get; set; }
    public List<string> Notas { get; set; } = new();
    public List<AplicacionesAgentResult> Comisionados { get; set; } = new();
}

public class AplicacionesApplyResponse : AplicacionesPreviewResponse
{
    public int TotalProcesados { get; set; }
    public int TotalErrores { get; set; }
}

public class AplicacionesAgentResult
{
    public string Carnet { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public decimal TotalAplicar { get; set; }
    public decimal SaldoInicial { get; set; }
    public decimal SaldoFinal { get; set; }
    public bool Procesado { get; set; }
    public bool ErrorGrave { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public List<AplicacionesOperation> Operaciones { get; set; } = new();
}

public class AplicacionesOperation
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
