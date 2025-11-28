using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using System.Linq.Expressions;
using Newtonsoft.Json;

namespace ApiGuardian.Infrastructure.Repositories;

public class AdministracionDescuentoComisionRepository : IAdministracionDescuentoComisionRepository
{
    private readonly DapperContext _context;
    private readonly ILogService _log;
    private string NOMBREARCHIVO = "UtilsRepository.CS";
    public AdministracionDescuentoComisionRepository(DapperContext context, ILogService log)
    {
        _context = context;
        _log = log;
    }

    public async Task<(DataComision Data, bool Success, string Mensaje)> GetComision(string LogTransaccionId, int LContactoId, int LCicloId, int LSemanaId)
    {
        string nombreMetodo = "GetComision()";

        const string query = @"
            SELECT 
                (SELECT IFNULL(SUM(dtotalbono),0) 
                FROM administracionbonoresidual 
                WHERE lcontacto_id = @LContactoId AND lciclo_id = @LCicloId) AS ComisionResidual,

                (SELECT IFNULL(SUM(dcomision),0) 
                FROM administracionventagrupo 
                WHERE lcontacto_id = @LContactoId AND lciclo_id = @LCicloId) AS ComisionVentaGrupo,

                (SELECT IFNULL(SUM(dcomision),0) 
                FROM administracionventapersonal 
                WHERE lcontacto_id = @LContactoId AND lciclo_id = @LCicloId) AS ComisionVentaPersonal,

                (SELECT IFNULL(SUM(PAGAR),0) 
                FROM t_bono_liderazgo 
                WHERE vendedores_id = @LContactoId AND lciclo_id = @LCicloId) AS ComisionLiderazgo,

                (SELECT IFNULL(SUM(montoretencion),0) 
                FROM tbl_retencionempresa 
                WHERE lcontacto_id = @LContactoId AND lciclo_id = @LCicloId) AS Retencion,

                (SELECT IFNULL(SUM(dtotal),0) 
                FROM administraciondescuentociclo 
                WHERE lcontacto_id = @LContactoId AND lciclo_id = @LCicloId) AS DescuentoLote,

                (SELECT IFNULL(MAX(porcentajeret),0) 
                FROM tbl_retencionempresa 
                WHERE lcontacto_id = @LContactoId AND lciclo_id = @LCicloId) AS PorcentajeRetencion,

                (SELECT IFNULL(MAX(ldescuentociclo_id),0) 
                FROM administraciondescuentociclo 
                WHERE lcontacto_id = @LContactoId AND lciclo_id = @LCicloId) AS LDescuentoId;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var data = await connection.QuerySingleOrDefaultAsync<DataComision>(query, new
            {
                LContactoId = LContactoId,
                LCicloId = LCicloId
            });

            if (data != null)
            {
                data.TotalComision = data.ComisionVentaPersonal 
                                + data.ComisionVentaGrupo 
                                + data.ComisionResidual 
                                + data.ComisionLiderazgo;

                data.TotalFinal = data.TotalComision - (data.Retencion + data.DescuentoLote);
            }

            string mensaje = data != null ? "Consulta realizada correctamente." : "No se encontraron datos de comisión.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, data:{JsonConvert.SerializeObject(data, Formatting.Indented)}]");

            return (data ?? new DataComision(), true, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (new DataComision(), false, $"Error al obtener comisiones: {ex.Message}");
        }
    }
    public async Task<(IEnumerable<ListaAdministracionDescuentoCiclo> Data, bool Success, string Mensaje)> GetDetalleDescuentoCiclo(string LogTransaccionId, int LCicloId, int LContactoId)
    {
        string nombreMetodo = "GetDetalleDescuentoCiclo()";

        const string query = @"
            SELECT 
                DC.sdetalles AS SDetalles,
                DCD.ldescuentociclodetalle_id AS LDescuentoCicloDetalleId,
                ADCTP.ldescuentociclotipo_id AS LTipoDescuentoId,
                UPPER(ADCTP.snombre) AS STipoDescuento,
                AC.lcomplejo_id AS LComplejoId,
                UPPER(AC.snombre) AS SComplejo,
                DCD.smanzano AS SMzna,
                DCD.slote AS SLote,
                DCD.suv AS SUv,
                DCD.dmonto AS DMonto,
                DCD.sobservacion AS SObservacion,
                DC.lcontacto_id AS LContactoId
            FROM administraciondescuentociclo DC
            INNER JOIN administraciondescuentociclodetalle DCD 
                ON DCD.ldescuentociclo_id = DC.ldescuentociclo_id
            INNER JOIN administracioncomplejo AC 
                ON AC.lcomplejo_id = DCD.lcomplejo_id
            INNER JOIN administraciondescuentociclotipo ADCTP 
                ON ADCTP.ldescuentociclotipo_id = DCD.ldescuentociclotipo_id
            WHERE DC.lciclo_id = @LCicloId 
            AND DC.lcontacto_id = @LContactoId
            ORDER BY DCD.ldescuentociclodetalle_id DESC;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var data = await connection.QueryAsync<ListaAdministracionDescuentoCiclo>(query, new
            {
                LCicloId,
                LContactoId
            });

            bool success = data != null && data.Any();
            string mensaje = success ? "Detalles de descuento obtenidos correctamente." : "No se encontraron detalles de descuento.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, data:{JsonConvert.SerializeObject(data, Formatting.Indented)}]");

            return (data ?? Enumerable.Empty<ListaAdministracionDescuentoCiclo>(), success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (Enumerable.Empty<ListaAdministracionDescuentoCiclo>(), false, $"Error al obtener detalles de descuento: {ex.Message}");
        }
    }
    public async Task<(bool Success, string Mensaje)> EliminarDescuento(string LogTransaccionId, int LDescuentoDetalleId, int LContactoId, int LCicloId, string? Usuario)
    {
        string nombreMetodo = "EliminarDescuento()";

        const string queryDelete = @"
            DELETE FROM administraciondescuentociclodetalle 
            WHERE ldescuentociclodetalle_id = @LDescuentoDetalleId;
        ";

        const string queryUpdate = @"
            UPDATE administraciondescuentociclo DC
            SET 
                DC.sdetalles = (
                    IFNULL((SELECT GROUP_CONCAT(
                            CONCAT(DCD.sobservacion, ' - Monto: ', DCD.dmonto, ' $us. - ', DCD.suv)
                            SEPARATOR ','
                        )
                    FROM administraciondescuentociclodetalle DCD
                    WHERE DCD.ldescuentociclo_id = DC.ldescuentociclo_id), '')
                ),
                DC.dtotal = (
                    IFNULL((SELECT SUM(DCD.dmonto)
                    FROM administraciondescuentociclodetalle DCD
                    WHERE DCD.ldescuentociclo_id = DC.ldescuentociclo_id), 0)
                )
            WHERE DC.lciclo_id = @LCicloId 
            AND DC.lcontacto_id = @LContactoId;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [scriptDelete: {queryDelete}]");
        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [scriptUpdate: {queryUpdate}]");

        try
        {
            using var connection = _context.CreateConnection();

            var rows = await connection.ExecuteAsync(queryDelete, new { LDescuentoDetalleId });
            var rowsUpdate = await connection.ExecuteAsync(queryUpdate, new { LContactoId, LCicloId });

            bool success = rows > 0;
            string mensaje = success ? "Descuento eliminado correctamente." : "No se realizó la eliminación.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, rowsDelete:{rows}, rowsUpdate:{rowsUpdate}]");

            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al eliminar descuento: {ex.Message}");
        }
    }
    public async Task<(bool Success, string Mensaje)> InsertarDescuento(string LogTransaccionId, DataDescuento DataDescuento)
    {
        string nombreMetodo = "InsertarDescuento()";

        const string queryInsert = @"
            INSERT INTO administraciondescuentociclodetalle (
                susuarioadd,
                dtfechaadd,
                susuariomod,
                dtfechamod,
                ldescuentociclodetalle_id,
                ldescuentociclo_id,
                ldescuentociclotipo_id,
                lcomplejo_id,
                smanzano,
                slote,
                suv,
                dmonto,
                sobservacion
            )
            SELECT
                @Usuario,
                NOW(),
                @Usuario,
                NOW(),
                IFNULL(MAX_ID, 0) + 1,
                @LDescuentoCicloId,
                @LTipoDescuentoId,
                @LComplejoId,
                UPPER(@Mz),
                UPPER(@Lote),
                UPPER(@Uv),
                @Monto,
                UPPER(@Descripcion)
            FROM (
                SELECT MAX(ldescuentociclodetalle_id) AS MAX_ID
                FROM administraciondescuentociclodetalle
            ) AS sub;
        ";

        const string queryUpdate = @"
            UPDATE administraciondescuentociclo DC
            SET 
                DC.sdetalles = (
                    IFNULL((SELECT GROUP_CONCAT(
                            CONCAT(DCD.sobservacion, ' - Monto: ', DCD.dmonto, ' $us. - ', DCD.suv)
                            SEPARATOR ','
                        )
                    FROM administraciondescuentociclodetalle DCD
                    WHERE DCD.ldescuentociclo_id = DC.ldescuentociclo_id), '')
                ),
                DC.dtotal = (
                    IFNULL((SELECT SUM(DCD.dmonto)
                    FROM administraciondescuentociclodetalle DCD
                    WHERE DCD.ldescuentociclo_id = DC.ldescuentociclo_id), 0)
                )
            WHERE DC.lciclo_id = @LCicloId 
            AND DC.lcontacto_id = @LContactoId;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [scriptInsert: {queryInsert}]");
        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [scriptUpdate: {queryUpdate}]");

        try
        {
            using var connection = _context.CreateConnection();

            // Si no existe ciclo de descuento, lo creamos primero
            if (DataDescuento.LDescuentoCicloId <= 0)
            {
                var responseInsert = await GuardarAdministracionDescuentoCiclo(LogTransaccionId, DataDescuento);
                DataDescuento.LDescuentoCicloId = responseInsert.Success ? responseInsert.LDescuentoCicloId : 0;
            }

            var rows = await connection.ExecuteAsync(queryInsert, new
            {
                DataDescuento.Usuario,
                DataDescuento.LDescuentoCicloId,
                DataDescuento.LTipoDescuentoId,
                DataDescuento.LComplejoId,
                DataDescuento.Mz,
                DataDescuento.Lote,
                DataDescuento.Uv,
                DataDescuento.Monto,
                DataDescuento.Descripcion
            });

            var rowsUpdate = await connection.ExecuteAsync(queryUpdate, new
            {
                DataDescuento.LContactoId,
                DataDescuento.LCicloId
            });

            bool success = rows > 0;
            string mensaje = success ? "Descuento aplicado correctamente." : "No se insertó ningún registro.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, rowsInsert:{rows}, rowsUpdate:{rowsUpdate}, data:{JsonConvert.SerializeObject(DataDescuento, Formatting.Indented)}]");

            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al aplicar el descuento: {ex.Message}");
        }
    }
    private async Task<(bool Success, string Mensaje, int LDescuentoCicloId)> GuardarAdministracionDescuentoCiclo(string LogTransaccionId, DataDescuento Data)
    {
        string nombreMetodo = "GuardarAdministracionDescuentoCiclo()";

        const string query = @"
            INSERT INTO administraciondescuentociclo (
                susuarioadd,
                dtfechaadd,
                susuariomod,
                dtfechamod,
                ldescuentociclo_id,
                lciclo_id,
                lcontacto_id,
                dtotal,
                sdetalles,
                lsemana_id
            )
            SELECT 
                @Usuario,
                NOW(),
                @Usuario,
                NOW(),
                IFNULL(MAX_ID, 0) + 1,
                @LCicloId,
                @LContactoId,
                0,
                '',
                1
            FROM (
                SELECT MAX(ldescuentociclo_id) AS MAX_ID 
                FROM administraciondescuentociclo
            ) AS sub;

            SELECT MAX(ldescuentociclo_id) 
            FROM administraciondescuentociclo;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var newId = await connection.QuerySingleAsync<int>(query, new
            {
                Data.Usuario,
                Data.LCicloId,
                Data.LContactoId
                //Data.LSemanaId (si en el futuro se parametriza)
            });

            bool success = newId > 0;
            string mensaje = success ? "Descuento registrado correctamente." : "No se realizó el registro.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, newId:{newId}, data:{JsonConvert.SerializeObject(Data, Formatting.Indented)}]");

            return (success, mensaje, newId);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al registrar descuento: {ex.Message}", 0);
        }
    }

}
