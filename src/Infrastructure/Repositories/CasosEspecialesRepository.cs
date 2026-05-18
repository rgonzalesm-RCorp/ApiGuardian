using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using Newtonsoft.Json;
using System.Text;
using Query.Cnx;
using Microsoft.Extensions.Configuration;

namespace ApiGuardian.Infrastructure.Repositories;

public class CasosEspecialesRepository : ICasosEspecialesRepository
{
    private readonly DapperContext _context;
    private readonly DapperContextSqlServer _contextSql;
    private readonly ILogService _log;
    private string NOMBREARCHIVO = "CasosEspecialesRepository.CS";
    private readonly IConfiguration _configuration;

    public CasosEspecialesRepository(DapperContext context, ILogService log, IConfiguration configuration, DapperContextSqlServer contextSql)
    {
        _context = context;
        _log = log;
        _configuration = configuration;
        _contextSql = contextSql;
    }
    public async Task<(IEnumerable<ItemVentaCnx> VentasCasosEspeciales, bool Success, string Mensaje)> GetVentasCasosEspeciales(string LogTransaccionId,string Usuario, string Inicio, string Fin)
    {
        string nombreMetodo = "GetVentasCasosEspeciales()";

        var query = ScriptCnx.QueryVentaCnx(_configuration, true);

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}, Usuario:{Usuario}, Inicio:{Inicio}], Fin:{Fin}");

        try
        {
            using var connection = _contextSql.CreateConnection();

            var Lista = await connection.QueryAsync<ItemVentaCnx>(query, new {Inicio, Fin});

            bool success = true;
            string mensaje = success ? "Casos especiales obtenidos correctamente." : "No se encontraron casos especiales.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Fin de metodo [mensaje: {mensaje}]");

            return (Lista ?? Enumerable.Empty<ItemVentaCnx>(), success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (Enumerable.Empty<ItemVentaCnx>(), false, $"Error al obtener los casos especiales: {ex.Message}");
        } 
    }
    public async Task<(IEnumerable<UpgradeSolicitudDto> Lista, bool Success, string Mensaje)> GetUpgradeSolicitudPorVentas(string LogTransaccionId, string Usuario, string UpgVentaIds)
    {
        string nombreMetodo = "GetUpgradeSolicitudPorVentas()";

        string query = @"
            SELECT 
                UpgradeSolicitudId AS SolicitudId,
                DocId,
                DocIdVendedor,

                SoliEmpresaId AS EmpresaHoldId,
                SoliProyectoId AS ProyectoHoldId,
                SoliVentaId AS VentaHoldId,
                SoliCodigoProducto AS ProductoHoldId,

                MontoVenta AS MontoHoldId,
                MontoPagado AS PagadoHoldId,
                MontoDeuda AS DeudaHoldId,

                UpgEmpresaId AS EmpresaId,
                UpgProyectoId AS ProyectoId,
                UpgVentaId AS VentaId,
                UpgCodigoProducto AS ProductoId,
                UpgMontoVenta AS Monto,
                UpgMontoDeuda AS Deuda,
                MontoCuotaUpgrade AS Cuota
            FROM BDBpmSion.dbo.UpgradeSolicitud
            WHERE UpgVentaId IN (
                SELECT TRY_CONVERT(INT, value)
                FROM STRING_SPLIT(@UpgVentaIds, ',')
                WHERE TRY_CONVERT(INT, value) IS NOT NULL
            );";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
            $"Inicio de metodo [script: {query}, Usuario:{Usuario}, UpgVentaIds:{UpgVentaIds}]");

        try
        {
            using var connection = _contextSql.CreateConnection();

            var lista = await connection.QueryAsync<UpgradeSolicitudDto>(
                query,
                new { UpgVentaIds }
            );

            bool success = lista.Any();

            string mensaje = success
                ? "Solicitudes de upgrade obtenidas correctamente."
                : "No se encontraron solicitudes de upgrade.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}]");

            return (lista ?? Enumerable.Empty<UpgradeSolicitudDto>(), success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);

            return (
                Enumerable.Empty<UpgradeSolicitudDto>(),
                false,
                $"Error al obtener solicitudes de upgrade: {ex.Message}"
            );
        }
    }
    public async Task<(bool Success, string Mensaje)> SaveUpgradeSolicitud(string LogTransaccionId, string Usuario, int LCicloId, List<UpgradeSolicitudDto> Listado)
    {
        string metodo = "SaveUpgradeSolicitud()";

        const string insertQuery = @"
            INSERT INTO upgrade_solicitud
            (
                solicitud_id,
                doc_id,
                doc_id_vendedor,

                empresa_hold_id,
                proyecto_hold_id,
                venta_hold_id,
                producto_hold_id,

                monto_hold,
                pagado_hold,
                deuda_hold,

                empresa_id,
                proyecto_id,
                venta_id,
                producto_id,

                monto,
                deuda,
                cuota,

                estado,
                usuario_creacion,
                fecha_creacion,
                usuario_modificacion,
                fecha_modificacion,
                lciclo_id
            )
            VALUES
            (
                @SolicitudId,
                @DocId,
                @DocIdVendedor,

                @EmpresaHoldId,
                @ProyectoHoldId,
                @VentaHoldId,
                @ProductoHoldId,

                @MontoHoldId,
                @PagadoHoldId,
                @DeudaHoldId,

                @EmpresaId,
                @ProyectoId,
                @VentaId,
                @ProductoId,

                @Monto,
                @Deuda,
                @Cuota,

                1,
                @Usuario,
                NOW(),
                @Usuario,
                NOW(),
                @LCicloId
            );

            SELECT LAST_INSERT_ID();
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo,
            $"Inicio de guardado UpgradeSolicitud. Usuario:{Usuario}");

        if (Listado == null || !Listado.Any())
            return (false, "No existen datos para guardar.");

        using var con = _context.CreateConnection();
        con.Open();

        using var transaction = con.BeginTransaction();

        try
        {
            int totalRegistros = 0;

            foreach (var item in Listado)
            {
                long upgradeSolicitudId = await con.ExecuteScalarAsync<long>(
                    insertQuery,
                    new
                    {
                        item.SolicitudId,
                        item.DocId,
                        item.DocIdVendedor,

                        item.EmpresaHoldId,
                        item.ProyectoHoldId,
                        item.VentaHoldId,
                        item.ProductoHoldId,

                        item.MontoHoldId,
                        item.PagadoHoldId,
                        item.DeudaHoldId,

                        item.EmpresaId,
                        item.ProyectoId,
                        item.VentaId,
                        item.ProductoId,

                        item.Monto,
                        item.Deuda,
                        item.Cuota,

                        Usuario,
                        LCicloId
                    },
                    transaction
                );

                item.UpgradeSolicitudId = upgradeSolicitudId;
                totalRegistros++;
            }

            transaction.Commit();

            string mensaje = $"Registros creados correctamente. Total: {totalRegistros}.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo,
                $"Fin de método [mensaje: {mensaje}]");

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
