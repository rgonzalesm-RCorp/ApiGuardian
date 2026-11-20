using System.Globalization;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.DTO;
using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Api.Services.Simulacion;

// Monte Sion de la simulación. Lógica copiada de MonteSionService.CalcularRangosAsync y GuardarRangosAsync,
// sin depender del servicio del proceso actual (R2): un cambio aquí no afecta a MonteSion/rangos/guardar.
// Lecturas: MonteSionRepository (solo ObtenerDatosCicloAsync y ObtenerNivelesAsync, ambas de solo lectura).
// Escritura: SimulacionRepositorio.ReemplazarMonteSionAsync.
// Diferencias con el proceso actual:
//  - La moneda llega del ciclo de la corrida (ya validada en el paso MONEDA CICLO).
//  - Evalúa a los contactos de la red comprimida del ciclo, sin exigir venta personal en el ciclo.
//  - Que nadie califique no es un error: la corrida sigue con 0 registros.
//  - No actualiza administracioncontacto.lnivel_id: cada corrida parte del rango real del contacto.
public sealed partial class SimulacionService
{
    private const string MonteSionRangoSinCalificacion = "Sin calificación";
    private static readonly NumberFormatInfo MonteSionFormatoMonto = new()
    {
        NumberGroupSeparator = ".",
        NumberDecimalSeparator = ",",
    };

    public async Task<(bool Exito, string Mensaje, int Rangos, int Ascensos)> MonteSionAsync(
        string logId, string usuario, int lCicloId, MonedaCiclo moneda)
    {
        var calculo = await CalcularRangosMonteSionAsync(logId, usuario, lCicloId, moneda);
        if (!calculo.Exito)
            return (false, calculo.Mensaje, 0, 0);

        var guardado = await _repositorio.ReemplazarMonteSionAsync(logId, lCicloId, usuario, calculo.Resultados);
        if (!guardado.Success)
            return (false, guardado.Mensaje, 0, 0);

        return (true, $"Monte Sion: {guardado.Rangos} rangos, {guardado.Ascensos} ascensos.", guardado.Rangos, guardado.Ascensos);
    }

    // Copia de MonteSionService.CalcularRangosAsync.
    private async Task<(bool Exito, string Mensaje, List<MonteSionResultadoRango> Resultados)> CalcularRangosMonteSionAsync(
        string logId, string usuario, int cicloId, MonedaCiclo moneda)
    {
        var vacio = new List<MonteSionResultadoRango>();
        if (_monteSionOpciones.ProfundidadMaxima <= 0)
            return (false, "La configuración de rangos MonteSion no es válida.", vacio);

        var datos = await _monteSionLectura.ObtenerDatosCicloAsync(logId, cicloId);
        if (!datos.Success)
            return (false, datos.Mensaje, vacio);

        // Regla "solo especiales": quien solo tiene ventas especiales en el ciclo queda fuera de la evaluación de rangos.
        var soloEspeciales = await _soloEspeciales.ObtenerAsync(logId, usuario, cicloId);
        if (!soloEspeciales.Success)
            return (false, soloEspeciales.Mensaje, vacio);

        var nivelesResultado = await _monteSionLectura.ObtenerNivelesAsync(logId, moneda);
        if (!nivelesResultado.Success)
            return (false, nivelesResultado.Mensaje, vacio);

        var contactosPorId = datos.Contactos.ToDictionary(contacto => contacto.EmprendedorId);
        // Upline directo (nivel 1) de cada contacto en la red comprimida del ciclo, con TODA la red.
        var padrePorAsesor = datos.RedNivel1
            .Where(relacion => relacion.VendedorId > 0 && relacion.PatrocinadorId > 0)
            .GroupBy(relacion => relacion.VendedorId)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.First().PatrocinadorId);
        var ventasPorEmprendedor = datos.Producciones
            .Where(venta => venta.Nivel >= 1 && venta.Nivel <= _monteSionOpciones.ProfundidadMaxima)
            .GroupBy(venta => venta.EmprendedorId)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.ToList());
        var rangos = nivelesResultado.Data
            .Where(nivel => nivel.NivelId > 0)
            .OrderByDescending(nivel => nivel.NivelId)
            .Select(nivel => new MonteSionRangoConfiguracion
            {
                NivelId = nivel.NivelId,
                Nombre = nivel.Nombre,
                ProduccionRequerida = nivel.ProduccionDesde,
                ProduccionHasta = nivel.ProduccionHasta,
                VmePorcentaje = nivel.VmePorcentaje,
                ProduccionCorteVme = nivel.ProduccionCorteVme,
                Bono = nivel.Bono,
                Incentivo = nivel.Incentivo,
                PorcentajeLiderazgo = nivel.PorcentajeLiderazgo,
                Moneda = moneda.ToString(),
            })
            .ToList();
        if (rangos.Count == 0)
            return (false, "No existen niveles configurados en administracionnivel.", vacio);
        var rangoEmprendedor = rangos.FirstOrDefault(rango => NormalizarRangoMonteSion(rango.Nombre) == "EMPRENDEDOR");
        if (rangoEmprendedor is null)
            return (false, "No existe el rango Emprendedor en administracionnivel.", vacio);

        var emprendedoresPrimeraVenta = datos.EmprendedoresPrimeraVenta.ToHashSet();
        var vendedoresAntesDeMayo2026 = datos.VendedoresAntesDeMayo2026.ToHashSet();
        // Diferencia con el proceso actual: se evalúa a todos los contactos de la red comprimida del ciclo
        // (como vendedor o como patrocinador), sin exigir venta personal en el ciclo (datos.Evaluables no se usa).
        var idsRedComprimida = datos.RedNivel1
            .SelectMany(relacion => new[] { relacion.VendedorId, relacion.PatrocinadorId })
            .Where(id => id > 0)
            .ToHashSet();
        var evaluables = idsRedComprimida
            .Where(contactosPorId.ContainsKey)
            .Select(id => contactosPorId[id]);
        var resultados = evaluables
            .Where(emprendedor => !soloEspeciales.Contactos.Contains(emprendedor.EmprendedorId))
            .Select(emprendedor => CalcularRangoMonteSion(
                emprendedor,
                cicloId,
                contactosPorId,
                padrePorAsesor,
                ventasPorEmprendedor,
                rangos,
                emprendedor.NivelActualId,
                emprendedor.RangoActual,
                emprendedoresPrimeraVenta.Contains(emprendedor.EmprendedorId),
                vendedoresAntesDeMayo2026.Contains(emprendedor.EmprendedorId),
                rangoEmprendedor))
            // Solo se guardan emprendedores que alcanzaron un rango.
            .Where(resultado => resultado.Califica)
            .ToList();
        foreach (var resultado in resultados)
            resultado.Moneda = moneda.ToString();

        return (true, "Rangos MonteSion calculados correctamente.", resultados);
    }

    // Copia de MonteSionService.CalcularRango.
    private MonteSionResultadoRango CalcularRangoMonteSion(
        MonteSionContactoRed emprendedor,
        int cicloId,
        IReadOnlyDictionary<int, MonteSionContactoRed> contactosPorId,
        IReadOnlyDictionary<int, int> padrePorAsesor,
        IReadOnlyDictionary<int, List<MonteSionProduccionContacto>> ventasPorEmprendedor,
        List<MonteSionRangoConfiguracion> rangos,
        int nivelActualId,
        string rangoActual,
        bool primeraVentaDesdeMayo2026,
        bool tieneVentaAntesDeMayo2026,
        MonteSionRangoConfiguracion rangoEmprendedor)
    {
        var equipos = ObtenerEquiposMonteSion(emprendedor.EmprendedorId, contactosPorId, padrePorAsesor, ventasPorEmprendedor);
        var produccionTotal = equipos.Sum(equipo => equipo.ProduccionReal);

        // Desde mayo de 2026, la primera venta ingresa al vendedor como Emprendedor.
        // Los vendedores con una venta anterior a mayo de 2026 no califican a Emprendedor y empiezan desde Leader.
        if (nivelActualId < rangoEmprendedor.NivelId)
        {
            if (primeraVentaDesdeMayo2026)
            {
                var evaluacionEmprendedor = EvaluarRangoMonteSion(rangoEmprendedor, equipos);
                evaluacionEmprendedor.Califica = true;
                return new MonteSionResultadoRango
                {
                    EmprendedorId = emprendedor.EmprendedorId,
                    Codigo = emprendedor.Codigo,
                    EmprendedorNombre = emprendedor.EmprendedorNombre,
                    FechaRegistro = emprendedor.FechaRegistro,
                    CicloId = cicloId,
                    ProduccionTotalRed = produccionTotal,
                    ProduccionValidaTotal = evaluacionEmprendedor.ProduccionValidaTotal,
                    NivelActualId = nivelActualId,
                    RangoActual = rangoActual,
                    NivelAlcanzadoId = rangoEmprendedor.NivelId,
                    RangoPotencial = rangoEmprendedor.Nombre,
                    RangoFinal = rangoEmprendedor.Nombre,
                    Califica = true,
                    Beneficio = CalcularBeneficioMonteSion(rangoEmprendedor, nivelActualId, rangoEmprendedor.NivelId, rangoActual),
                    RangoEvaluado = evaluacionEmprendedor,
                    Evaluaciones = [evaluacionEmprendedor],
                };
            }

            if (!tieneVentaAntesDeMayo2026)
            {
                return new MonteSionResultadoRango
                {
                    EmprendedorId = emprendedor.EmprendedorId,
                    Codigo = emprendedor.Codigo,
                    EmprendedorNombre = emprendedor.EmprendedorNombre,
                    FechaRegistro = emprendedor.FechaRegistro,
                    CicloId = cicloId,
                    ProduccionTotalRed = produccionTotal,
                    NivelActualId = nivelActualId,
                    RangoActual = rangoActual,
                    RangoPotencial = MonteSionRangoSinCalificacion,
                    RangoFinal = MonteSionRangoSinCalificacion,
                    Califica = false,
                };
            }
        }

        var rangosEvaluables = nivelActualId < rangoEmprendedor.NivelId
            ? rangos.Where(rango => rango.NivelId > rangoEmprendedor.NivelId).ToList()
            : rangos;
        var evaluaciones = new List<MonteSionEvaluacionRango>();
        var final = EvaluarRangosMonteSionRecursivamente(rangosEvaluables, 0, equipos, evaluaciones);
        var potencial = rangosEvaluables.FirstOrDefault(rango => EstaDentroDelRangoMonteSion(produccionTotal, rango));
        var rangoEvaluado = final ?? evaluaciones.LastOrDefault();
        var rangoFinalConfiguracion = final is null
            ? null
            : rangosEvaluables.First(rango => string.Equals(rango.Nombre, final.Rango, StringComparison.OrdinalIgnoreCase));
        var nivelAlcanzadoId = rangoFinalConfiguracion?.NivelId ?? 0;

        return new MonteSionResultadoRango
        {
            EmprendedorId = emprendedor.EmprendedorId,
            Codigo = emprendedor.Codigo,
            EmprendedorNombre = emprendedor.EmprendedorNombre,
            FechaRegistro = emprendedor.FechaRegistro,
            CicloId = cicloId,
            ProduccionTotalRed = produccionTotal,
            ProduccionValidaTotal = rangoEvaluado?.ProduccionValidaTotal ?? 0m,
            NivelActualId = nivelActualId,
            RangoActual = rangoActual,
            NivelAlcanzadoId = nivelAlcanzadoId,
            RangoPotencial = potencial?.Nombre ?? MonteSionRangoSinCalificacion,
            RangoFinal = final?.Rango ?? MonteSionRangoSinCalificacion,
            Califica = final is not null,
            Beneficio = rangoFinalConfiguracion is null
                ? null
                : CalcularBeneficioMonteSion(rangoFinalConfiguracion, nivelActualId, nivelAlcanzadoId, rangoActual),
            RangoEvaluado = rangoEvaluado,
            Evaluaciones = evaluaciones,
        };
    }

    // Copia de MonteSionService.CalcularBeneficio.
    private static MonteSionBeneficioRango CalcularBeneficioMonteSion(
        MonteSionRangoConfiguracion rango,
        int nivelActualId,
        int nivelAlcanzadoId,
        string rangoActual)
    {
        var esAscenso = nivelAlcanzadoId > nivelActualId;
        var esRecalificacion = nivelAlcanzadoId == nivelActualId;
        var tieneIncentivo = rango.Incentivo > 0;
        var incentivoTexto = tieneIncentivo ? TextoIncentivoMonteSion(rango.Incentivo, rango.Moneda) : null;

        var beneficio = new MonteSionBeneficioRango
        {
            Moneda = rango.Moneda,
            BonoConfigurado = rango.Bono,
            IncentivoConfiguradoMonto = rango.Incentivo,
            IncentivoConfigurado = incentivoTexto,
        };

        if (esAscenso && tieneIncentivo)
        {
            beneficio.PrimeraCalificacion = true;
            beneficio.EsRecalificacion = false;
            beneficio.BonoAPagar = 0m;
            beneficio.IncentivoAPagar = rango.Incentivo;
            beneficio.IncentivoAEntregar = incentivoTexto;
            beneficio.Motivo = $"Primera calificación a {rango.Nombre}. El incentivo reemplaza al bono del rango.";
            return beneficio;
        }

        beneficio.BonoAPagar = rango.Bono;
        beneficio.IncentivoAPagar = 0m;
        beneficio.IncentivoAEntregar = null;

        if (esAscenso || esRecalificacion)
        {
            beneficio.PrimeraCalificacion = esAscenso;
            beneficio.EsRecalificacion = esRecalificacion;
            beneficio.Motivo = esAscenso
                ? $"Ascenso a {rango.Nombre}. Corresponde el bono configurado."
                : $"Recalificación en {rango.Nombre}. Corresponde el bono configurado.";
            return beneficio;
        }

        beneficio.PrimeraCalificacion = false;
        beneficio.EsRecalificacion = false;
        beneficio.Motivo = $"Bonificación mensual por calificar a {rango.Nombre}. No corresponde incentivo porque el rango actual ({rangoActual}) es superior.";
        return beneficio;
    }

    private static string TextoIncentivoMonteSion(decimal monto, string moneda) =>
        $"Pago único de {monto.ToString("#,##0.##", MonteSionFormatoMonto)} {SimboloMonedaMonteSion(moneda)}";

    private static string SimboloMonedaMonteSion(string moneda) =>
        string.Equals(moneda, nameof(MonedaCiclo.BS), StringComparison.OrdinalIgnoreCase) ? "Bs" : "$us";

    private static MonteSionEvaluacionRango? EvaluarRangosMonteSionRecursivamente(
        IReadOnlyList<MonteSionRangoConfiguracion> rangos,
        int indice,
        IEnumerable<MonteSionEquipoProduccion> equipos,
        List<MonteSionEvaluacionRango> evaluaciones)
    {
        if (indice >= rangos.Count) return null;

        var evaluacion = EvaluarRangoMonteSion(rangos[indice], equipos);
        evaluaciones.Add(evaluacion);

        return evaluacion.Califica
            ? evaluacion
            : EvaluarRangosMonteSionRecursivamente(rangos, indice + 1, equipos, evaluaciones);
    }

    // Copia de MonteSionService.ObtenerEquipos.
    private List<MonteSionEquipoProduccion> ObtenerEquiposMonteSion(
        int emprendedorId,
        IReadOnlyDictionary<int, MonteSionContactoRed> contactosPorId,
        IReadOnlyDictionary<int, int> padrePorAsesor,
        IReadOnlyDictionary<int, List<MonteSionProduccionContacto>> ventasPorEmprendedor)
    {
        if (!ventasPorEmprendedor.TryGetValue(emprendedorId, out var ventas)) return [];

        var equiposDirectos = ventas
            .Where(venta => venta.Nivel == 1 && venta.VendedorId > 0)
            .Select(venta => venta.VendedorId)
            .Distinct()
            .ToList();
        var equipoDirectoUnico = equiposDirectos.Count == 1 ? equiposDirectos[0] : 0;

        return ventas
            .Where(venta => venta.VendedorId != emprendedorId && venta.Nivel <= _monteSionOpciones.ProfundidadMaxima)
            .Select(venta => new
            {
                Venta = venta,
                EquipoId = ObtenerEquipoDirectoMonteSion(emprendedorId, venta, padrePorAsesor) is var equipoDirecto && equipoDirecto > 0
                    ? equipoDirecto
                    : equipoDirectoUnico > 0
                        ? equipoDirectoUnico
                    : venta.VendedorId,
            })
            .GroupBy(item => item.EquipoId)
            .Select(grupo =>
            {
                contactosPorId.TryGetValue(grupo.Key, out var equipo);
                return new MonteSionEquipoProduccion
                {
                    EquipoId = grupo.Key,
                    EquipoNombre = equipo?.EmprendedorNombre ?? $"Equipo {grupo.Key}",
                    ProduccionReal = grupo.Sum(item => item.Venta.Produccion),
                    VentasPorNivel = grupo.GroupBy(item => item.Venta.Nivel).OrderBy(nivel => nivel.Key).Select(nivel => new MonteSionVentasNivel
                    {
                        Nivel = nivel.Key,
                        NumerosVenta = nivel.Select(item => item.Venta.NumeroVenta).Where(numero => !string.IsNullOrWhiteSpace(numero)).Select(numero => numero!).Distinct().Order().ToList(),
                    }).ToList(),
                };
            }).ToList();
    }

    private static int ObtenerEquipoDirectoMonteSion(
        int emprendedorId,
        MonteSionProduccionContacto venta,
        IReadOnlyDictionary<int, int> padrePorAsesor)
    {
        var equipoId = venta.VendedorId;
        for (var nivel = 1; nivel < venta.Nivel; nivel++)
        {
            if (!padrePorAsesor.TryGetValue(equipoId, out var padre)) return 0;
            equipoId = padre;
        }

        return venta.Nivel == 1 || padrePorAsesor.GetValueOrDefault(equipoId) == emprendedorId
            ? equipoId
            : 0;
    }

    // Copia de MonteSionService.EvaluarRango.
    private static MonteSionEvaluacionRango EvaluarRangoMonteSion(
        MonteSionRangoConfiguracion rango,
        IEnumerable<MonteSionEquipoProduccion> equipos)
    {
        // Producción con corte de VME: tope de producción válida por equipo (0 = sin corte).
        var limite = rango.ProduccionCorteVme > 0
            ? rango.ProduccionCorteVme
            : (decimal?)null;

        var detalleEquipos = equipos.Select(equipo =>
        {
            var produccionValida = limite is null
                ? equipo.ProduccionReal
                : Math.Min(equipo.ProduccionReal, limite.Value);

            return new MonteSionDetalleEquipo
            {
                EquipoId = equipo.EquipoId,
                EquipoNombre = equipo.EquipoNombre,
                ProduccionReal = equipo.ProduccionReal,
                LimiteVme = limite,
                ProduccionValida = produccionValida,
                ProduccionDescartada = equipo.ProduccionReal - produccionValida,
                VentasPorNivel = equipo.VentasPorNivel,
            };
        }).ToList();

        var produccionValidaTotal = detalleEquipos.Sum(equipo => equipo.ProduccionValida);
        return new MonteSionEvaluacionRango
        {
            Rango = rango.Nombre,
            ProduccionRequerida = rango.ProduccionRequerida,
            ProduccionHasta = rango.ProduccionHasta,
            VmePorcentaje = rango.VmePorcentaje,
            PorcentajeLiderazgo = rango.PorcentajeLiderazgo,
            LimiteMaximoPorEquipo = limite,
            ProduccionValidaTotal = produccionValidaTotal,
            Califica = produccionValidaTotal >= rango.ProduccionRequerida,
            Equipos = detalleEquipos,
        };
    }

    private static bool EstaDentroDelRangoMonteSion(decimal produccion, MonteSionRangoConfiguracion rango) =>
        produccion >= rango.ProduccionRequerida
        && (rango.ProduccionHasta <= 0 || produccion <= rango.ProduccionHasta);

    private static string NormalizarRangoMonteSion(string? rango)
    {
        var normalizado = (rango ?? string.Empty)
            .Trim()
            .Normalize(System.Text.NormalizationForm.FormD);
        normalizado = string.Concat(normalizado.Where(caracter =>
            CharUnicodeInfo.GetUnicodeCategory(caracter) != UnicodeCategory.NonSpacingMark))
            .Normalize(System.Text.NormalizationForm.FormC)
            .ToUpperInvariant();

        return normalizado switch
        {
            "LIDER" => "LEADER",
            "ZAFIRO" => "SAPPHIRE",
            "ESMERALDA" => "EMERALD",
            "DIAMANTE" => "DIAMOND",
            "EMBAJADOR REGIONAL" => "REGIONAL AMBASSADOR",
            "EMBAJADOR NACIONAL" => "NATIONAL AMBASSADOR",
            "EMBAJADOR INTERNACIONAL" => "INTERNATIONAL AMBASSADOR",
            _ => normalizado,
        };
    }
}
