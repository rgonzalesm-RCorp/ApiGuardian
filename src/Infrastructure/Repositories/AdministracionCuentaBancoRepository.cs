using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;

namespace ApiGuardian.Infrastructure.Repositories;

public class AdministracionCuentaBancoRepository : IAdministracionCuentaBancoRepository
{
    private readonly DapperContext _context;

    public AdministracionCuentaBancoRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<(CuentaBanco Data, bool Success, string Mensaje)> GetCuentaBanco(int lContactoId)
    {
        try
        {
            string query = @"
                select 
                    lcontacto_id LContactoId, 
                    ctienecuenta CTieneCuenta, 
                    lcuentabanco LCuentaBanco, 
                    lcodigobanco LCodigoBanco, 
                    cbaja CBaja, 
                    snombrecompleto SNombreCompleto, 
                    dtfecharegistro FechaRegistro, 
                    scedulaidentidad SCedulaIdentidad, 
                    lnit LNit 
                from administracioncontacto 
                where lcontacto_id = @lContactoId";

            using var connection = _context.CreateConnection();

            var data = await connection.QueryFirstOrDefaultAsync<CuentaBanco>(query, new { lContactoId });

            if (data == null)
            {
                return (new CuentaBanco(), false, "No se encontró el contacto solicitado.");
            }

            return (data, true, "Datos obtenidos correctamente.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return (new CuentaBanco(), false, $"Error: {ex.Message}");
        }
    }
    public async Task<(IEnumerable<CuentaBanco> Data, bool Success, string Mensaje, int Total)> GetAllCuentaBanco(int page, int pageSize, string? search)
    {
        try
        {
            search = search ?? "";

            string query = @"
                SELECT 
                    AC.lcontacto_id LContactoId, 
                    AC.ctienecuenta CTieneCuenta, 
                    AC.lcuentabanco LCuentaBanco, 
                    AC.lcodigobanco LCodigoBanco, 
                    AC.cbaja CBaja, 
                    AC.snombrecompleto SNombreCompleto, 
                    AC.dtfecharegistro FechaRegistro, 
                    AC.scedulaidentidad SCedulaIdentidad, 
                    AC.lnit LNit,
                    IFNULL(AB.lbanco_id, 0) LBancoId,
                    IFNULL(AB.snombre, '') Banco,
                    AC.sotro Comentario,
                    IFNULL( AM.snombre, '') Moneda
                FROM administracioncontacto AC
                LEFT JOIN administracionbanco AB ON AB.lbanco_id = AC.lbanco_id
                LEFT JOIN administracionmoneda AM on AM.lmoneda_id = AB.lmoneda_id
                WHERE (snombrecompleto LIKE @Search OR scedulaidentidad LIKE @Search) and AC.lcontacto_id  > 0 AND AC.cbaja = 0
                LIMIT @PageSize OFFSET @page;";

            string queryTotal = @"
                SELECT COUNT(*) 
                FROM administracioncontacto AC
                LEFT JOIN administracionbanco AB ON AB.lbanco_id = AC.lbanco_id
                LEFT JOIN administracionmoneda AM on AM.lmoneda_id = AB.lmoneda_id
                WHERE (snombrecompleto LIKE @Search OR scedulaidentidad LIKE @Search) and AC.lcontacto_id  > 0 AND AC.cbaja = 0;
            ";

            using var connection = _context.CreateConnection();

            var parameters = new
            {
                Search = $"%{search}%",
                PageSize = pageSize,
                page  
            };

            var data = await connection.QueryAsync<CuentaBanco>(query, parameters);
            var total = await connection.ExecuteScalarAsync<int>(queryTotal, parameters);

            return (data, true, "Datos obtenidos correctamente.", total);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return (Enumerable.Empty<CuentaBanco>(), false, $"Error: {ex.Message}", 0);
        }
    }
    public async Task<(bool Success, string Mensaje)> UpdateCuentaBanco(DataCuentaBanco data)
    {
        try
        {
            string query = @"
                UPDATE administracioncontacto
                SET
                    ctienecuenta = @CTieneCuenta,
                    lcuentabanco = @LCuentaBanco,
                    lcodigobanco = @LCodigoBanco,
                    cbaja = @CBaja,
                    snombrecompleto = @SNombreCompleto,
                    dtfecharegistro = @FechaRegistro,
                    scedulaidentidad = @SCedulaIdentidad,
                    lnit = @LNit,
                    susuariomod = @usuario,
                    dtfechamod = now(),
                    lbanco_id = @LBancoId,
                    sotro = @Comentario
                WHERE lcontacto_id = @LContactoId;
            ";

            using var connection = _context.CreateConnection();

            var rows = await connection.ExecuteAsync(query, new
            {
                data.LContactoId,
                data.CTieneCuenta,
                data.LCuentaBanco,
                data.LCodigoBanco,
                data.CBaja,
                data.SNombreCompleto,
                data.FechaRegistro,
                data.SCedulaIdentidad,
                data.LNit,
                data.usuario,
                data.LBancoId,
                data.Comentario
            });

            if (rows > 0)
            {
                return (true, "Registro actualizado correctamente.");
            }

            return (false, "No se encontró el registro o no se realizaron cambios.");
        }
        catch (Exception ex)
        {
            return (false, $"Error al actualizar: {ex.Message}");
        }
    }

 
}
