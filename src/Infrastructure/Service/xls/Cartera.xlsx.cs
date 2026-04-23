using ClosedXML.Excel;
using DocumentFormat.OpenXml.Office2016.Drawing.Command;
using DocumentFormat.OpenXml.Spreadsheet;

public class CarteraXls
{
    public async Task<(bool success, string base64)> GetCarteraXlS(List<TCartera> listado)
    {
        if (listado == null || !listado.Any())
            return (false, string.Empty);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Detalle de Cartera");

        const int headerRow = 2;
        const int firstDataRow = headerRow + 1;

        CrearEncabezados(worksheet, headerRow);

        int currentRow = firstDataRow;

        foreach (var item in listado)
        {
            EscribirFilaDetalle(worksheet, currentRow, item);
            currentRow++;
        }
        AplicarBordes(worksheet, headerRow, currentRow - 1);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return (true, Convert.ToBase64String(stream.ToArray()));
    }
    private static void EscribirFilaDetalle(IXLWorksheet ws, int row, TCartera v)
    {
        ws.Cell(row, 2).Value = row - 2;
        ws.Cell(row, 3).Value = v.Docid;
        ws.Cell(row, 4).Value = v.Cliente;
        ws.Cell(row, 5).Value = v.DocidVendedor;
        ws.Cell(row, 6).Value = v.Nombre;
        ws.Cell(row, 7).Value = v.Fecha;
        ws.Cell(row, 8).Value = v.Proyecto;
        ws.Cell(row, 9).Value = v.Idventa;
        ws.Cell(row, 10).Value = v.Lote;
        ws.Cell(row, 11).Value = v.Totalventa;
        ws.Cell(row, 12).Value = v.Cuotainicial;
        ws.Cell(row, 13).Value = v.Estado;
    }
    private static void CrearEncabezados(IXLWorksheet ws, int row)
    {
        string[] headers =
        {
            "#", "CI CLIENTE", "CLIENTE", "CI VENDEDOR", "VENDEDOR", 
            "FECHA", "PROYECTO", "NRO VENTA", "LOTE", "MONTO", "INICIAL", "ESTADO"
        };

        for (int i = 0; i < headers.Length; i++)
            ws.Cell(row, i + 2).Value = headers[i];

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
}


