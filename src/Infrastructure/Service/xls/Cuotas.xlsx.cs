using ClosedXML.Excel;
using DocumentFormat.OpenXml.Office2016.Drawing.Command;
using DocumentFormat.OpenXml.Spreadsheet;

public class CuotaXls
{
    public async Task<(bool success, string base64)> GetCuotaXlS(List<TCuota> listado)
    {
        if (listado == null || !listado.Any())
            return (false, string.Empty);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Detalle de Cuotas");

        const int headerRow = 2;
        const int firstDataRow = headerRow + 1;

        //ConfigurarColumnas(worksheet);
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
    private static void EscribirFilaDetalle(IXLWorksheet ws, int row, TCuota v)
    {
        ws.Cell(row, 2).Value = row - 2;
        ws.Cell(row, 3).Value = v.Docidven;
        ws.Cell(row, 4).Value = v.Vendedor;
        ws.Cell(row, 5).Value = v.Docidcli;
        ws.Cell(row, 6).Value = v.Cliente;
        ws.Cell(row, 7).Value = v.Proyecto;
        ws.Cell(row, 8).Value = v.Fecha_Venta;
        ws.Cell(row, 9).Value = v.Idventa;
        ws.Cell(row, 10).Value = v.Idproducto;
        ws.Cell(row, 11).Value = v.Capital;
        ws.Cell(row, 12).Value = v.Interes;
        ws.Cell(row, 13).Value = v.Seguro;
        ws.Cell(row, 14).Value = v.Expensa;
        ws.Cell(row, 15).Value = v.Multa;
        ws.Cell(row, 16).Value = v.Acuenta;
        ws.Cell(row, 17).Value = v.Amortizacion;
        ws.Cell(row, 18).Value = v.Totalpago;
        ws.Cell(row, 19).Value = v.Idrecibo;
        ws.Cell(row, 20).Value = v.Descripcion;
    }
    private static void ConfigurarColumnas(IXLWorksheet ws)
    {
        ws.Column(2).Width = 5;
        ws.Column(2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Column(3).Width = 15;
        ws.Column(3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Column(4).Width = 45;
        ws.Column(4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        ws.Column(5).Width = 15;
        ws.Column(5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        ws.Column(6).Width = 20;
        ws.Column(6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        ws.Column(7).Width = 20;
        ws.Column(7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        ws.Column(8).Width = 15;
        ws.Column(8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        ws.Column(9).Width = 15;
        ws.Column(9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        ws.Column(10).Width = 20;
        ws.Column(10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Column(11).Width = 15;

        ws.Column(12).Width = 30;
        ws.Column(12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Column(13).Width = 25;
        ws.Column(13).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        ws.Column(14).Width = 20;
        ws.Column(14).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        

        ws.Column(11).Style.NumberFormat.Format = "#,##0.00";
        ws.Column(11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        ws.Column(13).Style.NumberFormat.Format = "#,##0.00";
    }
    private static void CrearEncabezados(IXLWorksheet ws, int row)
    {
        string[] headers =
        {
            "#", "CI VENDEDOR", "VENDEDOR", "CI CLIENTE", "CLIENTE",
            "PROYECTO","FECHA VENTA", "NRO VENTA", "LOTE", "CAPITAL", "INTERES", "SEGURO", "EXPENSA", "MULTA", "A CUENTA", "AMORTIZACION",
            "TOTAL PAGO", "NRO RECIBO", "TIPO PAGO"
        };

        for (int i = 0; i < headers.Length; i++)
            ws.Cell(row, i + 2).Value = headers[i];

        var range = ws.Range(row, 2, row, 20);
        range.Style.Font.Bold = true;
        range.Style.Fill.BackgroundColor = XLColor.LightGray;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }
    private static void AplicarBordes(IXLWorksheet ws, int headerRow, int lastDataRow)
    {
        var range = ws.Range(headerRow, 2, lastDataRow, 20);
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }
    private static void EscribirTotalizador(IXLWorksheet ws, int row, List<TCuota> data)
    {
        decimal totalCapital = data?.Sum(x => x.Capital) ?? 0;
        decimal totalInteres = data?.Sum(x => x.Interes) ?? 0;
        decimal totalSeguro = data?.Sum(x => x.Seguro) ?? 0;
        decimal totalExpensa = data?.Sum(x => x.Expensa) ?? 0;
        decimal totalMulta = data?.Sum(x => x.Multa) ?? 0;
        decimal totalACuenta = data?.Sum(x => x.Acuenta) ?? 0;
        decimal totalTotalPago = data?.Sum(x => x.Totalpago) ?? 0;
        decimal totalAmortizacion = data?.Sum(x => x.Amortizacion) ?? 0;

        // ===============================
        // TEXTO TOTAL
        // ===============================
        ws.Cell(row, 2).Value = "TOTAL:";
        ws.Range(row, 2, row, 10).Merge();
        ws.Cell(row, 2).Style.Font.Bold = true;
        ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        // ===============================
        // VALORES
        // ===============================
        ws.Cell(row, 11).Value = totalCapital;
        ws.Cell(row, 12).Value = totalInteres; 
        ws.Cell(row, 13).Value = totalSeguro;

        ws.Cell(row, 14).Value = totalExpensa;
        ws.Cell(row, 15).Value = totalMulta;
        ws.Cell(row, 16).Value = totalACuenta;
        ws.Cell(row, 17).Value = totalAmortizacion;
        ws.Cell(row, 18).Value = totalTotalPago;
 
        // ===============================
        // ESTILO
        // ===============================
        var range = ws.Range(row, 2, row, 20);

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


