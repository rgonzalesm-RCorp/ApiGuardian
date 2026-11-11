using Dapper;

namespace ApiGuardian.Infrastructure.Repositories.Simulacion;

// F2-T2: reset del ciclo (plan, seccion 5.5). Sentencias copiadas de ControlProcesoRepository.ReiniciarCiclo,
// sin rezagadas, especiales, habilitaciones, ControlProceso ni contactos.
// Orden: hijos antes que padres.
public sealed partial class SimulacionRepositorio
{
    // Pasos dentro de la transaccion. Parametros: @LCicloId, @Inicio, @Fin.
    private static readonly string[] SqlResetTransaccional =
    {
        // 1. Detalle de bono par
        @"DELETE BPD FROM bonopardetalle BPD
          INNER JOIN bonopar BP ON BP.id = BPD.bonopar_id
          WHERE BP.lciclo_id = @LCicloId;",
        // 2. Bono par
        @"DELETE FROM bonopar WHERE lciclo_id = @LCicloId;",
        // 3. Detalle de bono residual
        @"DELETE FROM administracionbonoresidual_detalle WHERE lciclo_id = @LCicloId;",
        // 4. Bono residual
        @"DELETE FROM administracionbonoresidual WHERE lciclo_id = @LCicloId;",
        // 5. Bono completo
        @"DELETE FROM t_bonocompleto WHERE lciclo_id = @LCicloId;",
        // 6. Red por empresa y complejo
        @"DELETE FROM administracionredempresacomplejo WHERE lciclo_id = @LCicloId;",
        // 7. Cuotas de ventas residuales (por fecha de recibo del ciclo)
        @"DELETE FROM t_cuotas_ventas_productos_pagar_mensual
          WHERE FECHA_RECIBO BETWEEN @Inicio AND @Fin;",
        // 8. Descontar las cuotas pagadas del detalle que se va a borrar (debe ir antes del paso 9)
        @"UPDATE t_productos_pagar_mensuales m
          INNER JOIN (
              SELECT fk_id_producto_pagar, SUM(cant_cuotas) AS total_cuotas
              FROM t_productos_detalle_cuotas
              WHERE lciclo_id = @LCicloId
              GROUP BY fk_id_producto_pagar
          ) d ON m.id_producto_pagar = d.fk_id_producto_pagar
          SET m.cuot_pagadas = m.cuot_pagadas - d.total_cuotas;",
        // 9. Detalle de cuotas de productos
        @"DELETE FROM t_productos_detalle_cuotas WHERE lciclo_id = @LCicloId;",
        // 10. Productos a pagar mensuales (por fecha del ciclo)
        @"DELETE FROM t_productos_pagar_mensuales WHERE dtfecha BETWEEN @Inicio AND @Fin;",
        // 11. Cuotas (por fecha de pago del ciclo)
        @"DELETE FROM T_ACCIONESCUOTASGRL WHERE FECHA_PAGO BETWEEN @Inicio AND @Fin;",
        // 12. Venta de grupo
        @"DELETE FROM administracionventagrupo WHERE lciclo_id = @LCicloId;",
        // 13. Red completa
        @"DELETE FROM red_completa_cuotas WHERE lciclo_id = @LCicloId;",
        // 14. Red comprimida
        @"DELETE FROM red_comprimida WHERE lciclo_id = @LCicloId;",
        // 15. Venta personal
        @"DELETE FROM administracionventapersonal WHERE lciclo_id = @LCicloId;",
        // 16. Contratos (por fecha del ciclo; se recrean en Obtener Ventas, D-16)
        @"DELETE FROM administracioncontrato WHERE dtfecha BETWEEN @Inicio AND @Fin;",
    };

    // Fuera de la transaccion: TRUNCATE hace commit implicito en MySQL.
    private const string SqlTruncateCartera = "TRUNCATE TABLE Cartera;";
    private const string SqlMaxCiclo = "SELECT IFNULL(MAX(lciclo_id), 0) FROM administracionciclo;";

    public async Task<(bool Success, string Mensaje)> ResetCicloAsync(string logId, int lCicloId, string inicio, string fin)
    {
        const string metodo = nameof(ResetCicloAsync);

        if (lCicloId <= 0 || string.IsNullOrWhiteSpace(inicio) || string.IsNullOrWhiteSpace(fin))
            return (false, "Reset cancelado: ciclo o fechas no válidos.");

        using var connection = _context.CreateConnection();
        connection.Open();

        // R-17: Cartera solo se trunca para el ciclo de mayor lciclo_id. Se valida antes de borrar nada.
        var maxCiclo = await connection.ExecuteScalarAsync<int>(SqlMaxCiclo);
        if (lCicloId != maxCiclo)
            return (false, $"Reset cancelado: Cartera solo se trunca para el ciclo de mayor lciclo_id ({maxCiclo}).");

        var parametros = new { LCicloId = lCicloId, Inicio = inicio, Fin = fin };

        using var transaccion = connection.BeginTransaction();
        try
        {
            foreach (var sql in SqlResetTransaccional)
            {
                await connection.ExecuteAsync(sql, parametros, transaccion);
            }
            transaccion.Commit();
        }
        catch (Exception ex)
        {
            transaccion.Rollback();
            _log.Error(logId, NOMBRE_ARCHIVO, metodo, $"Error en reset del ciclo {lCicloId}: {ex.Message}");
            return (false, $"Error en reset del ciclo {lCicloId}: {ex.Message}");
        }

        try
        {
            await connection.ExecuteAsync(SqlTruncateCartera);
        }
        catch (Exception ex)
        {
            _log.Error(logId, NOMBRE_ARCHIVO, metodo, $"Error al truncar Cartera en el reset del ciclo {lCicloId}: {ex.Message}");
            return (false, $"Los DELETE del reset se aplicaron, pero falló TRUNCATE de Cartera: {ex.Message}");
        }

        _log.Info(logId, NOMBRE_ARCHIVO, metodo, $"Reset del ciclo {lCicloId} completado.");
        return (true, "Reset completado.");
    }
}
