using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;

namespace ApiGuardian.Infrastructure.Repositories;

public class AdministracionBancoRepository : IAdministracionBancoRepository
{
    private readonly DapperContext _context;

    public AdministracionBancoRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<ListaAdministracionBanco> Data, bool Success, string Mensaje)> GetAllBanco()
    {
        try
        {
            string query = @"
                SELECT 
                    ab.lbanco_id LBancoId
                    , ab.snombre SNombre
                    , ab.scodigo SCodigo
                    , ab.estado Estado
                    , ab.fechaadd FechaAdd
                    , ab.usuarioadd UsuarioAdd
                    , ab.fechamod FechaMod
                    , ab.usuariomod UsuarioMod
                    , am.snombre Moneda
                    , am.lmoneda_id LMonedaId
                FROM administracionbanco  ab
                INNER JOIN administracionmoneda am ON am.lmoneda_id = ab.lmoneda_id
                WHERE estado = 1";


            using var connection = _context.CreateConnection();


            var data = await connection.QueryAsync<ListaAdministracionBanco>(query);

            return (data, true, "Datos obtenidos correctamente.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return (Enumerable.Empty<ListaAdministracionBanco>(), false, $"Error: {ex.Message}");
        }
    }
    public async Task<(bool Success, string Mensaje)> UpdateBanco(AdministracionBanco data)
    {
        try
        {
            string query = @"
                update administracionbanco set
                    snombre = @SNombre
                    , scodigo = @SCodigo
                    , estado = @Estado
                    , fechamod = now()
                    , usuariomod = @Usuario
                    , lmoneda_id = @LMonedaId
                where lbanco_id = @LBancoId
            ";

            using var connection = _context.CreateConnection();

            var rows = await connection.ExecuteAsync(query, new
            {
                data.SNombre
                , data.SCodigo
                , data.Estado
                , data.Usuario
                , data.LBancoId
                , data.LMonedaId
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
    public async Task<(bool Success, string Mensaje)> InsertBanco(AdministracionBanco data)
    {
        try
        {
            string query = @"
                INSERT INTO administracionbanco 
                (snombre, scodigo, estado, usuarioadd, usuariomod, FECHAADD, fechamod, lmoneda_id)
                VALUES 
                (@SNombre, @SCodigo, @Estado, @Usuario, @Usuario, NOW(), NOW(), @LMonedaId)
            ";

            using var connection = _context.CreateConnection();

            var rows = await connection.ExecuteAsync(query, new
            {
                data.SNombre
                , data.SCodigo
                , data.Estado
                , data.Usuario
                , data.LBancoId
                , data.LMonedaId
            });

            if (rows > 0)
            {
                return (true, "Registro insertado correctamente.");
            }

            return (false, "No se realizo e guardado.");
        }
        catch (Exception ex)
        {
            return (false, $"Error al actualizar: {ex.Message}");
        }
    }
    public async Task<(bool Success, string Mensaje)> DeleteBanco(int LBancoId, string? usuario)
    {
        try
        {
            string query = @"
                update administracionbanco set
                    estado = 0,
                    usuariomod = @usuario,
                    fechamod = now()
                where lbanco_id = @LBancoId
            ";

            using var connection = _context.CreateConnection();

            var rows = await connection.ExecuteAsync(query, new
            {
                LBancoId, usuario
            });

            if (rows > 0)
            {
                return (true, "Registro eliminado correctamente.");
            }

            return (false, "No se encontró el registro o no se realizaron cambios.");
        }
        catch (Exception ex)
        {
            return (false, $"Error al actualizar: {ex.Message}");
        }
    }
    public async Task<(IEnumerable<ListaMoneda> Data, bool Success, string Mensaje)> GetAllMoneda()
    {
        try
        {
            string query = @"SELECT lmoneda_id LMonedaId, snombre SNombre FROM administracionmoneda";

            using var connection = _context.CreateConnection();


            var data = await connection.QueryAsync<ListaMoneda>(query);

            return (data, true, "Datos obtenidos correctamente.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return (Enumerable.Empty<ListaMoneda>(), false, $"Error: {ex.Message}");
        }
    }
 
}
