using System.Text.Json;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using Dapper;
public class BrConfiguracionRepository : IBrConfiguracionRepository
{
    private readonly DapperContext _context;
    private readonly ILogService _log;
    private const string NOMBREARCHIVO = "BrConfiguracionRepository.cs";

    public BrConfiguracionRepository(DapperContext context, ILogService log)
    {
        _context = context;
        _log = log;
    }

    // =========================================================
    // GET
    // =========================================================
    public async Task<(IEnumerable<DetailsBrConfiguracion>, bool, string)>GetConfiguracion(string LogTransaccionId, string Usuario)
    {
        const string metodo = "GetConfiguracion()";
        string query = @"select 
                            bc.brconfiguracion_id BrConfiguracionId
                            , bc.lciclo_id LCicloId
                            , bc.brtipoproducto_id TipoProductoId
                            , bn.brniveles_id NivelId
                            , bcd.brconfiguraciondetalle_id BrConfiguracionDetalleId
                            , ac.snombre Ciclo
                            , btp.descripcion TipoProducto
                            , bn.descripcion NombreNivel
                            , bn.nivel Nivel
                            , bcd.porcentaje PorcentajeComision
                            from br_configuracion bc
                        inner join br_configuracionDetalle bcd on bc.brconfiguracion_id = bcd.brconfiguracion_id
                        inner join br_niveles bn on bn.brniveles_id = bcd.brniveles_id
                        INNER join br_tipoproducto btp on btp.brtipoproducto_id = bc.brtipoproducto_id
                        inner join administracionciclo ac on ac.lciclo_id = bc.lciclo_id
                        where bc.estado = 1 and bcd.estado = 1
                        order by bc.lciclo_id desc, bc.brtipoproducto_id, bn.nivel ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo, $"Inicio: Usuario: {Usuario}");

        try
        {
            using var con = _context.CreateConnection();

            var cab = await con.QueryAsync<DetailsBrConfiguracion>(query);

            return (cab, true, "OK");
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, metodo, "Error", ex);
            return (Enumerable.Empty<DetailsBrConfiguracion>(), false, ex.Message);
        }
    }
    public async Task<(IEnumerable<BrNiveles> Data, bool Success, string Mensaje)>GetNivel(string LogTransaccionId, string Usuario)
    {
        const string metodo = "GetNivel()";
        string query = @"select 
                            brniveles_id NivelId
                            , descripcion NombreNivel
                            , nivel Nivel
                        from br_niveles;";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo, $"Query: {query} , Inicio: Usuario: {Usuario}");

        try
        {
            using var con = _context.CreateConnection();

            var cab = await con.QueryAsync<BrNiveles>(query);

            return (cab, true, "OK");
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, metodo, "Error", ex);
            return (Enumerable.Empty<BrNiveles>(), false, ex.Message);
        }
    }
public async Task<(IEnumerable<BrTipoProducto> Data, bool Success, string Mensaje)>GetTipoProducto(string LogTransaccionId, string Usuario)
    {
        const string metodo = "GetNivel()";
        string query = @"select 
                            brtipoproducto_id TipoProductoId
                            , descripcion TipoProducto
                        from br_tipoproducto";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo, $"Query: {query} , Inicio: Usuario: {Usuario}");

        try
        {
            using var con = _context.CreateConnection();

            var cab = await con.QueryAsync<BrTipoProducto>(query);

            return (cab, true, "OK");
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, metodo, "Error", ex);
            return (Enumerable.Empty<BrTipoProducto>(), false, ex.Message);
        }
    }

    // =========================================================
    // INSERT / UPDATE
    // =========================================================
    public async Task<(bool, string)> GuardarConfiguracion(string LogTransaccionId, string Usuario, BrConfiguracion data)
    {
        const string metodo = "GuardarConfiguracion()";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo, $"Data={JsonSerializer.Serialize(data)}, usuario: {Usuario}");

        string queryEmcabezadoInsert = $@"INSERT INTO br_configuracion
                    (brconfiguracion_id, lciclo_id, brtipoproducto_id, estado, fechaadd, usuarioadd)
                    VALUES
                    (@BrConfiguracionId, @LCicloId, @TipoProductoId, 1, NOW(), @Usuario);
                    SELECT IFNULL(MAX(brconfiguracion_id), 0) FROM br_configuracion;";
        string queryEmcabezadoUpdate = $@"UPDATE br_configuracion
                    SET lciclo_id=@LCicloId,
                        brtipoproducto_id = @TipoProductoId,
                        fechamod=NOW(),
                        usuariomod=@Usuario
                    WHERE brconfiguracion_id=@BrConfiguracionId";
        string queryDetalleInsert = $@"INSERT INTO br_configuraciondetalle
                    (brconfiguraciondetalle_id, brconfiguracion_id, brniveles_id, porcentaje, estado, fechaadd, usuarioadd)
                    VALUES
                    (@detId, @BrConfiguracionId, @BrNivelesId, @Porcentaje, 1, NOW(), @Usuario)";

        try
        {
            int BrConfiguracionId = data.BrConfiguracionId;
            using var con = _context.CreateConnection();


            if (BrConfiguracionId == 0)
            {
                BrConfiguracionId = await con.ExecuteScalarAsync<int>(queryEmcabezadoInsert, new { BrConfiguracionId, data.LCicloId, data.Usuario, data.TipoProductoId });
            }
            else
            {
                await con.QueryAsync(queryEmcabezadoUpdate, new { BrConfiguracionId, data.LCicloId, data.Usuario, data.TipoProductoId });

                // eliminar detalles previos
                await con.QueryAsync(@"
                    UPDATE br_configuraciondetalle
                    SET estado=0
                    WHERE brconfiguracion_id=@BrConfiguracionId",
                    new { BrConfiguracionId });
            }

            // INSERT DETALLE
            foreach (var d in data.Detalles)
            {
                await con.QueryAsync(queryDetalleInsert,
                    new
                    {
                        detId = 0,
                        BrConfiguracionId,
                        d.BrNivelesId,
                        d.Porcentaje,
                        data.Usuario
                    });
            }
            return (true, "Guardado correctamente");
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, metodo, "Error", ex);
            return (false, ex.Message);
        }
    }

    // =========================================================
    public async Task<(bool, string)> EliminarConfiguracion(string LogTransaccionId, string Usuario, int brConfiguracionId)
    {
        const string metodo = "EliminarConfiguracion()";
        string query = @"UPDATE br_configuracion SET estado=0 WHERE brconfiguracion_id=@brConfiguracionId;";
        string queryDetalle = @"UPDATE br_configuraciondetalle SET estado=0 WHERE brconfiguracion_id=@brConfiguracionId;";
        _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo, $"Inicio de metodo [Usuario: {Usuario}, script: {query}]");


        try
        {
            using var con = _context.CreateConnection();

            var rows = await con.ExecuteAsync(query, new { brConfiguracionId });
            var rowsDetalle = await con.ExecuteAsync(queryDetalle, new { brConfiguracionId });
            bool success = rows > 0;
            string mensaje = success ? "Configuracion eliminada correctamente" : "No se encontró al configuracion a eliminar";
            _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo, $"Fin de metodo [mensaje: {mensaje}, rowsAffected:{rows}]");
            
            return (success, mensaje); 
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, metodo, "Error", ex);
            return (false, ex.Message);
        }
    }
    public async Task<(bool Success, string Mensaje, bool existe)>ValidarRegistro(string LogTransaccionId, string Usuario, int LCicloId, int TipoProductoId)
    {
        //int total = await connection.ExecuteScalarAsync<int>(queryCount, parameters);
        const string metodo = "ValidarRegistro()";
        string query = @"select count(*) from br_configuracion where estado = 1 and lciclo_id = @LCicloId and brtipoproducto_id = @TipoProductoId;"; 
        _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo, $"Inicio de metodo [Usuario: {Usuario}, script: {query}]");


        try
        {
            using var con = _context.CreateConnection();

            int total = await con.ExecuteScalarAsync<int>(query, new {LCicloId, TipoProductoId}); 
             
            string mensaje = total > 0 ? "Ya existe una configuracion para el ciclo y tipo de producto seleccionado" : "No existe se puede crear";
            _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo, $"Fin de metodo [mensaje: {mensaje}, count registro:{total}]");
            
            return (true, mensaje, total >0? true: false); 
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, metodo, "Error", ex);
            return (false, ex.Message, true);
        }
    }

}