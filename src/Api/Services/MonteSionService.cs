using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.DTO;
using ApiGuardian.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace CleanDapperApi.Api.Services;

public sealed class MonteSionService : IMonteSionService
{
    private const string RangoSinCalificacion = "Independent Affiliate";
    private readonly IMonteSionRepository _repository;
    private readonly MonteSionOpciones _opciones;

    public MonteSionService(IMonteSionRepository repository, IOptions<MonteSionOpciones> opciones)
    {
        _repository = repository;
        _opciones = opciones.Value;
    }

    public async Task<(bool Success, string Mensaje, IEnumerable<MonteSionResultadoRango> Data)> CalcularRangosAsync(int cicloId)
    {
        if (cicloId <= 0)
            return (false, "El ciclo es obligatorio.", Enumerable.Empty<MonteSionResultadoRango>());

        if (_opciones.ProfundidadMaxima <= 0)
            return (false, "La configuración de rangos MonteSion no es válida.", Enumerable.Empty<MonteSionResultadoRango>());

        var datos = await _repository.ObtenerDatosCicloAsync(Guid.NewGuid().ToString("N"), cicloId);

        if (!datos.Success)
            return (false, datos.Mensaje, Enumerable.Empty<MonteSionResultadoRango>());

        var rangosActualesResultado = await _repository.ObtenerRangosActualesAsync(Guid.NewGuid().ToString("N"), cicloId);
        if (!rangosActualesResultado.Success)
            return (false, rangosActualesResultado.Mensaje, Enumerable.Empty<MonteSionResultadoRango>());

        var nivelesResultado = await _repository.ObtenerNivelesAsync(Guid.NewGuid().ToString("N"));
        if (!nivelesResultado.Success)
            return (false, nivelesResultado.Mensaje, Enumerable.Empty<MonteSionResultadoRango>());

        var contactos = datos.Contactos.ToList();
        var contactosPorId = contactos.ToDictionary(contacto => contacto.EmprendedorId);
        var padrePorAsesor = datos.Producciones
            .Where(venta => venta.Nivel == 1 && venta.VendedorId > 0)
            .GroupBy(venta => venta.VendedorId)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.First().EmprendedorId);
        var ventasPorEmprendedor = datos.Producciones
            .Where(venta => venta.Nivel >= 1 && venta.Nivel <= _opciones.ProfundidadMaxima)
            .GroupBy(venta => venta.EmprendedorId)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.ToList());
        var incentivosPorRango = _opciones.Rangos
            .GroupBy(rango => NormalizarRango(rango.Nombre))
            .ToDictionary(grupo => grupo.Key, grupo => grupo.First().Incentivo, StringComparer.OrdinalIgnoreCase);
        var rangos = nivelesResultado.Data
            .Where(nivel => nivel.NivelId > 0)
            .OrderByDescending(nivel => nivel.NivelId)
            .Select(nivel => new MonteSionRangoConfiguracion
            {
                NivelId = nivel.NivelId,
                Nombre = nivel.Nombre,
                ProduccionRequerida = nivel.ProduccionDesde,
                ProduccionHasta = nivel.ProduccionHasta,
                VmeMonto = nivel.VmeMonto,
                BonoUsd = nivel.BonoUsd,
                PorcentajeLiderazgo = nivel.PorcentajeLiderazgo,
                Incentivo = incentivosPorRango.GetValueOrDefault(NormalizarRango(nivel.Nombre)),
            })
            .ToList();
        if (rangos.Count == 0)
            return (false, "No existen niveles configurados en administracionnivel.", Enumerable.Empty<MonteSionResultadoRango>());
        var rangoEmprendedor = rangos.FirstOrDefault(rango => NormalizarRango(rango.Nombre) == "EMPRENDEDOR");
        var emprendedoresPrimeraVenta = datos.EmprendedoresPrimeraVenta.ToHashSet();
        if (emprendedoresPrimeraVenta.Count > 0 && rangoEmprendedor is null)
            return (false, "Existen asesores con primera venta, pero no existe el rango Emprendedor en administracionnivel.", Enumerable.Empty<MonteSionResultadoRango>());

        var rangosActualesPorEmprendedor = rangosActualesResultado.Data
            .GroupBy(rango => rango.EmprendedorId)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.First());

        var resultados = datos.Evaluables
            .GroupBy(emprendedor => emprendedor.EmprendedorId)
            .Select(grupo => grupo.First())
            .Select(emprendedor =>
            {
                rangosActualesPorEmprendedor.TryGetValue(emprendedor.EmprendedorId, out var rangoActual);
                var resultado = CalcularRango(
                    emprendedor,
                    cicloId,
                    contactosPorId,
                    padrePorAsesor,
                    ventasPorEmprendedor,
                    rangos,
                    rangoActual?.NivelActualId ?? 0,
                    rangoActual?.RangoActual ?? "Asesor Comercial",
                    emprendedoresPrimeraVenta.Contains(emprendedor.EmprendedorId),
                    rangoEmprendedor
                );

                return resultado;
            })
            .Where(resultado => resultado.ProduccionTotalRed > 0m || emprendedoresPrimeraVenta.Contains(resultado.EmprendedorId))
            .ToList();

        return (true, "Rangos MonteSion calculados correctamente.", resultados);
    }

    public async Task<(bool Success, string Mensaje, object Data)> ExportarRangosAsync(int cicloId)
    {
        var calculo = await CalcularRangosAsync(cicloId);
        if (!calculo.Success) return (false, calculo.Mensaje, new { });

        var archivo = await new MonteSionXls().GenerarAsync(calculo.Data);
        if (!archivo.Success) return (false, "No existen datos para exportar.", new { });

        return (true, "Archivo MonteSion generado correctamente.", new
        {
            FileNameXls = $"MONTE SION - RANGOS - CICLO {cicloId}.xlsx",
            Base64Xls = archivo.Base64,
        });
    }

    public async Task<(bool Success, string Mensaje, MonteSionGuardadoResultado Data)> GuardarRangosAsync(int cicloId, string? usuario)
    {
        var calculo = await CalcularRangosAsync(cicloId);
        if (!calculo.Success)
            return (false, calculo.Mensaje, new MonteSionGuardadoResultado { CicloId = cicloId });

        return await _repository.GuardarRangosAsync(
            Guid.NewGuid().ToString("N"),
            cicloId,
            string.IsNullOrWhiteSpace(usuario) ? "Sistema" : usuario.Trim(),
            calculo.Data.ToList()
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
        bool esPrimeraVentaHistorica,
        MonteSionRangoConfiguracion? rangoEmprendedor)
    {
        var equipos = ObtenerEquipos(emprendedor.EmprendedorId, contactosPorId, padrePorAsesor, ventasPorEmprendedor);
        var produccionTotal = equipos.Sum(equipo => equipo.ProduccionReal);
        if (esPrimeraVentaHistorica && rangoEmprendedor is not null)
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
        var evaluaciones = new List<MonteSionEvaluacionRango>();
        var final = EvaluarRangosRecursivamente(rangos, 0, equipos, evaluaciones);
        var potencial = rangos.FirstOrDefault(rango => EstaDentroDelRango(produccionTotal, rango));
        var rangoEvaluado = final ?? evaluaciones.LastOrDefault();
        var rangoFinalConfiguracion = final is null
            ? null
            : rangos.First(rango => string.Equals(rango.Nombre, final.Rango, StringComparison.OrdinalIgnoreCase));
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
        var tieneIncentivo = !string.IsNullOrWhiteSpace(rango.Incentivo);

        if (esAscenso && tieneIncentivo)
        {
            return new MonteSionBeneficioRango
            {
                BonoConfiguradoUsd = rango.BonoUsd,
                IncentivoConfigurado = rango.Incentivo,
                PrimeraCalificacion = true,
                EsRecalificacion = false,
                BonoAPagarUsd = 0m,
                IncentivoAEntregar = rango.Incentivo,
                Motivo = $"Primera calificación a {rango.Nombre}. El incentivo reemplaza al bono del rango.",
            };
        }

        if (esAscenso || esRecalificacion)
        {
            return new MonteSionBeneficioRango
            {
                BonoConfiguradoUsd = rango.BonoUsd,
                IncentivoConfigurado = rango.Incentivo,
                PrimeraCalificacion = esAscenso,
                EsRecalificacion = esRecalificacion,
                BonoAPagarUsd = rango.BonoUsd,
                IncentivoAEntregar = null,
                Motivo = esAscenso
                    ? $"Ascenso a {rango.Nombre}. Corresponde el bono configurado."
                    : $"Recalificación en {rango.Nombre}. Corresponde el bono configurado.",
            };
        }

        return new MonteSionBeneficioRango
        {
            BonoConfiguradoUsd = rango.BonoUsd,
            IncentivoConfigurado = rango.Incentivo,
            PrimeraCalificacion = false,
            EsRecalificacion = false,
            BonoAPagarUsd = rango.BonoUsd,
            IncentivoAEntregar = null,
            Motivo = $"Bonificación mensual por calificar a {rango.Nombre}. No corresponde incentivo porque el rango actual ({rangoActual}) es superior.",
        };
    }

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
        var limite = rango.VmeMonto is null || rango.VmeMonto <= 0
            ? (decimal?)null
            : rango.VmeMonto;

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
            VmeAplicado = rango.VmeMonto,
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
