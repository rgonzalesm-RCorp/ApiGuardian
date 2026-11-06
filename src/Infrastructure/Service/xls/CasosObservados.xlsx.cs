using ClosedXML.Excel;

public class CasosObservadosXls
{
    private const int LastColumn = 14;

    public (bool success, string base64) GetCasosObservadosXls(List<ItemCasoObservado> listado)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Casos Observados");

        const int headerRow = 1;
        const int firstDataRow = headerRow + 1;

        ConfigurarColumnas(worksheet);
        CrearEncabezados(worksheet, headerRow);

        var currentRow = firstDataRow;
        foreach (var item in listado)
        {
            EscribirFilaDetalle(worksheet, currentRow, item, currentRow - headerRow);
            currentRow++;
        }

        if (listado.Count > 0)
        {
            AplicarBordes(worksheet, headerRow, currentRow - 1);
        }
        worksheet.RangeUsed()?.SetAutoFilter();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return (true, Convert.ToBase64String(stream.ToArray()));
    }

    private static void EscribirFilaDetalle(IXLWorksheet ws, int row, ItemCasoObservado item, int numero)
    {
        ws.Cell(row, 1).Value = numero;
        ws.Cell(row, 2).Value = ObtenerTipoCasoLabel(item.TipoCaso);

        if (item.FechaVenta.HasValue)
        {
            ws.Cell(row, 3).Value = item.FechaVenta.Value;
            ws.Cell(row, 3).Style.DateFormat.Format = "dd/MM/yyyy";
        }
        else
        {
            ws.Cell(row, 3).Value = "-";
        }

        ws.Cell(row, 4).Value = item.NroVenta ?? string.Empty;
        ws.Cell(row, 5).Value = item.Cliente ?? string.Empty;
        ws.Cell(row, 6).Value = item.ClienteDocId ?? string.Empty;
        ws.Cell(row, 7).Value = item.ClienteId.HasValue ? ObtenerEstadoBaja(item.ClienteBaja) : "-";
        ws.Cell(row, 8).Value = item.Patrocinador ?? string.Empty;
        ws.Cell(row, 9).Value = item.PatrocinadorDocId ?? string.Empty;
        ws.Cell(row, 10).Value = item.Vendedor ?? string.Empty;
        ws.Cell(row, 11).Value = item.VendedorDocId ?? string.Empty;
        ws.Cell(row, 12).Value = item.VendedorId.HasValue ? ObtenerEstadoBaja(item.VendedorBaja) : "-";
        ws.Cell(row, 13).Value = item.Motivo ?? string.Empty;
        ws.Cell(row, 14).Value = item.Estado ?? string.Empty;
    }

    private static void ConfigurarColumnas(IXLWorksheet ws)
    {
        ws.Column(1).Width = 5;
        ws.Column(2).Width = 28;
        ws.Column(3).Width = 13;
        ws.Column(4).Width = 16;
        ws.Column(5).Width = 35;
        ws.Column(6).Width = 16;
        ws.Column(7).Width = 14;
        ws.Column(8).Width = 35;
        ws.Column(9).Width = 16;
        ws.Column(10).Width = 35;
        ws.Column(11).Width = 16;
        ws.Column(12).Width = 14;
        ws.Column(13).Width = 50;
        ws.Column(14).Width = 14;

        ws.Columns(1, LastColumn).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Column(3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Column(7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Column(12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Column(14).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    private static void CrearEncabezados(IXLWorksheet ws, int row)
    {
        string[] headers =
        {
            "#", "TIPO", "FECHA VENTA", "NRO VENTA", "CLIENTE", "DOCID CLIENTE", "ESTADO CLIENTE",
            "PATROCINADOR", "DOCID PATROCINADOR", "VENDEDOR", "DOCID VENDEDOR", "ESTADO VENDEDOR",
            "MOTIVO", "ESTADO"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(row, i + 1).Value = headers[i];
        }

        var range = ws.Range(row, 1, row, LastColumn);
        range.Style.Font.Bold = true;
        range.Style.Fill.BackgroundColor = XLColor.LightGray;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }

    private static void AplicarBordes(IXLWorksheet ws, int headerRow, int lastDataRow)
    {
        var range = ws.Range(headerRow, 1, lastDataRow, LastColumn);
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }

    private static string ObtenerEstadoBaja(string? valor) =>
        string.Equals(valor?.Trim(), "1", StringComparison.OrdinalIgnoreCase) ? "BAJA" : "ACTIVO";

    private static string ObtenerTipoCasoLabel(string tipoCaso) => tipoCaso switch
    {
        "DOBLE_PATROCINIO" => "Doble patrocinio",
        "VENDEDOR_DADO_BAJA" => "Vendedor dado de baja",
        "CLIENTE_DADO_BAJA" => "Cliente dado de baja",
        "VENDEDOR_SIN_VENTAS_UN_ANIO" => "Vendedor sin ventas en un año",
        "CLIENTE_SIN_COMPRAS_UN_ANIO" => "Cliente sin compras en un año",
        "AUTO_COMPRA" => "Auto compra",
        _ => tipoCaso
    };
}
