using System.Text;
using ApiGuardian.Domain.Entities;
using Dapper;

namespace ApiGuardian.Infrastructure.Repositories.Simulacion;

// F6-T2 y F6-T3: escrituras de Datos Residual (solo cuotas). SQL copiado de BonoResidualRepository (R2).
// La simulación no carga ni toca la tabla Cartera (compartida con el proceso principal). Cuotas: DELETE por FECHA_PAGO del ciclo (R11), no TRUNCATE.
// Sin excedente (D-03).
public sealed partial class SimulacionRepositorio
{
    private const string SqlDeleteCuotasCiclo = "DELETE FROM T_ACCIONESCUOTASGRL WHERE FECHA_PAGO BETWEEN @Inicio AND @Fin;";

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
