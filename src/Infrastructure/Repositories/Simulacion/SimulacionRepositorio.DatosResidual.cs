using System.Text;
using ApiGuardian.Domain.Entities;
using Dapper;

namespace ApiGuardian.Infrastructure.Repositories.Simulacion;

// F6-T2 y F6-T3: escrituras de Datos Residual (cartera y cuotas). SQL copiado de BonoResidualRepository (R2).
// Cartera: TRUNCATE solo si el ciclo es el de mayor lciclo_id (R-17). Cuotas: DELETE por FECHA_PAGO del ciclo (R11), no TRUNCATE.
// Sin excedente (D-03).
public sealed partial class SimulacionRepositorio
{
    // SqlMaxCiclo y SqlTruncateCartera ya están definidos en SimulacionRepositorio.Reset.cs (mismo SQL).
    private const string SqlDeleteCuotasCiclo = "DELETE FROM T_ACCIONESCUOTASGRL WHERE FECHA_PAGO BETWEEN @Inicio AND @Fin;";

    // Copia de BonoResidualRepository.GuardarCartera. Guardia R-17 antes de truncar.
    public async Task<(bool Success, string Mensaje)> ReemplazarCarteraAsync(string logId, int lCicloId, List<TCartera> listado)
    {
        const string metodo = nameof(ReemplazarCarteraAsync);
        const int batchSize = 1500;
        if (listado == null || listado.Count == 0)
            return (false, "No existen registros de cartera para guardar.");

        try
        {
            using var connection = _context.CreateConnection();

            var maxCiclo = await connection.ExecuteScalarAsync<int>(SqlMaxCiclo);
            if (lCicloId != maxCiclo)
                return (false, $"No se trunca Cartera: el ciclo {lCicloId} no es el mayor ({maxCiclo}) (R-17).");

            foreach (var item in listado)
            {
                item.Fecha = item.Fecha?.Replace("-", "") ?? "";
                item.UltimoPago = item.UltimoPago?.Replace("-", "");
                item.FUltimoVenc = item.FUltimoVenc?.Replace("-", "");
                item.FVencMasAnt = item.FVencMasAnt?.Replace("-", "");
            }

            // TRUNCATE hace commit implicito en MySQL: va fuera de la transaccion (igual que en Reset).
            await connection.ExecuteAsync(SqlTruncateCartera);

            connection.Open();
            using var transaccion = connection.BeginTransaction();
            int total = 0;
            for (int i = 0; i < listado.Count; i += batchSize)
            {
                var lote = listado.Skip(i).Take(batchSize).ToList();
                var sql = new StringBuilder(@"
                    INSERT INTO Cartera
                    (
                        EMPRESA, LOTE, DOCID, CLIENTE, DOCID_VENDEDOR, NOMBRE, IDTIPOVENTA, IDPROYECTO,
                        IDVENTA, CUOTAINICIAL, TOTALVENTA, TOTALDEUDA, FECHA, PROYECTO, CUOTAS_LOTES_VENCIDAS,
                        ULTIMO_PAGO, ESTADO, TRANS, NIT, TEL_CEL, TELEFONO, DIRECCION, EMAIL, UV, MZNO, NRO_LOTE,
                        PRECIO_LISTA, CIUDAD_RESIDENCIA, MONTO_CAPITAL_VENC, MONTO_INTERES_VENC, MONTO_MULTA, MONTO_EXPENSA,
                        F_VENC_MAS_ANT, F_ULTIMO_VENC, fecha_registro
                    )
                    VALUES ");
                var parametros = new DynamicParameters();

                for (int j = 0; j < lote.Count; j++)
                {
                    var c = lote[j];
                    sql.Append($@"(
                        @Empresa{j}, @Lote{j}, @Docid{j}, @Cliente{j}, @DocidVendedor{j}, @Nombre{j}, @Idtipoventa{j}, @Idproyecto{j},
                        @Idventa{j}, @Cuotainicial{j}, @Totalventa{j}, @Totaldeuda{j}, @Fecha{j}, @Proyecto{j}, @CuotasLotesVencidas{j},
                        @UltimoPago{j}, @Estado{j}, @Trans{j}, @Nit{j}, @TelCel{j}, @Telefono{j}, @Direccion{j}, @Email{j}, @Uv{j}, @Mzno{j}, @NroLote{j},
                        @PrecioLista{j}, @CiudadResidencia{j}, @MontoCapitalVenc{j}, @MontoInteresVenc{j}, @MontoMulta{j}, @MontoExpensa{j},
                        @FVencMasAnt{j}, @FUltimoVenc{j}, NOW())");
                    if (j < lote.Count - 1)
                        sql.Append(',');

                    parametros.Add($"Empresa{j}", c.Empresa);
                    parametros.Add($"Lote{j}", c.Lote);
                    parametros.Add($"Docid{j}", c.Docid);
                    parametros.Add($"Cliente{j}", c.Cliente);
                    parametros.Add($"DocidVendedor{j}", c.DocidVendedor);
                    parametros.Add($"Nombre{j}", c.Nombre);
                    parametros.Add($"Idtipoventa{j}", c.Idtipoventa);
                    parametros.Add($"Idproyecto{j}", c.Idproyecto);
                    parametros.Add($"Idventa{j}", c.Idventa);
                    parametros.Add($"Cuotainicial{j}", c.Cuotainicial);
                    parametros.Add($"Totalventa{j}", c.Totalventa);
                    parametros.Add($"Totaldeuda{j}", c.Totaldeuda);
                    parametros.Add($"Fecha{j}", c.Fecha);
                    parametros.Add($"Proyecto{j}", c.Proyecto);
                    parametros.Add($"CuotasLotesVencidas{j}", c.CuotasLotesVencidas);
                    parametros.Add($"UltimoPago{j}", c.UltimoPago);
                    parametros.Add($"Estado{j}", c.Estado);
                    parametros.Add($"Trans{j}", c.Trans);
                    parametros.Add($"Nit{j}", c.Nit);
                    parametros.Add($"TelCel{j}", c.TelCel);
                    parametros.Add($"Telefono{j}", c.Telefono);
                    parametros.Add($"Direccion{j}", c.Direccion);
                    parametros.Add($"Email{j}", c.Email);
                    parametros.Add($"Uv{j}", c.Uv);
                    parametros.Add($"Mzno{j}", c.Mzno);
                    parametros.Add($"NroLote{j}", c.NroLote);
                    parametros.Add($"PrecioLista{j}", c.PrecioLista);
                    parametros.Add($"CiudadResidencia{j}", c.CiudadResidencia);
                    parametros.Add($"MontoCapitalVenc{j}", c.MontoCapitalVenc);
                    parametros.Add($"MontoInteresVenc{j}", c.MontoInteresVenc);
                    parametros.Add($"MontoMulta{j}", c.MontoMulta);
                    parametros.Add($"MontoExpensa{j}", c.MontoExpensa);
                    parametros.Add($"FVencMasAnt{j}", c.FVencMasAnt);
                    parametros.Add($"FUltimoVenc{j}", c.FUltimoVenc);
                }
                sql.Append(';');
                total += await connection.ExecuteAsync(sql.ToString(), parametros, transaccion);
            }
            transaccion.Commit();
            return (true, $"Cartera guardada. Total insertado: {total}");
        }
        catch (Exception ex)
        {
            _log.Error(logId, NOMBRE_ARCHIVO, metodo, $"Error al guardar cartera: {ex.Message}");
            return (false, $"Error al guardar cartera: {ex.Message}");
        }
    }

    // Copia de BonoResidualRepository.GuardarCuota con excedente = false: DELETE por FECHA_PAGO del ciclo (R11), luego INSERT por lotes.
    public async Task<(bool Success, string Mensaje)> ReemplazarCuotasAsync(string logId, string inicio, string fin, List<TCuota> listado)
    {
        const string metodo = nameof(ReemplazarCuotasAsync);
        const int batchSize = 500;
        if (listado == null || listado.Count == 0)
            return (false, "No existen cuotas para guardar.");

        try
        {
            using var connection = _context.CreateConnection();
            connection.Open();
            using var transaccion = connection.BeginTransaction();

            await connection.ExecuteAsync(SqlDeleteCuotasCiclo, new { Inicio = inicio, Fin = fin }, transaccion);

            int total = 0;
            for (int i = 0; i < listado.Count; i += batchSize)
            {
                var lote = listado.Skip(i).Take(batchSize).ToList();
                var sql = new StringBuilder(@"
                    INSERT INTO T_ACCIONESCUOTASGRL
                    (
                        IDPRODUCTO, IDPROYECTO, PROYECTO, IDRECIBO, IDVENTA, IDTIPOPAGO, DESCRIPCION,
                        IDCLIENTE, CLIENTE, DOCIDCLI, IDVENDEDOR, VENDEDOR, DOCIDVEN, BONO,
                        AMORTIZACION, CAPITAL, INTERES, SEGURO, EXPENSA, MULTA, FECHA_VENTA, FECHA_PAGO,
                        ACUENTA, TOTALPAGO, MONTODEUDA, PAGOSACUENTA, NROCUOTA, empresa, FECHAINS
                    )
                    VALUES ");
                var parametros = new DynamicParameters();

                for (int j = 0; j < lote.Count; j++)
                {
                    var c = lote[j];
                    sql.Append($@"(
                        @IDPRODUCTO{j}, @IDPROYECTO{j}, @PROYECTO{j}, @IDRECIBO{j}, @IDVENTA{j}, @IDTIPOPAGO{j}, @DESCRIPCION{j},
                        @IDCLIENTE{j}, @CLIENTE{j}, @DOCIDCLI{j}, @IDVENDEDOR{j}, @VENDEDOR{j}, @DOCIDVEN{j}, @BONO{j},
                        @AMORTIZACION{j}, @CAPITAL{j}, @INTERES{j}, @SEGURO{j}, @EXPENSA{j}, @MULTA{j}, @FECHA_VENTA{j}, @FECHA_PAGO{j},
                        @ACUENTA{j}, @TOTALPAGO{j}, @MONTODEUDA{j}, @PAGOSACUENTA{j}, @NROCUOTA{j}, @Empresa{j}, NOW())");
                    if (j < lote.Count - 1)
                        sql.Append(',');

                    parametros.Add($"IDPRODUCTO{j}", c.Idproducto);
                    parametros.Add($"IDPROYECTO{j}", c.LComplejoId);
                    parametros.Add($"PROYECTO{j}", c.Proyecto);
                    parametros.Add($"IDRECIBO{j}", c.Idrecibo);
                    parametros.Add($"IDVENTA{j}", c.Idventa);
                    parametros.Add($"IDTIPOPAGO{j}", c.Idtipopago);
                    parametros.Add($"DESCRIPCION{j}", c.Descripcion);
                    parametros.Add($"IDCLIENTE{j}", c.Idcliente);
                    parametros.Add($"CLIENTE{j}", c.Cliente);
                    parametros.Add($"DOCIDCLI{j}", c.Docidcli);
                    parametros.Add($"IDVENDEDOR{j}", c.Idvendedor);
                    parametros.Add($"VENDEDOR{j}", c.Vendedor);
                    parametros.Add($"DOCIDVEN{j}", c.Docidven);
                    parametros.Add($"BONO{j}", c.Bono);
                    parametros.Add($"AMORTIZACION{j}", c.Amortizacion);
                    parametros.Add($"CAPITAL{j}", c.Capital);
                    parametros.Add($"INTERES{j}", c.Interes);
                    parametros.Add($"SEGURO{j}", c.Seguro);
                    parametros.Add($"EXPENSA{j}", c.Expensa);
                    parametros.Add($"MULTA{j}", c.Multa);
                    parametros.Add($"FECHA_VENTA{j}", c.Fecha_Venta);
                    parametros.Add($"FECHA_PAGO{j}", c.Fecha_Pago);
                    parametros.Add($"ACUENTA{j}", c.Acuenta);
                    parametros.Add($"TOTALPAGO{j}", c.Totalpago);
                    parametros.Add($"MONTODEUDA{j}", c.Montodeuda);
                    parametros.Add($"PAGOSACUENTA{j}", c.Pagosacuenta);
                    parametros.Add($"NROCUOTA{j}", c.Nrocuota);
                    parametros.Add($"Empresa{j}", c.Empresa);
                }
                sql.Append(';');
                total += await connection.ExecuteAsync(sql.ToString(), parametros, transaccion);
            }
            transaccion.Commit();
            return (true, $"Cuotas guardadas. Total insertado: {total}");
        }
        catch (Exception ex)
        {
            _log.Error(logId, NOMBRE_ARCHIVO, metodo, $"Error al guardar cuotas: {ex.Message}");
            return (false, $"Error al guardar cuotas: {ex.Message}");
        }
    }
}
