using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;

public class ComisionResidualXls
{
    public async Task<(bool success, string base64)> GetComisionResidualXls(List<BrCalculoItem> listado)
    {
        if (listado == null || !listado.Any())
            return (false, string.Empty);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Comision residual");

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
    private static void EscribirFilaDetalle(IXLWorksheet ws, int row, BrCalculoItem v)
    {
        ws.Cell(row, 2).Value = row - 2;
        ws.Cell(row, 3).Value = v.NombreCompletoHijo;
        ws.Cell(row, 4).Value = v.DocumentoHijo;
        ws.Cell(row, 5).Value = v.NombreCompleto;
        ws.Cell(row, 6).Value = v.Documento;
      
        ws.Cell(row, 7).Value = v.Empresa;
        ws.Cell(row, 8).Value = v.Complejo;
        ws.Cell(row, 9).Value = v.Nivel;
        ws.Cell(row, 10).Value = v.ProductoId;
        ws.Cell(row, 11).Value = v.Bono;
        ws.Cell(row, 12).Value = v.PorcentajeComision; 
        ws.Cell(row, 13).Value = v.BonoResidual; 
    }
    private static void ConfigurarColumnas(IXLWorksheet ws)
    {
        ws.Column(2).Width = 5;
        ws.Column(2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        ws.Column(3).Width = 45;
        ws.Column(3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        ws.Column(4).Width = 15;
        ws.Column(4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        ws.Column(5).Width = 45;
        ws.Column(5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        ws.Column(6).Width = 15;
        ws.Column(6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        ws.Column(7).Width = 15;
        ws.Column(7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        ws.Column(8).Width = 25;
        ws.Column(8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        ws.Column(9).Width = 15;
        ws.Column(9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Column(10).Width = 15;
        ws.Column(10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Column(11).Width = 15;
        ws.Column(11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        ws.Column(12).Width = 15;
        ws.Column(12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;


        ws.Column(13).Width = 15;
        ws.Column(13).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        ws.Column(12).Style.NumberFormat.Format = "#,##0.00";
        ws.Column(13).Style.NumberFormat.Format = "#,##0.00";
    }
    private static void CrearEncabezados(IXLWorksheet ws, int row)
    {
        string[] headers =
        {
            "#", "CLIENTE", "CLIENTE CI", "ASESOR", "CI ASESOR",
            "EMPRESA", "COMPLEJO", "NIVEL","PRODUCTO", "PAGO", "%", "BONO"
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
    private static void EscribirTotalizador(IXLWorksheet ws, int row, List<BrCalculoItem> data)
    {
        decimal totalBono = data?.Sum(x => x.Bono) ?? 0;
        decimal totalResidual = data?.Sum(x => x.BonoResidual) ?? 0;

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
        ws.Cell(row, 11).Value = totalBono;
        ws.Cell(row, 12).Value = ""; 
        ws.Cell(row, 13).Value = totalResidual;
 
 
        // ===============================
        // ESTILO
        // ===============================
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


