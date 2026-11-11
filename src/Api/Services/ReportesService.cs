using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.DTO;
using ApiGuardian.Infrastructure.Services.Pdf;
using ApiGuardian.Infrastructure.Services;
using ApiGuardian.Models;
using QuestPDF.Fluent;
using Microsoft.Extensions.Options;

namespace CleanDapperApi.Api.Services;

public sealed class ReportesService : IReportesService
{
    private readonly IReportesRepository _repository;
    private readonly IAdministracionDescuentoComisionRepository _comision;
    private readonly IAdministracionDetalleFacturaRepository _detalleFactura;
    private readonly IAdministracionCicloRepository _cicloRepository;
    private readonly PagoComisionOpciones _pagoComisionOpciones;
    private readonly MonteSionOpciones _monteSionOpciones;
    private readonly CambioDolarService _cambioDolar;

    public ReportesService(
        IReportesRepository repository,
        IAdministracionDescuentoComisionRepository comision,
        IAdministracionDetalleFacturaRepository detalleFactura,
        IAdministracionCicloRepository cicloRepository,
        IOptions<PagoComisionOpciones> pagoComisionOpciones,
        IOptions<MonteSionOpciones> monteSionOpciones,
        CambioDolarService cambioDolar
    )
    {
        _repository = repository;
        _comision = comision;
        _detalleFactura = detalleFactura;
        _cicloRepository = cicloRepository;
        _pagoComisionOpciones = pagoComisionOpciones.Value;
        _monteSionOpciones = monteSionOpciones.Value;
        _cambioDolar = cambioDolar;
    }

    public async Task<(bool Success, string Mensaje, object Data)> ReporteComisionesAsync(
        int cicloId,
        int contactoId
    )
    {
        try
        {
            var moneda = await ObtenerMonedaCicloAsync(cicloId);
            if (!moneda.Success)
                return (false, moneda.Mensaje, SinArchivo());

            var r = await _repository.GetReporteComision(Id(), cicloId, contactoId);
            var c = await _comision.GetComision(Id(), contactoId, cicloId, 1);
            r.Data.Comisiones = c.Data;
            bool tiene =
                r.Data.Comisiones.TotalComision > 0
                || r.Data.VentasPersonales.Any()
                || r.Data.VentasGrupo.Any()
                || r.Data.BonoRedisual.Any()
                || r.Data.BonoPar.Any()
                || r.Data.BonoCarrera.Any();
            var pdf = Convert.ToBase64String(new ReporteComisionesDocumento(r.Data, moneda.Moneda).GeneratePdf());
            return (
                true,
                "Reporte generado correctamente.",
                new
                {
                    FileName = $"REPORTE DE COMISIONES {r.Data.Encabezado.NombreCompleto} - {r.Data.Encabezado.Ciclo}.pdf",
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

    public async Task<(bool Success, string Mensaje, object Data)> ReporteAplicacionesAsync(
        int cicloId,
        int contactoId
    )
    {
        try
        {
            var moneda = await ObtenerMonedaCicloAsync(cicloId);
            if (!moneda.Success)
                return (false, moneda.Mensaje, SinArchivo());

            var r = await _repository.GetReporteAplicacines(Id(), cicloId, contactoId);
            if (r.Data.Aplicaciones == null || !r.Data.Aplicaciones.Any())
                return (
                    false,
                    "No existe aplicaciones para el ciclo o asesor seleccionado",
                    SinArchivo()
                );
            var pdf = Convert.ToBase64String(
                new ReporteAplicacionesDocumento(r.Data, moneda.Moneda).GeneratePdf()
            );
            return (
                true,
                "Reporte generado correctamente.",
                new
                {
                    FileName = "REPORTE DE APLICACIONES.pdf",
                    FileBase64 = pdf,
                    ContentType = "application/pdf",
                }
            );
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje, object Data)> ReporteDescuentoEmpresaAsync(
        int cicloId,
        int empresaId
    )
    {
        try
        {
            var moneda = await ObtenerMonedaCicloAsync(cicloId);
            if (!moneda.Success) return (false, moneda.Mensaje, SinArchivo());
            var r = await _repository.GetReporteDecuentoEmpresa(Id(), cicloId, empresaId);
            if (r.Data == null || !r.Data.Any())
                return (false, "No existe descuentos para el ciclo seleccionado", SinArchivo());
            var lista = r.Data.ToList();
            var pdf = Convert.ToBase64String(new ReporteDescuentoEmpresa(lista, moneda.Moneda).GeneratePdf());
            var xls = await new DescuentoEmpresaXls().GetDescuentoEmpresaXls(lista, moneda.Moneda);
            return (
                true,
                "Reporte generado correctamente.",
                new
                {
                    FileName = $"REPORTE DE DESCUENTO POR EMPRESA - {lista[0].Empresa}.pdf",
                    FileBase64 = pdf,
                    ContentType = "application/pdf",
                    FileNameXls = $"REPORTE DE PRORRATEO  {lista[0].Empresa}.xlsx",
                    base64Xls = xls.base64,
                }
            );
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    private static string NormalizarRango(string? rango)
    {
        var normalizado = (rango ?? string.Empty).Trim().Normalize(System.Text.NormalizationForm.FormD);
        return string.Concat(normalizado.Where(caracter => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(caracter) != System.Globalization.UnicodeCategory.NonSpacingMark))
            .Normalize(System.Text.NormalizationForm.FormC)
            .ToUpperInvariant()
            .Replace("ZAFIRO", "SAPPHIRE")
            .Replace("ESMERALDA", "EMERALD")
            .Replace("DIAMANTE", "DIAMOND")
            .Replace("EMBAJADOR REGIONAL", "REGIONAL AMBASSADOR")
            .Replace("EMBAJADOR NACIONAL", "NATIONAL AMBASSADOR")
            .Replace("EMBAJADOR INTERNACIONAL", "INTERNATIONAL AMBASSADOR");
    }

    public async Task<(bool Success, string Mensaje, object Data)> ReporteFacturacionAsync(
        int cicloId,
        int contactoId
    )
    {
        try
        {
            var moneda = await ObtenerMonedaCicloAsync(cicloId);
            if (!moneda.Success)
                return (false, moneda.Mensaje, SinArchivo());

            var r = await _repository.GetReporteFacturacion(Id(), cicloId, contactoId);
            var detalle = await _detalleFactura.GetDetalleFacturaPagination(Id(), 0, 10);
            if (r.Data == null || !r.Data.Any())
                return (false, "No existe datos para el ciclo y asesor seleccionado", SinArchivo());
            var lista = r.Data.ToList();
            var logo = File.ReadAllBytes(
                Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Logokalomai.png")
            );
            var pdf = Convert.ToBase64String(
                new ReporteFacturacion(lista, logo, detalle.Data.ToList(), moneda.Moneda).GeneratePdf()
            );
            return (
                true,
                "Reporte generado correctamente.",
                new
                {
                    FileName = $"REPORTE DE FACTURACION {lista[0].NombreCiclo} - {lista[0].SNombreCompleto}.pdf",
                    FileBase64 = pdf,
                    ContentType = "application/pdf",
                }
            );
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje, object Data)> ReporteProrrateoAsync(
        int cicloId
    )
    {
        try
        {
            var moneda = await ObtenerMonedaCicloAsync(cicloId);
            if (!moneda.Success) return (false, moneda.Mensaje, SinArchivo());
            var r = await _repository.GetReporteProrrateo(Id(), cicloId);
            if (r.Data == null || !r.Data.Any())
                return (false, "No existe datos para el ciclo seleccionado", SinArchivo());
            var lista = r.Data.ToList();
            var pdf = Convert.ToBase64String(new ReporteProrrateo(lista, moneda.Moneda).GeneratePdf());
            var xls = await new ProrrateoXls().GetProrrateoXls(lista, moneda.Moneda);
            return (
                true,
                "Reporte generado correctamente.",
                new
                {
                    FileName = $"REPORTE DE PRORRATEO {lista[0].Ciclo}.pdf",
                    FileBase64 = pdf,
                    ContentType = "application/pdf",
                    FileNameXls = $"REPORTE DE PRORRATEO  {lista[0].Ciclo}.xlsx",
                    base64Xls = xls.base64,
                }
            );
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje, object Data)> ReporteComisionServicioAsync(
        int cicloId,
        int empresaId
    )
    {
        try
        {
            var moneda = await ObtenerMonedaCicloAsync(cicloId);
            if (!moneda.Success) return (false, moneda.Mensaje, SinArchivo());
            var r = await _repository.GetReporteComisionServicio(Id(), cicloId, empresaId);
            if (r.Data == null || !r.Data.Any())
                return (false, "No existe datos para el ciclo seleccionado", SinArchivo());
            var lista = r.Data.ToList();
            var nombre = empresaId == -1 ? "TODAS" : lista[0].Empresa;
            var pdf = Convert.ToBase64String(
                new ReporteComisionServicio(lista, empresaId, moneda.Moneda).GeneratePdf()
            );
            var xls = await new ComisionServicioXls().GetComisionServicioXls(lista, empresaId, moneda.Moneda);
            return (
                true,
                "Reporte generado correctamente.",
                new
                {
                    FileName = $"REPORTE DE COMISION - SERVICIO  {nombre}.pdf",
                    FileBase64 = pdf,
                    ContentType = "application/pdf",
                    FileNameXls = $"REPORTE DE COMISION - SERVICIO  {nombre}.xlsx",
                    base64Xls = xls.base64,
                }
            );
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje, object Data)> ReportePagarComisionAsync(
        int cicloId
    )
    {
        try
        {
            var moneda = await ObtenerMonedaCicloAsync(cicloId);
            if (!moneda.Success) return (false, moneda.Mensaje, SinArchivo());
            var pagar = await _repository.GetReportePagarComision(Id(), cicloId);
            var prorrateo = await _repository.GetReporteProrrateo(Id(), cicloId);
            var descuentosAplicaciones = await _repository.GetDescuentosAplicacionesProrrateo(Id(), cicloId);
            if (!descuentosAplicaciones.Success)
                return (false, $"No se pudieron obtener los descuentos del ciclo: {descuentosAplicaciones.Mensaje}", SinArchivo());
            var listaP = prorrateo.Data.ToList();
            var headers = listaP
                .GroupBy(x => x.EmpresaId)
                .Select(g => new EmpresaHeaderPagarComision
                {
                    EmpresaId = g.Key,
                    SEmpresa = g.First().SEmpresa,
                })
                .ToList();
            if (pagar.Data == null || !pagar.Data.Any())
                return (false, "No existe datos para el ciclo seleccionado", SinArchivo());
            var lista = pagar.Data.ToList();
            var descuentosPorContacto = descuentosAplicaciones.Data
                .GroupBy(item => item.ContactoId)
                .ToDictionary(
                    grupo => grupo.Key,
                    grupo => grupo.Sum(item => item.Monto)
                );
            foreach (var item in lista)
            {
                item.ComisionDespuesRetencion =
                    item.Personal + item.BonoPar + item.Residual + item.Grupo - item.Retencion;
                item.TotalDescuento = descuentosPorContacto.GetValueOrDefault(item.LContactold);
            }
            var pdf = Convert.ToBase64String(
                new ReportePagarComision(lista, listaP, headers, _pagoComisionOpciones.RedistribucionesPorRetencion, moneda.Moneda).GeneratePdf()
            );
            var xls = await new PagarComisionxls().GetPagarComisionXls(lista, listaP, headers, _pagoComisionOpciones.RedistribucionesPorRetencion, moneda.Moneda);
            return (
                true,
                "Reporte generado correctamente.",
                new
                {
                    FileName = $"REPORTE DE PAGAR COMISION - {lista[0].Ciclo}.pdf",
                    FileNameXls = $"REPORTE DE PAGAR COMISION - {lista[0].Ciclo}.xlsx",
                    FileBase64 = pdf,
                    ContentType = "application/pdf",
                    base64Xls = xls.base64,
                }
            );
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje, object Data)> ReportePlanCarreraAsync(
        int cicloId
    )
    {
        try
        {
            var moneda = await ObtenerMonedaCicloAsync(cicloId);
            if (!moneda.Success) return (false, moneda.Mensaje, SinArchivo());
            var r = await _repository.GetReportePlanCarrera(Id(), cicloId);
            if (r.Data == null || !r.Data.Any())
                return (false, "No existe datos para el ciclo seleccionado", SinArchivo());
            var incentivosPorRango = _monteSionOpciones.Rangos
                .Where(rango => rango.IncentivoUsd > 0)
                .ToDictionary(rango => NormalizarRango(rango.Nombre), rango => rango.IncentivoUsd, StringComparer.OrdinalIgnoreCase);
            var lista = r.Data.Select(item =>
            {
                if (item.Escalados > 0 && incentivosPorRango.TryGetValue(NormalizarRango(item.NivelAlcanzadoCiclo), out var incentivoUsd))
                    item.Monto = incentivoUsd;
                return item;
            }).Where(item => item.Monto > 0).ToList();
            if (lista.Count == 0)
                return (false, "No existen bonos o incentivos con importe mayor a cero para el ciclo seleccionado", SinArchivo());
            for (var indice = 0; indice < lista.Count; indice++)
                lista[indice].Nro = indice + 1;
            var pdf = Convert.ToBase64String(new ReportePlanCarrera(lista, moneda.Moneda, _cambioDolar.TipoCambioOficial).GeneratePdf());
            var xls = await new PlanCarreraXls().GetPlanCarreraXls(lista, moneda.Moneda, _cambioDolar.TipoCambioOficial);
            return (
                true,
                "Reporte generado correctamente.",
                new
                {
                    FileName = $"REPORTE DE PLAN DE CARRERA - {lista[0].Ciclo}.pdf",
                    FileNameXls = $"REPORTE DE PLAN DE CARRERA - {lista[0].Ciclo}.xlsx",
                    FileBase64 = pdf,
                    ContentType = "application/pdf",
                    base64Xls = xls.base64,
                }
            );
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje, object Data)> ReporteAscensoRangoAsync(
        int cicloId
    )
    {
        try
        {
            var moneda = await ObtenerMonedaCicloAsync(cicloId);
            if (!moneda.Success) return (false, moneda.Mensaje, SinArchivo());

            var r = await _repository.GetReporteAscensoRango(Id(), cicloId);
            if (r.Data == null || !r.Data.Any())
                return (false, "No existe datos para el ciclo seleccionado", SinArchivo());
            var lista = r.Data.ToList();
            var pdf = Convert.ToBase64String(new ReporteAscensoRango(lista).GeneratePdf());
            var xls = await new AscensoRangoXls().GetAscensoRangoXls(lista);
            return (
                true,
                "Reporte generado correctamente.",
                new
                {
                    FileName = $"REPORTE DE ASCENSO DE RANGO - {lista[0].Mes}.pdf",
                    FileNameXls = $"REPORTE DE ASCENSO DE RANGO - {lista[0].Mes}.xlsx",
                    FileBase64 = pdf,
                    ContentType = "application/pdf",
                    base64Xls = xls.base64,
                }
            );
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    private static string Id() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

    private async Task<(bool Success, string Mensaje, string Moneda)> ObtenerMonedaCicloAsync(int cicloId)
    {
        var ciclo = await _cicloRepository.GetCiclo(Id(), cicloId);
        if (!ciclo.Success)
            return (false, "No se encontró el ciclo seleccionado para obtener su moneda.", string.Empty);

        var moneda = ciclo.Data.Moneda?.Trim().ToUpperInvariant();
        if (moneda is not ("BS" or "SUS"))
            return (false, "El ciclo seleccionado no tiene una moneda válida. Debe ser BS o SUS.", string.Empty);

        return (true, string.Empty, moneda);
    }

    private static object SinArchivo() =>
        new
        {
            FileName = "",
            FileBase64 = "",
            ContentType = "",
        };
}
