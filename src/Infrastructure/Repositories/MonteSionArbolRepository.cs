using System.Text;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.DTO;
using ApiGuardian.Infrastructure.Persistence;
using Dapper;

namespace ApiGuardian.Infrastructure.Repositories;

public sealed class MonteSionArbolRepository : IMonteSionArbolRepository
{
    private const string NombreArchivo = "MonteSionArbolRepository.cs";
    private const int TamanioLote = 500;
    private readonly DapperContext _context;
    private readonly ILogService _log;

    public MonteSionArbolRepository(DapperContext context, ILogService log)
    {
        _context = context;
        _log = log;
    }

    public async Task<(bool Success, string Mensaje, IReadOnlyList<MonteSionArbolOrigen> Contactos)> ObtenerContactosAsync(string logTransaccionId)
    {
        const string metodo = nameof(ObtenerContactosAsync);
        const string sql = @"
            SELECT lcontacto_id AS LContactoId,
                   lpatrocinante_id AS LPatrocinanteId,
                   snombrecompleto AS SNombreCompleto
            FROM administracioncontacto
            WHERE lcontacto_id > 0;";

        try
        {
            using var connection = _context.CreateConnection();
            var contactos = (await connection.QueryAsync<MonteSionArbolOrigen>(sql)).ToList();
            return (true, "Contactos obtenidos correctamente.", contactos);
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId, NombreArchivo, metodo, "Error al obtener contactos para el árbol.", ex);
            return (false, $"Error al obtener contactos: {ex.Message}", []);
        }
    }

    public async Task<(bool Success, string Mensaje, int Total)> ReemplazarArbolAsync(
        string logTransaccionId,
        int cicloId,
        string usuario,
        IReadOnlyList<MonteSionArbolNodo> nodos
    )
    {
        const string metodo = nameof(ReemplazarArbolAsync);
        const string borrar = "DELETE FROM montesion_arbol WHERE lciclo_id = @LCicloId;";

        try
        {
            using var connection = _context.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            await connection.ExecuteAsync(borrar, new { LCicloId = cicloId }, transaction);

            for (int i = 0; i < nodos.Count; i += TamanioLote)
            {
                var lote = nodos.Skip(i).Take(TamanioLote).ToList();
                var sql = new StringBuilder(@"
                    INSERT INTO montesion_arbol
                        (lciclo_id, lcontacto_id, lpadre_contacto_id, nivel, ruta, snombrecompleto, usuario, fecharegistro)
                    VALUES ");
                var parametros = new DynamicParameters();
                parametros.Add("LCicloId", cicloId);
                parametros.Add("Usuario", usuario);

                for (int j = 0; j < lote.Count; j++)
                {
                    if (j > 0) sql.Append(',');
                    sql.Append($"(@LCicloId, @LContactoId{j}, @LPadreContactoId{j}, @Nivel{j}, @Ruta{j}, @SNombreCompleto{j}, @Usuario, NOW())");
                    parametros.Add($"LContactoId{j}", lote[j].LContactoId);
                    parametros.Add($"LPadreContactoId{j}", lote[j].LPadreContactoId);
                    parametros.Add($"Nivel{j}", lote[j].Nivel);
                    parametros.Add($"Ruta{j}", lote[j].Ruta);
                    parametros.Add($"SNombreCompleto{j}", lote[j].SNombreCompleto);
                }

                sql.Append(';');
                await connection.ExecuteAsync(sql.ToString(), parametros, transaction);
            }

            transaction.Commit();
            return (true, "Árbol armado correctamente.", nodos.Count);
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId, NombreArchivo, metodo, "Error al reemplazar el árbol del ciclo.", ex);
            return (false, $"Error al armar el árbol: {ex.Message}", 0);
        }
    }

    public async Task<(bool Success, string Mensaje, MonteSionArbolNodo? Persona)> ObtenerPersonaAsync(
        string logTransaccionId,
        int cicloId,
        int contactoId
    )
    {
        const string metodo = nameof(ObtenerPersonaAsync);
        const string sql = @"
            SELECT lcontacto_id AS LContactoId,
                   lpadre_contacto_id AS LPadreContactoId,
                   snombrecompleto AS SNombreCompleto,
                   nivel AS Nivel,
                   ruta AS Ruta,
                   (SELECT COUNT(*) FROM montesion_arbol hijo
                    WHERE hijo.lciclo_id = montesion_arbol.lciclo_id
                      AND hijo.lpadre_contacto_id = montesion_arbol.lcontacto_id) AS CantidadHijos
            FROM montesion_arbol
            WHERE lciclo_id = @LCicloId AND lcontacto_id = @LContactoId;";

        try
        {
            using var connection = _context.CreateConnection();
            var persona = await connection.QueryFirstOrDefaultAsync<MonteSionArbolNodo>(
                sql,
                new { LCicloId = cicloId, LContactoId = contactoId }
            );
            return (true, persona is null ? "La persona no existe en el árbol de este ciclo." : "Persona obtenida.", persona);
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId, NombreArchivo, metodo, "Error al obtener la persona del árbol.", ex);
            return (false, $"Error al obtener la persona: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Mensaje, IReadOnlyList<MonteSionArbolNodo> Nodos)> ObtenerPorIdsAsync(
        string logTransaccionId,
        int cicloId,
        IReadOnlyList<int> contactoIds
    )
    {
        const string metodo = nameof(ObtenerPorIdsAsync);
        if (contactoIds.Count == 0)
            return (true, "Sin contactos que consultar.", []);

        const string sql = @"
            SELECT lcontacto_id AS LContactoId,
                   lpadre_contacto_id AS LPadreContactoId,
                   snombrecompleto AS SNombreCompleto,
                   nivel AS Nivel,
                   ruta AS Ruta
            FROM montesion_arbol
            WHERE lciclo_id = @LCicloId AND lcontacto_id IN @ContactoIds
            ORDER BY nivel;";

        try
        {
            using var connection = _context.CreateConnection();
            var nodos = (await connection.QueryAsync<MonteSionArbolNodo>(
                sql,
                new { LCicloId = cicloId, ContactoIds = contactoIds }
            )).ToList();
            return (true, "Contactos obtenidos correctamente.", nodos);
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId, NombreArchivo, metodo, "Error al obtener contactos por id.", ex);
            return (false, $"Error al obtener contactos: {ex.Message}", []);
        }
    }

    public async Task<(bool Success, string Mensaje, IReadOnlyList<MonteSionArbolNodo> Nodos)> ObtenerHijosAsync(
        string logTransaccionId,
        int cicloId,
        int padreId
    )
    {
        const string metodo = nameof(ObtenerHijosAsync);
        const string sql = @"
            SELECT lcontacto_id AS LContactoId,
                   lpadre_contacto_id AS LPadreContactoId,
                   snombrecompleto AS SNombreCompleto,
                   nivel AS Nivel,
                   ruta AS Ruta,
                   (SELECT COUNT(*) FROM montesion_arbol hijo
                    WHERE hijo.lciclo_id = montesion_arbol.lciclo_id
                      AND hijo.lpadre_contacto_id = montesion_arbol.lcontacto_id) AS CantidadHijos
            FROM montesion_arbol
            WHERE lciclo_id = @LCicloId AND lpadre_contacto_id = @PadreId
            ORDER BY snombrecompleto;";

        try
        {
            using var connection = _context.CreateConnection();
            var hijos = (await connection.QueryAsync<MonteSionArbolNodo>(
                sql,
                new { LCicloId = cicloId, PadreId = padreId }
            )).ToList();
            return (true, "Hijos obtenidos correctamente.", hijos);
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId, NombreArchivo, metodo, "Error al obtener los hijos del árbol.", ex);
            return (false, $"Error al obtener los hijos: {ex.Message}", []);
        }
    }

    public async Task<(bool Success, string Mensaje, IReadOnlyList<MonteSionArbolNodo> Nodos)> ObtenerRaicesAsync(
        string logTransaccionId,
        int cicloId
    )
    {
        const string metodo = nameof(ObtenerRaicesAsync);
        const string sql = @"
            SELECT lcontacto_id AS LContactoId,
                   lpadre_contacto_id AS LPadreContactoId,
                   snombrecompleto AS SNombreCompleto,
                   nivel AS Nivel,
                   ruta AS Ruta,
                   (SELECT COUNT(*) FROM montesion_arbol hijo
                    WHERE hijo.lciclo_id = montesion_arbol.lciclo_id
                      AND hijo.lpadre_contacto_id = montesion_arbol.lcontacto_id) AS CantidadHijos
            FROM montesion_arbol
            WHERE lciclo_id = @LCicloId AND lpadre_contacto_id IS NULL
            ORDER BY snombrecompleto;";

        try
        {
            using var connection = _context.CreateConnection();
            var raices = (await connection.QueryAsync<MonteSionArbolNodo>(sql, new { LCicloId = cicloId })).ToList();
            return (true, "Raíces obtenidas correctamente.", raices);
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId, NombreArchivo, metodo, "Error al obtener las raíces del árbol.", ex);
            return (false, $"Error al obtener las raíces: {ex.Message}", []);
        }
    }

    public async Task<(bool Success, string Mensaje, IReadOnlyList<MonteSionArbolNodo> Nodos)> BuscarAsync(
        string logTransaccionId,
        int cicloId,
        string texto
    )
    {
        const string metodo = nameof(BuscarAsync);
        const string sql = @"
            SELECT lcontacto_id AS LContactoId,
                   lpadre_contacto_id AS LPadreContactoId,
                   snombrecompleto AS SNombreCompleto,
                   nivel AS Nivel,
                   ruta AS Ruta,
                   (SELECT COUNT(*) FROM montesion_arbol hijo
                    WHERE hijo.lciclo_id = montesion_arbol.lciclo_id
                      AND hijo.lpadre_contacto_id = montesion_arbol.lcontacto_id) AS CantidadHijos
            FROM montesion_arbol
            WHERE lciclo_id = @LCicloId AND snombrecompleto LIKE @Texto
            ORDER BY snombrecompleto
            LIMIT 20;";

        try
        {
            using var connection = _context.CreateConnection();
            var nodos = (await connection.QueryAsync<MonteSionArbolNodo>(
                sql,
                new { LCicloId = cicloId, Texto = $"%{texto}%" }
            )).ToList();
            return (true, "Búsqueda realizada correctamente.", nodos);
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId, NombreArchivo, metodo, "Error al buscar personas en el árbol.", ex);
            return (false, $"Error al buscar personas: {ex.Message}", []);
        }
    }
}
