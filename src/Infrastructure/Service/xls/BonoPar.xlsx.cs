using ClosedXML.Excel;

public class BonoParXls
{
    
    #region "Informe principal"
    public async Task<(bool success, string base64)> GetBonoParXls(List<ItemBonoPar> listado)
    {
        if (listado == null || !listado.Any())
            return (false, string.Empty);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Bono par");

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
        InformeDetalle(listado, workbook);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return (true, Convert.ToBase64String(stream.ToArray()));
    }
    private static void EscribirFilaDetalle(IXLWorksheet ws, int row, ItemBonoPar v)
    {
        ws.Cell(row, 2).Value = row - 2;
        ws.Cell(row, 3).Value = v.LContctoGanadorId;
        ws.Cell(row, 4).Value = v.SCedulaIdentidadGanador;
        ws.Cell(row, 5).Value = v.SNombreGanador;
        ws.Cell(row, 6).Value = v.PersonaQueVendieron;
        ws.Cell(row, 7).Value = v.CantidadVenta;
        ws.Cell(row, 8).Value = v.MontoVentas;
        ws.Cell(row, 9).Value = v.CuotasIniciales;
        ws.Cell(row, 10).Value = v.Bono;
    }
    private static void ConfigurarColumnas(IXLWorksheet ws)
    {
        ws.Column(2).Width = 5;
        ws.Column(2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Column(3).Width = 15;
        ws.Column(3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Column(4).Width = 15;
        ws.Column(4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        ws.Column(5).Width = 45;
        ws.Column(5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        ws.Column(6).Width = 15;
        ws.Column(6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Column(7).Width = 15;
        ws.Column(7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Column(8).Width = 15;
        ws.Column(8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        ws.Column(9).Width = 15;
        ws.Column(9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        ws.Column(10).Width = 15;
        ws.Column(10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right; 

        ws.Column(8).Style.NumberFormat.Format = "#,##0.00";
        ws.Column(9).Style.NumberFormat.Format = "#,##0.00";
        ws.Column(910).Style.NumberFormat.Format = "#,##0.00";
    }
    private static void CrearEncabezados(IXLWorksheet ws, int row)
    {
        string[] headers =
        {
            "#", "GANADOR ID","DOC ID", "NOMBRE", "REDES ACTIVAS", "CANT. VTA",
            "MONTO $us", "CUOTA INICIAL $us", "BONO $US"
        };

        for (int i = 0; i < headers.Length; i++)
            ws.Cell(row, i + 2).Value = headers[i];

        var range = ws.Range(row, 2, row, 10);
        range.Style.Font.Bold = true;
        range.Style.Fill.BackgroundColor = XLColor.LightGray;
        //range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }
    private static void AplicarBordes(IXLWorksheet ws, int headerRow, int lastDataRow)
    {
        var range = ws.Range(headerRow, 2, lastDataRow, 10);
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }
    private static void EscribirTotalizador(IXLWorksheet ws, int row, List<ItemBonoPar> data)
    {
        decimal TotalVentas = data?.Sum(x => x.MontoVentas) ?? 0;
        decimal TotalInicial = data?.Sum(x => x.CuotasIniciales) ?? 0;
        decimal TotalBono = data?.Sum(x => x.Bono) ?? 0;

        ws.Cell(row, 2).Value = "TOTAL:";
        ws.Range(row, 2, row, 7).Merge();
        ws.Cell(row, 2).Style.Font.Bold = true;
        ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        ws.Cell(row, 8).FormulaA1 = $"SUM(H3:H{row - 1})";
        ws.Cell(row, 9).FormulaA1 = $"SUM(I3:I{row - 1})";
        ws.Cell(row, 10).FormulaA1 = $"SUM(J3:J{row - 1})"; 
 
        var range = ws.Range(row, 2, row, 10);

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
    #endregion
    #region "Informe detalle"
    public static void InformeDetalle(List<ItemBonoPar> listado, XLWorkbook workbook)
    {
        if (listado == null || !listado.Any())
            return ;
 
        var worksheet = workbook.Worksheets.Add("Detalle Bono par");

        const int headerRow = 2;
        const int firstDataRow = headerRow + 1;

        ConfigurarColumnasDetalle(worksheet);
        CrearEncabezadosDetalle(worksheet, headerRow);

        int currentRow = firstDataRow;
        var listaFinal = listado.SelectMany(x => x.ListaDetalleBonoPar)
                            .Select(d => new ItemBonoParDetalle
                            {
                                LContactoGanadorId = d.LContactoGanadorId,
                                LContactoVendedorId = d.LContactoVendedorId,
                                SNombreVendedor = d.SNombreVendedor,
                                SCedulaIdentidadVendedor = d.SCedulaIdentidadVendedor,
                                LContactoClienteId = d.LContactoClienteId,
                                SNombreCliente = d.SNombreCliente,
                                SCedulaCliente = d.SCedulaCliente,
                                LContratoId = d.LContratoId,
                                DtFecha = d.DtFecha,
                                SNroVenta = d.SNroVenta,
                                DPrecio = d.DPrecio,
                                DCuotaInicial = d.DCuotaInicial
                            })
                            .ToList();
        listaFinal = listaFinal.OrderBy(x => x.LContactoGanadorId).ToList();

        foreach (var item in listaFinal)
        { 
            EscribirFilaDetalleDetalle(worksheet, currentRow, item);
            currentRow++;
        }
        
        EscribirTotalizadorDetalle(worksheet, currentRow, listaFinal);
        AplicarBordesDetalle(worksheet, headerRow, currentRow - 1);

        return ;
    }
    private static void CrearEncabezadosDetalle(IXLWorksheet ws, int row)
    {
        string[] headers =
        {
            "#","GANADOR ID", "FECHA", "CI VENDEDOR", "VENDEDOR", "CI CLIENTE","CLIENTE", "PRODUCTO",
            "MONTO $us", "CUOTA INICIAL $us"
        };

        for (int i = 0; i < headers.Length; i++)
            ws.Cell(row, i + 2).Value = headers[i];

        var range = ws.Range(row, 2, row, 11);
        range.Style.Font.Bold = true;
        range.Style.Fill.BackgroundColor = XLColor.LightGray;
        //range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }
    private static void EscribirFilaDetalleDetalle(IXLWorksheet ws, int row, ItemBonoParDetalle v)
    {
        ws.Cell(row, 2).Value = row - 2;
        ws.Cell(row, 3).Value = v.LContactoGanadorId;
        ws.Cell(row, 4).Value = v.DtFecha.ToString("dd/MM/yyyy");
        ws.Cell(row, 5).Value = v.SCedulaIdentidadVendedor;
        ws.Cell(row, 6).Value = v.SNombreVendedor;
        ws.Cell(row, 7).Value = v.SCedulaCliente;
        ws.Cell(row, 8).Value = v.SNombreCliente;
        ws.Cell(row, 9).Value = v.SNroVenta;
        ws.Cell(row, 10).Value = v.DPrecio;
        ws.Cell(row, 11).Value = v.DCuotaInicial;
    }
    private static void EscribirTotalizadorDetalle(IXLWorksheet ws, int row, List<ItemBonoParDetalle> data)
    {

        ws.Cell(row, 2).Value = "TOTAL:";
        ws.Range(row, 2, row, 9).Merge();
        ws.Cell(row, 2).Style.Font.Bold = true;
        ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        ws.Cell(row, 10).FormulaA1 = $"SUM(J3:J{row - 1})";
        ws.Cell(row, 11).FormulaA1 = $"SUM(K3:K{row - 1})";

        var range = ws.Range(row, 2, row, 11);

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
    private static void ConfigurarColumnasDetalle(IXLWorksheet ws)
    {
        ws.Column(2).Width = 5;
        ws.Column(2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Column(3).Width = 15;
        ws.Column(3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Column(4).Width = 15;
        ws.Column(4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Column(5).Width = 15;
        ws.Column(5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        ws.Column(6).Width = 45;
        ws.Column(6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        ws.Column(7).Width = 15;
        ws.Column(7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        ws.Column(8).Width = 45;
        ws.Column(8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        ws.Column(9).Width = 25;
        ws.Column(9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Column(10).Width = 15;
        ws.Column(10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right; 
        ws.Column(11).Width = 15;
        ws.Column(11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right; 
 
        ws.Column(10).Style.NumberFormat.Format = "#,##0.00";
        ws.Column(11).Style.NumberFormat.Format = "#,##0.00";
    }
    private static void AplicarBordesDetalle(IXLWorksheet ws, int headerRow, int lastDataRow)
    {
        var range = ws.Range(headerRow, 2, lastDataRow, 11);
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }
    #endregion
}


