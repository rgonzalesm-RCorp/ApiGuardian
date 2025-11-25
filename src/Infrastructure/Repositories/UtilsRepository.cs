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
                UPPER(ASE.lnombre) AS NombreSemana,
                ASE.idsemana AS IdSemana,
                ACS.lsemana_id AS LSemanaId,
                UPPER(ACS.nombre) AS Descripcion,
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
            select lcomplejo_id LComplejoId, scodigo SCodigo, UPPER(snombre) SNombre from administracioncomplejo
        ";

        try
        {
            using var connection = _context.CreateConnection();

            var proyecto = await connection.QueryAsync<AdministracionComplejo>(query);

            bool success = proyecto != null && proyecto.Any();
            string mensaje = success ? "Complejos obtenidos correctamente." : "No se encontraron complejos.";

            return (proyecto ?? Enumerable.Empty<AdministracionComplejo>(), success, mensaje);
        }
        catch (Exception ex)
        {
            return (Enumerable.Empty<AdministracionComplejo>(), false, $"Error al obtener los complejos: {ex.Message}");
        }
    }
    public async Task<(IEnumerable<BasePaisDepartamento> Departamento, bool Success, string Mensaje)> GetDepartamento(int lPaisId)
    {
        const string query = @"
             select lPaisDepartamento_id LDepartamentoId, lpais_id LPaisId, UPPER(sNombre) SNombre from basepaisdepartamento where lpais_id = @lPaisId;
        ";

        try
        {
            using var connection = _context.CreateConnection();

            var dpto = await connection.QueryAsync<BasePaisDepartamento>(query, new {lPaisId});

            bool success = dpto != null && dpto.Any();
            string mensaje = success ? "Departamentos obtenidos correctamente." : "No se encontraron Departamentos.";

            return (dpto ?? Enumerable.Empty<BasePaisDepartamento>(), success, mensaje);
        }
        catch (Exception ex)
        {
            return (Enumerable.Empty<BasePaisDepartamento>(), false, $"Error al obtener los Departamentos: {ex.Message}");
        }
    }
    public async Task<(IEnumerable<AdministracionTipoContrato> TipoContrato, bool Success, string Mensaje)> GetTipoContrato()
    {
         const string query = @"
             select ltipocontrato_id LTipoContratoId, UPPER(snombre) SNombre from administraciontipocontrato
        ";

        try
        {
            using var connection = _context.CreateConnection();

            var tipo = await connection.QueryAsync<AdministracionTipoContrato>(query);

            bool success = tipo != null && tipo.Any();
            string mensaje = success ? "Tipo contrato obtenidos correctamente." : "No se encontraron Tipo contrato.";

            return (tipo ?? Enumerable.Empty<AdministracionTipoContrato>(), success, mensaje);
        }
        catch (Exception ex)
        {
            return (Enumerable.Empty<AdministracionTipoContrato>(), false, $"Error al obtener los Tipos contratos: {ex.Message}");
        }
    }
    public async Task<(IEnumerable<AdministracionEstadoContrato> EstadoContrato, bool Success, string Mensaje)> GetEstadoContrato()
    {
         const string query = @"
            select lestadocontrato_id LEstadoContratoId, UPPER(snombre) SNombre from administracionestadocontrato
        ";

        try
        {
            using var connection = _context.CreateConnection();

            var estado = await connection.QueryAsync<AdministracionEstadoContrato>(query);

            bool success = estado != null && estado.Any();
            string mensaje = success ? "Estado contratos obtenidos correctamente." : "No se encontraron los estado de los contratos.";

            return (estado ?? Enumerable.Empty<AdministracionEstadoContrato>(), success, mensaje);
        }
        catch (Exception ex)
        {
            return (Enumerable.Empty<AdministracionEstadoContrato>(), false, $"Error al obtener los estado de los contratos: {ex.Message}");
        }
    }
    public async Task<(IEnumerable<ListaAdministracionNivel> Nivel, bool Success, string Mensaje)> GetNivel()
    {
         const string query = @"
            SELECT lnivel_id LNivelId, UPPER(ssigla) SSigla, UPPER(snombre) SNombre FROM administracionnivel
        ";

        try
        {
            using var connection = _context.CreateConnection();

            var niveles = await connection.QueryAsync<ListaAdministracionNivel>(query);

            bool success = niveles != null && niveles.Any();
            string mensaje = success ? "Niveles obtenidos correctamente." : "No se encontraron los niveles.";

            return (niveles ?? Enumerable.Empty<ListaAdministracionNivel>(), success, mensaje);
        }
        catch (Exception ex)
        {
            return (Enumerable.Empty<ListaAdministracionNivel>(), false, $"Error al obtener los niveles: {ex.Message}");
        }
    }
    public async Task<(IEnumerable<ListaAdministracionPais> Pais, bool Success, string Mensaje)> GetPais()
    {
         const string query = @"
            SELECT lPais_id LPaisId, UPPER(snombre) SNombre FROM basepais
        ";

        try
        {
            using var connection = _context.CreateConnection();

            var niveles = await connection.QueryAsync<ListaAdministracionPais>(query);

            bool success = niveles != null && niveles.Any();
            string mensaje = success ? "Paises obtenidos correctamente." : "No se encontraron los paises.";

            return (niveles ?? Enumerable.Empty<ListaAdministracionPais>(), success, mensaje);
        }
        catch (Exception ex)
        {
            return (Enumerable.Empty<ListaAdministracionPais>(), false, $"Error al obtener los Paises: {ex.Message}");
        }
    }
    public async Task<(IEnumerable<ListaAdministracionTipoBaja> TipoBaja, bool Success, string Mensaje)> GetTipoBaja()
    {
         const string query = @"
            select ltipobaja_id LTipoBajaId, UPPER(snombre) SNombre from administraciontipobaja
        ";

        try
        {
            using var connection = _context.CreateConnection();

            var niveles = await connection.QueryAsync<ListaAdministracionTipoBaja>(query);

            bool success = niveles != null && niveles.Any();
            string mensaje = success ? "Tipo Baja obtenidos correctamente." : "No se encontraron los tipos de baja.";

            return (niveles ?? Enumerable.Empty<ListaAdministracionTipoBaja>(), success, mensaje);
        }
        catch (Exception ex)
        {
            return (Enumerable.Empty<ListaAdministracionTipoBaja>(), false, $"Error al obtener los tipos de baja: {ex.Message}");
        }
    }
    public async Task<(IEnumerable<ListaTipoDescuento> TipoDescuento, bool Success, string Mensaje)> GetTipoDescuento()
    {
         const string query = @"
            select ldescuentociclotipo_id LDescuentoTipoId, UPPER(snombre) SNombre from administraciondescuentociclotipo
        ";

        try
        {
            using var connection = _context.CreateConnection();

            var niveles = await connection.QueryAsync<ListaTipoDescuento>(query);

            bool success = niveles != null && niveles.Any();
            string mensaje = success ? "Tipo Descuento obtenidos correctamente." : "No se encontraron los tipos de descuentos.";

            return (niveles ?? Enumerable.Empty<ListaTipoDescuento>(), success, mensaje);
        }
        catch (Exception ex)
        {
            return (Enumerable.Empty<ListaTipoDescuento>(), false, $"Error al obtener los tipos de descuentos: {ex.Message}");
        }
    }

}
