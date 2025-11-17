using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;

namespace ApiGuardian.Infrastructure.Repositories;

public class AdministracionObservacionComisionRepository : IAdministracionObservacionComisionRepository
{
    private readonly DapperContext _context;

    public AdministracionObservacionComisionRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<ListaAdministracionObservacionComision> Data, bool Success, string Mensaje, int Total)> GetAllAdministracionCObservacionComisionAsync(int page, int pageSize, string? search, int lCicloId)
    {
        const string queryData = @"
            SELECT 
                AOC.lobservacioncomision_id AS ObservacionId,
                AOC.dtfechaadd AS Fecha,
                AOC.lcontacto_id AS LContactoId,
                ACT.snombrecompleto AS SNombreCompleto,
                AC.lciclo_id AS LCicloId,
                UPPER(AC.snombre) AS Ciclo,
                AOC.sobservacion AS SObservacion
            FROM administracionobservacioncomision AS AOC
            INNER JOIN administracionciclo AS AC ON AC.lciclo_id = AOC.lciclo_id
            INNER JOIN administracioncontacto AS ACT ON ACT.lcontacto_id = AOC.lcontacto_id
            WHERE AOC.lciclo_id = @lCicloId
            AND (@search IS NULL OR ACT.snombrecompleto LIKE CONCAT('%', @search, '%'))
            ORDER BY AOC.dtfechaadd DESC
            LIMIT @pageSize OFFSET @page;
        ";

        const string queryCount = @"
            SELECT COUNT(*)
            FROM administracionobservacioncomision AS AOC
            INNER JOIN administracionciclo AS AC ON AC.lciclo_id = AOC.lciclo_id
            INNER JOIN administracioncontacto AS ACT ON ACT.lcontacto_id = AOC.lcontacto_id
            WHERE AOC.lciclo_id = @lCicloId
            AND (@search IS NULL OR ACT.snombrecompleto LIKE CONCAT('%', @search, '%'));
        ";

        try
        {
            using var connection = _context.CreateConnection();

            // Obtener el total
            int total = await connection.ExecuteScalarAsync<int>(queryCount, new { lCicloId, search });

            // Obtener la lista paginada
            var data = await connection.QueryAsync<ListaAdministracionObservacionComision>(
                queryData, new { lCicloId, search, pageSize, page }
            );

            bool success = data != null && data.Any();
            string mensaje = success
                ? "Registros obtenidos correctamente."
                : "No se encontraron registros para los criterios especificados.";

            return (data ?? Enumerable.Empty<ListaAdministracionObservacionComision>(), success, mensaje, total);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error al obtener observaciones de comisión: {ex.Message}");
            return (Enumerable.Empty<ListaAdministracionObservacionComision>(), false, $"Error al obtener observaciones: {ex.Message}", 0);
        }
    }

    public async Task<(bool succes, string mensaje)> InsertAdministracionObservacionComision(AdministracionObservacionComision data)
    {
        try
        {
            var query = @"
                INSERT INTO administracionobservacioncomision (
                            susuarioadd,
                            dtfechaadd,
                            lobservacioncomision_id,
                            susuariomod,
                            dtfechamod,
                            lcontacto_id,
                            lciclo_id,
                            sobservacion
                        )
                SELECT 
                     @usuario,
                    NOW(),
                    IFNULL(MAX_ID, 0) + 1,
                    @usuario,
                    NOW(),
                    @LContactoId,
                    @LCicloId,
                    @SObservacion
                FROM (SELECT MAX(lobservacioncomision_id) AS MAX_ID 
                    FROM administracionobservacioncomision) AS sub;";

            using var connection = _context.CreateConnection();

            var rowsAffected = await connection.ExecuteAsync(query, new
            {
                data.usuario,
                data.LContactoId,
                data.LCicloId,
                data.SObservacion
            });

            if (rowsAffected > 0)
            {
                Console.WriteLine("Registro insertado correctamente.");
                return (true, "Registro insertado correctamente.");
            }
            else
            {
                Console.WriteLine("No se insertó ningún registro.");
                return (false, "No se insertó ningún registro.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al insertar observación de comisión: {ex.Message}");
            return (false, $"Error al insertar observación de comisión: {ex.Message}");
        }
    }
    public async Task<(bool succes, string mensaje)> UpdateAdministracionObservacionComision(AdministracionObservacionComision data)
    {
        try
        {
            var query = @"
                    update administracionobservacioncomision set 
                        susuariomod = @usuario, 
                        dtfechamod = now(), 
                        lcontacto_id = @LContactoId, 
                        lciclo_id = @LCicloId, 
                        sobservacion = @SObservacion
                    where lobservacioncomision_id = @lObservacionId";

            using var connection = _context.CreateConnection();

            var rowsAffected = await connection.ExecuteAsync(query, new {
                data.lObservacionId, data.LContactoId, data.LCicloId, data.usuario, data.SObservacion
            });

            if (rowsAffected > 0)
            {
                return (true, "Registro actualizado correctamente.");
            }
            else
            {
                return (false, "No se encontró ningún registro con el ID especificado.");
            }
        }
        catch (Exception ex)
        {
            return (false, $"Error al actualizar observación de comisión: {ex.Message}");
        }
    }

    public async Task<int> GetCountObservacionComicion( string? search,int lCicloId)
    {
        try
        {
            using var connection = _context.CreateConnection();
            var query = $@"select 
                            COUNT(*)
                            from administracionobservacioncomision AOC 
                            inner join administracionciclo AC on AC.lciclo_id = AOC.lciclo_id
                            inner join administracioncontacto ACT on ACT.lcontacto_id = AOC.lcontacto_id
                        WHERE AOC.lciclo_id ={lCicloId} and ACT.snombrecompleto LIKE '%{search}%'";
            int cantidadCiclofactura = await connection.ExecuteScalarAsync<int>(query);
            return cantidadCiclofactura;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return 0;
        }
        
    }
}
