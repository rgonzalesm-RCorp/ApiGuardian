using ClosedXML.Excel;

public class ComisionServicioXls
{
    public async Task<(bool success, string base64)> GetComisionServicioXls(
        List<RptComisionServicio> listado)
    {
        if (listado == null || !listado.Any())
            return (false, string.Empty);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Comisión Servicio");

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
        private static void EscribirFilaDetalle(
        IXLWorksheet ws,
        int row,
        RptComisionServicio v)
    {
        decimal totalComision = v.Comision + v.Servicio;

        ws.Cell(row, 2).Value = v.SCodigo;
        ws.Cell(row, 3).Value = v.SNombreCompleto;
        ws.Cell(row, 4).Value = v.Comision;
        ws.Cell(row, 5).Value = v.Servicio;
        ws.Cell(row, 6).Value = totalComision;

        // 13%
        ws.Cell(row, 7).Value =
            v.PorcentajeRetencion <= 0
                ? totalComision * 0.13m
                : 0;

        // 87%
        ws.Cell(row, 8).Value =
            v.PorcentajeRetencion <= 0
                ? totalComision * 0.87m
                : 0;

        ws.Cell(row, 9).Value = v.PorcentajeRetencion;
        ws.Cell(row,10).Value = v.MontoRetencion;
        ws.Cell(row,11).Value = totalComision;
        ws.Cell(row,12).Value = totalComision - v.MontoRetencion;
    }
    private static void ConfigurarColumnas(IXLWorksheet ws)
    {
        ws.Column(2).Width = 10;   // COD
        ws.Column(3).Width = 35;   // ASESOR
        ws.Column(4).Width = 14;
        ws.Column(5).Width = 14;
        ws.Column(6).Width = 16;
        ws.Column(7).Width = 14;
        ws.Column(8).Width = 14;
        ws.Column(9).Width = 10;
        ws.Column(10).Width = 14;
        ws.Column(11).Width = 14;
        ws.Column(12).Width = 16;

        ws.Columns(4, 12).Style.NumberFormat.Format = "#,##0.00";
        ws.Columns(4, 12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
    }
    private static void CrearEncabezados(IXLWorksheet ws, int row)
    {
        string[] headers =
        {
            "COD.", "ASESOR", "COMISION $", "SERVICIO $", "TOTAL COM. $",
            "13%", "87%", "RET. %", "RET. $", "TOTAL $", "TOTAL PAGAR $"
        };

        for (int i = 0; i < headers.Length; i++)
            ws.Cell(row, i + 2).Value = headers[i];

        var range = ws.Range(row, 2, row, 12);
        range.Style.Font.Bold = true;
        range.Style.Fill.BackgroundColor = XLColor.LightGray;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }
    private static void AplicarBordes(
        IXLWorksheet ws,
        int headerRow,
        int lastDataRow)
    {
        var range = ws.Range(headerRow, 2, lastDataRow, 12);
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }
    private static void EscribirTotalizador(
        IXLWorksheet ws,
        int row,
        List<RptComisionServicio> data)
    {
        decimal totalComision = data?.Sum(x => x.Comision) ?? 0;
        decimal totalServicio = data?.Sum(x => x.Servicio) ?? 0;

        decimal totalTotalComision = totalComision + totalServicio;

        decimal totalTrece = data?.Sum(x =>
            x.PorcentajeRetencion <= 0
                ? (x.Comision + x.Servicio) * 0.13m
                : 0) ?? 0;

        decimal totalOchoSiete = data?.Sum(x =>
            x.PorcentajeRetencion <= 0
                ? (x.Comision + x.Servicio) * 0.87m
                : 0) ?? 0;

        decimal totalRetencion = data?.Sum(x => x.MontoRetencion) ?? 0;

        decimal total = totalComision + totalServicio;
        decimal totalPagar = total - totalRetencion;

        // ===============================
        // TEXTO TOTAL
        // ===============================
        ws.Cell(row, 2).Value = "TOTAL:";
        ws.Range(row, 2, row, 3).Merge();
        ws.Cell(row, 2).Style.Font.Bold = true;
        ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        // ===============================
        // VALORES
        // ===============================
        ws.Cell(row, 4).Value = totalComision;
        ws.Cell(row, 5).Value = totalServicio;
        ws.Cell(row, 6).Value = totalTotalComision;
        ws.Cell(row, 7).Value = totalTrece;
        ws.Cell(row, 8).Value = totalOchoSiete;

        ws.Cell(row, 9).Value = ""; // RET % (vacío como en el footer)
        ws.Cell(row,10).Value = totalRetencion;
        ws.Cell(row,11).Value = total;
        ws.Cell(row,12).Value = totalPagar;

        // ===============================
        // ESTILO
        // ===============================
        var range = ws.Range(row, 2, row, 12);

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


