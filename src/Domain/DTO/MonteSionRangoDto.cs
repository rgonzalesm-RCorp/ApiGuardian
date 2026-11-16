namespace ApiGuardian.Domain.DTO;

public sealed class MonteSionRangoConfiguracion
{
    public int NivelId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal ProduccionRequerida { get; set; }
    public decimal ProduccionHasta { get; set; }
    public decimal VmePorcentaje { get; set; }
    // Producción con corte de VME: tope de producción válida por equipo. 0 = sin corte.
    public decimal ProduccionCorteVme { get; set; }
    public decimal Bono { get; set; }
    // Incentivo (pago único). 0 = sin incentivo.
    public decimal Incentivo { get; set; }
    public decimal PorcentajeLiderazgo { get; set; }
    public string Moneda { get; set; } = string.Empty;
}

// El plan de carrera (umbrales, VME, bonos e incentivos) se lee de administracionnivelplancarrera.
public sealed class MonteSionOpciones
{
    public int ProfundidadMaxima { get; set; }
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
    public int NivelActualId { get; set; }
    public string RangoActual { get; set; } = "Asesor Comercial";
}

public sealed class MonteSionRangoHistorico
{
    public int EmprendedorId { get; set; }
    public string Rango { get; set; } = string.Empty;
}

// Relación de nivel 1 de la red comprimida del ciclo (vendedor -> su upline directo).
// Se usa para ubicar el equipo (línea directa) de cada venta, aunque los intermedios no tengan ventas.
public sealed class MonteSionRedNivel1
{
    public int VendedorId { get; set; }
    public int PatrocinadorId { get; set; }
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
    public decimal VmePorcentaje { get; set; }
    public decimal PorcentajeLiderazgo { get; set; }
    public decimal? LimiteMaximoPorEquipo { get; set; }
    public decimal ProduccionValidaTotal { get; set; }
    public bool Califica { get; set; }
    public List<MonteSionDetalleEquipo> Equipos { get; set; } = [];
}

public sealed class MonteSionBeneficioRango
{
    public string Moneda { get; set; } = string.Empty;
    public decimal BonoConfigurado { get; set; }
    public decimal IncentivoConfiguradoMonto { get; set; }
    public string? IncentivoConfigurado { get; set; }
    public bool PrimeraCalificacion { get; set; }
    public bool EsRecalificacion { get; set; }
    public decimal BonoAPagar { get; set; }
    public decimal IncentivoAPagar { get; set; }
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
    public string Moneda { get; set; } = string.Empty;
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

// Nivel de administracionnivel (nombre, % liderazgo) con su plan de carrera vigente
// para una moneda (administracionnivelplancarrera).
public sealed class MonteSionNivel
{
    public int NivelId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal PorcentajeLiderazgo { get; set; }
    // NULL cuando el nivel no tiene configuración vigente para la moneda.
    public int? PlanCarreraId { get; set; }
    public string Moneda { get; set; } = string.Empty;
    public decimal ProduccionDesde { get; set; }
    public decimal ProduccionHasta { get; set; }
    public decimal VmePorcentaje { get; set; }
    public decimal ProduccionCorteVme { get; set; }
    public decimal Bono { get; set; }
    public decimal Incentivo { get; set; }
}

public sealed class MonteSionGuardadoResultado
{
    public int CicloId { get; set; }
    public int RegistrosPlan { get; set; }
    public int RegistrosReporte { get; set; }
    public int Ascensos { get; set; }
}

public sealed class MonteSionMigracionResultado
{
    public int Planes { get; set; }
    public int Reportes { get; set; }
    public int NuevosRangos { get; set; }
}
