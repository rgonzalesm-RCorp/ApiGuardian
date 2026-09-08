namespace ApiGuardian.Domain.DTO;

public sealed class MonteSionRangoConfiguracion
{
    public int NivelId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal ProduccionRequerida { get; set; }
    public decimal ProduccionHasta { get; set; }
    public decimal? VmeMonto { get; set; }
    public decimal BonoUsd { get; set; }
    public decimal IncentivoUsd { get; set; }
    public decimal PorcentajeLiderazgo { get; set; }
    public string? Incentivo { get; set; }
}

public sealed class MonteSionOpciones
{
    public int ProfundidadMaxima { get; set; }
    public List<MonteSionRangoConfiguracion> Rangos { get; set; } = [];
}

public sealed class MonteSionEquipoProduccion
{
    public int EquipoId { get; set; }
    public string EquipoNombre { get; set; } = string.Empty;
    public decimal ProduccionReal { get; set; }
    public List<MonteSionVentasNivel> VentasPorNivel { get; set; } = [];
}

public sealed class MonteSionContactoRed
{
    public int EmprendedorId { get; set; }
    public string? Codigo { get; set; }
    public string EmprendedorNombre { get; set; } = string.Empty;
    public DateTime? FechaRegistro { get; set; }
}

public sealed class MonteSionRangoHistorico
{
    public int EmprendedorId { get; set; }
    public string Rango { get; set; } = string.Empty;
}

public sealed class MonteSionProduccionContacto
{
    public int EmprendedorId { get; set; }
    public int VendedorId { get; set; }
    public int Nivel { get; set; }
    public decimal Produccion { get; set; }
    public string? NumeroVenta { get; set; }
}

public sealed class MonteSionDetalleEquipo
{
    public int EquipoId { get; set; }
    public string EquipoNombre { get; set; } = string.Empty;
    public decimal ProduccionReal { get; set; }
    public decimal? LimiteVme { get; set; }
    public decimal ProduccionValida { get; set; }
    public decimal ProduccionDescartada { get; set; }
    public List<MonteSionVentasNivel> VentasPorNivel { get; set; } = [];
}

public sealed class MonteSionVentasNivel
{
    public int Nivel { get; set; }
    public List<string> NumerosVenta { get; set; } = [];
}

public sealed class MonteSionEvaluacionRango
{
    public string Rango { get; set; } = string.Empty;
    public decimal ProduccionRequerida { get; set; }
    public decimal ProduccionHasta { get; set; }
    public decimal? VmeAplicado { get; set; }
    public decimal PorcentajeLiderazgo { get; set; }
    public decimal? LimiteMaximoPorEquipo { get; set; }
    public decimal ProduccionValidaTotal { get; set; }
    public bool Califica { get; set; }
    public List<MonteSionDetalleEquipo> Equipos { get; set; } = [];
}

public sealed class MonteSionBeneficioRango
{
    public decimal BonoConfiguradoUsd { get; set; }
    public string? IncentivoConfigurado { get; set; }
    public bool PrimeraCalificacion { get; set; }
    public bool EsRecalificacion { get; set; }
    public decimal BonoAPagarUsd { get; set; }
    public string? IncentivoAEntregar { get; set; }
    public string Motivo { get; set; } = string.Empty;
}

public sealed class MonteSionResultadoRango
{
    public int EmprendedorId { get; set; }
    public string? Codigo { get; set; }
    public string EmprendedorNombre { get; set; } = string.Empty;
    public DateTime? FechaRegistro { get; set; }
    public int CicloId { get; set; }
    public decimal ProduccionTotalRed { get; set; }
    public decimal ProduccionValidaTotal { get; set; }
    public int NivelActualId { get; set; }
    public string RangoActual { get; set; } = "Asesor Comercial";
    public int NivelAlcanzadoId { get; set; }
    public string? RangoPotencial { get; set; }
    public string? RangoFinal { get; set; }
    public bool Califica { get; set; }
    public MonteSionBeneficioRango? Beneficio { get; set; }
    public MonteSionEvaluacionRango? RangoEvaluado { get; set; }
    public List<MonteSionEvaluacionRango> Evaluaciones { get; set; } = [];
}

public sealed class MonteSionRangoActual
{
    public int EmprendedorId { get; set; }
    public int NivelActualId { get; set; }
    public string RangoActual { get; set; } = "Asesor Comercial";
}

public sealed class MonteSionNivel
{
    public int NivelId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal ProduccionDesde { get; set; }
    public decimal ProduccionHasta { get; set; }
    public decimal BonoUsd { get; set; }
    public decimal PorcentajeLiderazgo { get; set; }
    public decimal? VmeMonto { get; set; }
}

public sealed class MonteSionGuardadoResultado
{
    public int CicloId { get; set; }
    public int RegistrosPlan { get; set; }
    public int RegistrosReporte { get; set; }
    public int Ascensos { get; set; }
}
