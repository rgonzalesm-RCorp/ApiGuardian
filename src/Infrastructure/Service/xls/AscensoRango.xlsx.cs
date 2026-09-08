using ClosedXML.Excel;

public class AscensoRangoXls
{
    public Task<(bool success, string base64)> GetAscensoRangoXls(List<ItemAscensoRango> listado)
    {
        if (listado.Count == 0) return Task.FromResult((false, string.Empty));
        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Ascenso de rango");
        var columnas = new[] { "#", "MES", "FREELANCE", "CEDULA DE IDENTIDAD", "CELULAR", "CIUDAD DE RESIDENCIA", "PAIS", "NIVEL ALCANZADO", "ADICIONAL", "RANGOS TOTALES ASCENDIDOS" };
        const int encabezado = 4;
        hoja.Cell(1, 1).Value = $"LISTADO DE NUEVOS ASCENSOS - MONTE SION {listado[0].Mes?.ToUpper()}";
        hoja.Range(1, 1, 1, columnas.Length).Merge();
        hoja.Cell(1, 1).Style.Font.Bold = true;
        hoja.Cell(1, 1).Style.Font.FontColor = XLColor.Red;
        hoja.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        for (var col = 0; col < columnas.Length; col++) hoja.Cell(encabezado, col + 1).Value = columnas[col];
        var cabecera = hoja.Range(encabezado, 1, encabezado, columnas.Length).Style;
        cabecera.Font.Bold = true; cabecera.Font.FontColor = XLColor.White; cabecera.Fill.BackgroundColor = XLColor.FromHtml("00B050"); cabecera.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; cabecera.Alignment.WrapText = true;
        hoja.Cell(encabezado, 8).Style.Fill.BackgroundColor = XLColor.Yellow;
        hoja.Cell(encabezado, 8).Style.Font.FontColor = XLColor.Black;
        var fila = encabezado + 1;
        foreach (var item in listado)
        {
            hoja.Cell(fila, 1).Value = item.Nro; hoja.Cell(fila, 2).Value = item.Mes ?? string.Empty; hoja.Cell(fila, 3).Value = item.Nombre ?? string.Empty; hoja.Cell(fila, 4).Value = item.CI ?? string.Empty; hoja.Cell(fila, 5).Value = item.Telefono ?? string.Empty; hoja.Cell(fila, 6).Value = item.Ciudad ?? string.Empty; hoja.Cell(fila, 7).Value = item.Pais ?? string.Empty; hoja.Cell(fila, 8).Value = item.NivelAlcanzado ?? string.Empty; hoja.Cell(fila, 9).Value = item.Adicional ?? string.Empty; hoja.Cell(fila, 10).Value = item.RangosTotalesAscendidos ?? string.Empty; fila++;
        }
        var tabla = hoja.Range(encabezado, 1, fila - 1, columnas.Length);
        tabla.Style.Border.OutsideBorder = XLBorderStyleValues.Thin; tabla.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        hoja.SheetView.FreezeRows(encabezado); hoja.Range(encabezado, 1, fila - 1, columnas.Length).SetAutoFilter();
        foreach (var (col, width) in new[] { (1, 6d), (2, 15d), (3, 42d), (4, 18d), (5, 18d), (6, 20d), (7, 14d), (8, 26d), (9, 36d), (10, 42d) }) hoja.Column(col).Width = width;
        using var stream = new MemoryStream(); libro.SaveAs(stream); return Task.FromResult((true, Convert.ToBase64String(stream.ToArray())));
    }
}
