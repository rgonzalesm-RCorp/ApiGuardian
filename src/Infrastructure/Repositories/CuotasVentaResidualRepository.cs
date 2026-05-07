using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using Newtonsoft.Json;
using Query.Cnx;

namespace ApiGuardian.Infrastructure.Repositories;

public class CuotasVentaResidualRepository : ICuotasVentaResidualRepository
{
    private readonly DapperContext _context;
    private readonly DapperContextSqlServer  _contextSqlServer;
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

            var Lista = await connection.QueryAsync<VentaResidual>(query, new {Inicio, Fin});

            bool success = true;
            string mensaje = success ? "cuotas ventas residual obtenidos correctamente." : "No se encontraron cuotas ventas residual.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}]");

            return ( success, mensaje, Lista ?? new List<VentaResidual>());
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

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]" );

        try
        {
            using var connection = _context.CreateConnection();

            var lista = await connection.QueryAsync<ProductosPagarMensuales>(query);

            bool success = lista != null && lista.Any();

            string mensaje = success ? "Productos pagar mensuales obtenidos correctamente." : "No se encontraron productos pagar mensuales.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Fin de metodo [mensaje: {mensaje}]" );

            return (success, mensaje, lista ?? new List<ProductosPagarMensuales>());
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);

            return (false, $"Error al obtener productos pagar mensuales: {ex.Message}", Enumerable.Empty<ProductosPagarMensuales>());
        }
    }
}
