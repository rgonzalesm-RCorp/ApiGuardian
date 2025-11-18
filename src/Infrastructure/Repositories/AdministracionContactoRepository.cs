using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;

namespace ApiGuardian.Infrastructure.Repositories;

public class AdministracionContactoRepository : IAdministracionContactoRepository
{
    private readonly DapperContext _context;

    public AdministracionContactoRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AdministracionContacto>> GetAllAdministracionContacto(int page, int pageSize, string? search)
    {
        try
        {
            var query = $@"SELECT  
                            A.lcontacto_id lContactoId,
                            A.snombrecompleto,
                            A.scedulaidentidad,
                            A.lnit AS lNit,
                            A.stelefonofijo,
                            A.stelefonomovil,
                            A.stelefonooficina,
                            A.scorreoelectronico,
                            A.sciudad,
                            A.lpatrocinante_id lPatrocinanteId,
                            P.snombrecompleto AS patrocinanteNombre,
                            A.lnivel_id nNivelId,
                            N.snombre AS nivelNombre,
                            A.lpais_id lPaisId,
                            BP.snombre AS paisNombre,
                            IFNULL(AB.lbanco_id, 0) LBancoId,
                            IFNULL(AB.snombre, '') Banco,
                            A.sotro Comentario,
                            IFNULL( AM.snombre, '') Moneda
                        FROM administracioncontacto A
                        INNER JOIN administracioncontacto P ON P.lcontacto_id = A.lpatrocinante_id 
                        INNER JOIN administracionnivel N ON N.lnivel_id = A.lnivel_id
                        INNER JOIN basepais BP ON BP.lpais_id = A.lpais_id
                        LEFT JOIN administracionbanco AB ON AB.lbanco_id = A.lbanco_id
                        LEFT JOIN administracionmoneda AM on AM.lmoneda_id = AB.lmoneda_id
                        WHERE A.snombrecompleto LIKE '%{ search }%' OR A.scedulaidentidad LIKE '%{search}%'
                        LIMIT {pageSize} OFFSET {page}; ";
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<AdministracionContacto>(query);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return Enumerable.Empty<AdministracionContacto>();
        }
        
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        var query = "SELECT Id, Name, Price FROM Products WHERE Id = @Id";
        using var connection = _context.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Product>(query, new { Id = id });
    }

    public async Task<int> AddAsync(Product product)
    {
        var query = "INSERT INTO Products (Name, Price) VALUES (@Name, @Price)";
        using var connection = _context.CreateConnection();
        return await connection.ExecuteAsync(query, product);
    }

    public async Task<int> UpdateAsync(Product product)
    {
        var query = "UPDATE Products SET Name = @Name, Price = @Price WHERE Id = @Id";
        using var connection = _context.CreateConnection();
        return await connection.ExecuteAsync(query, product);
    }

    public async Task<int> DeleteAsync(int id)
    {
        var query = "DELETE FROM Products WHERE Id = @Id";
        using var connection = _context.CreateConnection();
        return await connection.ExecuteAsync(query, new { Id = id });
    }
}
