using ClosedXML.Excel;

public class CasosEspecialesXls
{
    public async Task<(bool success, string base64)> GetCasosEspecialesXls(List<ItemVentaCnx> listado)
    {
        if (listado == null || !listado.Any())
        {
            return (false, string.Empty);
        }

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Casos Especiales");

        const int headerRow = 2;
        const int firstDataRow = headerRow + 1;

        ConfigurarColumnas(worksheet);
        CrearEncabezados(worksheet, headerRow);

        var currentRow = firstDataRow;

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

    private static void EscribirFilaDetalle(IXLWorksheet ws, int row, ItemVentaCnx item)
    {
        ws.Cell(row, 2).Value = row - 2;
        ws.Cell(row, 3).Value = item.DFecha;
        ws.Cell(row, 3).Style.DateFormat.Format = "dd/MM/yyyy";
        ws.Cell(row, 4).Value = item.SCedulaIdentidad ?? string.Empty;
        ws.Cell(row, 5).Value = item.SNombreCompleto;
        ws.Cell(row, 6).Value = item.SCedulaIdentidadVendedor ?? string.Empty;
        ws.Cell(row, 7).Value = item.SNombreCompletoVendedor;
        ws.Cell(row, 8).Value = item.NombreTipoComision;
        ws.Cell(row, 9).Value = item.Complejo ?? string.Empty;
        ws.Cell(row, 10).Value = $"{item.IdVenta}-{item.Lote}";
        ws.Cell(row, 11).Value = item.DPrecio;
        ws.Cell(row, 12).Value = item.PorcentajeCuotaInicial;
        ws.Cell(row, 13).Value = item.SCuotaInicial;
    }

    private static void ConfigurarColumnas(IXLWorksheet ws)
    {
        ws.Column(2).Width = 5;
        ws.Column(3).Width = 13;
        ws.Column(4).Width = 18;
        ws.Column(5).Width = 35;
        ws.Column(6).Width = 18;
        ws.Column(7).Width = 35;
        ws.Column(8).Width = 18;
        ws.Column(9).Width = 28;
        ws.Column(10).Width = 18;
        ws.Column(11).Width = 15;
        ws.Column(12).Width = 12;
        ws.Column(13).Width = 15;

        ws.Columns(2, 13).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Column(11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        ws.Column(12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Column(13).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        ws.Column(11).Style.NumberFormat.Format = "#,##0.00";
        ws.Column(13).Style.NumberFormat.Format = "#,##0.00";
    }

    private static void CrearEncabezados(IXLWorksheet ws, int row)
    {
        string[] headers =
        {
            "#", "FECHA", "DOCID CLIENTE", "CLIENTE", "DOCID VENDEDOR",
            "VENDEDOR", "TIPO", "COMPLEJO", "LOTE", "PRECIO", "% INICIAL", "INICIAL"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(row, i + 2).Value = headers[i];
        }

        var range = ws.Range(row, 2, row, 13);
        range.Style.Font.Bold = true;
        range.Style.Fill.BackgroundColor = XLColor.LightGray;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }

    private static void AplicarBordes(IXLWorksheet ws, int headerRow, int lastDataRow)
    {
        var range = ws.Range(headerRow, 2, lastDataRow, 13);
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }

    private static void EscribirTotalizador(IXLWorksheet ws, int row, List<ItemVentaCnx> data)
    {
        var totalPrecio = data.Sum(x => x.DPrecio);
        var totalInicial = data.Sum(x => x.SCuotaInicial);

        ws.Cell(row, 2).Value = "TOTAL:";
        ws.Range(row, 2, row, 10).Merge();
        ws.Cell(row, 2).Style.Font.Bold = true;
        ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        ws.Cell(row, 11).Value = totalPrecio;
        ws.Cell(row, 12).Value = string.Empty;
        ws.Cell(row, 13).Value = totalInicial;

        var range = ws.Range(row, 2, row, 13);
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
