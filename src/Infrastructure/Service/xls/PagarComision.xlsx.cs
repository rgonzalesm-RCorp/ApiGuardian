
using ClosedXML.Excel;

public class PagarComisionxls
{
 

    public async Task<(bool success, string base64)> GetPagarComisionXls(
    List<RptPagarComision> listado, List<RptProrrateo> prorrateo, List<EmpresaHeaderPagarComision> headerEmpresa)
    {
        if (listado == null || !listado.Any())
            return (false, string.Empty);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Pagar Comisión");

        const int headerRow = 2;
        const int firstDataRow = headerRow + 1;

        // CONFIGURACIÓN DE COLUMNAS
        ConfigurarColumnas(worksheet, headerEmpresa);

        // ENCABEZADOS
        CrearEncabezados(worksheet, headerRow, headerEmpresa);

        // DETALLE
        int currentRow = firstDataRow;
        var grupos = prorrateo
        .GroupBy(x => new { x.LContactoId, x.EmpresaId })
        .Select(g => new
        {
            g.Key.LContactoId,
            g.Key.EmpresaId,
            Prorrateo = g.Sum(x => x.Prorrateo)
        })
        .ToList();

        var retencionPorContacto = prorrateo
        .GroupBy(x => x.LContactoId)
        .ToDictionary(
            g => g.Key,
            g => g.Sum(x => x.Retencion)
        );

        var prorrateoLookup = new Dictionary<(int LContactoId, int EmpresaId), decimal>();

        foreach (var contacto in grupos.GroupBy(x => x.LContactoId))
        {
            var lContactoId = contacto.Key;
            var retencionTotal = retencionPorContacto.GetValueOrDefault(lContactoId);

            var empresa21 = contacto.FirstOrDefault(x => x.EmpresaId == 21);
            var empresa2 = contacto.FirstOrDefault(x => x.EmpresaId == 2);

            foreach (var item in contacto)
            {
                prorrateoLookup[(item.LContactoId, item.EmpresaId)] = item.Prorrateo;
            }

            if (retencionTotal <= 0 && empresa21 != null)
            {
                prorrateoLookup[(lContactoId, 21)] = 0m;
                if (empresa2 != null)
                {
                    prorrateoLookup[(lContactoId, 2)] = empresa2.Prorrateo + empresa21.Prorrateo;
                }
                else
                {
                    prorrateoLookup[(lContactoId, 2)] = empresa21.Prorrateo;
                }
            }
        }




        foreach (var item in listado)
        {
            EscribirFilaDetalle(worksheet, currentRow, item);
            int rowAux = 8;
            decimal montoTotal = 0;
            foreach (var itemH in headerEmpresa)
            {
                if (prorrateoLookup.TryGetValue((item.LContactold, itemH.EmpresaId), out var monto))
                {
                    montoTotal += monto;
                    worksheet.Cell(currentRow, rowAux).Value = monto;
                }
                else
                {
                    worksheet.Cell(currentRow, rowAux).Value = 0;
                }
                rowAux +=1;
            }
            worksheet.Cell(currentRow, rowAux).Value = montoTotal;
            currentRow++;
        }

        // TOTALIZADORES
        EscribirTotalizador(worksheet, currentRow, listado, headerEmpresa, prorrateoLookup);

        // BORDES
        AplicarBordes(worksheet, headerRow, currentRow, headerEmpresa);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return (true, Convert.ToBase64String(stream.ToArray()));
    }
    private static void ConfigurarColumnas(IXLWorksheet ws, List<EmpresaHeaderPagarComision> headerEmpresa)
    {
        ws.Column(2).Width = 18;
        ws.Column(3).Width = 12;
        ws.Column(4).Width = 22;
        ws.Column(5).Width = 20;
        ws.Column(6).Width = 35;
        ws.Column(7).Width = 14;

        int rowAux = 8;
        foreach (var item in headerEmpresa)
        {
            ws.Column(rowAux).Width = 16;
            ws.Column(rowAux).Style.NumberFormat.Format = "#,##0.00";
            ws.Column(rowAux).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            rowAux += 1;
        }
        ws.Column(rowAux).Width = 16;
        ws.Column(rowAux).Style.NumberFormat.Format = "#,##0.00";
        ws.Column(rowAux).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        
    }
    private static void CrearEncabezados(IXLWorksheet ws, int row, List<EmpresaHeaderPagarComision> headerEmpresa)
    {
        int rowAux = 8;
        ws.Cell(row, 2).Value = "TIPO CTA";
        ws.Cell(row, 3).Value = "COD. BANCO";
        ws.Cell(row, 4).Value = "CTA. BANCO";
        ws.Cell(row, 5).Value = "CIUDAD";
        ws.Cell(row, 6).Value = "ASESOR";
        ws.Cell(row, 7).Value = "CI";
        foreach (var item in headerEmpresa)
        {
            string nombre = item.SEmpresa;
            nombre = nombre.Replace("S.R.L.", "");
            nombre = nombre.Replace("S.R.L", "");
            nombre = nombre.Replace("INMOBILIARIA", "");
            ws.Cell(row, rowAux).Value = nombre;
            rowAux += 1;
        }

        ws.Cell(row, rowAux).Value = "TOTAL A PAGAR";

        var range = ws.Range(row, 2, row, rowAux);
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

        /*ws.Cell(row, 8).Value =
            item.Personal +
            item.Grupo +
            item.Residual +
            item.Liderazgo -
            item.Descuento -
            item.Retencion;*/
    }
    private static void EscribirTotalizador(IXLWorksheet ws, int row, List<RptPagarComision> listado, List<EmpresaHeaderPagarComision> headerEmpresa, Dictionary<(int LContactoId, int EmpresaId), decimal> prorrateoLookup)
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

        int rowAux = 8;
        decimal totalGeneral = 0;

        

        foreach(var item in headerEmpresa)
        {
            decimal totalEmpresa = 0;

            foreach (var v in listado)
            {
                if (prorrateoLookup.TryGetValue((v.LContactold, item.EmpresaId), out var monto))
                {
                    totalEmpresa += monto;
                }
            }
            totalGeneral += totalEmpresa;

            ws.Cell(row, rowAux).Value = totalEmpresa;
            ws.Cell(row, rowAux).Style.Font.Bold = true;
            ws.Cell(row, rowAux).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            rowAux +=1;
        }

        ws.Cell(row, rowAux).Value = totalGeneral;
        ws.Cell(row, rowAux).Style.Font.Bold = true;
        ws.Cell(row, rowAux).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        var range = ws.Range(row, 2, row, (8 + headerEmpresa.Count));
        range.Style.Fill.BackgroundColor = XLColor.LightGray;
        range.Style.Border.TopBorder = XLBorderStyleValues.Thin;
        range.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        range.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
        range.Style.Border.RightBorder = XLBorderStyleValues.Thin;
    }
    private static void AplicarBordes(IXLWorksheet ws, int headerRow, int totalRow, List<EmpresaHeaderPagarComision> headerEmpresa)
    {
        var detailRange = ws.Range(headerRow, 2, totalRow - 1, (8 + headerEmpresa.Count));
        detailRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        detailRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }

}