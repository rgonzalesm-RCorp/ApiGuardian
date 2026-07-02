using ClosedXML.Excel;
using DocumentFormat.OpenXml.Office2016.Drawing.Command;
using DocumentFormat.OpenXml.Spreadsheet;

public class ComisionVentaResidualXls
{
    public async Task<(bool success, string base64)> GetComisionVentaResidualXlS(List<ListadoComisionCuotaResidual> listado)
    {
        if (listado == null || !listado.Any())
            return (false, string.Empty);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Venta Residual");

        const int headerRow = 2;
        const int firstDataRow = headerRow + 1;

        CrearEncabezados(worksheet, headerRow);
        ConfigurarColumnas(worksheet);

        int currentRow = firstDataRow;

        foreach (var item in listado)
        {
            EscribirFilaDetalle(worksheet, currentRow, item);
            currentRow++;
        }
        AplicarBordes(worksheet, headerRow, currentRow - 1);
        EscribirTotalizador(worksheet, currentRow);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return (true, Convert.ToBase64String(stream.ToArray()));
    }
     private static void ConfigurarColumnas(IXLWorksheet ws)
    {
        ws.Column(2).Width = 5;
        ws.Column(2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Column(3).Width = 15;
        ws.Column(3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Column(4).Width = 15;
        ws.Column(4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Column(5).Width = 15;
        ws.Column(5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Column(6).Width = 15;
        ws.Column(6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        ws.Column(7).Width = 25;
        ws.Column(7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        ws.Column(8).Width = 15;
        ws.Column(8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        ws.Column(9).Width = 45;
        ws.Column(9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        ws.Column(10).Width = 15;
        ws.Column(10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        ws.Column(11).Width = 45;
        ws.Column(11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left; 

        ws.Column(12).Width = 25;
        ws.Column(12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; 

        ws.Column(13).Width = 15;
        ws.Column(13).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        ws.Column(14).Width = 15;
        ws.Column(14).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right; 

        ws.Column(15).Width = 15;
        ws.Column(15).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        ws.Column(16).Width = 15;
        ws.Column(16).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right; 

        ws.Column(13).Style.NumberFormat.Format = "#,##0.00";
        ws.Column(14).Style.NumberFormat.Format = "#,##0.00";
        ws.Column(15).Style.NumberFormat.Format = "#,##0.00";
        ws.Column(16).Style.NumberFormat.Format = "#,##0.00";
    }
   
    private static void EscribirFilaDetalle(IXLWorksheet ws, int row, ListadoComisionCuotaResidual v)
    {
        ws.Cell(row, 2).Value = row - 2;
        ws.Cell(row, 3).Value = v.Recibe ? "SI":  "NO";
        ws.Cell(row, 4).Value = v.Fecha;
        ws.Cell(row, 5).Value = v.FechaRecibo;
        ws.Cell(row, 6).Value = v.Empresa;
        ws.Cell(row, 7).Value = v.Proyecto;

        ws.Cell(row, 8).Value = v.CiVendedor;
        ws.Cell(row, 9).Value = v.Vendedor;
        ws.Cell(row, 10).Value = v.CiCliente;
        ws.Cell(row, 11).Value = v.NombreCliente;
        ws.Cell(row, 12).Value = v.NroVenta;

        ws.Cell(row, 13).Value = v.Precio;
        ws.Cell(row, 14).Value = v.CuotaInicial;
        ws.Cell(row, 15).Value = v.Porcentaje;
        ws.Cell(row, 16).Value = v.TotalComision;
    }
    private static void CrearEncabezados(IXLWorksheet ws, int row)
    {
        string[] headers =
        {
            "#", "RECIBE BONO", "FECHA VTA", "FECHA PAGO", "EMPRESA", "PROYECTO", 
            "CI VENDEDOR", "VENDEDOR", "CI CLIENTE", "CLIENTE", "NRO VENTA", 
            "PRECIO", "INCIAL", "% INICIAL", "COMISION MES"
        };

        for (int i = 0; i < headers.Length; i++)
            ws.Cell(row, i + 2).Value = headers[i];

        var range = ws.Range(row, 2, row, 16);
        range.Style.Font.Bold = true;
        range.Style.Fill.BackgroundColor = XLColor.LightGray;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }
    private static void EscribirTotalizador(IXLWorksheet ws, int row)
    {
        

        ws.Cell(row, 2).Value = "TOTAL:";
        ws.Range(row, 2, row, 12).Merge();
        ws.Cell(row, 2).Style.Font.Bold = true;
        ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        ws.Cell(row, 12).FormulaA1 = $"SUM(L3:L{row - 1})";
        ws.Cell(row, 13).FormulaA1 = $"SUM(M3:M{row - 1})";
        ws.Cell(row, 14).FormulaA1 = $""; 
        ws.Cell(row, 15).FormulaA1 = $"SUM(O3:O{row - 1})"; 
 
        var range = ws.Range(row, 2, row, 16);

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
    private static void AplicarBordes(IXLWorksheet ws, int headerRow, int lastDataRow)
    {
        var range = ws.Range(headerRow, 2, lastDataRow, 16);
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }
}


