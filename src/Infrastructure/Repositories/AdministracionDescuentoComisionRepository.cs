using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using System.Linq.Expressions;

namespace ApiGuardian.Infrastructure.Repositories;

public class AdministracionDescuentoComisionRepository : IAdministracionDescuentoComisionRepository
{
    private readonly DapperContext _context;

    public AdministracionDescuentoComisionRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<(DataComision Data, bool Success, string Mensaje)> GetComision(int LContactoId, int LCicloId, int LSemanaId)
    {
        try
        {
            string query = @"
                SELECT 
                    (SELECT IFNULL(SUM(dtotalbono),0) 
                    FROM administracionbonoresidual 
                    WHERE lcontacto_id = @LContactoId AND lciclo_id = @LCicloId ) AS ComisionResidual,

                    (SELECT IFNULL(SUM(dcomision),0) 
                    FROM administracionventagrupo 
                    WHERE lcontacto_id = @LContactoId AND lciclo_id = @LCicloId ) AS ComisionVentaGrupo,

                    (SELECT IFNULL(SUM(dcomision),0) 
                    FROM administracionventapersonal 
                    WHERE lcontacto_id = @LContactoId AND lciclo_id = @LCicloId ) AS ComisionVentaPersonal,

                    (SELECT IFNULL(SUM(PAGAR),0) 
                    FROM t_bono_liderazgo where vendedores_id = @LContactoId and lciclo_id = @LCicloId) ComisionLiderazgo,

                    (SELECT IFNULL(SUM(montoretencion),0) 
                    FROM tbl_retencionempresa where lcontacto_id = @LContactoId and lciclo_id = @LCicloId) Retencion,

                    (SELECT IFNULL(SUM(dtotal),0) 
                    FROM administraciondescuentociclo where lcontacto_id = @LContactoId and lciclo_id = @LCicloId)DescuentoLote,

                    (SELECT IFNULL(MAX(porcentajeret),0) 
                    FROM tbl_retencionempresa where lcontacto_id = @LContactoId and lciclo_id = @LCicloId) PorcentajeRetencion,

                    (SELECT IFNULL(MAX(ldescuentociclo_id),0) 
                    FROM administraciondescuentociclo where lcontacto_id = @LContactoId and lciclo_id = @LCicloId)LDescuentoId
            ";

            using var connection = _context.CreateConnection();

            var data = await connection.QuerySingleOrDefaultAsync<DataComision>(query, new
            {
                LContactoId,
                LCicloId
            });
            data.TotalComision = data.ComisionVentaPersonal + data.ComisionVentaGrupo + data.ComisionResidual + data.ComisionLiderazgo;
            data.TotalFinal = (data.ComisionVentaPersonal + data.ComisionVentaGrupo + data.ComisionResidual + data.ComisionLiderazgo)-(data.Retencion + data.DescuentoLote);
            return (data ?? new DataComision(), true, "Consulta realizada correctamente.");
        }
        catch (Exception ex)
        {
            return (new DataComision(), false, $"Error al obtener comisiones: {ex.Message}");
        }
    }
    public async Task<(IEnumerable<ListaAdministracionDescuentoCiclo> Data, bool Success, string Mensaje)> GetDetalleDescuentoCiclo(int LCicloId, int LContactoId)
    {
        try
        {
            string query = @"
                SELECT 
                    dc.sdetalles AS SDetalles,
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
                    DC.lcontacto_id LContactoId
                FROM administraciondescuentociclo DC
                INNER JOIN administraciondescuentociclodetalle DCD 
                    ON DCD.ldescuentociclo_id = DC.ldescuentociclo_id
                INNER JOIN administracioncomplejo AC 
                    ON AC.lcomplejo_id = DCD.lcomplejo_id
                INNER JOIN administraciondescuentociclotipo ADCTP 
                    ON ADCTP.ldescuentociclotipo_id = DCD.ldescuentociclotipo_id
                WHERE DC.lciclo_id = @LCicloId 
                AND DC.lcontacto_id = @LContactoId;
            ";

            using var connection = _context.CreateConnection();

            var data = await connection.QueryAsync<ListaAdministracionDescuentoCiclo>(query, new
            {
                LCicloId,
                LContactoId
            });

            return (data, true, "Consulta realizada correctamente.");
        }
        catch (Exception ex)
        {
            return (Enumerable.Empty<ListaAdministracionDescuentoCiclo>(), false, $"Error al obtener complejos: {ex.Message}");
        }
    }
    public async Task<(bool Success, string Mensaje)> EliminarDescuento(int LDescuentoDetalleId, int LContactoId, int LCicloId, string? Usuario)
    {
        try
        {
            string query = @"
                DELETE FROM administraciondescuentociclodetalle 
                WHERE ldescuentociclodetalle_id = @LDescuentoDetalleId;
            ";
            string queryUpdate =@"
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

            using var connection = _context.CreateConnection();

            var rows = await connection.ExecuteAsync(query, new
            {
                LDescuentoDetalleId,
                Usuario
            });
            var rowsUpdate = await connection.ExecuteAsync(queryUpdate, new
            {
                LContactoId,
                LCicloId
            });

            if (rows > 0)
            {
                return (true, "Descuento eliminado correctamente.");
            }

            return (false, "No se realizó la eliminación.");
        }
        catch (Exception ex)
        {
            return (false, $"Error al eliminar descuento: {ex.Message}");
        }
    }
    public async Task<(bool Success, string Mensaje)> InsertarDescuento(DataDescuento DataDescuento)
    {
        try
        {
            string query = @"
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

                SELECT MAX(ldescuentociclodetalle_id)
                FROM administraciondescuentociclodetalle;
            ";
            string queryUpdate =@"
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
            using var connection = _context.CreateConnection();
            if(DataDescuento.LDescuentoCicloId  <= 0)
            {
               var responseInsert =  await GuardarAdministracionDescuentoCiclo(DataDescuento);
               DataDescuento.LDescuentoCicloId = responseInsert.Success ? responseInsert.LDescuentoCicloId : 0;
            }
            var rows = await connection.ExecuteAsync(query, new
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
            if (rows > 0)
            {
                return (true, "Descuento aplicado correctamente.");
            }

            return (false, "No se realizó la eliminación.");
        }
        catch (Exception ex)
        {
            return (false, $"Error al aplicar el descuento: {ex.Message}");
        }
    }

    private async Task<(bool Success, string Mensaje, int LDescuentoCicloId)> GuardarAdministracionDescuentoCiclo(DataDescuento Data)
    {
        try
        {
            string query = @"
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

            using var connection = _context.CreateConnection();

            var newId = await connection.QuerySingleAsync<int>(query, new
            {
                Usuario = Data.Usuario,
                LCicloId = Data.LCicloId,
                LContactoId = Data.LContactoId,
                //LSemanaId = Data.LSemanaId
            });

            if (newId > 0)
            {
                return (true, "Descuento registrado correctamente.", newId);
            }

            return (false, "No se realizó el registro.", 0);
        }
        catch (Exception ex)
        {
            return (false, $"Error al registrar descuento: {ex.Message}", 0);
        }
    }

}
