using ClosedXML.Excel;

public class PlanCarreraXls
{
    private const decimal TipoCambio = 6.96m;

    public Task<(bool success, string base64)> GetPlanCarreraXls(List<ItemPlanCarrera> listado)
    {
        if (listado.Count == 0) return Task.FromResult((false, string.Empty));
        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Bono Monte Sion");
        var columnas = new[] { "#", "CODIGO DE CLIENTE", "NRO DE CUENTA", "EMPRENDEDOR INDEPENDIENTE", "DOC. DE IDENTIDAD", "IMPORTE $US", "MONTO BS", "FECHA DE SOLIC. DE PAGO", "FORMA DE PAGO", "MONEDA DE DESTINO", "ENTIDAD DESTINO", "SUCURSAL DESTINO", "GLOSA", "EMPRESA QUE ASUME" };
        const int encabezado = 3;

        hoja.Cell(1, 1).Value = $"BONO MONTE SION - {listado[0].Ciclo?.ToUpper()}";
        hoja.Range(1, 1, 1, columnas.Length).Merge();
        hoja.Cell(1, 1).Style.Font.Bold = true;
        hoja.Cell(1, 1).Style.Font.FontColor = XLColor.White;
        hoja.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("00B050");
        hoja.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        for (var columna = 0; columna < columnas.Length; columna++) hoja.Cell(encabezado, columna + 1).Value = columnas[columna];
        var estiloEncabezado = hoja.Range(encabezado, 1, encabezado, columnas.Length).Style;
        estiloEncabezado.Font.Bold = true;
        estiloEncabezado.Font.FontColor = XLColor.White;
        estiloEncabezado.Fill.BackgroundColor = XLColor.FromHtml("00B050");
        estiloEncabezado.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        estiloEncabezado.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        estiloEncabezado.Alignment.WrapText = true;

        var fila = encabezado + 1;
        foreach (var item in listado)
        {
            hoja.Cell(fila, 1).Value = item.Nro;
            hoja.Cell(fila, 2).Value = item.Codigo ?? string.Empty;
            hoja.Cell(fila, 3).Value = item.Cuenta ?? string.Empty;
            hoja.Cell(fila, 4).Value = item.Nombre ?? string.Empty;
            hoja.Cell(fila, 5).Value = item.Carnet ?? string.Empty;
            hoja.Cell(fila, 6).Value = item.Monto;
            hoja.Cell(fila, 7).Value = item.Monto * TipoCambio;
            hoja.Cell(fila, 8).Value = DateTime.Today;
            hoja.Cell(fila, 9).Value = string.Empty;
            hoja.Cell(fila, 10).Value = string.Empty;
            hoja.Cell(fila, 11).Value = string.Empty;
            hoja.Cell(fila, 12).Value = item.Ciudad ?? string.Empty;
            hoja.Cell(fila, 13).Value = item.SubieronNivel
                ? $"BONO MONTE SION {item.Ciclo?.ToUpper()} - ASCENSO AL RANGO {item.NivelAlcanzadoCiclo?.ToUpper()}"
                : $"BONO MONTE SION {item.Ciclo?.ToUpper()}";
            hoja.Cell(fila, 14).Value = string.Empty;
            fila++;
        }

        hoja.Cell(fila, 1).Value = "TOTAL:";
        hoja.Range(fila, 1, fila, 5).Merge();
        hoja.Cell(fila, 6).Value = listado.Sum(item => item.Monto);
        hoja.Cell(fila, 7).Value = listado.Sum(item => item.Monto) * TipoCambio;
        var total = hoja.Range(fila, 1, fila, columnas.Length).Style;
        total.Font.Bold = true;
        total.Font.FontColor = XLColor.White;
        total.Fill.BackgroundColor = XLColor.FromHtml("00B050");

        var tabla = hoja.Range(encabezado, 1, fila, columnas.Length);
        tabla.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        tabla.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        hoja.Range(encabezado + 1, 6, fila, 7).Style.NumberFormat.Format = "#,##0.00";
        hoja.Range(encabezado + 1, 8, fila - 1, 8).Style.DateFormat.Format = "dd/MM/yyyy";
        hoja.SheetView.FreezeRows(encabezado);
        hoja.Range(encabezado, 1, fila - 1, columnas.Length).SetAutoFilter();
        foreach (var (columna, ancho) in new[] { (1, 6d), (2, 16d), (3, 18d), (4, 42d), (5, 16d), (6, 14d), (7, 14d), (8, 16d), (9, 16d), (10, 16d), (11, 16d), (12, 18d), (13, 42d), (14, 18d) }) hoja.Column(columna).Width = ancho;
        using var stream = new MemoryStream();
        libro.SaveAs(stream);
        return Task.FromResult((true, Convert.ToBase64String(stream.ToArray())));
    }
}
