using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;

namespace ApiGuardian.Infrastructure.Repositories;

public class UtilsRepository : IUtilsRepository
{
    private readonly DapperContext _context;

    public UtilsRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<ConfiguracionUtils> GetCountContacto(string? search)
    {
        try
        {
            var query = $@"select COUNT(*) FROM administracioncontacto WHERE snombrecompleto LIKE '%{search}%' OR scedulaidentidad LIKE '%{search}%'";
            using var connection = _context.CreateConnection();
            int cantidadContacto = await connection.ExecuteScalarAsync<int>(query);
            query = $@"select 
                            COUNT(*)
                        from administracioncontrato C
                        inner join administracioncontacto P on P.lcontacto_id = C.lcontacto_id
                        inner join administracioncontacto A on A.lcontacto_id = C.lasesor_id
                        inner join administracioncomplejo AC on AC.lcomplejo_id = C.lcomplejo_id
                        inner join administraciontipocontrato ATC on ATC.ltipocontrato_id = C.ltipocontrato_id
                        WHERE P.snombrecompleto LIKE '%{search}%' OR C.snroventa LIKE '%{search}%'";
            int cantidadContrato = await connection.ExecuteScalarAsync<int>(query);
  
            return new ConfiguracionUtils
            {
                cantidadContactos = cantidadContacto,
                cantidadContratos = cantidadContrato
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return new ConfiguracionUtils();
        }

    }
    public async Task<(IEnumerable<AdministracionCiclo> Ciclos, bool Success, string Mensaje)> GetCiclos()
    {
        const string query = @"
            SELECT 
                lciclo_id AS LCicloId,
                UPPER(snombre) AS NombreCiclo,
                dtfechainicio AS FechaInicio,
                dtfechafin AS FechaFin,
                cverenweb AS CVerEnWeb
            FROM administracionciclo
            ORDER BY lciclo_id DESC;
        ";

        try
        {
            using var connection = _context.CreateConnection();

            var ciclos = await connection.QueryAsync<AdministracionCiclo>(query);

            bool success = ciclos != null && ciclos.Any();
            string mensaje = success ? "Ciclos obtenidos correctamente." : "No se encontraron ciclos.";

            return (ciclos ?? Enumerable.Empty<AdministracionCiclo>(), success, mensaje);
        }
        catch (Exception ex)
        {
            return (Enumerable.Empty<AdministracionCiclo>(), false, $"Error al obtener los ciclos: {ex.Message}");
        }
    }

    public async Task<(IEnumerable<AdministracionSemanaCiclo> Semanas, bool Success, string Mensaje)> GetSemanaCiclosAsync(int lCicloId)
    {
        const string query = @"
            SELECT 
                ACS.lciclo_id AS LCicloId,
                ASE.lnombre AS NombreSemana,
                ASE.idsemana AS IdSemana,
                ACS.lsemana_id AS LSemanaId,
                ACS.nombre AS Descripcion,
                ACS.dtfechainicio AS FechaInicio,
                ACS.dtfechafin AS FechaFin
            FROM administracionsemanaciclo AS ACS
            INNER JOIN administracionsemana AS ASE ON ASE.idsemana = ACS.lnrosemana
            INNER JOIN administracionciclo AS AC ON AC.lciclo_id = ACS.lciclo_id
            WHERE ACS.lciclo_id = @lCicloId
            ORDER BY ASE.idsemana;
        ";

        try
        {
            using var connection = _context.CreateConnection();

            var semanas = await connection.QueryAsync<AdministracionSemanaCiclo>(query, new { lCicloId });

            bool success = semanas != null && semanas.Any();
            string mensaje = success
                ? "Semanas del ciclo obtenidas correctamente."
                : "No se encontraron semanas para el ciclo especificado.";

            return (semanas ?? Enumerable.Empty<AdministracionSemanaCiclo>(), success, mensaje);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"rror al obtener semanas del ciclo: {ex.Message}");
            return (Enumerable.Empty<AdministracionSemanaCiclo>(), false, $"Error al obtener semanas del ciclo: {ex.Message}");
        }
    }
    public async Task<(IEnumerable<AdministracionComplejo> Complejo, bool Success, string Mensaje)> GetComplejo()
    {
        const string query = @"
            select lcomplejo_id LComplejoId, scodigo SCodigo, snombre SNombre from administracioncomplejo
        ";

        try
        {
            using var connection = _context.CreateConnection();

            var ciclos = await connection.QueryAsync<AdministracionComplejo>(query);

            bool success = ciclos != null && ciclos.Any();
            string mensaje = success ? "Complejos obtenidos correctamente." : "No se encontraron complejos.";

            return (ciclos ?? Enumerable.Empty<AdministracionComplejo>(), success, mensaje);
        }
        catch (Exception ex)
        {
            return (Enumerable.Empty<AdministracionComplejo>(), false, $"Error al obtener los complejos: {ex.Message}");
        }
    }
    public async Task<(IEnumerable<BasePaisDepartamento> Departamento, bool Success, string Mensaje)> GetDepartamento()
    {
        const string query = @"
             select lPaisDepartamento_id LDepartamentoId, lpais_id LPaisId, sNombre SNombre from basepaisdepartamento where lpais_id = 2;
        ";

        try
        {
            using var connection = _context.CreateConnection();

            var ciclos = await connection.QueryAsync<BasePaisDepartamento>(query);

            bool success = ciclos != null && ciclos.Any();
            string mensaje = success ? "Departamentos obtenidos correctamente." : "No se encontraron Departamentos.";

            return (ciclos ?? Enumerable.Empty<BasePaisDepartamento>(), success, mensaje);
        }
        catch (Exception ex)
        {
            return (Enumerable.Empty<BasePaisDepartamento>(), false, $"Error al obtener los Departamentos: {ex.Message}");
        }
    }
    public async Task<(IEnumerable<AdministracionTipoContrato> TipoContrato, bool Success, string Mensaje)> GetTipoContrato()
    {
         const string query = @"
             select ltipocontrato_id LTipoContratoId, snombre SNombre from administraciontipocontrato
        ";

        try
        {
            using var connection = _context.CreateConnection();

            var ciclos = await connection.QueryAsync<AdministracionTipoContrato>(query);

            bool success = ciclos != null && ciclos.Any();
            string mensaje = success ? "Tipo contrato obtenidos correctamente." : "No se encontraron Tipo contrato.";

            return (ciclos ?? Enumerable.Empty<AdministracionTipoContrato>(), success, mensaje);
        }
        catch (Exception ex)
        {
            return (Enumerable.Empty<AdministracionTipoContrato>(), false, $"Error al obtener los Tipos contratos: {ex.Message}");
        }
    }
    public async Task<(IEnumerable<AdministracionEstadoContrato> EstadoContrato, bool Success, string Mensaje)> GetEstadoContrato()
    {
         const string query = @"
            select lestadocontrato_id LEstadoContratoId, snombre SNombre from administracionestadocontrato
        ";

        try
        {
            using var connection = _context.CreateConnection();

            var ciclos = await connection.QueryAsync<AdministracionEstadoContrato>(query);

            bool success = ciclos != null && ciclos.Any();
            string mensaje = success ? "Estado contratos obtenidos correctamente." : "No se encontraron los estado de los contratos.";

            return (ciclos ?? Enumerable.Empty<AdministracionEstadoContrato>(), success, mensaje);
        }
        catch (Exception ex)
        {
            return (Enumerable.Empty<AdministracionEstadoContrato>(), false, $"Error al obtener los estado de los contratos: {ex.Message}");
        }
    }

}
