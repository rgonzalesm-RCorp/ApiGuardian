using ApiGuardian.Domain.Entities;
using Dapper;

namespace ApiGuardian.Infrastructure.Repositories.Simulacion;

// F5-T2: escritura de Comision Grupo. SQL copiado de AdministracionVentaGrupoRepository.InsertAdministracionVentaGrupo (R2).
public sealed partial class SimulacionRepositorio
{
    private const string SqlMaxVentaGrupo = "SELECT IFNULL(MAX(lventagrupo_id), 0) FROM administracionventagrupo;";

    private const string SqlInsertVentaGrupo = @"INSERT INTO administracionventagrupo
        (
            susuarioadd, dtfechaadd, susuariomod, dtfechamod, lventagrupo_id, dtfechacalculo,
            lciclo_id, lcontacto_id, lgeneracion, lasesor_id, dporcentajecomision, dcomision,
            dventapersonal, cestado, dventapersonalinicial, lcontrato_id, lnrosemana, lsemana_id
        )
        VALUES
        (
            @usuario, NOW(), @usuario, NOW(), @lventagrupo_id, NOW(),
            @lciclo_id, @lcontacto_id, @lgeneracion, @lasesor_id, @dporcentajecomision, @dcomision,
            @dventapersonal, 1, @dventapersonalinicial, @lcontrato_id, @lnrosemana, @lsemana_id
        );";

    public async Task<(bool Success, string Mensaje)> InsertVentaGrupoAsync(string logId, List<ItemVentaGrupo> data)
    {
        const string metodo = nameof(InsertVentaGrupoAsync);
        try
        {
            using var connection = _context.CreateConnection();
            var siguiente = await connection.ExecuteScalarAsync<int>(SqlMaxVentaGrupo);
            foreach (var item in data)
            {
                siguiente++;
                item.lventagrupo_id = siguiente;
            }

            var filas = await connection.ExecuteAsync(SqlInsertVentaGrupo, data);
            return filas > 0
                ? (true, "Comisiones de grupo registradas correctamente.")
                : (false, "No se realizó el guardado de comisiones de grupo.");
        }
        catch (Exception ex)
        {
            _log.Error(logId, NOMBRE_ARCHIVO, metodo, $"Error al insertar comisiones de grupo: {ex.Message}");
            return (false, $"Error al insertar las comisiones de grupo: {ex.Message}");
        }
    }
}
