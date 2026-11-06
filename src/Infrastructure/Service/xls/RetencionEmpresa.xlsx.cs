using ApiGuardian.Domain.Entities;
using ClosedXML.Excel;

public class RetencionEmpresaXls
{
    private static readonly string[] Encabezados =
    {
        "#", "Empresa", "Nombre", "CI", "Personal", "Grupo", "Residual", "Comisión",
        "Factura", "% Retención", "Retención", "Total a pagar"
    };

    public (bool success, string base64) GetRetencionEmpresaXls(List<RetencionEmpresaItem> listado)
    {
        if (listado == null || !listado.Any())
            return (false, string.Empty);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Retención");

        for (int columna = 0; columna < Encabezados.Length; columna++)
            worksheet.Cell(1, columna + 1).Value = Encabezados[columna];

        var encabezado = worksheet.Range(1, 1, 1, Encabezados.Length);
        encabezado.Style.Font.Bold = true;
        encabezado.Style.Fill.BackgroundColor = XLColor.LightGray;

        int fila = 2;
        int numero = 1;
        foreach (var item in listado)
        {
            worksheet.Cell(fila, 1).Value = numero++;
            worksheet.Cell(fila, 2).Value = item.EmpresaNombre;
            worksheet.Cell(fila, 3).Value = item.SNombreCompleto;
            worksheet.Cell(fila, 4).Value = item.Carnet;
            worksheet.Cell(fila, 5).Value = item.VPers;
            worksheet.Cell(fila, 6).Value = item.VGrupo;
            worksheet.Cell(fila, 7).Value = item.Residual;
            worksheet.Cell(fila, 8).Value = item.MontoComision;
            worksheet.Cell(fila, 9).Value = item.LPresentaFactura == 1 ? "Sí" : "No";
            worksheet.Cell(fila, 10).Value = item.PorcentajeRetencion / 100m;
            worksheet.Cell(fila, 11).Value = item.MontoRetencion;
            worksheet.Cell(fila, 12).Value = item.TotalComision;
            fila++;
        }

        worksheet.Range(2, 5, fila - 1, 8).Style.NumberFormat.Format = "#,##0.00";
        worksheet.Range(2, 10, fila - 1, 10).Style.NumberFormat.Format = "0.00%";
        worksheet.Range(2, 11, fila - 1, 12).Style.NumberFormat.Format = "#,##0.00";
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return (true, Convert.ToBase64String(stream.ToArray()));
    }
}
