using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Services.Pdf;
using ApiGuardian.Models;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;

namespace CleanDapperApi.Api.Services;

/// <summary>
/// Reporte mensual de comisiones por asesor. Todos los montos se presentan en BS:
/// si el ciclo es en BS no se convierte; si es en SUS se multiplica por el tipo de
/// cambio configurado para el ciclo en "ReporteMensual:TipoCambioPorCiclo".
/// </summary>
public sealed class ReporteMensualService : IReporteMensualService
{
    private const string MonedaReporte = "BS";

    private readonly IReporteMensualRepository _repository;
    private readonly IAdministracionDescuentoComisionRepository _comision;
    private readonly IAdministracionCicloRepository _cicloRepository;
    private readonly ReporteMensualOpciones _opciones;

    public ReporteMensualService(
        IReporteMensualRepository repository,
        IAdministracionDescuentoComisionRepository comision,
        IAdministracionCicloRepository cicloRepository,
        IOptions<ReporteMensualOpciones> opciones
    )
    {
        _repository = repository;
        _comision = comision;
        _cicloRepository = cicloRepository;
        _opciones = opciones.Value;
    }

    public async Task<(bool Success, string Mensaje, object Data)> ReporteMensualAsync(
        int cicloId,
        int contactoId
    )
    {
        try
        {
            var moneda = await ObtenerMonedaCicloAsync(cicloId);
            if (!moneda.Success)
                return (false, moneda.Mensaje, SinArchivo());

            var tipoCambio = ObtenerTipoCambio(cicloId, moneda.Moneda);
            if (!tipoCambio.Success)
                return (false, tipoCambio.Mensaje, SinArchivo());

            var r = await _repository.GetReporteMensual(Id(), cicloId, contactoId);
            var c = await _comision.GetComision(Id(), contactoId, cicloId, 1);
            r.Data.Comisiones = c.Data;

            if (tipoCambio.Valor != 1m)
                ConvertirABolivianos(r.Data, tipoCambio.Valor);

            bool tiene =
                r.Data.Comisiones.TotalComision > 0
                || r.Data.VentasPersonales.Any()
                || r.Data.VentasGrupo.Any()
                || r.Data.BonoRedisual.Any()
                || r.Data.BonoPar.Any()
                || r.Data.BonoCarrera.Any();
            var pdf = Convert.ToBase64String(new ReporteMensualDocumento(r.Data, MonedaReporte, moneda.Simulacion).GeneratePdf());
            return (
                true,
                "Reporte generado correctamente.",
                new
                {
                    FileName = $"REPORTE MENSUAL {r.Data.Encabezado.NombreCompleto} - {r.Data.Encabezado.Ciclo}.pdf",
                    FileBase64 = pdf,
                    ContentType = "application/pdf",
                    tieneComicion = tiene,
                }
            );
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    /// <summary>BS -> 1 (sin conversión). SUS -> tipo de cambio configurado del ciclo.</summary>
    private (bool Success, string Mensaje, decimal Valor) ObtenerTipoCambio(int cicloId, string monedaCiclo)
    {
        if (monedaCiclo == "BS")
            return (true, string.Empty, 1m);

        if (_opciones.TipoCambioPorCiclo.TryGetValue(cicloId, out var tc) && tc > 0)
            return (true, string.Empty, tc);

        return (
            false,
            $"El ciclo {cicloId} es en SUS y no tiene tipo de cambio configurado en ReporteMensual:TipoCambioPorCiclo.",
            0m
        );
    }

    /// <summary>Convierte a BS solo los montos (no porcentajes ni cantidades).</summary>
    private static void ConvertirABolivianos(ReporteComisionesDto data, decimal tc)
    {
        decimal Bs(decimal monto) => Math.Round(monto * tc, 2, MidpointRounding.AwayFromZero);

        foreach (var v in data.VentasPersonales)
        {
            v.CuotaInicial = Bs(v.CuotaInicial);
            v.Comision = Bs(v.Comision);
        }

        foreach (var g in data.VentasGrupo)
        {
            g.VentaPersonal = Bs(g.VentaPersonal);
            g.Comision = Bs(g.Comision);
        }

        // Terrenos (cantidad de terrenos sin mora) y Bxl no son montos.
        foreach (var b in data.BonoRedisual)
            b.Total = Bs(b.Total);

        foreach (var b in data.BonoPar)
            b.Bono = Bs(b.Bono);

        foreach (var b in data.BonoCarrera)
            b.Monto = Bs(b.Monto);

        var c = data.Comisiones;
        c.ComisionVentaPersonal = Bs(c.ComisionVentaPersonal);
        c.ComisionVentaGrupo = Bs(c.ComisionVentaGrupo);
        c.ComisionResidual = Bs(c.ComisionResidual);
        c.ComisionLiderazgo = Bs(c.ComisionLiderazgo);
        c.ComisionBonoPar = Bs(c.ComisionBonoPar);
        c.Retencion = Bs(c.Retencion);
        c.DescuentoLote = Bs(c.DescuentoLote);
        c.TotalComision = Bs(c.TotalComision);
        c.TotalFinal = Bs(c.TotalFinal);
    }

    private static string Id() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

    private async Task<(bool Success, string Mensaje, string Moneda, bool Simulacion)> ObtenerMonedaCicloAsync(int cicloId)
    {
        var ciclo = await _cicloRepository.GetCiclo(Id(), cicloId);
        if (!ciclo.Success)
            return (false, "No se encontró el ciclo seleccionado para obtener su moneda.", string.Empty, false);

        var moneda = ciclo.Data.Moneda?.Trim().ToUpperInvariant();
        if (moneda is not ("BS" or "SUS"))
            return (false, "El ciclo seleccionado no tiene una moneda válida. Debe ser BS o SUS.", string.Empty, false);

        return (true, string.Empty, moneda, ciclo.Data.Simulacion);
    }

    private static object SinArchivo() =>
        new
        {
            FileName = "",
            FileBase64 = "",
            ContentType = "",
        };
}
