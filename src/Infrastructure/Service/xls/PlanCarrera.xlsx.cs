using ClosedXML.Excel;

public class PlanCarreraXls
{
    public async Task<(bool success, string base64)> GetPlanCarreraXls(List<ItemPlanCarrera> listado)
    {
        if (listado == null || !listado.Any())
            return (false, string.Empty);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Plan Carrera");

        const int headerRow = 2;
        const int firstDataRow = headerRow + 1;

        ConfigurarColumnas(worksheet);
        CrearEncabezados(worksheet, headerRow);

        int currentRow = firstDataRow;

        foreach (var item in listado)
        {
            EscribirFilaDetalle(worksheet, currentRow, item);
            currentRow++;
        }

        EscribirTotalizador(worksheet, currentRow, listado);
        AplicarBordes(worksheet, headerRow, currentRow - 1);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return (true, Convert.ToBase64String(stream.ToArray()));
    }
    private static void EscribirFilaDetalle(IXLWorksheet ws, int row, ItemPlanCarrera v)
    {
        ws.Cell(row, 2).Value = v.Nro;
        ws.Cell(row, 3).Value = v.Ciclo;
        ws.Cell(row, 4).Value = v.Tipo;
        ws.Cell(row, 5).Value = v.Cuenta;
        ws.Cell(row, 6).Value = v.CodigoBanco;
        ws.Cell(row, 7).Value = v.Nombre;
        ws.Cell(row, 8).Value = v.Carnet;
        ws.Cell(row, 9).Value = v.Ciudad;
        ws.Cell(row, 10).Value = v.PuntosR;
        ws.Cell(row, 11).Value = v.Monto;
        ws.Cell(row, 12).Value = v.NivelAlcanzadoCiclo;
        ws.Cell(row, 13).Value = v.NivelConsolidado;
        ws.Cell(row, 14).Value = v.Escalados;
    }
    private static void ConfigurarColumnas(IXLWorksheet ws)
    {
        ws.Column(2).Width = 5;
        ws.Column(2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Column(3).Width = 15;
        ws.Column(3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Column(4).Width = 20;
        ws.Column(4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Column(5).Width = 15;
        ws.Column(5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Column(6).Width = 15;
        ws.Column(6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Column(7).Width = 45;
        ws.Column(7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        ws.Column(8).Width = 15;
        ws.Column(8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        ws.Column(9).Width = 20;
        ws.Column(9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        ws.Column(10).Width = 15;
        ws.Column(11).Width = 15;

        ws.Column(12).Width = 25;
        ws.Column(12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Column(13).Width = 25;
        ws.Column(13).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Column(14).Width = 20;
        ws.Column(14).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        

        ws.Column(11).Style.NumberFormat.Format = "#,##0.00";
        ws.Column(11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        ws.Column(10).Style.NumberFormat.Format = "#,##0.00";
        ws.Column(10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
    }
    private static void CrearEncabezados(IXLWorksheet ws, int row)
    {
        string[] headers =
        {
            "#", "MES", "TIPO", "CTA. BANCO", "COD. BANCO",
            "ASESOR", "CI", "CIUDAD", "PRODUCCION", "MONTO", "NIVEL CICLO", "NIVE ALCANZADO", "NIVELES ALCANZADOS"
        };

        for (int i = 0; i < headers.Length; i++)
            ws.Cell(row, i + 2).Value = headers[i];

        var range = ws.Range(row, 2, row, 14);
        range.Style.Font.Bold = true;
        range.Style.Fill.BackgroundColor = XLColor.LightGray;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }
    private static void AplicarBordes(IXLWorksheet ws, int headerRow, int lastDataRow)
    {
        var range = ws.Range(headerRow, 2, lastDataRow, 14);
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }
    private static void EscribirTotalizador(
        IXLWorksheet ws,
        int row,
        List<ItemPlanCarrera> data)
    {
        decimal totalProduccion = data?.Sum(x => x.PuntosR) ?? 0;
        decimal totalMonto = data?.Sum(x => x.Monto) ?? 0;

        // ===============================
        // TEXTO TOTAL
        // ===============================
        ws.Cell(row, 2).Value = "TOTAL:";
        ws.Range(row, 2, row, 9).Merge();
        ws.Cell(row, 2).Style.Font.Bold = true;
        ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        // ===============================
        // VALORES
        // ===============================
        ws.Cell(row, 10).Value = totalProduccion;
        ws.Cell(row, 11).Value = totalMonto;

        ws.Cell(row, 12).Value = ""; // RET % (vacío como en el footer)
        ws.Cell(row, 13).Value = "";
        ws.Cell(row, 14).Value = "";

        // ===============================
        // ESTILO
        // ===============================
        var range = ws.Range(row, 2, row, 14);

        range.Style.Font.Bold = true;
        range.Style.NumberFormat.Format = "#,##0.00";
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        range.Style.Fill.BackgroundColor = XLColor.LightGray;

        range.Style.Border.TopBorder = XLBorderStyleValues.Thin;
        range.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        range.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
        range.Style.Border.RightBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }

}


