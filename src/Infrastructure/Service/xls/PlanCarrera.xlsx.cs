using ClosedXML.Excel;

public class PlanCarreraXls
{
    public Task<(bool success, string base64)> GetPlanCarreraXls(List<ItemPlanCarrera> listado, string moneda, decimal tipoCambioOficial)
    {
        if (listado.Count == 0) return Task.FromResult((false, string.Empty));
        var equivalente = moneda == "SUS";
        var columnas = new List<string> { "#", "CODIGO DE CLIENTE", "NRO DE CUENTA", "EMPRENDEDOR INDEPENDIENTE", "DOC. DE IDENTIDAD", $"IMPORTE {moneda}" };
        if (equivalente) columnas.Add("MONTO BS");
        columnas.AddRange(["FECHA DE SOLIC. DE PAGO", "FORMA DE PAGO", "MONEDA DE DESTINO", "ENTIDAD DESTINO", "SUCURSAL DESTINO", "GLOSA", "EMPRESA QUE ASUME"]);
        using var libro = new XLWorkbook(); var hoja = libro.Worksheets.Add("Bono Monte Sion"); const int encabezado = 3;
        hoja.Cell(1, 1).Value = $"BONO MONTE SION - {listado[0].Ciclo?.ToUpper()}"; hoja.Range(1, 1, 1, columnas.Count).Merge(); hoja.Cell(1, 1).Style.Font.Bold = true; hoja.Cell(1, 1).Style.Font.FontColor = XLColor.White; hoja.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("00B050"); hoja.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        for (var i = 0; i < columnas.Count; i++) hoja.Cell(encabezado, i + 1).Value = columnas[i];
        var estilo = hoja.Range(encabezado, 1, encabezado, columnas.Count).Style; estilo.Font.Bold = true; estilo.Font.FontColor = XLColor.White; estilo.Fill.BackgroundColor = XLColor.FromHtml("00B050"); estilo.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; estilo.Alignment.WrapText = true;
        var fila = encabezado + 1;
        foreach (var i in listado)
        {
            var c = 1; hoja.Cell(fila, c++).Value = i.Nro; hoja.Cell(fila, c++).Value = i.Codigo ?? string.Empty; hoja.Cell(fila, c++).Value = i.Cuenta ?? string.Empty; hoja.Cell(fila, c++).Value = i.Nombre ?? string.Empty; hoja.Cell(fila, c++).Value = i.Carnet ?? string.Empty; hoja.Cell(fila, c++).Value = i.Monto;
            if (equivalente) hoja.Cell(fila, c++).Value = i.Monto * tipoCambioOficial;
            hoja.Cell(fila, c++).Value = DateTime.Today; hoja.Cell(fila, c++).Value = string.Empty; hoja.Cell(fila, c++).Value = string.Empty; hoja.Cell(fila, c++).Value = string.Empty; hoja.Cell(fila, c++).Value = i.Ciudad ?? string.Empty; hoja.Cell(fila, c++).Value = i.SubieronNivel ? $"BONO MONTE SION {i.Ciclo?.ToUpper()} - ASCENSO AL RANGO {i.NivelAlcanzadoCiclo?.ToUpper()}" : $"BONO MONTE SION {i.Ciclo?.ToUpper()}"; hoja.Cell(fila, c).Value = string.Empty; fila++;
        }
        hoja.Cell(fila, 1).Value = "TOTAL:"; hoja.Range(fila, 1, fila, 5).Merge(); hoja.Cell(fila, 6).Value = listado.Sum(i => i.Monto); if (equivalente) hoja.Cell(fila, 7).Value = listado.Sum(i => i.Monto) * tipoCambioOficial;
        var total = hoja.Range(fila, 1, fila, columnas.Count).Style; total.Font.Bold = true; total.Font.FontColor = XLColor.White; total.Fill.BackgroundColor = XLColor.FromHtml("00B050");
        var tabla = hoja.Range(encabezado, 1, fila, columnas.Count); tabla.Style.Border.OutsideBorder = XLBorderStyleValues.Thin; tabla.Style.Border.InsideBorder = XLBorderStyleValues.Thin; hoja.Range(encabezado + 1, 6, fila, equivalente ? 7 : 6).Style.NumberFormat.Format = "#,##0.00"; hoja.Range(encabezado + 1, equivalente ? 8 : 7, fila - 1, equivalente ? 8 : 7).Style.DateFormat.Format = "dd/MM/yyyy"; hoja.SheetView.FreezeRows(encabezado); hoja.Range(encabezado, 1, fila - 1, columnas.Count).SetAutoFilter();
        for (var c = 1; c <= columnas.Count; c++) hoja.Column(c).AdjustToContents();
        using var stream = new MemoryStream(); libro.SaveAs(stream); return Task.FromResult((true, Convert.ToBase64String(stream.ToArray())));
    }
}
