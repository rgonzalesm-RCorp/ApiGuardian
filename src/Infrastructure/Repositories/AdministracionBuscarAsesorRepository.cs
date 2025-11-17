using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;

namespace ApiGuardian.Infrastructure.Repositories;

public class AdministracionBuscarAsesorRepository : IAdministracionBuscarAsesorRepository
{
    private readonly DapperContext _context;

    public AdministracionBuscarAsesorRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<ListaBuscarAsesor> DataFijos, IEnumerable<ListaBuscarAsesor> DataActivos, bool Success, string Mensaje)> GetAsesoreSieteNiveles(int lContactoId)
    {
        try
        {
            string queryFijos = @"CALL sp_GetPadresHasta7Fijos(@lContactoId);";
            string queryActivos = @"CALL sp_GetPadresHasta7Activos(@lContactoId);";

            using var connection = _context.CreateConnection();

            var resultFijos = await connection.QueryAsync<ListaBuscarAsesor>(queryFijos, new { lContactoId });
            var resultActivos = await connection.QueryAsync<ListaBuscarAsesor>(queryActivos, new { lContactoId });

            return (resultFijos, resultActivos, true, "Datos obtenidos correctamente.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return (Enumerable.Empty<ListaBuscarAsesor>(), Enumerable.Empty<ListaBuscarAsesor>(), false, $"Error: {ex.Message}");
        }
    }

 
}
