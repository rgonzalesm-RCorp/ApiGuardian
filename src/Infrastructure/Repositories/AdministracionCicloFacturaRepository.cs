using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;

namespace ApiGuardian.Infrastructure.Repositories;

public class AdministracionCicloFacturaRepository : IAdministracionCicloFacturaRepository
{
    private readonly DapperContext _context;

    public AdministracionCicloFacturaRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<ListaAdministracionCicloFactura> Data, bool Success, string Mensaje, int Total)> GetAllAdministracionCiclofactura(int page, int pageSize, int lCicloId)
    {
        try
        {
            var query = @"
                SELECT 
                    AF.lciclopresentafactura_id AS lCicloFacturaId,
                    AF.dtfechaadd AS fechaRegistro,
                    AC.lciclo_id AS lCicloId,
                    UPPER(AC.snombre) AS ciclo,
                    ACT.lcontacto_id AS lContactoId,
                    UPPER(ACT.snombrecompleto) AS sNombreCompleto,
                    AF.lsemana_id AS lSeemanaId,
                    UPPER(S.lnombre) AS semana
                FROM administracionciclopresentafactura AF
                INNER JOIN administracionciclo AC ON AC.lciclo_id = AF.lciclo_id
                INNER JOIN administracionsemanaciclo SC ON SC.lsemana_id = AF.lsemana_id
                INNER JOIN administracionsemana S ON S.idsemana = SC.lnrosemana
                INNER JOIN administracioncontacto ACT ON ACT.lcontacto_id = AF.lcontacto_id
                WHERE AC.lciclo_id = @LCicloId
                LIMIT @PageSize OFFSET @Offset;
            ";
            string queryCount = $@"select 
                                COUNT(*)
                            from administracionciclopresentafactura AF
                            inner join administracionciclo AC on AC.lciclo_id = AF.lciclo_id
                            inner join administracionsemanaciclo SC on SC.lsemana_id = AF.lsemana_id
                            inner join administracionsemana S on S.idsemana = SC.lnrosemana
                            inner join administracioncontacto ACT on ACT.lcontacto_id = AF.lcontacto_id
                            where AC.lciclo_id  ={lCicloId}";

            using var connection = _context.CreateConnection();

            // Obtener el total
            int total = await connection.ExecuteScalarAsync<int>(queryCount, new { lCicloId });

            var result = await connection.QueryAsync<ListaAdministracionCicloFactura>(
                query,
                new
                {
                    LCicloId = lCicloId,
                    PageSize = pageSize,
                    Offset = page
                });

            if (!result.Any())
            {
                return (Enumerable.Empty<ListaAdministracionCicloFactura>(), false, "No se encontraron registros.", 0);
            }

            return (result, true, "Datos obtenidos correctamente.", total);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return (Enumerable.Empty<ListaAdministracionCicloFactura>(), false, $"Error: {ex.Message}", 0);
        }
    }

    public async Task<(bool success, string mensaje)> InsertAdministracionCiclofactura(AdministracionCicloFactura data)
    {
        try
        {
            using var connection = _context.CreateConnection();
            var existeQuery = @"
                SELECT COUNT(*) 
                FROM administracionciclopresentafactura
                WHERE lciclo_id = @LCicloId
                AND lcontacto_id = @LContactoId
                AND lsemana_id = @LSemanaId;
            ";

            var existe = await connection.ExecuteScalarAsync<int>(existeQuery, new
            {
                data.LCicloId,
                data.LContactoId,
                data.LSemanaId
            });

            if (existe > 0)
            {
                return (false, "El registro ya existe para este ciclo, contacto y semana.");
            }

            var nextIdQuery = @"SELECT IFNULL(MAX(lciclopresentafactura_id), 0) + 1 
                                FROM administracionciclopresentafactura;";

            var nextId = await connection.ExecuteScalarAsync<int>(nextIdQuery);

            var insertQuery = @"
                INSERT INTO administracionciclopresentafactura (
                    susuarioadd,
                    dtfechaadd,
                    susuariomod,
                    dtfechamod,
                    lciclopresentafactura_id,
                    lciclo_id,
                    lcontacto_id,
                    lsemana_id
                ) VALUES (
                    @Usuario,
                    NOW(),
                    @Usuario,
                    NOW(),
                    @Id,
                    @LCicloId,
                    @LContactoId,
                    @LSemanaId
                );
            ";

            var rowsAffected = await connection.ExecuteAsync(insertQuery, new
            {
                Usuario = data.Usuario,
                Id = nextId,
                data.LCicloId,
                data.LContactoId,
                data.LSemanaId
            });

            if (rowsAffected > 0)
                return (true, "Registro insertado correctamente.");
            else
                return (false, "No se insertó ningún registro.");
        }
        catch (Exception ex)
        {
            return (false, $"Error al insertar ciclo factura: {ex.Message}");
        }
    }

   
}
