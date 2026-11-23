using System.Text;
using ApiGuardian.Domain.DTO;
using Dapper;

namespace ApiGuardian.Infrastructure.Repositories.Simulacion;

// Armado del árbol Monte Sion de la simulación. Basado en MonteSionArbolRepository.ReemplazarArbolAsync (R2).
// Diferencia: no borra y vuelve a insertar todo el ciclo (~110 mil filas, lento en el servidor actual).
// Compara con lo guardado y solo escribe la diferencia:
//  - DELETE de los contactos que ya no están en el árbol.
//  - INSERT ... ON DUPLICATE KEY UPDATE (clave única lciclo_id + lcontacto_id) de los nuevos y de los que cambiaron
//    de padre, nivel, ruta o nombre. Las filas sin cambios no se tocan (conservan usuario y fecharegistro).
// El proceso real y el endpoint api/MonteSionArbol/armar siguen usando ReemplazarArbolAsync.
public sealed partial class SimulacionRepositorio
{
    private const int TamanioLoteArbol = 500;

    private const string SqlArbolActual = @"
        SELECT lcontacto_id AS LContactoId,
               lpadre_contacto_id AS LPadreContactoId,
               snombrecompleto AS SNombreCompleto,
               nivel AS Nivel,
               ruta AS Ruta
        FROM montesion_arbol
        WHERE lciclo_id = @LCicloId;";

    private const string SqlArbolBorrar = "DELETE FROM montesion_arbol WHERE lciclo_id = @LCicloId AND lcontacto_id IN @Ids;";

    public async Task<(bool Success, string Mensaje, int Total)> SincronizarArbolAsync(
        string logId,
        int cicloId,
        string usuario,
        IReadOnlyList<MonteSionArbolNodo> nodos)
    {
        const string metodo = nameof(SincronizarArbolAsync);
        try
        {
            using var connection = _context.CreateConnection();
            connection.Open();

            var actuales = (await connection.QueryAsync<MonteSionArbolNodo>(SqlArbolActual, new { LCicloId = cicloId }))
                .GroupBy(x => x.LContactoId)
                .ToDictionary(g => g.Key, g => g.First());

            var nuevosIds = new HashSet<int>(nodos.Select(x => x.LContactoId));
            var borrar = actuales.Keys.Where(id => !nuevosIds.Contains(id)).ToList();
            var escribir = nodos
                .Where(n => !actuales.TryGetValue(n.LContactoId, out var a) || CambioNodo(a, n))
                .ToList();
            int insertados = escribir.Count(n => !actuales.ContainsKey(n.LContactoId));
            int actualizados = escribir.Count - insertados;

            if (borrar.Count == 0 && escribir.Count == 0)
                return (true, $"Árbol sin cambios: {nodos.Count} nodos.", nodos.Count);

            using var transaction = connection.BeginTransaction();

            for (int i = 0; i < borrar.Count; i += TamanioLoteArbol)
            {
                var lote = borrar.Skip(i).Take(TamanioLoteArbol).ToList();
                await connection.ExecuteAsync(SqlArbolBorrar, new { LCicloId = cicloId, Ids = lote }, transaction);
            }

            for (int i = 0; i < escribir.Count; i += TamanioLoteArbol)
            {
                var lote = escribir.Skip(i).Take(TamanioLoteArbol).ToList();
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

                sql.Append(@"
                    ON DUPLICATE KEY UPDATE
                        lpadre_contacto_id = VALUES(lpadre_contacto_id),
                        nivel = VALUES(nivel),
                        ruta = VALUES(ruta),
                        snombrecompleto = VALUES(snombrecompleto),
                        usuario = VALUES(usuario),
                        fecharegistro = VALUES(fecharegistro);");
                await connection.ExecuteAsync(sql.ToString(), parametros, transaction);
            }

            transaction.Commit();
            return (true,
                $"Árbol sincronizado: {nodos.Count} nodos ({insertados} nuevos, {actualizados} actualizados, {borrar.Count} borrados).",
                nodos.Count);
        }
        catch (Exception ex)
        {
            _log.Error(logId, NOMBRE_ARCHIVO, metodo, $"Error al sincronizar el árbol del ciclo {cicloId}: {ex.Message}");
            return (false, $"Error al armar el árbol: {ex.Message}", 0);
        }
    }

    private static bool CambioNodo(MonteSionArbolNodo actual, MonteSionArbolNodo nuevo) =>
        actual.LPadreContactoId != nuevo.LPadreContactoId
        || actual.Nivel != nuevo.Nivel
        || !string.Equals(actual.Ruta, nuevo.Ruta, StringComparison.Ordinal)
        || !string.Equals(actual.SNombreCompleto ?? "", nuevo.SNombreCompleto ?? "", StringComparison.Ordinal);
}
