using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using Newtonsoft.Json;
using Query.Cnx;
using Org.BouncyCastle.Math.EC.Rfc7748;
using Microsoft.Extensions.Configuration;

namespace ApiGuardian.Infrastructure.Repositories;

public class CuotasVentaResidualRepository : ICuotasVentaResidualRepository
{
    private readonly DapperContext _context;
    private readonly DapperContextSqlServer _contextSqlServer;
    private readonly ILogService _log;
    private string NOMBREARCHIVO = "CuotasVentaResidualRepository.CS";
    private readonly IConfiguration _configuration;
    public CuotasVentaResidualRepository(DapperContext context, ILogService log, DapperContextSqlServer contextSqlServer, IConfiguration configuration)
    {
        _context = context;
        _log = log;
        _contextSqlServer = contextSqlServer;
        _configuration = configuration;
    }
    public async Task<(bool Success, string Mensaje, IEnumerable<VentaResidual> ListadoCuotasVentasResidual)> GetCuotasVentasResidual(string LogTransaccionId, string Usuario, string Inicio, string Fin, int LCicloId)
    {
        List<EmpresaCalculoComision> empresas = _configuration.GetSection("EmpresaCalculoComisiones").Get<List<EmpresaCalculoComision>>() ?? new List<EmpresaCalculoComision>();

        //string query = ScriptCnx.GetQueryVentaResidual(LCicloId, _configuration);
        string nombreMetodo = "GetObetenerContactoVentasMes()";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [inicio:{Inicio}, fin:{Fin}, LCicloId:{LCicloId}]");
        try
        {
            using var connection = _contextSqlServer.CreateConnection();
            List<VentaResidual> Lista = new List<VentaResidual>();
            foreach (var empresa in empresas)
            {
                string query = ScriptCnx.GetQueryVentaResidual(LCicloId, empresa.DataBase, empresa.Nombre);
                var ListaEmpresa = await connection.QueryAsync<VentaResidual>(query, new { Inicio, Fin });

                Lista.AddRange(ListaEmpresa);
            }

           
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
        FROM t_productos_pagar_mensuales WHERE Terminado = 0 and Cuot_Pagadas < Cuot_Acc_Pen;";
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

    public async Task<(bool Success, string Mensaje)> SaveControlProductos(string logTransaccionId, string usuario, List<ProductosPagarMensualUpdate> productos)
    {
        const string queryUpdate = @"
            UPDATE t_productos_pagar_mensuales 
            SET cuot_pagadas = cuot_pagadas + @CuotasPagadas 
            WHERE id_producto_pagar = @IdProductoPagar;";

        const string queryInsertDetalle = @"
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
                lciclo_id,
                no_pagables,
                pagables,
                cant_recibo,
                estado
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
                @LcicloId,
                @NoPagables,
                @Pagables,
                @CantRecibo,
                @Estado
            );";

         const string nextIdQueryAdinistracionVentaPersonal = @"SELECT IFNULL(MAX(lventapersonal_id), 0)
            FROM administracionventapersonal;
        ";

        const string queryAdministracionVentaPersonal = @"INSERT INTO administracionventapersonal (
                    susuarioadd,
                    dtfechaadd,
                    susuariomod,
                    dtfechamod,
                    lventapersonal_id,
                    dtfechacalculo,
                    lciclo_id,
                    lcontacto_id,
                    dpreciolote,
                    dporcentajecomision,
                    dcomision,
                    lcontrato_id,
                    ddescuentoatencion,
                    ddescuentotramite,
                    ddescuentoreferido,
                    latencion_id,
                    ltramite_id,
                    lreferido_id,
                    ddescuentolote,
                    snotadescuentolote,
                    cventapagada,
                    dtfechapago,
                    lnrosemana,
                    dporcentajeretencion,
                    dmontoretencion,
                    cpresentafactura,
                    dtotaapagar,
                    lsemana_id
                )
                VALUES (
                    @susuarioadd,
                    @dtfechaadd,
                    @susuariomod,
                    @dtfechamod,
                    @lventapersonal_id,
                    @dtfechacalculo,
                    @lciclo_id,
                    @lcontacto_id,
                    @dpreciolote,
                    @dporcentajecomision,
                    @dcomision,
                    @lcontrato_id,
                    @ddescuentoatencion,
                    @ddescuentotramite,
                    @ddescuentoreferido,
                    @latencion_id,
                    @ltramite_id,
                    @lreferido_id,
                    @ddescuentolote,
                    @snotadescuentolote,
                    @cventapagada,
                    @dtfechapago,
                    @lnrosemana,
                    @dporcentajeretencion,
                    @dmontoretencion,
                    @cpresentafactura,
                    @dtotaapagar,
                    @lsemana_id
                );
        ";


        if (productos == null || productos.Count == 0) return (false, "No existen productos para procesar.");

        if (productos.Count == 0) return (false, "No existen productos activos con cuotas pendientes.");

        using var connection = _context.CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();

        try
        {
            int idProductoDetalle = await connection.ExecuteScalarAsync<int>(
                "SELECT IFNULL(MAX(id_producto_detalle), 0) + 1 FROM t_productos_detalle_cuotas;",
                transaction: transaction
            );

            string fechaActual = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            foreach (var item in productos)
            {
                int cuotasFaltantes = Convert.ToInt32(item.CuotasTotalesAPagar) - Convert.ToInt32(item.CuotasPagadas);

                int cuotasAProcesar = Math.Min(item.CantidadNroCuotas, cuotasFaltantes);

                int nuevasCuotasPagadas = Convert.ToInt32(item.CuotasPagadas + cuotasAProcesar);

                await connection.ExecuteAsync(
                    queryUpdate,
                    new
                    {
                        CuotasPagadas = item.TotalCuotasContabilizar,
                        IdProductoPagar = item.IdProductoPagar
                    },
                    transaction
                );

                var detalles = new List<ProductosDetalleCuotas>();

                int cuotasPendientesDetalle = cuotasAProcesar;

                foreach (var detalleOriginal in item._ProductosDetalleCuotas)
                {
                    if (cuotasPendientesDetalle <= 0)
                        break;

                    int cuotasDelDetalle = Math.Min(
                        Convert.ToInt32(detalleOriginal.CantCuotas),
                        cuotasPendientesDetalle
                    );

                    var detalle = new ProductosDetalleCuotas
                    {
                        IdProductoDetalle = idProductoDetalle++,
                        UsuarioAdd = usuario,
                        FechaAdd = fechaActual,
                        FkIdProductoPagar = detalleOriginal.FkIdProductoPagar,
                        LcontratoId = detalleOriginal.LcontratoId,
                        CantCuotas = cuotasDelDetalle,
                        ExcCuotas = detalleOriginal.ExcCuotas,
                        Pagado = detalleOriginal.Pagado,
                        Habilitado = detalleOriginal.Habilitado,
                        LcicloId = detalleOriginal.LcicloId,
                        NoPagables = detalleOriginal.NoPagables,
                        Pagables = detalleOriginal.Pagables,
                        CantRecibo = detalleOriginal.CantRecibo,
                        Estado = detalleOriginal.Estado
                    };

                    detalles.Add(detalle);
                    cuotasPendientesDetalle -= cuotasDelDetalle;
                }

                if (detalles.Count > 0)
                {
                    await connection.ExecuteAsync(
                        queryInsertDetalle,
                        detalles,
                        transaction
                    );
                }
                //insertar administracion venta personal  
                detalles = detalles.Where(d => d.Habilitado == "1").ToList();
                var obj = detalles.GroupBy(x => new {x.LcontratoId, x.LcicloId}).Select(s => new AdministracionVentaPersonal
                {
                    lventapersonal_id = 0,
                    susuarioadd = usuario,
                    susuariomod = usuario,
                    lciclo_id = Convert.ToInt32(s.Key.LcicloId),
                    lcontacto_id = item.LContactoId,
                    dpreciolote = 1,
                    dporcentajecomision =0,
                    dcomision = item.TotalComision, //  Convert.ToDecimal(s.Sum(z => z.CantCuotas)) * item.MontoPagarMes,
                    lcontrato_id = (long) Convert.ToInt32(s.Key.LcontratoId) ,
                    lnrosemana = 1,
                    lsemana_id = 1    
                }).ToList(); 
                if (obj.Count > 0)
                {
                    int AdministracionVentaPersonalId = await connection.ExecuteScalarAsync<int>(
                        nextIdQueryAdinistracionVentaPersonal,
                        transaction: transaction
                    );
                    foreach (var x in obj)
                    {
                        AdministracionVentaPersonalId = AdministracionVentaPersonalId + 1;
                        x.lventapersonal_id = AdministracionVentaPersonalId;
                    }
                    await connection.ExecuteAsync(
                        queryAdministracionVentaPersonal,
                        obj,
                        transaction
                    );
                }
                
            }

            transaction.Commit();
            return (true, "Control de productos guardado correctamente.");
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            return (false, ex.Message);
        }
    }
    public async Task<(bool Success, string Mensaje)> InsertProductosPagarMensuales(string LogTransaccionId, string Usuario, List<ProductosPagarMensuales> Listado)
    {
        string query = @"
            INSERT INTO t_productos_pagar_mensuales
            (
                id_Producto_Pagar,
                lcontrato_id,
                lcomplejo_id,
                snroventa,
                lcontacto_id,
                lasesor_id,
                dtfecha,
                PRECIO,
                CUOTA_INICIAL,
                PORCENTAJE,
                Comision,
                Cuot_Acc_Pen,
                Cuot_Pagadas,
                Inicial_10,
                Mont_Pagar,
                Mens_Pagar,
                ciclos_habilitados,
                Terminado
            )
            VALUES
            (
                @IdProductoPagar,
                @LcontratoId,
                @LcomplejoId,
                @Snroventa,
                @LcontactoId,
                @LasesorId,
                @Dtfecha,
                @Precio,
                @CuotaInicial,
                @Porcentaje,
                @Comision,
                @CuotAccPen,
                @CuotPagadas,
                @Inicial10,
                @MontPagar,
                @MensPagar,
                @CiclosHabilitados,
                @Terminado
            );
        ";

        string nombreMetodo = "InsertProductosPagarMensuales()";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            if (Listado == null || !Listado.Any())
            {
                return (false, "No existen productos pagar mensuales para guardar.");
            }

            using var connection = _context.CreateConnection();

            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                int correlativo = await connection.ExecuteScalarAsync<int>(
                    @"SELECT IFNULL(MAX(id_Producto_Pagar), 0) + 1 
                    FROM t_productos_pagar_mensuales",
                    transaction: transaction
                );

                foreach (var item in Listado)
                {
                    item.IdProductoPagar = correlativo++;
                    item.Terminado ??= 0;
                    item.CiclosHabilitados ??= string.Empty;
                }

                int response = await connection.ExecuteAsync(query, Listado, transaction);

                transaction.Commit();

                string mensaje = "Productos pagar mensuales guardados correctamente.";

                _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Fin de metodo [response:{response}, mensaje: {mensaje}]");

                return (true, mensaje);
            }
            catch (Exception ex)
            {
                transaction.Rollback();

                _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Error en transacción", ex);

                return (false, $"Error al guardar productos pagar mensuales: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);

            return (false, $"Error al guardar productos pagar mensuales: {ex.Message}");
        }
    }
}
