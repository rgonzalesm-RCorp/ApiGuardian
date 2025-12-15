
using ClosedXML.Excel;

public class PagarComisionxls
{
 

    public async Task<(bool success, string base64)> GetPagarComisionXls(
    List<RptPagarComision> listado)
    {
        if (listado == null || !listado.Any())
            return (false, string.Empty);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Pagar Comisión");

        const int headerRow = 2;
        const int firstDataRow = headerRow + 1;

        // CONFIGURACIÓN DE COLUMNAS
        ConfigurarColumnas(worksheet);

        // ENCABEZADOS
        CrearEncabezados(worksheet, headerRow);

        // DETALLE
        int currentRow = firstDataRow;

        foreach (var item in listado)
        {
            EscribirFilaDetalle(worksheet, currentRow, item);
            currentRow++;
        }

        // TOTALIZADORES
        EscribirTotalizador(worksheet, currentRow, listado);

        // BORDES
        AplicarBordes(worksheet, headerRow, currentRow);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return (true, Convert.ToBase64String(stream.ToArray()));
    }
    private static void ConfigurarColumnas(IXLWorksheet ws)
    {
        ws.Column(2).Width = 18;
        ws.Column(3).Width = 12;
        ws.Column(4).Width = 22;
        ws.Column(5).Width = 20;
        ws.Column(6).Width = 35;
        ws.Column(7).Width = 14;
        ws.Column(8).Width = 16;

        ws.Column(8).Style.NumberFormat.Format = "#,##0.00";
        ws.Column(8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
    }
    private static void CrearEncabezados(IXLWorksheet ws, int row)
    {
        ws.Cell(row, 2).Value = "TIPO CTA";
        ws.Cell(row, 3).Value = "COD. BANCO";
        ws.Cell(row, 4).Value = "CTA. BANCO";
        ws.Cell(row, 5).Value = "CIUDAD";
        ws.Cell(row, 6).Value = "ASESOR";
        ws.Cell(row, 7).Value = "CI";
        ws.Cell(row, 8).Value = "TOTAL A PAGAR";

        var range = ws.Range(row, 2, row, 8);
        range.Style.Font.Bold = true;
        range.Style.Fill.BackgroundColor = XLColor.LightGray;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }
    private static void EscribirFilaDetalle(IXLWorksheet ws, int row, RptPagarComision item)
    {
        ws.Cell(row, 2).Value = item.TipoCuenta;
        ws.Cell(row, 3).Value = item.CodigoBanco;
        ws.Cell(row, 4).Value = item.CuentaBanco;
        ws.Cell(row, 5).Value = item.Ciudad;
        ws.Cell(row, 6).Value = item.NombreCompleto;
        ws.Cell(row, 7).Value = item.CedulaIdentidad;

        ws.Cell(row, 8).Value =
            item.Personal +
            item.Grupo +
            item.Residual +
            item.Liderazgo -
            item.Descuento -
            item.Retencion;
    }
    private static void EscribirTotalizador(IXLWorksheet ws, int row, List<RptPagarComision> listado)
    {
        decimal total =
            listado.Sum(x => x.Personal)
            + listado.Sum(x => x.Liderazgo)
            + listado.Sum(x => x.Grupo)
            + listado.Sum(x => x.Residual)
            - listado.Sum(x => x.Descuento)
            - listado.Sum(x => x.Retencion);

        ws.Cell(row, 2).Value = "TOTAL";
        ws.Range(row, 2, row, 7).Merge();
        ws.Cell(row, 2).Style.Font.Bold = true;
        ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        ws.Cell(row, 8).Value = total;
        ws.Cell(row, 8).Style.Font.Bold = true;
        ws.Cell(row, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        var range = ws.Range(row, 2, row, 8);
        range.Style.Fill.BackgroundColor = XLColor.LightGray;
        range.Style.Border.TopBorder = XLBorderStyleValues.Thin;
        range.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        range.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
        range.Style.Border.RightBorder = XLBorderStyleValues.Thin;
    }
    private static void AplicarBordes(IXLWorksheet ws, int headerRow, int totalRow)
    {
        var detailRange = ws.Range(headerRow, 2, totalRow - 1, 8);
        detailRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        detailRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }

}