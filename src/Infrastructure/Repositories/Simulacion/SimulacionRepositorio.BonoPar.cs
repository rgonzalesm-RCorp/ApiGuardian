using ApiGuardian.Domain.Entities;
using Dapper;
using Query.Grd;

namespace ApiGuardian.Infrastructure.Repositories.Simulacion;

// F8-T2: Bono Par. Lectura con los builders de solo lectura de ScriptGrd (R3). Escritura copiada de BonoParRepository.SaveBonoPar (R2).
// Diferencia: si no hay ganadores no se ejecuta la consulta de detalle (en el original produce "IN ()" y falla).
public sealed partial class SimulacionRepositorio
{
    // Copia propia de ScriptGrd.QueryBonoPar() para la simulación (el original lo usa el proceso principal).
    // Diferencia: 10 personas que vendieron pagan 2000 (el original usa "> 10" y con 10 exactas el bono queda en 0).
    private const string SqlBonoPar = @"
        SELECT * FROM(
            SELECT
                CT.lcontacto_id LContctoGanadorId
                , CT.SNombreCompleto SNombreGanador
                , CT.scedulaidentidad SCedulaIdentidadGanador
                , COUNT(*)PersonaQueVendieron
                , case
                    when  TRUNCATE(COUNT(*), 0) BETWEEN 2 AND 3 THEN 200
                    when  TRUNCATE(COUNT(*), 0) BETWEEN 4 AND 5 THEN 700
                    when  TRUNCATE(COUNT(*), 0) BETWEEN 6 AND 7 THEN 1100
                    when  TRUNCATE(COUNT(*), 0) BETWEEN 8 AND 9 THEN 1400
                    when  TRUNCATE(COUNT(*), 0) >= 10 THEN 2000
                    ELSE 0
                END Bono
                , sum(DAT.CantVta) CantidadVenta
                , GROUP_CONCAT(DAT.VId)VendedoresId
                , GROUP_CONCAT(DAT.LContratoId)LContratoId
                , GROUP_CONCAT(DAT.SNroVenta)SNroVenta
                , sum(DAT.MontoVentas)MontoVentas
                , sum(DAT.CuotasIniciales)CuotasIniciales
            FROM (
                SELECT
                    V.lpatrocinante_id GId
                    , V.lcontacto_id VId
                    , GROUP_CONCAT(ACT.snroventa) SNroVenta
                    , GROUP_CONCAT(ACT.lcontrato_id) LContratoId
                    , GROUP_CONCAT(ACT.dtfecha) Fecha
                    , COUNT(*) CantVta
                    , sum(act.dprecio) MontoVentas
                    , sum(act.dcuota_inicial) CuotasIniciales
                FROM administracioncontrato ACT
                INNER JOIN administracioncontacto V ON v.lcontacto_id = ACT.lasesor_id
                WHERE ACT.dtfecha BETWEEN @Inicio AND @Fin and
                    CASE WHEN (ACT.dcuota_inicial * 100 / ACT.dprecio) BETWEEN 2.9999 AND 4.98 THEN
                        CASE WHEN  ACT.dprecio >= 10000 THEN 1 ELSE 0 END
                    ELSE 1 END = 1 and ACT.ltipocontrato_id in (1,2) and ACT.lcontacto_id != ACT.lasesor_id
                    AND ACT.snroventa not like '%KTRB5%'
                GROUP BY V.lpatrocinante_id, V.lcontacto_id
            )DAT
            INNER JOIN administracioncontacto CT ON CT.lcontacto_id = DAT.GId
            GROUP BY CT.lcontacto_id, CT.SNombreCompleto, CT.scedulaidentidad
        ) DATG where DATG.Bono > 0 ORDER BY SNombreGanador";

    // Asesores con ventas tipo 1 y 2 (crédito y contado) en un periodo. Lo usa el bono par para la ventana de 3 meses.
    private const string SqlAsesoresConVenta = @"
        SELECT DISTINCT lasesor_id
        FROM administracioncontrato
        WHERE dtfecha BETWEEN @Inicio AND @Fin
          AND ltipocontrato_id IN (1, 2)
          AND lasesor_id > 0;";

    private const string SqlInsertBonoParCabecera = @"
        INSERT INTO bonopar
        (
            l_contacto_ganador_id, s_nombre_ganador, s_cedula_identidad_ganador, persona_que_vendieron,
            bono, cantidad_venta, vendedores_id, l_contrato_id, s_nro_venta, monto_ventas, cuotas_iniciales,
            estado, usuario_creacion, fecha_creacion, usuario_modificacion, fecha_modificacion, lciclo_id
        )
        VALUES
        (
            @LContctoGanadorId, @SNombreGanador, @SCedulaIdentidadGanador, @PersonaQueVendieron,
            @Bono, @CantidadVenta, @VendedoresId, @LContratoId, @SNroVenta, @MontoVentas, @CuotasIniciales,
            1, @Usuario, NOW(), @Usuario, NOW(), @LCicloId
        );
        SELECT LAST_INSERT_ID();";

    private const string SqlInsertBonoParDetalle = @"
        INSERT INTO bonopardetalle
        (
            bonopar_id, l_contacto_ganador_id, l_contacto_vendedor_id, s_nombre_vendedor, s_cedula_identidad_vendedor,
            l_contacto_cliente_id, s_nombre_cliente, s_cedula_cliente, l_contrato_id, dt_fecha, s_nro_venta,
            d_precio, d_cuota_inicial, estado, usuario_creacion, fecha_creacion, usuario_modificacion, fecha_modificacion
        )
        VALUES
        (
            @BonoParId, @LContactoGanadorId, @LContactoVendedorId, @SNombreVendedor, @SCedulaIdentidadVendedor,
            @LContactoClienteId, @SNombreCliente, @SCedulaCliente, @LContratoId, @DtFecha, @SNroVenta,
            @DPrecio, @DCuotaInicial, 1, @Usuario, NOW(), @Usuario, NOW()
        );";

    public async Task<(List<ItemBonoPar> Data, bool Success, string Mensaje)> GetBonoParAsync(string logId, string inicio, string fin)
    {
        const string metodo = nameof(GetBonoParAsync);
        try
        {
            using var connection = _context.CreateConnection();
            var lista = (await connection.QueryAsync<ItemBonoPar>(SqlBonoPar, new { Inicio = inicio, Fin = fin })).ToList();
            if (lista.Count == 0)
                return (lista, true, "No existen ganadores de bono par.");

            var contratos = string.Join(",", lista.Select(x => x.LContratoId).Where(x => !string.IsNullOrEmpty(x)));
            var detalles = (await connection.QueryAsync<ItemBonoParDetalle>(ScriptGrd.QueryDetalleBonoPar(contratos))).ToList();
            foreach (var item in lista)
                item.ListaDetalleBonoPar = detalles.Where(x => x.LContactoGanadorId == item.LContctoGanadorId).ToList();

            return (lista, true, "Bono par obtenido.");
        }
        catch (Exception ex)
        {
            _log.Error(logId, NOMBRE_ARCHIVO, metodo, $"Error al obtener bono par: {ex.Message}");
            return (new List<ItemBonoPar>(), false, $"Error al obtener bono par: {ex.Message}");
        }
    }

    public async Task<(HashSet<int> Asesores, bool Success, string Mensaje)> GetAsesoresConVentaAsync(string logId, string inicio, string fin)
    {
        const string metodo = nameof(GetAsesoresConVentaAsync);
        try
        {
            using var connection = _context.CreateConnection();
            var asesores = (await connection.QueryAsync<int>(SqlAsesoresConVenta, new { Inicio = inicio, Fin = fin })).ToHashSet();
            return (asesores, true, $"Asesores con venta: {asesores.Count}.");
        }
        catch (Exception ex)
        {
            _log.Error(logId, NOMBRE_ARCHIVO, metodo, $"Error al obtener asesores con venta: {ex.Message}");
            return (new HashSet<int>(), false, $"Error al obtener asesores con venta: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Mensaje)> InsertBonoParAsync(string logId, string usuario, int lCicloId, List<ItemBonoPar> listado)
    {
        const string metodo = nameof(InsertBonoParAsync);
        if (listado == null || listado.Count == 0)
            return (false, "No existen datos para guardar.");

        using var con = _context.CreateConnection();
        con.Open();
        using var transaccion = con.BeginTransaction();
        try
        {
            int cabeceras = 0;
            int detalles = 0;
            foreach (var item in listado)
            {
                long bonoParId = await con.ExecuteScalarAsync<long>(SqlInsertBonoParCabecera, new
                {
                    item.LContctoGanadorId,
                    item.SNombreGanador,
                    item.SCedulaIdentidadGanador,
                    item.PersonaQueVendieron,
                    item.Bono,
                    item.CantidadVenta,
                    item.VendedoresId,
                    item.LContratoId,
                    item.SNroVenta,
                    item.MontoVentas,
                    item.CuotasIniciales,
                    Usuario = usuario,
                    LCicloId = lCicloId,
                }, transaccion);
                cabeceras++;

                if (item.ListaDetalleBonoPar != null && item.ListaDetalleBonoPar.Any())
                {
                    var filas = item.ListaDetalleBonoPar.Select(d => new
                    {
                        BonoParId = bonoParId,
                        d.LContactoGanadorId,
                        d.LContactoVendedorId,
                        d.SNombreVendedor,
                        d.SCedulaIdentidadVendedor,
                        d.LContactoClienteId,
                        d.SNombreCliente,
                        d.SCedulaCliente,
                        d.LContratoId,
                        d.DtFecha,
                        d.SNroVenta,
                        d.DPrecio,
                        d.DCuotaInicial,
                        Usuario = usuario,
                    }).ToList();
                    detalles += await con.ExecuteAsync(SqlInsertBonoParDetalle, filas, transaccion);
                }
            }

            transaccion.Commit();
            return (true, $"Bono par guardado. Cabeceras: {cabeceras}, detalles: {detalles}.");
        }
        catch (Exception ex)
        {
            transaccion.Rollback();
            _log.Error(logId, NOMBRE_ARCHIVO, metodo, $"Error al guardar bono par: {ex.Message}");
            return (false, $"Error al guardar bono par: {ex.Message}");
        }
    }
}
