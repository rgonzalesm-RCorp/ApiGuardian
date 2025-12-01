using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using Newtonsoft.Json;

namespace ApiGuardian.Infrastructure.Repositories;

public class UtilsRepository : IUtilsRepository
{
    private readonly DapperContext _context;
    private readonly ILogService _log;
    private string NOMBREARCHIVO = "UtilsRepository.CS";
    public UtilsRepository(DapperContext context, ILogService log)
    {
        _context = context;
        _log = log;
    }
    public async Task<(IEnumerable<AdministracionCiclo> Ciclos, bool Success, string Mensaje)> GetCiclos(string LogTransaccionId)
    {
        string NombreMetodo = "GetCiclos()";

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
        _log.Info(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var ciclos = await connection.QueryAsync<AdministracionCiclo>(query);

            bool success = ciclos != null && ciclos.Any();
            string mensaje = success ? "Ciclos obtenidos correctamente." : "No se encontraron ciclos.";
        
            _log.Info(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, $"Inicio de metodo [mensaje: {mensaje}, ciclos:{JsonConvert.SerializeObject(ciclos, Formatting.Indented)}]");

            return (ciclos ?? Enumerable.Empty<AdministracionCiclo>(), success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, "Fin de metodo", ex);
            return (Enumerable.Empty<AdministracionCiclo>(), false, $"Error al obtener los ciclos: {ex.Message}");
        }
    }
    public async Task<(IEnumerable<AdministracionSemanaCiclo> Semanas, bool Success, string Mensaje)> GetSemanaCiclosAsync(string LogTransaccionId, int lCicloId)
    {
        string NombreMetodo = "GetSemanaCiclosAsync()";

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
        _log.Info(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var semanas = await connection.QueryAsync<AdministracionSemanaCiclo>(query, new { lCicloId });

            bool success = semanas != null && semanas.Any();
            string mensaje = success
                ? "Semanas del ciclo obtenidas correctamente."
                : "No se encontraron semanas para el ciclo especificado.";
            _log.Info(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, $"Inicio de metodo [mensaje: {mensaje}, ciclos:{JsonConvert.SerializeObject(semanas, Formatting.Indented)}]");

            return (semanas ?? Enumerable.Empty<AdministracionSemanaCiclo>(), success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, "Fin de metodo", ex);
            return (Enumerable.Empty<AdministracionSemanaCiclo>(), false, $"Error al obtener semanas del ciclo: {ex.Message}");
        }
    }
    public async Task<(IEnumerable<AdministracionComplejo> Complejo, bool Success, string Mensaje)> GetComplejo(string LogTransaccionId)
    {
        string nombreMetodo = "GetComplejo()";

        const string query = @"
            SELECT 
                lcomplejo_id AS LComplejoId, 
                scodigo AS SCodigo, 
                UPPER(snombre) AS SNombre 
            FROM administracioncomplejo
            ORDER BY lcomplejo_id DESC;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var complejos = await connection.QueryAsync<AdministracionComplejo>(query);

            bool success = complejos != null && complejos.Any();
            string mensaje = success ? "Complejos obtenidos correctamente." : "No se encontraron complejos.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, complejos:{JsonConvert.SerializeObject(complejos, Formatting.Indented)}]");

            return (complejos ?? Enumerable.Empty<AdministracionComplejo>(), success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (Enumerable.Empty<AdministracionComplejo>(), false, $"Error al obtener los complejos: {ex.Message}");
        }
    }
    public async Task<(IEnumerable<BasePaisDepartamento> Departamento, bool Success, string Mensaje)> GetDepartamento(string LogTransaccionId, int lPaisId)
    {
        string nombreMetodo = "GetDepartamento()";

        const string query = @"
            SELECT 
                lpaisdepartamento_id AS LDepartamentoId, 
                lpais_id AS LPaisId, 
                UPPER(snombre) AS SNombre 
            FROM basepaisdepartamento 
            WHERE lpais_id = @lPaisId
            ORDER BY lpaisdepartamento_id DESC;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var departamentos = await connection.QueryAsync<BasePaisDepartamento>(query, new { lPaisId });

            bool success = departamentos != null && departamentos.Any();
            string mensaje = success ? "Departamentos obtenidos correctamente." : "No se encontraron departamentos.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, departamentos:{JsonConvert.SerializeObject(departamentos, Formatting.Indented)}]");

            return (departamentos ?? Enumerable.Empty<BasePaisDepartamento>(), success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (Enumerable.Empty<BasePaisDepartamento>(), false, $"Error al obtener los departamentos: {ex.Message}");
        }
    }
    public async Task<(IEnumerable<AdministracionTipoContrato> TipoContrato, bool Success, string Mensaje)> GetTipoContrato(string LogTransaccionId)
    {
         string nombreMetodo = "GetTipoContrato()";

        const string query = @"
            SELECT 
                ltipocontrato_id AS LTipoContratoId, 
                UPPER(snombre) AS SNombre 
            FROM administraciontipocontrato
            ORDER BY ltipocontrato_id DESC;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var tipoContrato = await connection.QueryAsync<AdministracionTipoContrato>(query);

            bool success = tipoContrato != null && tipoContrato.Any();
            string mensaje = success ? "Tipos de contrato obtenidos correctamente." : "No se encontraron tipos de contrato.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, tipoContrato:{JsonConvert.SerializeObject(tipoContrato, Formatting.Indented)}]");

            return (tipoContrato ?? Enumerable.Empty<AdministracionTipoContrato>(), success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (Enumerable.Empty<AdministracionTipoContrato>(), false, $"Error al obtener los tipos de contrato: {ex.Message}");
        }
    }
    public async Task<(IEnumerable<AdministracionEstadoContrato> EstadoContrato, bool Success, string Mensaje)> GetEstadoContrato(string LogTransaccionId)
    {
         string nombreMetodo = "GetEstadoContrato()";

        const string query = @"
            SELECT 
                lestadocontrato_id AS LEstadoContratoId, 
                UPPER(snombre) AS SNombre 
            FROM administracionestadocontrato
            ORDER BY lestadocontrato_id DESC;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var estados = await connection.QueryAsync<AdministracionEstadoContrato>(query);

            bool success = estados != null && estados.Any();
            string mensaje = success ? "Estados de contrato obtenidos correctamente." : "No se encontraron estados de contrato.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, estados:{JsonConvert.SerializeObject(estados, Formatting.Indented)}]");

            return (estados ?? Enumerable.Empty<AdministracionEstadoContrato>(), success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (Enumerable.Empty<AdministracionEstadoContrato>(), false, $"Error al obtener los estados de contrato: {ex.Message}");
        }
    }
    public async Task<(IEnumerable<ListaAdministracionPais> Pais, bool Success, string Mensaje)> GetPais(string LogTransaccionId)
    {
        string nombreMetodo = "GetPais()";

        const string query = @"
            SELECT 
                lpais_id AS LPaisId, 
                UPPER(snombre) AS SNombre 
            FROM basepais
            ORDER BY lpais_id DESC;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var paises = await connection.QueryAsync<ListaAdministracionPais>(query);

            bool success = paises != null && paises.Any();
            string mensaje = success ? "Países obtenidos correctamente." : "No se encontraron países.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, paises:{JsonConvert.SerializeObject(paises, Formatting.Indented)}]");

            return (paises ?? Enumerable.Empty<ListaAdministracionPais>(), success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (Enumerable.Empty<ListaAdministracionPais>(), false, $"Error al obtener los países: {ex.Message}");
        }
    }
    public async Task<(IEnumerable<ListaAdministracionTipoBaja> TipoBaja, bool Success, string Mensaje)> GetTipoBaja(string LogTransaccionId)
    {
        string nombreMetodo = "GetTipoBaja()";

        const string query = @"
            SELECT 
                ltipobaja_id AS LTipoBajaId, 
                UPPER(snombre) AS SNombre 
            FROM administraciontipobaja
            ORDER BY ltipobaja_id DESC;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var tiposBaja = await connection.QueryAsync<ListaAdministracionTipoBaja>(query);

            bool success = tiposBaja != null && tiposBaja.Any();
            string mensaje = success ? "Tipos de baja obtenidos correctamente." : "No se encontraron tipos de baja.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, tiposBaja:{JsonConvert.SerializeObject(tiposBaja, Formatting.Indented)}]");

            return (tiposBaja ?? Enumerable.Empty<ListaAdministracionTipoBaja>(), success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (Enumerable.Empty<ListaAdministracionTipoBaja>(), false, $"Error al obtener los tipos de baja: {ex.Message}");
        }
    }
    public async Task<(IEnumerable<ListaTipoDescuento> TipoDescuento, bool Success, string Mensaje)> GetTipoDescuento(string LogTransaccionId)
    {
        string nombreMetodo = "GetTipoDescuento()";

        const string query = @"
            SELECT 
                ldescuentociclotipo_id AS LDescuentoTipoId, 
                UPPER(snombre) AS SNombre 
            FROM administraciondescuentociclotipo
            ORDER BY ldescuentociclotipo_id DESC;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var tiposDescuento = await connection.QueryAsync<ListaTipoDescuento>(query);

            bool success = tiposDescuento != null && tiposDescuento.Any();
            string mensaje = success ? "Tipos de descuento obtenidos correctamente." : "No se encontraron tipos de descuento.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, tiposDescuento:{JsonConvert.SerializeObject(tiposDescuento, Formatting.Indented)}]");

            return (tiposDescuento ?? Enumerable.Empty<ListaTipoDescuento>(), success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (Enumerable.Empty<ListaTipoDescuento>(), false, $"Error al obtener los tipos de descuento: {ex.Message}");
        }
    }

}
