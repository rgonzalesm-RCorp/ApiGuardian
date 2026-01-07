using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using System.Text.Json;

namespace ApiGuardian.Infrastructure.Repositories;

public class AdministracionDetalleFacturaRepository : IAdministracionDetalleFacturaRepository
{
    private readonly DapperContext _context;
    private readonly ILogService _log;
    private const string NOMBREARCHIVO = "AdministracionDetalleFacturaRepository.cs";

    public AdministracionDetalleFacturaRepository(
        DapperContext context,
        ILogService log)
    {
        _context = context;
        _log = log;
    }

    // =====================================================
    public async Task<(IEnumerable<AdministracionDetalleFactura> Data, bool Success, string Mensaje)>GetDetalleFactura(string logId)
    {
        const string metodo = "GetDetalleFactura()";

        const string query = @"
            SELECT
                ldetallefactura_id AS LDetalleFacturaId,
                ltipocomision_id AS LTipoComisionId,
                sdetalle AS SDetalle,
                estado AS Estado,
                usuarioadd AS Usuario
            FROM administraciondetallefactura
            WHERE estado = 1
            ORDER BY ldetallefactura_id DESC;
        ";

        _log.Info(logId, NOMBREARCHIVO, metodo, $"Inicio. Script={query}");

        try
        {
            using var con = _context.CreateConnection();
            var data = await con.QueryAsync<AdministracionDetalleFactura>(query);

            bool success = data.Any();
            return (data, success, success ? "Datos obtenidos." : "Sin registros.");
        }
        catch (Exception ex)
        {
            _log.Error(logId, NOMBREARCHIVO, metodo, "Error obteniendo datos", ex);
            return (Enumerable.Empty<AdministracionDetalleFactura>(), false, ex.Message);
        }
    }

    // =====================================================
    public async Task<(IEnumerable<ItemAdministracionDetalleFactura> Data, bool Success, string Mensaje, int Total)>GetDetalleFacturaPagination(string logId, int page, int pageSize)
    {
        const string metodo = "GetDetalleFacturaPagination()";

        const string query = @"
            SELECT
                ADF.ldetallefactura_id AS LDetalleFacturaId,
                ADF.ltipocomision_id AS LTipoComisionId,
                ATC.SNOMBRE AS TipoComision,
                ADF.sdetalle AS SDetalle,
                ADF.estado AS Estado,
                ADF.usuarioadd AS Usuario
            FROM administraciondetallefactura ADF
            INNER JOIN administraciontipocomision ATC ON ATC.ltipocomision_id = ADF.ltipocomision_id
            WHERE ADF.estado = 1
            ORDER BY ADF.ldetallefactura_id DESC
            LIMIT @pageSize OFFSET @page;
        ";

        const string countQuery = "SELECT COUNT(*) FROM administraciondetallefactura;";

        _log.Info(logId, NOMBREARCHIVO, metodo, $"Inicio paginación page={page}, pageSize={pageSize}");

        try
        {
            using var con = _context.CreateConnection();
            var data = await con.QueryAsync<ItemAdministracionDetalleFactura>(query, new { page, pageSize });
            int total = await con.ExecuteScalarAsync<int>(countQuery);

            bool success = data.Any();
            return (data, success, success ? "Datos obtenidos." : "Sin registros.", total);
        }
        catch (Exception ex)
        {
            _log.Error(logId, NOMBREARCHIVO, metodo, "Error en paginación", ex);
            return (Enumerable.Empty<ItemAdministracionDetalleFactura>(), false, ex.Message, 0);
        }
    }

    // =====================================================
    public async Task<(bool Success, string Mensaje)>GuardarDetalleFactura(string logId, AdministracionDetalleFactura data)
    {
        const string metodo = "GuardarDetalleFactura()";

        const string nextIdQuery = "SELECT IFNULL(MAX(ldetallefactura_id),0)+1 FROM administraciondetallefactura;";

        string queryBuscar = @"select COUNT(*) from administraciondetallefactura where estado = 1 and ltipocomision_id = 1";

        const string insertQuery = @"
            INSERT INTO administraciondetallefactura
            (ldetallefactura_id, ltipocomision_id, sdetalle, estado, fechaadd, usuarioadd)
            VALUES
            (@nextId, @LTipoComisionId, @SDetalle, 1, NOW(), @Usuario);
        ";

        _log.Info(logId, NOMBREARCHIVO, metodo, $"Insert Data={JsonSerializer.Serialize(data)}");

        try
        {
            using var con = _context.CreateConnection();
            int existeRegistro = await con.ExecuteScalarAsync<int>(queryBuscar);
            if (existeRegistro > 0)
            {
                return (false, "Ya existe un detalle con el tipo seleccionada.");
            }

            int nextId = await con.ExecuteScalarAsync<int>(nextIdQuery);

            int rows = await con.ExecuteAsync(insertQuery, new
            {
                nextId,
                data.LTipoComisionId,
                data.SDetalle,
                data.Usuario
            });

            return (rows > 0, rows > 0 ? "Registro insertado." : "No se insertó.");
        }
        catch (Exception ex)
        {
            _log.Error(logId, NOMBREARCHIVO, metodo, "Error insertando", ex);
            return (false, ex.Message);
        }
    }

    // =====================================================
    public async Task<(bool Success, string Mensaje)>ModificarDetalleFactura(string logId, AdministracionDetalleFactura data)
    {
        const string metodo = "ModificarDetalleFactura()";

        const string query = @"
            UPDATE administraciondetallefactura
            SET ltipocomision_id=@LTipoComisionId,
                sdetalle=@SDetalle,
                fechamod=NOW(),
                usuariomod=@Usuario
            WHERE ldetallefactura_id=@LDetalleFacturaId;
        ";

        _log.Info(logId, NOMBREARCHIVO, metodo, $"Update Data={JsonSerializer.Serialize(data)}");

        try
        {
            using var con = _context.CreateConnection();
            int rows = await con.ExecuteAsync(query, data);

            return (rows > 0, rows > 0 ? "Registro actualizado." : "No se actualizó.");
        }
        catch (Exception ex)
        {
            _log.Error(logId, NOMBREARCHIVO, metodo, "Error actualizando", ex);
            return (false, ex.Message);
        }
    }

    // =====================================================
    public async Task<(bool Success, string Mensaje)> EliminarDetalleFactura(string logId, int lDetalleFacturaId)
    {
        const string metodo = "EliminarDetalleFactura()";

        const string query = @"
            UPDATE administraciondetallefactura
            SET estado = 0,
                fechamod = NOW()
            WHERE ldetallefactura_id = @lDetalleFacturaId;
        ";

        _log.Info(logId, NOMBREARCHIVO, metodo, $"SoftDelete ID={lDetalleFacturaId}");

        try
        {
            using var con = _context.CreateConnection();
            int rows = await con.ExecuteAsync(query, new { lDetalleFacturaId });

            return (rows > 0, rows > 0 ? "Registro inactivado." : "No se pudo inactivar.");
        }
        catch (Exception ex)
        {
            _log.Error(logId, NOMBREARCHIVO, metodo, "Error eliminando", ex);
            return (false, ex.Message);
        }
    }
    public async  Task<(IEnumerable<AdministracionTipoComision> Data, bool Success, string Mensaje)>GetTipoComision(string logId)
    {
        const string metodo = "GetTipoComision()";

        const string query = @"
            select ltipocomision_id TipoComisionId, snombre Descripcion from administraciontipocomision
        ";

        _log.Info(logId, NOMBREARCHIVO, metodo, $"Inicio. Script={query}");

        try
        {
            using var con = _context.CreateConnection();
            var data = await con.QueryAsync<AdministracionTipoComision>(query);

            bool success = data.Any();
            return (data, success, success ? "Datos obtenidos." : "Sin registros.");
        }
        catch (Exception ex)
        {
            _log.Error(logId, NOMBREARCHIVO, metodo, "Error obteniendo datos", ex);
            return (Enumerable.Empty<AdministracionTipoComision>(), false, ex.Message);
        }
    }

}
