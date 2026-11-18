namespace ApiGuardian.Models;

public sealed class ReporteMensualOpciones
{
    /// <summary>Tipo de cambio SUS -> BS por ciclo (clave: lCicloId).</summary>
    public Dictionary<int, decimal> TipoCambioPorCiclo { get; set; } = new();
}
