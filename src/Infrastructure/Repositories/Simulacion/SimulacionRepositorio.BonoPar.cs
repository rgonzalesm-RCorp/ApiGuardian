using ApiGuardian.Domain.Entities;
using Dapper;
using Query.Grd;

namespace ApiGuardian.Infrastructure.Repositories.Simulacion;

// F8-T2: Bono Par. Lectura con los builders de solo lectura de ScriptGrd (R3). Escritura copiada de BonoParRepository.SaveBonoPar (R2).
// Diferencia: si no hay ganadores no se ejecuta la consulta de detalle (en el original produce "IN ()" y falla).
public sealed partial class SimulacionRepositorio
{
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
            var lista = (await connection.QueryAsync<ItemBonoPar>(ScriptGrd.QueryBonoPar(), new { Inicio = inicio, Fin = fin })).ToList();
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
