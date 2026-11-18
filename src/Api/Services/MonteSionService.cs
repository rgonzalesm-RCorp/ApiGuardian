using System.Globalization;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.DTO;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace CleanDapperApi.Api.Services;

public sealed class MonteSionService : IMonteSionService
{
    private const string RangoSinCalificacion = "Sin calificación";
    private static readonly NumberFormatInfo FormatoMonto = new()
    {
        NumberGroupSeparator = ".",
        NumberDecimalSeparator = ",",
    };
    private readonly IMonteSionRepository _repository;
    private readonly IAdministracionCicloRepository _cicloRepository;
    private readonly IContactosSoloEspecialesService _soloEspecialesService;
    private readonly MonteSionOpciones _opciones;

    public MonteSionService(
        IMonteSionRepository repository,
        IAdministracionCicloRepository cicloRepository,
        IContactosSoloEspecialesService soloEspecialesService,
        IOptions<MonteSionOpciones> opciones)
    {
        _soloEspecialesService = soloEspecialesService;
        _repository = repository;
        _cicloRepository = cicloRepository;
        _opciones = opciones.Value;
    }

    public async Task<(bool Success, string Mensaje, IEnumerable<MonteSionResultadoRango> Data)> CalcularRangosAsync(int cicloId)
    {
        if (cicloId <= 0)
            return (false, "El ciclo es obligatorio.", Enumerable.Empty<MonteSionResultadoRango>());

        if (_opciones.ProfundidadMaxima <= 0)
            return (false, "La configuración de rangos MonteSion no es válida.", Enumerable.Empty<MonteSionResultadoRango>());

        // La moneda del ciclo define qué plan de carrera (umbrales, VME, bonos e incentivos) se aplica.
        var monedaCiclo = await _cicloRepository.ObtenerMonedaAsync(Guid.NewGuid().ToString("N"), cicloId);
        if (!monedaCiclo.Success)
            return (false, monedaCiclo.Mensaje, Enumerable.Empty<MonteSionResultadoRango>());
        var moneda = monedaCiclo.Moneda;

        var datos = await _repository.ObtenerDatosCicloAsync(Guid.NewGuid().ToString("N"), cicloId);

        if (!datos.Success)
            return (false, datos.Mensaje, Enumerable.Empty<MonteSionResultadoRango>());

        // Regla "solo especiales": quien solo tiene ventas especiales en el ciclo queda fuera de la evaluación de rangos.
        var soloEspeciales = await _soloEspecialesService.ObtenerAsync(Guid.NewGuid().ToString("N"), "MonteSion", cicloId);
        if (!soloEspeciales.Success)
            return (false, soloEspeciales.Mensaje, Enumerable.Empty<MonteSionResultadoRango>());

        var nivelesResultado = await _repository.ObtenerNivelesAsync(Guid.NewGuid().ToString("N"), moneda);
        if (!nivelesResultado.Success)
            return (false, nivelesResultado.Mensaje, Enumerable.Empty<MonteSionResultadoRango>());

        var contactos = datos.Contactos.ToList();
        var contactosPorId = contactos.ToDictionary(contacto => contacto.EmprendedorId);
        // Upline directo (nivel 1) de cada contacto en la red comprimida del ciclo. Se arma con TODA la red,
        // no solo con las filas que tienen ventas: si un intermedio no vendió, la cadena hasta la línea
        // directa del emprendedor no se debe cortar (si se corta, las ventas quedan como equipos sueltos
        // y el corte VME no se aplica).
        var padrePorAsesor = datos.RedNivel1
            .Where(relacion => relacion.VendedorId > 0 && relacion.PatrocinadorId > 0)
            .GroupBy(relacion => relacion.VendedorId)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.First().PatrocinadorId);
        var ventasPorEmprendedor = datos.Producciones
            .Where(venta => venta.Nivel >= 1 && venta.Nivel <= _opciones.ProfundidadMaxima)
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
            return (false, "No existen niveles configurados en administracionnivel.", Enumerable.Empty<MonteSionResultadoRango>());
        var rangoEmprendedor = rangos.FirstOrDefault(rango => NormalizarRango(rango.Nombre) == "EMPRENDEDOR");
        if (rangoEmprendedor is null)
            return (false, "No existe el rango Emprendedor en administracionnivel.", Enumerable.Empty<MonteSionResultadoRango>());

        var emprendedoresPrimeraVenta = datos.EmprendedoresPrimeraVenta.ToHashSet();
        var vendedoresAntesDeMayo2026 = datos.VendedoresAntesDeMayo2026.ToHashSet();
        var resultados = datos.Evaluables
            .Where(emprendedor => !soloEspeciales.Contactos.Contains(emprendedor.EmprendedorId))
            .GroupBy(emprendedor => emprendedor.EmprendedorId)
            .Select(grupo => grupo.First())
            .Select(emprendedor =>
            {
                var resultado = CalcularRango(
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
                    rangoEmprendedor
                );

                return resultado;
            })
            // Datos.Evaluables ya contiene únicamente asesores con venta personal en el ciclo.
            // Solo se muestran y guardan emprendedores que alcanzaron un rango.
            .Where(resultado => resultado.Califica)
            .ToList();
        foreach (var resultado in resultados)
            resultado.Moneda = moneda.ToString();

        return (true, "Rangos MonteSion calculados correctamente.", resultados);
    }

    public async Task<(bool Success, string Mensaje, object Data)> ExportarRangosAsync(int cicloId)
    {
        var calculo = await CalcularRangosAsync(cicloId);
        if (!calculo.Success) return (false, calculo.Mensaje, new { });

        var moneda = calculo.Data.FirstOrDefault()?.Moneda ?? string.Empty;
        var archivo = await new MonteSionXls().GenerarAsync(calculo.Data, moneda);
        if (!archivo.Success) return (false, "No existen datos para exportar.", new { });

        return (true, "Archivo MonteSion generado correctamente.", new
        {
            FileNameXls = $"MONTE SION - RANGOS - CICLO {cicloId}.xlsx",
            Base64Xls = archivo.Base64,
        });
    }

    public async Task<(bool Success, string Mensaje, MonteSionGuardadoResultado Data)> GuardarRangosAsync(int cicloId, string? usuario, bool esSimulacion = false)
    {
        var calculo = await CalcularRangosAsync(cicloId);
        if (!calculo.Success)
            return (false, calculo.Mensaje, new MonteSionGuardadoResultado { CicloId = cicloId });

        var resultados = calculo.Data.ToList();
        // En la simulación, que nadie califique no es un error: la corrida sigue con 0 registros.
        if (esSimulacion && resultados.Count == 0)
            return (true, "Ningún emprendedor califica a un rango Monte Sion en el ciclo.", new MonteSionGuardadoResultado { CicloId = cicloId });

        return await _repository.GuardarRangosAsync(
            Guid.NewGuid().ToString("N"),
            cicloId,
            string.IsNullOrWhiteSpace(usuario) ? "Sistema" : usuario.Trim(),
            resultados,
            esSimulacion
        );
    }

    private MonteSionResultadoRango CalcularRango(
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
        var equipos = ObtenerEquipos(emprendedor.EmprendedorId, contactosPorId, padrePorAsesor, ventasPorEmprendedor);
        var produccionTotal = equipos.Sum(equipo => equipo.ProduccionReal);

        // Desde mayo de 2026, la primera venta ingresa al vendedor como Emprendedor.
        // Los vendedores con una venta anterior a mayo de 2026 no califican a Emprendedor y empiezan desde Leader.
        if (nivelActualId < rangoEmprendedor.NivelId)
        {
            if (primeraVentaDesdeMayo2026)
            {
                var evaluacionEmprendedor = EvaluarRango(rangoEmprendedor, equipos);
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
                    Beneficio = CalcularBeneficio(rangoEmprendedor, nivelActualId, rangoEmprendedor.NivelId, rangoActual),
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
                    RangoPotencial = RangoSinCalificacion,
                    RangoFinal = RangoSinCalificacion,
                    Califica = false,
                };
            }
        }

        var rangosEvaluables = nivelActualId < rangoEmprendedor.NivelId
            ? rangos.Where(rango => rango.NivelId > rangoEmprendedor.NivelId).ToList()
            : rangos;
        var evaluaciones = new List<MonteSionEvaluacionRango>();
        var final = EvaluarRangosRecursivamente(rangosEvaluables, 0, equipos, evaluaciones);
        var potencial = rangosEvaluables.FirstOrDefault(rango => EstaDentroDelRango(produccionTotal, rango));
        var rangoEvaluado = final ?? evaluaciones.LastOrDefault();
        var rangoFinalConfiguracion = final is null
            ? null
            : rangosEvaluables.First(rango => string.Equals(rango.Nombre, final.Rango, StringComparison.OrdinalIgnoreCase));
        var nivelAlcanzadoId = rangoFinalConfiguracion is null
            ? 0
            : rangoFinalConfiguracion.NivelId;

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
            RangoPotencial = potencial?.Nombre ?? RangoSinCalificacion,
            RangoFinal = final?.Rango ?? RangoSinCalificacion,
            Califica = final is not null,
            Beneficio = rangoFinalConfiguracion is null
                ? null
                : CalcularBeneficio(rangoFinalConfiguracion, nivelActualId, nivelAlcanzadoId, rangoActual),
            RangoEvaluado = rangoEvaluado,
            Evaluaciones = evaluaciones,
        };
    }

    private static MonteSionBeneficioRango CalcularBeneficio(
        MonteSionRangoConfiguracion rango,
        int nivelActualId,
        int nivelAlcanzadoId,
        string rangoActual)
    {
        var esAscenso = nivelAlcanzadoId > nivelActualId;
        var esRecalificacion = nivelAlcanzadoId == nivelActualId;
        var tieneIncentivo = rango.Incentivo > 0;
        var incentivoTexto = tieneIncentivo ? TextoIncentivo(rango.Incentivo, rango.Moneda) : null;

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

    private static string TextoIncentivo(decimal monto, string moneda) =>
        $"Pago único de {monto.ToString("#,##0.##", FormatoMonto)} {SimboloMoneda(moneda)}";

    private static string SimboloMoneda(string moneda) =>
        string.Equals(moneda, nameof(MonedaCiclo.BS), StringComparison.OrdinalIgnoreCase) ? "Bs" : "$us";

    private static MonteSionEvaluacionRango? EvaluarRangosRecursivamente(
        IReadOnlyList<MonteSionRangoConfiguracion> rangos,
        int indice,
        IEnumerable<MonteSionEquipoProduccion> equipos,
        List<MonteSionEvaluacionRango> evaluaciones)
    {
        if (indice >= rangos.Count) return null;

        var evaluacion = EvaluarRango(rangos[indice], equipos);
        evaluaciones.Add(evaluacion);

        return evaluacion.Califica
            ? evaluacion
            : EvaluarRangosRecursivamente(rangos, indice + 1, equipos, evaluaciones);
    }

    private List<MonteSionEquipoProduccion> ObtenerEquipos(
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
            .Where(venta => venta.VendedorId != emprendedorId && venta.Nivel <= _opciones.ProfundidadMaxima)
            .Select(venta => new
            {
                Venta = venta,
                EquipoId = ObtenerEquipoDirecto(emprendedorId, venta, padrePorAsesor) is var equipoDirecto && equipoDirecto > 0
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

    private static int ObtenerEquipoDirecto(
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

    private static MonteSionEvaluacionRango EvaluarRango(
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

    private static bool EstaDentroDelRango(decimal produccion, MonteSionRangoConfiguracion rango) =>
        produccion >= rango.ProduccionRequerida
        && (rango.ProduccionHasta <= 0 || produccion <= rango.ProduccionHasta);

    private static string NormalizarRango(string? rango)
    {
        var normalizado = (rango ?? string.Empty)
            .Trim()
            .Normalize(System.Text.NormalizationForm.FormD);
        normalizado = string.Concat(normalizado.Where(caracter =>
            System.Globalization.CharUnicodeInfo.GetUnicodeCategory(caracter) != System.Globalization.UnicodeCategory.NonSpacingMark))
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
