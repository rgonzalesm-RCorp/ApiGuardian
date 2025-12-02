using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using System.Text.Json;

namespace ApiGuardian.Infrastructure.Repositories;

public class AdministracionSemanaCicloRepository : IAdministracionSemanaCicloRepository
{
    private readonly DapperContext _context;
    private readonly ILogService _log;
    private readonly string NOMBREARCHIVO = "AdministracionSemanaCicloRepository.cs";

    public AdministracionSemanaCicloRepository(DapperContext context, ILogService log)
    {
        _context = context;
        _log = log;
    }

    public async Task<(IEnumerable<AdministracionSemanaCicloList> Semanas, bool Success, string Mensaje)>
        GetSemanaCiclo(string LogTransaccionId)
    {
        string metodo = "GetSemanaCiclo()";

        const string query = @"
             SELECT
                SCA.lsemana_id AS LSemanaId,
                UPPER(SCA.nombre) AS Nombre,
                SCA.lnrosemana AS LNroSemana,
                SCA.lciclo_id AS LCicloId,
                SCA.dtfechainicio AS DtFechaInicio,
                SCA.dtfechafin AS DtFechaFin,
                SCA.lvalidacion AS LValidacion,
                UPPER(SA.lnombre) AS Semana,
                UPPER(AC.snombre) AS Ciclo
            FROM administracionsemanaciclo SCA
            INNER JOIN administracionsemana SA ON SA.idsemana = SCA.lnrosemana
            INNER JOIN administracionciclo AC ON AC.lciclo_id = SCA.lciclo_id
            ORDER BY lsemana_id DESC;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo, $"Inicio consulta. Script={query}");

        try
        {
            using var con = _context.CreateConnection();
            var lista = await con.QueryAsync<AdministracionSemanaCicloList>(query);

            bool success = lista.Any();
            return (lista, success, success ? "Datos obtenidos." : "No hay registros.");
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, metodo, "Error obteniendo datos", ex);
            return (Enumerable.Empty<AdministracionSemanaCicloList>(), false, ex.Message);
        }
    }

    public async Task<(IEnumerable<AdministracionSemanaCicloList> Semanas, bool Success, string Mensaje, int Total)>
        GetSemanaCicloPagination(string LogTransaccionId, int page, int pageSize, string? search)
    {
        string metodo = "GetSemanaCicloPagination()";

        const string query = @"
            SELECT
                SCA.lsemana_id AS LSemanaId,
                UPPER(SCA.nombre) AS Nombre,
                SCA.lnrosemana AS LNroSemana,
                SCA.lciclo_id AS LCicloId,
                SCA.dtfechainicio AS DtFechaInicio,
                SCA.dtfechafin AS DtFechaFin,
                SCA.lvalidacion AS LValidacion,
                UPPER(SA.lnombre) AS Semana,
                UPPER(AC.snombre) AS Ciclo
            FROM administracionsemanaciclo SCA
            INNER JOIN administracionsemana SA ON SA.idsemana = SCA.lnrosemana
            INNER JOIN administracionciclo AC ON AC.lciclo_id = SCA.lciclo_id
            ORDER BY SCA.lsemana_id DESC
            LIMIT @pageSize OFFSET @page;
        ";

        const string countQuery = "SELECT COUNT(*) FROM administracionsemanaciclo;";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo,
            $"Inicio paginación. Page={page}, PageSize={pageSize}, Search={search}");

        try
        {
            using var con = _context.CreateConnection();

            var lista = await con.QueryAsync<AdministracionSemanaCicloList>(query, new { page, pageSize });
            int total = await con.ExecuteScalarAsync<int>(countQuery);

            bool success = lista.Any();
            return (lista, success, success ? "Datos obtenidos." : "Sin registros.", total);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, metodo, "Error paginando", ex);
            return (Enumerable.Empty<AdministracionSemanaCicloList>(), false, ex.Message, 0);
        }
    }

    public async Task<(bool Success, string Mensaje)> GuardarSemanaCiclo(string LogTransaccionId, AdministracionSemanaCicloABM data)
    {
        string metodo = "GuardarSemanaCiclo()";

        const string nextIdQuery = "SELECT IFNULL(MAX(lsemana_id),0)+1 FROM administracionsemanaciclo;";

        const string insertQuery = @"
            INSERT INTO administracionsemanaciclo
            (lsemana_id, nombre, lnrosemana, lciclo_id, dtfechainicio, dtfechafin, lvalidacion,
             susuarioadd, dtfechaadd, susuariomod, dtfechamod)
            VALUES
            (@nextId, @Nombre, @LNroSemana, @LCicloId, @DtFechaInicio, @DtFechaFin, @LValidacion,
             @Usuario, NOW(), @Usuario, NOW());
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo,
            $"Inicio inserción. Data={JsonSerializer.Serialize(data)}");

        try
        {
            using var con = _context.CreateConnection();

            int nextId = await con.ExecuteScalarAsync<int>(nextIdQuery);

            int result = await con.ExecuteAsync(insertQuery, new
            {
                nextId,
                data.Nombre,
                data.LNroSemana,
                data.LCicloId,
                data.DtFechaInicio,
                data.DtFechaFin,
                data.LValidacion,
                data.Usuario
            });

            bool success = result > 0;
            return (success, success ? "Registro insertado." : "No se pudo insertar.");
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, metodo, "Error insertando", ex);
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Mensaje)> ModificarSemanaCiclo(string LogTransaccionId, AdministracionSemanaCicloABM data)
    {
        string metodo = "ModificarSemanaCiclo()";

        const string query = @"
            UPDATE administracionsemanaciclo
            SET nombre=@Nombre,
                lnrosemana=@LNroSemana,
                lciclo_id=@LCicloId,
                dtfechainicio=@DtFechaInicio,
                dtfechafin=@DtFechaFin,
                lvalidacion=@LValidacion,
                susuariomod=@Usuario,
                dtfechamod=NOW()
            WHERE lsemana_id=@LSemanaId;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo,
            $"Inicio actualización. Data={JsonSerializer.Serialize(data)}");

        try
        {
            using var con = _context.CreateConnection();
            int result = await con.ExecuteAsync(query, data);

            bool success = result > 0;
            return (success, success ? "Registro actualizado." : "No se pudo actualizar.");
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, metodo, "Error actualizando", ex);
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Mensaje)> EliminarSemanaCiclo(string LogTransaccionId, int LSemanaId)
    {
        string metodo = "EliminarSemanaCiclo()";

        const string query = @"DELETE FROM administracionsemanaciclo WHERE lsemana_id=@LSemanaId;";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, metodo,
            $"Inicio eliminación. ID={LSemanaId}");

        try
        {
            using var con = _context.CreateConnection();
            int result = await con.ExecuteAsync(query, new { LSemanaId });

            bool success = result > 0;
            return (success, success ? "Eliminado correctamente." : "No se pudo eliminar.");
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, metodo, "Error eliminando", ex);
            return (false, ex.Message);
        }
    }
}
