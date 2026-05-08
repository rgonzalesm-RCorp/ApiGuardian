using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using Newtonsoft.Json;
using Query.Cnx;
using Org.BouncyCastle.Math.EC.Rfc7748;

namespace ApiGuardian.Infrastructure.Repositories;

public class CuotasVentaResidualRepository : ICuotasVentaResidualRepository
{
    private readonly DapperContext _context;
    private readonly DapperContextSqlServer _contextSqlServer;
    private readonly ILogService _log;
    private string NOMBREARCHIVO = "ControlProcesoRepository.CS";
    public CuotasVentaResidualRepository(DapperContext context, ILogService log, DapperContextSqlServer contextSqlServer)
    {
        _context = context;
        _log = log;
        _contextSqlServer = contextSqlServer;
    }
    public async Task<(bool Success, string Mensaje, IEnumerable<VentaResidual> ListadoCuotasVentasResidual)> GetCuotasVentasResidual(string LogTransaccionId, string Usuario, string Inicio, string Fin)
    {
        string query = ScriptCnx.GetQueryVentaResidual;
        string nombreMetodo = "GetObetenerContactoVentasMes()";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");
        try
        {
            using var connection = _contextSqlServer.CreateConnection();

            var Lista = await connection.QueryAsync<VentaResidual>(query, new { Inicio, Fin });

            bool success = true;
            string mensaje = success ? "cuotas ventas residual obtenidos correctamente." : "No se encontraron cuotas ventas residual.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}]");

            return (success, mensaje, Lista ?? new List<VentaResidual>());
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al obtener los tipos de descuento: {ex.Message}", Enumerable.Empty<VentaResidual>());
        }
    }
    public async Task<(bool Success, string Mensaje, IEnumerable<ProductosPagarMensuales> ListadoProductosPagarMensuales)> GetProductosPagarMensuales(string LogTransaccionId, string Usuario)
    {
        string query = @$"
            SELECT
            id_Producto_Pagar AS IdProductoPagar,
            lcontrato_id AS LcontratoId,
            lcomplejo_id AS LcomplejoId,
            TRIM(TRAILING ' ' FROM snroventa) AS SnroVenta,
            lcontacto_id AS LcontactoId,
            lasesor_id AS LasesorId,
            dtfecha AS Dtfecha,
            PRECIO AS Precio,
            CUOTA_INICIAL AS CuotaInicial,
            PORCENTAJE AS Porcentaje,
            Comision AS Comision,
            Cuot_Acc_Pen AS CuotAccPen,
            Cuot_Pagadas AS CuotPagadas,
            Inicial_10 AS Inicial10,
            Mont_Pagar AS MontPagar,
            Mens_Pagar AS MensPagar,
            TRIM(TRAILING ' ' FROM ciclos_habilitados) AS CiclosHabilitados,
            Terminado AS Terminado
        FROM t_productos_pagar_mensuales WHERE Terminado = 0;";
        string nombreMetodo = "GetProductosPagarMensuales()";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var lista = await connection.QueryAsync<ProductosPagarMensuales>(query);

            bool success = lista != null && lista.Any();

            string mensaje = success ? "Productos pagar mensuales obtenidos correctamente." : "No se encontraron productos pagar mensuales.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Fin de metodo [mensaje: {mensaje}]");

            return (success, mensaje, lista ?? new List<ProductosPagarMensuales>());
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);

            return (false, $"Error al obtener productos pagar mensuales: {ex.Message}", Enumerable.Empty<ProductosPagarMensuales>());
        }
    }
    /*public async Task<(bool Success, string Mensaje)> SaveProductosDetalleCuotas(string LogTransaccionId, string Usuario, List<ProductosDetalleCuotas> listado)
    {
        string query = @"
            INSERT INTO t_productos_detalle_cuotas
            (
                id_producto_detalle,
                usuario_add,
                fecha_add,
                fk_id_producto_pagar,
                lcontrato_id,
                Cant_Cuotas,
                Exc_Cuotas,
                pagado,
                habilitado,
                lciclo_id
            )
            VALUES
            (
                @IdProductoDetalle,
                @UsuarioAdd,
                @FechaAdd,
                @FkIdProductoPagar,
                @LcontratoId,
                @CantCuotas,
                @ExcCuotas,
                @Pagado,
                @Habilitado,
                @LcicloId
            );
        ";

        string nombreMetodo = "SaveProductosDetalleCuotas()";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();
            using var transaction = connection.BeginTransaction();

            try
            {
                int idProductoDetalle = await connection.ExecuteScalarAsync<int>(
                    "SELECT IFNULL(MAX(id_producto_detalle),0) + 1 FROM t_productos_detalle_cuotas",
                    transaction: transaction
                );

                foreach (var item in listado)
                {
                    item.IdProductoDetalle = idProductoDetalle++;
                    item.UsuarioAdd = Usuario;
                    item.FechaAdd = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    await connection.ExecuteAsync(
                        query,
                        item,
                        transaction
                    );
                }

                transaction.Commit();

                string mensaje = "Detalle de cuotas guardado correctamente.";

                _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Fin de metodo [mensaje: {mensaje}]");

                return (true, mensaje);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Error en transacción", ex);
                return (false, $"Error al guardar detalle de cuotas: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);

            return (false, $"Error al guardar detalle de cuotas: {ex.Message}");
        }
    }*/
    public async Task<(bool Success, string Mensaje)> SaveCuotasVentasProductosPagarMensual(string LogTransaccionId, string Usuario, List<VentaResidual> listado)
    {
        string query = @"
        INSERT INTO t_cuotas_ventas_productos_pagar_mensual
        (
            ID_CUOTPRODUC,
            Nro_venta,
            EMPRESA,
            IDVENTA,
            FECHA,
            IDALMACEN,
            PROYECTO,
            LOTES,
            IDRECIBO,
            FECHA_RECIBO,
            NROCUOTA,
            IMPORTETOTAL,
            IDCLIENTE,
            NOMBRE_CLIENTE,
            CI_CLIENTE,
            IDVENDEDOR,
            VENDEDOR,
            CI_VENDEDOR,
            CONCEPTO1,
            LCICLO_ID
        )
        VALUES
        (
            @IdCuotproduc,
            @NroVenta,
            @Empresa,
            @IdVenta,
            @Fecha,
            @IdAlmacen,
            @Proyecto,
            @Lotes,
            @IdRecibo,
            @FechaRecibo,
            @NroCuota,
            @ImporteTotal,
            @IdCliente,
            @NombreCliente,
            @CiCliente,
            @IdVendedor,
            @Vendedor,
            @CiVendedor,
            @Concepto1,
            @LcicloId
        );
    ";

        string nombreMetodo = "SaveCuotasVentasProductosPagarMensual()";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                int correlativo = await connection.ExecuteScalarAsync<int>(@"SELECT IFNULL(MAX(ID_CUOTPRODUC),0) + 1 FROM t_cuotas_ventas_productos_pagar_mensual", transaction: transaction);

                foreach (var item in listado)
                {
                    item.IdCuotproduc = correlativo ++;
                }
                var response = await connection.ExecuteAsync(query, listado, transaction);


                transaction.Commit();

                string mensaje = "Cuotas ventas productos pagar mensual guardados correctamente.";

                _log.Info(LogTransaccionId,  NOMBREARCHIVO, nombreMetodo, $"Fin de metodo [response:{response}, mensaje: {mensaje}]");

                return (true, mensaje);
            }
            catch (Exception ex)
            {
                transaction.Rollback();

                _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Error en transacción", ex);

                return (false, $"Error al guardar cuotas ventas productos pagar mensual: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo",ex);

            return (false, $"Error al guardar cuotas ventas productos pagar mensual: {ex.Message}");
        }
    }
    public async Task<(bool Success, string Mensaje)> SaveControlProductos(string LogTransaccionId, string Usuario, List<ProductosPagarMensualUpdate> ProductosPagarMensualUpdate)
    {
        string QueryUpdateProductoPagarMensual = @$"update t_productos_pagar_mensuales set cuot_pagadas = @CuotasPagadas where id_producto_pagar = @IdProductoPagar";
        string QueryInsertProductoDetallCuotas = @"
            INSERT INTO t_productos_detalle_cuotas
            (
                id_producto_detalle,
                usuario_add,
                fecha_add,
                fk_id_producto_pagar,
                lcontrato_id,
                Cant_Cuotas,
                Exc_Cuotas,
                pagado,
                habilitado,
                lciclo_id
            )
            VALUES
            (
                @IdProductoDetalle,
                @UsuarioAdd,
                @FechaAdd,
                @FkIdProductoPagar,
                @LcontratoId,
                @CantCuotas,
                @ExcCuotas,
                @Pagado,
                @Habilitado,
                @LcicloId
            );
        ";

        try
        {   
            List<ProductosPagarMensualUpdate> productosPagarMensualUpdates = ProductosPagarMensualUpdate.Where(x => x.ActivoMes == true && x.IdProductoPagar == 2271).ToList();
            using var connection = _context.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                int couter = 0;
                foreach (var item in productosPagarMensualUpdates)
                {
                    if (couter <= 0)
                    {
                        if((item.CantidadNroCuotas + item.CuotasPagadas) <= item.CuotasTotalesAPagar)
                        {
                            await connection.ExecuteScalarAsync(QueryUpdateProductoPagarMensual, new
                            {
                                CuotasPagadas = item.CantidadNroCuotas + item.CuotasPagadas,
                                IdProductoPagar = item.IdProductoPagar

                            }, transaction);
                            int idProductoDetalle = await connection.ExecuteScalarAsync<int>(
                                "SELECT IFNULL(MAX(id_producto_detalle),0) + 1 FROM t_productos_detalle_cuotas",
                                transaction: transaction
                            );
                            foreach (var itemDetalle in item._ProductosDetalleCuotas)
                            {
                                itemDetalle.IdProductoDetalle = idProductoDetalle++;
                                itemDetalle.UsuarioAdd = Usuario;
                                itemDetalle.FechaAdd = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                await connection.ExecuteAsync(
                                    QueryInsertProductoDetallCuotas,
                                    itemDetalle,
                                    transaction
                                );
                            }

                            couter ++;
                        }else if (item.CuotasPagadas < item.CuotasTotalesAPagar)
                        {
                            int? CuotasFaltantes = item.CuotasTotalesAPagar - item.CuotasPagadas;
                            if (item.CantidadNroCuotas > CuotasFaltantes)
                            {
                                await connection.ExecuteScalarAsync(QueryUpdateProductoPagarMensual, new
                                {
                                    CuotasPagadas =  item.CuotasTotalesAPagar,
                                    IdProductoPagar = item.IdProductoPagar

                                }, transaction);
                                int idProductoDetalle = await connection.ExecuteScalarAsync<int>(
                                    "SELECT IFNULL(MAX(id_producto_detalle),0) + 1 FROM t_productos_detalle_cuotas",
                                    transaction: transaction
                                );
                                List<ProductosDetalleCuotas> Detalle = item._ProductosDetalleCuotas.Take((int)CuotasFaltantes).ToList();
                                foreach (var itemDetalle in item._ProductosDetalleCuotas)
                                {
                                    itemDetalle.IdProductoDetalle = idProductoDetalle++;
                                    itemDetalle.UsuarioAdd = Usuario;
                                    itemDetalle.FechaAdd = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                    await connection.ExecuteScalarAsync(
                                        QueryInsertProductoDetallCuotas,
                                        itemDetalle,
                                        transaction
                                    );
                                }
                                couter ++;
                            }
                            else
                            {
                               await connection.ExecuteScalarAsync(QueryUpdateProductoPagarMensual, new
                                {
                                    CuotasPagadas =  item.CuotasTotalesAPagar,
                                    IdProductoPagar = item.IdProductoPagar

                                }, transaction);
                                int idProductoDetalle = await connection.ExecuteScalarAsync<int>(
                                    "SELECT IFNULL(MAX(id_producto_detalle),0) + 1 FROM t_productos_detalle_cuotas",
                                    transaction: transaction
                                );
                                foreach (var itemDetalle in item._ProductosDetalleCuotas)
                                {
                                    itemDetalle.IdProductoDetalle = idProductoDetalle++;
                                    itemDetalle.UsuarioAdd = Usuario;
                                    itemDetalle.FechaAdd = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                    await connection.ExecuteAsync(
                                        QueryInsertProductoDetallCuotas,
                                        itemDetalle,
                                        transaction
                                    );
                                }
                                couter ++; 
                            }
                        }
                        
                    }
                    
                }
                transaction.Commit();
                return(true, "");
            }
            catch (System.Exception ex)
            {
                transaction.Rollback();
                return (false, ex.Message.ToString());
            }
        }
        catch (System.Exception ex)
        {
            return (false, ex.Message.ToString());

        }
        
    }
}
