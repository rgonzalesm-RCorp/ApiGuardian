using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using Newtonsoft.Json;

namespace ApiGuardian.Infrastructure.Repositories
{
    public class AdministracionCicloRepository : IAdministracionCicloRepository
    {
        private readonly DapperContext _context;
        private readonly ILogService _log;
        private readonly string NOMBREARCHIVO = "AdministracionCicloRepository.cs";

        public AdministracionCicloRepository(DapperContext context, ILogService log)
        {
            _context = context;
            _log = log;
        }

        public async Task<(IEnumerable<AdministracionCicloABM> Ciclos, bool Success, string Mensaje)> GetCiclos(string log)
        {
            string metodo = "GetCiclos()";
            string query = @"
                SELECT 
                    lciclo_id AS LCicloId,
                    UPPER(snombre) AS SNombre,
                    dtfechainicio AS DtFechaInicio,
                    dtfechafin AS DtFechaFin,
                    lestado AS LEstado,
                    dtfechacierre AS DtFechaCierre,
                    dtfechaprecierre1 AS DtFechaPreCierre1,
                    dtfechaprecierre2 AS DtFechaPreCierre2,
                    cverenweb AS CVerEnWeb,
                    estadogestor AS EstadoGestor,
                    susuarioadd AS SUsuarioAdd,
                    dtfechaadd AS DtFechaAdd,
                    susuariomod AS SUsuarioMod,
                    dtfechamod AS DtFechaMod
                FROM administracionciclo
                ORDER BY lciclo_id DESC;
            ";

            _log.Info(log, NOMBREARCHIVO, metodo, $"Inicio [script: {query}]");

            try
            {
                using var con = _context.CreateConnection();

                var result = await con.QueryAsync<AdministracionCicloABM>(query);

                bool success = result != null && result.Any();
                string mensaje = success ? "Ciclos obtenidos correctamente." : "No se encontraron ciclos.";
                _log.Info(log, NOMBREARCHIVO, metodo, $"Fin de metodo [mensaje: {mensaje}]");

                return (result ?? Enumerable.Empty<AdministracionCicloABM>(), success, mensaje);
            }
            catch (Exception ex)
            {
                _log.Error(log, NOMBREARCHIVO, metodo, "Fin con error", ex);
                return (Enumerable.Empty<AdministracionCicloABM>(), false, ex.Message);
            }
        }

        public async Task<(IEnumerable<AdministracionCicloABM> Ciclos, bool Success, string Mensaje, int Total)> GetCiclosPagination(string log, int page, int pageSize, string? search)
        {
            string metodo = "GetCiclosPagination()";

            string query = @"
                SELECT 
                    lciclo_id AS LCicloId,
                    snombre AS SNombre,
                    dtfechainicio AS DtFechaInicio,
                    dtfechafin AS DtFechaFin,
                    lestado AS LEstado,
                    dtfechacierre AS DtFechaCierre,
                    dtfechaprecierre1 AS DtFechaPreCierre1,
                    dtfechaprecierre2 AS DtFechaPreCierre2,
                    cverenweb AS CVerEnWeb,
                    estadogestor AS EstadoGestor,
                    susuarioadd AS SUsuarioAdd,
                    dtfechaadd AS DtFechaAdd,
                    susuariomod AS SUsuarioMod,
                    dtfechamod AS DtFechaMod
                FROM administracionciclo
                ORDER BY lciclo_id DESC
                LIMIT @pageSize OFFSET @page;
            ";

            string countQuery = "SELECT COUNT(*) FROM administracionciclo;";

            _log.Info(log, NOMBREARCHIVO, metodo, $"Inicio query: {query}");

            try
            {
                using var con = _context.CreateConnection();

                var ciclos = await con.QueryAsync<AdministracionCicloABM>(query, new { page, pageSize });
                var total = await con.ExecuteScalarAsync<int>(countQuery);

                bool success = ciclos != null && ciclos.Any();
                string mensaje = success ? "Ciclos obtenidos." : "No hay registros.";
                _log.Info(log, NOMBREARCHIVO, metodo, $"Fin de metodo [mensaje: {mensaje}]");

                return (ciclos ?? Enumerable.Empty<AdministracionCicloABM>(), success, mensaje, total);
            }
            catch (Exception ex)
            {
                _log.Error(log, NOMBREARCHIVO, metodo, "Fin con error", ex);
                return (Enumerable.Empty<AdministracionCicloABM>(), false, ex.Message, 0);
            }
        }

        public async Task<(bool Success, string Mensaje)> GuardarCiclo(string log, AdministracionCicloABM ciclo)
        {
            string metodo = "GuardarCiclo()";

            const string nextIdQuery = "SELECT IFNULL(MAX(lciclo_id), 0) + 1 FROM administracionciclo;";

            string query = @"
                INSERT INTO administracionciclo
                (lciclo_id, snombre, dtfechainicio, dtfechafin, lestado, 
                dtfechacierre, dtfechaprecierre1, dtfechaprecierre2, 
                cverenweb, estadogestor, susuarioadd, dtfechaadd, susuariomod, dtfechamod)
                VALUES
                (@nextId, @SNombre, @DtFechaInicio, @DtFechaFin, @LEstado,
                @DtFechaCierre, @DtFechaPreCierre1, @DtFechaPreCierre2, 
                @CVerEnWeb, @EstadoGestor, @Usuario, NOW(), @Usuario, NOW());
            ";
            _log.Info(log, NOMBREARCHIVO, metodo, $"Inicio query: {query}");
            _log.Info(log, NOMBREARCHIVO, metodo, $"nextIdQuery : {nextIdQuery}");

            try
            {
                using var con = _context.CreateConnection();

                int nextId = await con.ExecuteScalarAsync<int>(nextIdQuery);

                var rows = await con.ExecuteAsync(query, new
                {
                    nextId,
                    ciclo.SNombre,
                    ciclo.DtFechaInicio,
                    ciclo.DtFechaFin,
                    ciclo.LEstado,
                    ciclo.DtFechaCierre,
                    ciclo.DtFechaPreCierre1,
                    ciclo.DtFechaPreCierre2,
                    ciclo.CVerEnWeb,
                    ciclo.EstadoGestor,
                    ciclo.Usuario
                });
                _log.Info(log, NOMBREARCHIVO, metodo, $"Fin de metodo [mensaje: { (rows > 0 ? "Registro creado correctamente." : "No se insertó el registro.") }]");

                return (rows > 0, rows > 0 ? "Registro creado correctamente." : "No se insertó el registro.");
            }
            catch (Exception ex)
            {
                _log.Error(log, NOMBREARCHIVO, metodo, "Fin con error", ex);
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, string Mensaje)> ModificarCiclo(string log, AdministracionCicloABM ciclo)
        {
            string metodo = "ModificarCiclo()";

            string query = @"
                UPDATE administracionciclo SET
                    snombre = @SNombre,
                    dtfechainicio = @DtFechaInicio,
                    dtfechafin = @DtFechaFin,
                    lestado = @LEstado,
                    dtfechacierre = @DtFechaCierre,
                    dtfechaprecierre1 = @DtFechaPreCierre1,
                    dtfechaprecierre2 = @DtFechaPreCierre2,
                    cverenweb = @CVerEnWeb,
                    estadogestor = @EstadoGestor,
                    susuariomod = @Usuario,
                    dtfechamod = NOW()
                WHERE lciclo_id = @LCicloId;
            ";
            _log.Info(log, NOMBREARCHIVO, metodo, $"Inicio query: {query}");

            try
            {
                using var con = _context.CreateConnection();

                var rows = await con.ExecuteAsync(query, ciclo);
            _log.Info(log, NOMBREARCHIVO, metodo, $"Fin metodo: {(rows > 0 ? "Registro modificado." : "No se encontró el ciclo.")}");

                return (rows > 0, rows > 0 ? "Registro modificado." : "No se encontró el ciclo.");
            }
            catch (Exception ex)
            {
                _log.Error(log, NOMBREARCHIVO, metodo, "Fin con error", ex);
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, string Mensaje)> EliminarCiclo(string log, int LCicloId)
        {
            string metodo = "EliminarCiclo()";

            const string query = @"
                DELETE FROM administracionciclo 
                WHERE lciclo_id = @LCicloId;
            ";
            _log.Info(log, NOMBREARCHIVO, metodo, $"Inicio query: {query}");

            try
            {
                using var con = _context.CreateConnection();

                var rows = await con.ExecuteAsync(query, new { LCicloId });
                _log.Info(log, NOMBREARCHIVO, metodo, $"Inicio query: {(rows > 0 ? "Registro eliminado." : "No se eliminó el registro.")}");

                return (rows > 0, rows > 0 ? "Registro eliminado." : "No se eliminó el registro.");
            }
            catch (Exception ex)
            {
                _log.Error(log, NOMBREARCHIVO, metodo, "Fin con error", ex);
                return (false, ex.Message);
            }
        }
    }
}
