using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;

public class ComisionServicioXls
{
    public async Task<(bool success, string base64)> GetComisionServicioXls(
        List<RptComisionServicio> listado, int empresaId)
    {
        if (listado == null || !listado.Any())
            return (false, string.Empty);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Comisión Servicio");

        const int headerRow = 2;
        const int firstDataRow = headerRow + 1;

        ConfigurarColumnas(worksheet, empresaId);
        CrearEncabezados(worksheet, headerRow, empresaId);

        int currentRow = firstDataRow;

        foreach (var item in listado)
        {
            EscribirFilaDetalle(worksheet, currentRow, item, empresaId);
            currentRow++;
        }

        EscribirTotalizador(worksheet, currentRow, listado, empresaId);
        AplicarBordes(worksheet, headerRow, currentRow - 1,empresaId);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return (true, Convert.ToBase64String(stream.ToArray()));
    }
    private static void EscribirFilaDetalle(IXLWorksheet ws, int row, RptComisionServicio v , int empresaId)
    {
        decimal totalComision = v.Comision + v.Servicio;
        int fila = 2;

        ws.Cell(row, fila).Value = v.SCodigo;
        fila = fila + 1;
        ws.Cell(row, fila).Value = v.SNombreCompleto;
        if (empresaId == -1)
        {
            fila = fila + 1;
            ws.Cell(row, fila).Value = v.Empresa;
            
        }
        fila = fila + 1;
        ws.Cell(row, fila).Value = v.Comision;
        fila = fila + 1;
        ws.Cell(row, fila).Value = v.Servicio;
        fila = fila + 1;
        ws.Cell(row, fila).Value = totalComision;
        fila = fila + 1;

        // 13%
        ws.Cell(row, fila).Value =
            v.PorcentajeRetencion <= 0
                ? totalComision * 0.13m
                : 0;
        fila = fila + 1;

        // 87%
        ws.Cell(row, fila).Value =
            v.PorcentajeRetencion <= 0
                ? totalComision * 0.87m
                : 0;
        fila = fila + 1;
        ws.Cell(row, fila).Value = v.PorcentajeRetencion;
        fila = fila + 1;
        ws.Cell(row, fila).Value = v.MontoRetencion;
        fila = fila + 1;
        ws.Cell(row, fila).Value = totalComision;
        fila = fila + 1;
        ws.Cell(row,fila).Value = totalComision - v.MontoRetencion;
    }
    private static void ConfigurarColumnas(IXLWorksheet ws, int empresaId)
    {
        int fila = 2;
        ws.Column(fila).Width = 10;   // COD
        fila = fila + 1;
        ws.Column(fila).Width = 35;   // ASESOR
        if(empresaId == -1)
        {
            fila = fila + 1;
            ws.Column(fila).Width = 20;   // ASESOR
        }
        fila = fila + 1;
        ws.Column(fila).Width = 14;
        fila = fila + 1;
        ws.Column(fila).Width = 14;
        fila = fila + 1;
        ws.Column(fila).Width = 16;
        fila = fila + 1;
        ws.Column(fila).Width = 14;
        fila = fila + 1;
        ws.Column(fila).Width = 14;
        fila = fila + 1;
        ws.Column(fila).Width = 10;
        fila = fila + 1;
        ws.Column(fila).Width = 14;
        fila = fila + 1;
        ws.Column(fila).Width = 14;
        fila = fila + 1;
        ws.Column(fila).Width = 16;

        ws.Columns(4, fila).Style.NumberFormat.Format = "#,##0.00";
        ws.Columns(4, fila).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
    }
    private static void CrearEncabezados(IXLWorksheet ws, int row, int empresaId)
    {
        string[] headers = {};
        if(empresaId == -1)  
        {
            headers = new string[] {
            "COD.", "ASESOR","EMPRESA", "COMISION $", "SERVICIO $", "TOTAL COM. $",
            "13%", "87%", "RET. %", "RET. $", "TOTAL $", "TOTAL PAGAR $"};
        } else {
            headers = new string[]
            {
                "COD.", "ASESOR", "COMISION $", "SERVICIO $", "TOTAL COM. $",
                "13%", "87%", "RET. %", "RET. $", "TOTAL $", "TOTAL PAGAR $"
            };
            
        }
        ;

        for (int i = 0; i < headers.Length; i++)
            ws.Cell(row, i + 2).Value = headers[i];

        var range = ws.Range(row, 2, row, 12);
        range.Style.Font.Bold = true;
        range.Style.Fill.BackgroundColor = XLColor.LightGray;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }
    private static void AplicarBordes(IXLWorksheet ws,int headerRow,int lastDataRow, int empresaId)
    {
        var range = ws.Range(headerRow, 2, lastDataRow, empresaId == -1 ? 13 : 12);
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }
    private static void EscribirTotalizador(IXLWorksheet ws, int row, List<RptComisionServicio> data, int empresaId)
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
        ws.Range(row, 2, row, empresaId == -1 ? 4 : 3).Merge();
        ws.Cell(row, 2).Style.Font.Bold = true;
        ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        // ===============================
        // VALORES
        // ===============================
        int fila = empresaId == -1 ? 5 : 4;
        ws.Cell(row, fila).Value = totalComision;
        fila = fila + 1;
        ws.Cell(row, fila).Value = totalServicio;
        fila = fila + 1;
        ws.Cell(row, fila).Value = totalTotalComision;
        fila = fila + 1;
        ws.Cell(row, fila).Value = totalTrece;
        fila = fila + 1;
        ws.Cell(row, fila).Value = totalOchoSiete;

        fila = fila + 1;
        ws.Cell(row, fila).Value = ""; // RET % (vacío como en el footer)
        fila = fila + 1;
        ws.Cell(row, fila).Value = totalRetencion;
        fila = fila + 1;
        ws.Cell(row, fila).Value = total;
        fila = fila + 1;
        ws.Cell(row, fila).Value = totalPagar;

        // ===============================
        // ESTILO
        // ===============================
        var range = ws.Range(row, 2, row, empresaId == -1 ? 13: 12);

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


