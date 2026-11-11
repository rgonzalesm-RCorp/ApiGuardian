using Dapper;

namespace ApiGuardian.Infrastructure.Repositories.Simulacion;

// F3-T2: escrituras de Comision Directa. SQL copiado de AdministracionVentaPersonalRepository.InsertVentaPersonal
// y CuotasVentaResidualRepository.InsertProductosPagarMensuales, sin reutilizar esos repositorios (R2).
public sealed partial class SimulacionRepositorio
{
    private const string SqlMaxVentaPersonal = @"SELECT IFNULL(MAX(lventapersonal_id), 0) FROM administracionventapersonal;";

    private const string SqlInsertVentaPersonal = @"INSERT INTO administracionventapersonal (
            susuarioadd, dtfechaadd, susuariomod, dtfechamod, lventapersonal_id, dtfechacalculo,
            lciclo_id, lcontacto_id, dpreciolote, dporcentajecomision, dcomision, lcontrato_id,
            ddescuentoatencion, ddescuentotramite, ddescuentoreferido, latencion_id, ltramite_id, lreferido_id,
            ddescuentolote, snotadescuentolote, cventapagada, dtfechapago, lnrosemana,
            dporcentajeretencion, dmontoretencion, cpresentafactura, dtotaapagar, lsemana_id
        ) VALUES (
            @susuarioadd, @dtfechaadd, @susuariomod, @dtfechamod, @lventapersonal_id, @dtfechacalculo,
            @lciclo_id, @lcontacto_id, @dpreciolote, @dporcentajecomision, @dcomision, @lcontrato_id,
            @ddescuentoatencion, @ddescuentotramite, @ddescuentoreferido, @latencion_id, @ltramite_id, @lreferido_id,
            @ddescuentolote, @snotadescuentolote, @cventapagada, @dtfechapago, @lnrosemana,
            @dporcentajeretencion, @dmontoretencion, @cpresentafactura, @dtotaapagar, @lsemana_id
        );";

    public async Task<(bool Success, string Mensaje)> InsertVentaPersonalAsync(string logId, List<AdministracionVentaPersonal> data)
    {
        const string metodo = nameof(InsertVentaPersonalAsync);
        try
        {
            using var connection = _context.CreateConnection();
            var siguiente = await connection.ExecuteScalarAsync<int>(SqlMaxVentaPersonal);
            foreach (var item in data)
            {
                siguiente++;
                item.lventapersonal_id = siguiente;
            }

            var filas = await connection.ExecuteAsync(SqlInsertVentaPersonal, data);
            return filas > 0
                ? (true, "Comisiones registradas correctamente.")
                : (false, "No se realizó el guardado de comisiones directas.");
        }
        catch (Exception ex)
        {
            _log.Error(logId, NOMBRE_ARCHIVO, metodo, $"Error al insertar comisiones directas: {ex.Message}");
            return (false, $"Error al insertar las comisiones: {ex.Message}");
        }
    }

    private const string SqlInsertProductosPagarMensuales = @"
        INSERT INTO t_productos_pagar_mensuales
        (
            id_Producto_Pagar, lcontrato_id, lcomplejo_id, snroventa, lcontacto_id, lasesor_id, dtfecha,
            PRECIO, CUOTA_INICIAL, PORCENTAJE, Comision, Cuot_Acc_Pen, Cuot_Pagadas, Inicial_10,
            Mont_Pagar, Mens_Pagar, ciclos_habilitados, Terminado
        )
        VALUES
        (
            @IdProductoPagar, @LcontratoId, @LcomplejoId, @Snroventa, @LcontactoId, @LasesorId, @Dtfecha,
            @Precio, @CuotaInicial, @Porcentaje, @Comision, @CuotAccPen, @CuotPagadas, @Inicial10,
            @MontPagar, @MensPagar, @CiclosHabilitados, @Terminado
        );";

    public async Task<(bool Success, string Mensaje)> InsertProductosPagarMensualesAsync(string logId, List<ProductosPagarMensuales> listado)
    {
        const string metodo = nameof(InsertProductosPagarMensualesAsync);

        if (listado == null || listado.Count == 0)
            return (false, "No existen productos pagar mensuales para guardar.");

        try
        {
            using var connection = _context.CreateConnection();
            connection.Open();
            using var transaccion = connection.BeginTransaction();
            try
            {
                int correlativo = await connection.ExecuteScalarAsync<int>(
                    "SELECT IFNULL(MAX(id_Producto_Pagar), 0) + 1 FROM t_productos_pagar_mensuales",
                    transaction: transaccion);

                foreach (var item in listado)
                {
                    item.IdProductoPagar = correlativo++;
                    item.CuotPagadas ??= 0;
                    item.Terminado ??= 0;
                    item.CiclosHabilitados ??= string.Empty;
                }

                await connection.ExecuteAsync(SqlInsertProductosPagarMensuales, listado, transaccion);
                transaccion.Commit();
                return (true, "Productos pagar mensuales guardados correctamente.");
            }
            catch (Exception ex)
            {
                transaccion.Rollback();
                _log.Error(logId, NOMBRE_ARCHIVO, metodo, $"Error en transacción: {ex.Message}");
                return (false, $"Error al guardar productos pagar mensuales: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            _log.Error(logId, NOMBRE_ARCHIVO, metodo, $"Error al guardar productos pagar mensuales: {ex.Message}");
            return (false, $"Error al guardar productos pagar mensuales: {ex.Message}");
        }
    }
}
