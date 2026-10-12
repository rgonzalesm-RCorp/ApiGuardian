using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Infrastructure.Persistence;
using Dapper;

namespace ApiGuardian.Infrastructure.Repositories;

public sealed class RedSerRepository : IRedSerRepository
{
    private const string NombreArchivo = "RedSerRepository.cs";
    private readonly DapperContext _context;
    private readonly ILogService _log;

    public RedSerRepository(DapperContext context, ILogService log)
    {
        _context = context;
        _log = log;
    }

    public async Task<(bool Success, string Mensaje, IReadOnlyList<int> Vendedores)> ObtenerVendedoresAsync(
        string logTransaccionId,
        DateTime inicio,
        DateTime fin
    )
    {
        const string metodo = nameof(ObtenerVendedoresAsync);
        const string sql = @"
            SELECT DISTINCT lasesor_id
            FROM administracioncontrato
            WHERE dtfecha >= @Inicio
              AND dtfecha < DATE_ADD(@Fin, INTERVAL 1 DAY)
              AND lasesor_id > 0;";

        try
        {
            using var connection = _context.CreateConnection();
            var vendedores = (await connection.QueryAsync<int>(sql, new { Inicio = inicio.Date, Fin = fin.Date }))
                .Distinct()
                .ToList();
            return (true, "Vendedores de septiembre obtenidos correctamente.", vendedores);
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId, NombreArchivo, metodo, "Error al obtener vendedores.", ex);
            return (false, $"Error al obtener vendedores: {ex.Message}", []);
        }
    }

    public async Task<(bool Success, string Mensaje, IReadOnlyList<RedSerContacto> Contactos)> ObtenerContactosAsync(
        string logTransaccionId
    )
    {
        const string metodo = nameof(ObtenerContactosAsync);
        const string sql = @"
            SELECT lcontacto_id AS LContactoId,
                   lpatrocinante_id AS LPatrocinanteId,
                   snombrecompleto AS SNombreCompleto,
                   scedulaidentidad AS SCedulaIdentidad
            FROM administracioncontacto
            WHERE lcontacto_id > 0;";

        try
        {
            using var connection = _context.CreateConnection();
            var contactos = (await connection.QueryAsync<RedSerContacto>(sql)).ToList();
            return (true, "Contactos obtenidos correctamente.", contactos);
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId, NombreArchivo, metodo, "Error al obtener contactos.", ex);
            return (false, $"Error al obtener contactos: {ex.Message}", []);
        }
    }

    public async Task<(bool Success, string Mensaje)> GuardarAsync(
        string logTransaccionId,
        string usuario,
        DateTime inicio,
        DateTime fin,
        IReadOnlyCollection<RedSerNodo> nodos
    )
    {
        const string metodo = nameof(GuardarAsync);
        if (nodos.Count == 0)
            return (false, "No existen nodos de RedSer para guardar.");

        const string borrar = @"
            DELETE FROM red_ser
            WHERE dt_inicio = @Inicio AND dt_fin = @Fin;";
        const string insertar = @"
            INSERT INTO red_ser
                (lcontacto_id, lpadre_contacto_id, snombrecompleto, scedulaidentidad, nivel, cestructural, dt_inicio, dt_fin, usuario, fecharegistro)
            VALUES
                (@LContactoId, @LPadreContactoId, @SNombreCompleto, @SCedulaIdentidad, @Nivel, @EsEstructural, @Inicio, @Fin, @Usuario, NOW());";

        try
        {
            using var connection = _context.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();
            var parametros = new { Inicio = inicio.Date, Fin = fin.Date };
            await connection.ExecuteAsync(borrar, parametros, transaction);
            await connection.ExecuteAsync(
                insertar,
                nodos.Select(n => new
                {
                    n.LContactoId,
                    n.LPadreContactoId,
                    n.SNombreCompleto,
                    n.SCedulaIdentidad,
                    n.Nivel,
                    EsEstructural = n.EsEstructural ? 1 : 0,
                    Inicio = inicio.Date,
                    Fin = fin.Date,
                    Usuario = usuario,
                }),
                transaction
            );
            transaction.Commit();
            return (true, $"RedSer guardada correctamente. Total de nodos: {nodos.Count}.");
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId, NombreArchivo, metodo, "Error al guardar RedSer.", ex);
            return (false, $"Error al guardar RedSer: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Mensaje, IReadOnlyList<RedSerNodo> Nodos)> ObtenerAsync(
        string logTransaccionId,
        DateTime inicio,
        DateTime fin
    )
    {
        const string metodo = nameof(ObtenerAsync);
        const string sql = @"
            SELECT lcontacto_id AS LContactoId,
                   lpadre_contacto_id AS LPadreContactoId,
                   snombrecompleto AS SNombreCompleto,
                   scedulaidentidad AS SCedulaIdentidad,
                   nivel AS Nivel,
                   cestructural AS EsEstructural
            FROM red_ser
            WHERE dt_inicio = @Inicio AND dt_fin = @Fin
            ORDER BY nivel, snombrecompleto, lcontacto_id;";

        try
        {
            using var connection = _context.CreateConnection();
            var nodos = (await connection.QueryAsync<RedSerNodo>(sql, new { Inicio = inicio.Date, Fin = fin.Date })).ToList();
            return (true, nodos.Count == 0 ? "No existe una RedSer armada para el período indicado." : "RedSer obtenida correctamente.", nodos);
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId, NombreArchivo, metodo, "Error al obtener RedSer.", ex);
            return (false, $"Error al obtener RedSer: {ex.Message}", []);
        }
    }

    public async Task<(bool Success, string Mensaje, DateTime? Inicio, DateTime? Fin, IReadOnlyList<RedSerNodo> Nodos)> ObtenerUltimaAsync(
        string logTransaccionId
    )
    {
        const string metodo = nameof(ObtenerUltimaAsync);
        const string sql = @"
            SELECT lcontacto_id AS LContactoId,
                   lpadre_contacto_id AS LPadreContactoId,
                   snombrecompleto AS SNombreCompleto,
                   scedulaidentidad AS SCedulaIdentidad,
                   nivel AS Nivel,
                   cestructural AS EsEstructural,
                   dt_inicio AS Inicio,
                   dt_fin AS Fin
            FROM red_ser
            WHERE (dt_inicio, dt_fin) = (
                SELECT dt_inicio, dt_fin
                FROM red_ser
                ORDER BY fecharegistro DESC, redser_id DESC
                LIMIT 1
            )
            ORDER BY nivel, snombrecompleto, lcontacto_id;";

        try
        {
            using var connection = _context.CreateConnection();
            var filas = (await connection.QueryAsync<RedSerNodoPeriodo>(sql)).ToList();
            if (filas.Count == 0)
                return (true, "No existe una RedSer armada.", null, null, []);

            return (
                true,
                "RedSer obtenida correctamente.",
                filas[0].Inicio.Date,
                filas[0].Fin.Date,
                filas.Select(f => new RedSerNodo
                {
                    LContactoId = f.LContactoId,
                    LPadreContactoId = f.LPadreContactoId,
                    SNombreCompleto = f.SNombreCompleto,
                    SCedulaIdentidad = f.SCedulaIdentidad,
                    Nivel = f.Nivel,
                    EsEstructural = f.EsEstructural,
                }).ToList()
            );
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId, NombreArchivo, metodo, "Error al obtener la última RedSer.", ex);
            return (false, $"Error al obtener la última RedSer: {ex.Message}", null, null, []);
        }
    }

    private sealed class RedSerNodoPeriodo : RedSerNodo
    {
        public DateTime Inicio { get; set; }
        public DateTime Fin { get; set; }
    }
}
