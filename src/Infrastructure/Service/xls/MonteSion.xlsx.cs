using ApiGuardian.Domain.DTO;
using ClosedXML.Excel;

public sealed class MonteSionXls
{
    public Task<(bool Success, string Base64)> GenerarAsync(IEnumerable<MonteSionResultadoRango> resultados)
    {
        var lista = resultados.ToList();
        if (lista.Count == 0) return Task.FromResult((false, string.Empty));

        using var libro = new XLWorkbook();
        CrearConsolidado(libro.Worksheets.Add("Consolidado"), lista);
        CrearDetalle(libro.Worksheets.Add("Detalle por equipo"), lista);

        using var stream = new MemoryStream();
        libro.SaveAs(stream);
        return Task.FromResult((true, Convert.ToBase64String(stream.ToArray())));
    }

    private static void CrearConsolidado(IXLWorksheet hoja, List<MonteSionResultadoRango> resultados)
    {
        hoja.Cell(1, 1).Value = "MONTE SION · CONSOLIDADO DE RANGOS";
        hoja.Range(1, 1, 1, 12).Merge();
        EstiloTitulo(hoja.Range(1, 1, 1, 12));

        var encabezados = new[] { "Código", "Emprendedor", "Fecha registro", "Producción neta", "Producción válida", "Rango actual", "Rango ascenso", "Primera calificación", "Recalificación", "Bono", "Incentivo", "Motivo" };
        for (var columna = 0; columna < encabezados.Length; columna++) hoja.Cell(3, columna + 1).Value = encabezados[columna];
        EstiloEncabezado(hoja.Range(3, 1, 3, encabezados.Length));

        var fila = 4;
        foreach (var resultado in resultados.OrderByDescending(item => item.ProduccionValidaTotal))
        {
            hoja.Cell(fila, 1).Value = resultado.Codigo ?? string.Empty;
            hoja.Cell(fila, 2).Value = resultado.EmprendedorNombre;
            hoja.Cell(fila, 3).Value = resultado.FechaRegistro;
            hoja.Cell(fila, 4).Value = resultado.ProduccionTotalRed;
            hoja.Cell(fila, 5).Value = resultado.ProduccionValidaTotal;
            hoja.Cell(fila, 6).Value = resultado.RangoActual;
            hoja.Cell(fila, 7).Value = resultado.RangoFinal ?? "Independent Affiliate";
            hoja.Cell(fila, 8).Value = resultado.Beneficio?.PrimeraCalificacion == true ? "Sí" : "No";
            hoja.Cell(fila, 9).Value = resultado.Beneficio?.EsRecalificacion == true ? "Sí" : "No";
            hoja.Cell(fila, 10).Value = resultado.Beneficio?.BonoAPagarUsd ?? 0m;
            hoja.Cell(fila, 11).Value = resultado.Beneficio?.IncentivoAEntregar ?? "Ninguno";
            hoja.Cell(fila, 12).Value = resultado.Beneficio?.Motivo ?? "Sin rango final.";
            fila++;
        }

        hoja.Range(4, 3, fila - 1, 3).Style.DateFormat.Format = "dd/MM/yyyy";
        hoja.Range(4, 4, fila - 1, 5).Style.NumberFormat.Format = "#,##0.00";
        hoja.Range(4, 10, fila - 1, 10).Style.NumberFormat.Format = "#,##0.00";
        AplicarFormatoTabla(hoja, 3, fila - 1, encabezados.Length);
    }

    private static void CrearDetalle(IXLWorksheet hoja, List<MonteSionResultadoRango> resultados)
    {
        hoja.Cell(1, 1).Value = "MONTE SION · DETALLE POR EQUIPO Y NIVEL";
        hoja.Range(1, 1, 1, 13).Merge();
        EstiloTitulo(hoja.Range(1, 1, 1, 13));

        var encabezados = new[] { "ID EI", "Emprendedor", "Rango evaluado", "Equipo ID", "Equipo", "Nivel", "Producción real", "Límite VME", "Producción válida", "Descartada VME", "VME %", "Números de venta", "Resultado" };
        for (var columna = 0; columna < encabezados.Length; columna++) hoja.Cell(3, columna + 1).Value = encabezados[columna];
        EstiloEncabezado(hoja.Range(3, 1, 3, encabezados.Length));

        var fila = 4;
        foreach (var resultado in resultados)
        {
            var evaluacion = resultado.RangoEvaluado;
            if (evaluacion is null) continue;

            foreach (var equipo in evaluacion.Equipos)
            {
                var niveles = equipo.VentasPorNivel.Count == 0
                    ? [new MonteSionVentasNivel { Nivel = 0, NumerosVenta = [] }]
                    : equipo.VentasPorNivel;

                foreach (var nivel in niveles)
                {
                    hoja.Cell(fila, 1).Value = resultado.EmprendedorId;
                    hoja.Cell(fila, 2).Value = resultado.EmprendedorNombre;
                    hoja.Cell(fila, 3).Value = evaluacion.Rango;
                    hoja.Cell(fila, 4).Value = equipo.EquipoId;
                    hoja.Cell(fila, 5).Value = equipo.EquipoNombre;
                    hoja.Cell(fila, 6).Value = nivel.Nivel == 0 ? "Sin ventas" : $"Nivel {nivel.Nivel}";
                    hoja.Cell(fila, 7).Value = equipo.ProduccionReal;
                    hoja.Cell(fila, 8).Value = equipo.LimiteVme ?? 0m;
                    hoja.Cell(fila, 9).Value = equipo.ProduccionValida;
                    hoja.Cell(fila, 10).Value = equipo.ProduccionDescartada;
                    hoja.Cell(fila, 11).Value = evaluacion.VmeAplicado ?? 0m;
                    hoja.Cell(fila, 12).Value = string.Join(", ", nivel.NumerosVenta);
                    hoja.Cell(fila, 13).Value = evaluacion.Califica ? "CALIFICA" : "NO CALIFICA";
                    fila++;
                }
            }
        }

        hoja.Range(4, 7, Math.Max(4, fila - 1), 10).Style.NumberFormat.Format = "#,##0.00";
        hoja.Range(4, 11, Math.Max(4, fila - 1), 11).Style.NumberFormat.Format = "0.00";
        AplicarFormatoTabla(hoja, 3, Math.Max(3, fila - 1), encabezados.Length);
    }

    private static void EstiloTitulo(IXLRange rango)
    {
        rango.Style.Font.Bold = true;
        rango.Style.Font.FontSize = 14;
        rango.Style.Font.FontColor = XLColor.White;
        rango.Style.Fill.BackgroundColor = XLColor.FromHtml("1F4E78");
        rango.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    private static void EstiloEncabezado(IXLRange rango)
    {
        rango.Style.Font.Bold = true;
        rango.Style.Font.FontColor = XLColor.White;
        rango.Style.Fill.BackgroundColor = XLColor.FromHtml("2F75B5");
        rango.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        rango.Style.Alignment.WrapText = true;
    }

    private static void AplicarFormatoTabla(IXLWorksheet hoja, int filaInicial, int filaFinal, int columnas)
    {
        var rango = hoja.Range(filaInicial, 1, filaFinal, columnas);
        rango.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        rango.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        hoja.SheetView.FreezeRows(3);
        hoja.Columns().AdjustToContents();
    }
}
