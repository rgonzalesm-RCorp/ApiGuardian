using ClosedXML.Excel;

public class ContratosXls
{
    public (bool Success, string Base64) Generar(
        IReadOnlyCollection<ListaAdministracionContrato> contratos,
        DateTime fechaInicio,
        DateTime fechaFin
    )
    {
        if (contratos.Count == 0)
        {
            return (false, string.Empty);
        }

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Contratos");

        worksheet.Cell(1, 1).Value = "REPORTE DE CONTRATOS";
        worksheet.Range(1, 1, 1, 14).Merge();
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 14;
        worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        worksheet.Cell(2, 1).Value = $"Periodo: {fechaInicio:dd/MM/yyyy} - {fechaFin:dd/MM/yyyy}";
        worksheet.Range(2, 1, 2, 14).Merge();
        worksheet.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        string[] headers =
        {
            "ID",
            "FECHA REGISTRO",
            "FECHA",
            "NRO. VENTA",
            "PROPIETARIO",
            "COMPLEJO",
            "MANZANO",
            "LOTE",
            "PRECIO INICIAL",
            "CUOTA INICIAL",
            "PRECIO",
            "ESTADO",
            "TIPO CONTRATO",
            "ASESOR"
        };

        const int headerRow = 4;
        for (var index = 0; index < headers.Length; index++)
        {
            worksheet.Cell(headerRow, index + 1).Value = headers[index];
        }

        var headerRange = worksheet.Range(headerRow, 1, headerRow, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Fill.BackgroundColor = XLColor.DarkBlue;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        var row = headerRow + 1;
        foreach (var contrato in contratos)
        {
            worksheet.Cell(row, 1).Value = contrato.LcontratoId;
            worksheet.Cell(row, 2).Value = contrato.FechaRegistro;
            worksheet.Cell(row, 3).Value = contrato.Fecha;
            worksheet.Cell(row, 4).Value = contrato.NroVenta ?? string.Empty;
            worksheet.Cell(row, 5).Value = contrato.Propietario ?? string.Empty;
            worksheet.Cell(row, 6).Value = contrato.Complejo ?? string.Empty;
            worksheet.Cell(row, 7).Value = contrato.NroMnzo ?? string.Empty;
            worksheet.Cell(row, 8).Value = contrato.NroLote ?? string.Empty;
            worksheet.Cell(row, 9).Value = contrato.DPecioInicial;
            worksheet.Cell(row, 10).Value = contrato.CuotaInicial;
            worksheet.Cell(row, 11).Value = contrato.Precio;
            worksheet.Cell(row, 12).Value = contrato.EstadoContrato ?? string.Empty;
            worksheet.Cell(row, 13).Value = contrato.TipoContrato ?? string.Empty;
            worksheet.Cell(row, 14).Value = contrato.Asesor ?? string.Empty;
            row++;
        }

        worksheet.Cell(row, 1).Value = "TOTALES:";
        worksheet.Range(row, 1, row, 8).Merge();
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        worksheet.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        worksheet.Cell(row, 9).Value = contratos.Sum(item => item.DPecioInicial);
        worksheet.Cell(row, 10).Value = contratos.Sum(item => item.CuotaInicial);
        worksheet.Cell(row, 11).Value = contratos.Sum(item => item.Precio);
        worksheet.Range(row, 9, row, 11).Style.Font.Bold = true;

        worksheet.Range(headerRow + 1, 2, row - 1, 3).Style.DateFormat.Format = "dd/MM/yyyy";
        worksheet.Range(headerRow + 1, 9, row, 11).Style.NumberFormat.Format = "#,##0.00";
        worksheet.Range(headerRow, 1, row, headers.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        worksheet.Range(headerRow, 1, row, headers.Length).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        worksheet.Range(headerRow, 1, row - 1, headers.Length).SetAutoFilter();
        worksheet.SheetView.FreezeRows(headerRow);
        worksheet.Columns(1, headers.Length).AdjustToContents(5, 45);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return (true, Convert.ToBase64String(stream.ToArray()));
    }
}
