using ClosedXML.Excel;

public class DescuentoEmpresaXls
{
    public async Task<(bool success, string base64)> GetDescuentoEmpresaXls(
        List<RptDescuentoEmpresa> listado)
    {
        if (listado == null || !listado.Any())
            return (false, string.Empty);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Descuento Empresa");

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
        AplicarBordes(worksheet, headerRow, currentRow);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return (true, Convert.ToBase64String(stream.ToArray()));
    }

    // ===============================
    // COLUMNAS
    // ===============================
    private static void ConfigurarColumnas(IXLWorksheet ws)
    {
        ws.Column(2).Width = 30; // Asesor
        ws.Column(3).Width = 22; // Complejo
        ws.Column(4).Width = 30; // Empresa
        ws.Column(5).Width = 8;  // MZ
        ws.Column(6).Width = 8;  // LT
        ws.Column(7).Width = 8;  // UV
        ws.Column(8).Width = 14; // Tipo
        ws.Column(9).Width = 14; // Monto
        ws.Column(10).Width = 40; // Observación

        ws.Column(9).Style.NumberFormat.Format = "#,##0.00";
        ws.Column(9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
    }

    // ===============================
    // HEADERS
    // ===============================
    private static void CrearEncabezados(IXLWorksheet ws, int row)
    {
        string[] headers =
        {
            "ASESOR",
            "COMPLEJO",
            "EMPRESA",
            "MZ",
            "LT",
            "UV",
            "TIPO",
            "MONTO",
            "OBSERVACION"
        };

        for (int i = 0; i < headers.Length; i++)
            ws.Cell(row, i + 2).Value = headers[i];

        var range = ws.Range(row, 2, row, 10);
        range.Style.Font.Bold = true;
        range.Style.Fill.BackgroundColor = XLColor.LightGray;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }

    // ===============================
    // DETALLE (según PDF)
    // ===============================
    private static void EscribirFilaDetalle(
        IXLWorksheet ws,
        int row,
        RptDescuentoEmpresa v)
    {
        ws.Cell(row, 2).Value = v.Asesor;
        ws.Cell(row, 3).Value = v.Complejo;
        ws.Cell(row, 4).Value = v.Empresa;
        ws.Cell(row, 5).Value = v.Mz;
        ws.Cell(row, 6).Value = v.Lote;
        ws.Cell(row, 7).Value = v.Uv;
        ws.Cell(row, 8).Value = v.Tipo;
        ws.Cell(row, 9).Value = v.Monto;
        ws.Cell(row,10).Value = v.Observacion;
    }

    // ===============================
    // TOTALIZADOR (idéntico al footer)
    // ===============================
    private static void EscribirTotalizador(
        IXLWorksheet ws,
        int row,
        List<RptDescuentoEmpresa> data)
    {
        decimal totalMonto = data?.Sum(x => x.Monto) ?? 0;

        ws.Cell(row, 2).Value = "TOTAL:";
        ws.Range(row, 2, row, 8).Merge();
        ws.Cell(row, 2).Style.Font.Bold = true;
        ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        ws.Cell(row, 9).Value = totalMonto;
        ws.Cell(row,10).Value = "";

        var range = ws.Range(row, 2, row, 10);
        range.Style.Font.Bold = true;
        range.Style.NumberFormat.Format = "#,##0.00";
        range.Style.Fill.BackgroundColor = XLColor.LightGray;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        range.Style.Border.TopBorder = XLBorderStyleValues.Thin;
        range.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        range.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
        range.Style.Border.RightBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }

    // ===============================
    // BORDES
    // ===============================
    private static void AplicarBordes(
        IXLWorksheet ws,
        int headerRow,
        int totalRow)
    {
        var range = ws.Range(headerRow, 2, totalRow, 10);
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }
}
