using ClosedXML.Excel;

public class ProrrateoXls
{
    public async Task<(bool success, string base64)> GetProrrateoXls(
        List<RptProrrateo> listado)
    {
        if (listado == null || !listado.Any())
            return (false, string.Empty);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Prorrateo");

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
        ws.Column(2).Width = 14; // Cod Cliente
        ws.Column(3).Width = 22; // Nro Cuenta
        ws.Column(4).Width = 35; // Asesor
        ws.Column(5).Width = 14; // Carnet
        ws.Column(6).Width = 30; // Empresa
        ws.Column(7).Width = 14;
        ws.Column(8).Width = 14;
        ws.Column(9).Width = 14;
        ws.Column(10).Width = 14;
        ws.Column(11).Width = 14;

        ws.Columns(7, 11).Style.NumberFormat.Format = "#,##0.00";
        ws.Columns(7, 11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
    }

    // ===============================
    // HEADERS
    // ===============================
    private static void CrearEncabezados(IXLWorksheet ws, int row)
    {
        string[] headers =
        {
            "Cod. Cliente",
            "Nro. Cuenta",
            "Asesor",
            "Carnet",
            "Empresa",
            "Importe $",
            "Retención $",
            "Líquido $",
            "Desc. $",
            "Prorrateo $"
        };

        for (int i = 0; i < headers.Length; i++)
            ws.Cell(row, i + 2).Value = headers[i];

        var range = ws.Range(row, 2, row, 11);
        range.Style.Font.Bold = true;
        range.Style.Fill.BackgroundColor = XLColor.LightGray;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }

    // ===============================
    // DETALLE (1:1 con PDF)
    // ===============================
    private static void EscribirFilaDetalle(
        IXLWorksheet ws,
        int row,
        RptProrrateo v)
    {
        ws.Cell(row, 2).Value = v.LCodigoBanco;
        ws.Cell(row, 3).Value = v.LCuentaBanco;
        ws.Cell(row, 4).Value = v.SNombreCompleto;
        ws.Cell(row, 5).Value = v.SCedulaIdentidad;
        ws.Cell(row, 6).Value = v.SEmpresa;

        ws.Cell(row, 7).Value = v.Importe;
        ws.Cell(row, 8).Value = v.Retencion;
        ws.Cell(row, 9).Value = v.Liquido;
        ws.Cell(row,10).Value = v.Descuento;
        ws.Cell(row,11).Value = v.Prorrateo;
    }

    // ===============================
    // TOTALIZADOR (según footer)
    // ===============================
    private static void EscribirTotalizador(
        IXLWorksheet ws,
        int row,
        List<RptProrrateo> data)
    {
        decimal importe   = data?.Sum(x => x.Importe) ?? 0;
        decimal retencion = data?.Sum(x => x.Retencion) ?? 0;
        decimal liquido   = data?.Sum(x => x.Liquido) ?? 0;
        decimal descuento = data?.Sum(x => x.Descuento) ?? 0;
        decimal prorrateo = data?.Sum(x => x.Prorrateo) ?? 0;

        ws.Cell(row, 2).Value = "TOTAL:";
        ws.Range(row, 2, row, 6).Merge();
        ws.Cell(row, 2).Style.Font.Bold = true;
        ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        ws.Cell(row, 7).Value = importe;
        ws.Cell(row, 8).Value = retencion;
        ws.Cell(row, 9).Value = liquido;
        ws.Cell(row,10).Value = descuento;
        ws.Cell(row,11).Value = prorrateo;

        var range = ws.Range(row, 2, row, 11);
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
        var range = ws.Range(headerRow, 2, totalRow, 11);
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }
}
