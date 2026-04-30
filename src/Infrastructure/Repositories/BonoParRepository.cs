using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using Newtonsoft.Json;
using Query.Grd;

namespace ApiGuardian.Infrastructure.Repositories;

public class BonoParRepository : IBonoParRepository
{
    private readonly DapperContext _context;
    private readonly ILogService _log;
    private string NOMBREARCHIVO = "BonoParRepository.CS";
    public BonoParRepository(DapperContext context, ILogService log)
    {
        _context = context;
        _log = log;
    }
    public async Task<(IEnumerable<ItemBonoPar> Data, bool Success, string Mensaje)> GetBonoPar(string LogTransaccionId, string Usuario, string Inicio, string Fin)
    {
        string nombreMetodo = "GetBonoPar()";

        string query = ScriptGrd.QueryBonoPar();
        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [QueryBonoPar script: {query} Usuario: {Usuario}]");

        try
        {
            using var connection = _context.CreateConnection();

            var Lista = await connection.QueryAsync<ItemBonoPar>(query, new {Inicio, Fin});
            string LContratoIdString = string.Join(",", 
                Lista.Select(x => x.LContratoId)
                    .Where(x => !string.IsNullOrEmpty(x))
            );
            query = ScriptGrd.QueryDetalleBonoPar(LContratoIdString);
            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"QueryDetalleBonoPar [script: {query} Usuario: {Usuario}]");

            var ListaDetalleBonoPar =  await connection.QueryAsync<ItemBonoParDetalle>(query);
            foreach (var item in Lista)
            {
                item.ListaDetalleBonoPar = ListaDetalleBonoPar.Where(x => x.LContactoGanadorId == item.LContctoGanadorId).ToList();
            }

            bool success = true;
            string mensaje = success ? "Tipos de descuento obtenidos correctamente." : "No se encontraron tipos de descuento.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, Usuario: {Usuario}]");

            return (Lista ?? Enumerable.Empty<ItemBonoPar>(), success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (Enumerable.Empty<ItemBonoPar>(),false, $"Error al obtener la comision de bono par: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Mensaje)> SaveBonoPar(string LogTransaccionId, string Usuario, int LCicloId, List<ItemBonoPar> Listado)
    {
        string metodo = "SaveBonoPar()";

        const string insertBonoParQuery = @"
            INSERT INTO bonopar
            (
                l_contacto_ganador_id,
                s_nombre_ganador,
                s_cedula_identidad_ganador,
                persona_que_vendieron,
                bono,
                cantidad_venta,
                vendedores_id,
                l_contrato_id,
                s_nro_venta,
                monto_ventas,
                cuotas_iniciales,
                estado,
                usuario_creacion,
                fecha_creacion,
                usuario_modificacion,
                fecha_modificacion,
                lciclo_id
            )
            VALUES
            (
                @LContctoGanadorId,
                @SNombreGanador,
                @SCedulaIdentidadGanador,
                @PersonaQueVendieron,
                @Bono,
                @CantidadVenta,
                @VendedoresId,
                @LContratoId,
                @SNroVenta,
                @MontoVentas,
                @CuotasIniciales,
                1,
                @Usuario,
                NOW(),
                @Usuario,
                NOW(),
                @LCicloId
            );

            SELECT LAST_INSERT_ID();
        ";

        const string insertBonoParDetalleQuery = @"
            INSERT INTO bonopardetalle
            (
                bonopar_id,
                l_contacto_ganador_id,
                l_contacto_vendedor_id,
                s_nombre_vendedor,
                s_cedula_identidad_vendedor,
                l_contacto_cliente_id,
                s_nombre_cliente,
                s_cedula_cliente,
                l_contrato_id,
                dt_fecha,
                s_nro_venta,
                d_precio,
                d_cuota_inicial,
                estado,
                usuario_creacion,
                fecha_creacion,
                usuario_modificacion,
                fecha_modificacion
            )
            VALUES
            (
                @BonoParId,
                @LContactoGanadorId,
                @LContactoVendedorId,
                @SNombreVendedor,
                @SCedulaIdentidadVendedor,
                @LContactoClienteId,
                @SNombreCliente,
                @SCedulaCliente,
                @LContratoId,
                @DtFecha,
                @SNroVenta,
                @DPrecio,
                @DCuotaInicial,
                1,
                @Usuario,
                NOW(),
                @Usuario,
                NOW()
            );
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo, "Inicio de guardado BonoPar.");

        if (Listado == null || !Listado.Any())
            return (false, "No existen datos para guardar.");

        using var con = _context.CreateConnection();
        con.Open();

        using var transaction = con.BeginTransaction();

        try
        {
            int totalCabeceras = 0;
            int totalDetalles = 0;

            foreach (var item in Listado)
            {
                long bonoParId = await con.ExecuteScalarAsync<long>(
                    insertBonoParQuery,
                    new
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
                        Usuario,
                        LCicloId
                    },
                    transaction
                );

                totalCabeceras++;

                if (item.ListaDetalleBonoPar != null && item.ListaDetalleBonoPar.Any())
                {
                    var detalles = item.ListaDetalleBonoPar.Select(detalle => new
                    {
                        BonoParId = bonoParId,
                        detalle.LContactoGanadorId,
                        detalle.LContactoVendedorId,
                        detalle.SNombreVendedor,
                        detalle.SCedulaIdentidadVendedor,
                        detalle.LContactoClienteId,
                        detalle.SNombreCliente,
                        detalle.SCedulaCliente,
                        detalle.LContratoId,
                        detalle.DtFecha,
                        detalle.SNroVenta,
                        detalle.DPrecio,
                        detalle.DCuotaInicial,
                        Usuario
                    }).ToList();

                    int rowsDetalle = await con.ExecuteAsync(
                        insertBonoParDetalleQuery,
                        detalles,
                        transaction
                    );

                    totalDetalles += rowsDetalle;
                }
            }

            transaction.Commit();

            string mensaje = $"Registro creado correctamente. Cabeceras: {totalCabeceras}, Detalles: {totalDetalles}.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo, $"Fin de método [mensaje: {mensaje}]");

            return (true, mensaje);
        }
        catch (Exception ex)
        {
            transaction.Rollback();

            _log.Error(LogTransaccionId, NOMBREARCHIVO, metodo, "Fin con error", ex);

            return (false, ex.Message);
        }
    }


}
