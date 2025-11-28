using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using Newtonsoft.Json;

namespace ApiGuardian.Infrastructure.Repositories;

public class AdministracionCicloFacturaRepository : IAdministracionCicloFacturaRepository
{
    private readonly DapperContext _context;
    private readonly ILogService _log;
    private string NOMBREARCHIVO = "AdministracionCicloFacturaRepository.cs";
    public AdministracionCicloFacturaRepository(DapperContext context, ILogService log)
    {
        _context = context;
        _log = log;
    }
    public async Task<(IEnumerable<ListaAdministracionCicloFactura> Data, bool Success, string Mensaje, int Total)> GetAllAdministracionCiclofactura(string LogTransaccionId, int page, int pageSize, int lCicloId)
    {
        string nombreMetodo = "GetAllAdministracionCiclofactura()";

        const string queryData = @"
            SELECT 
                AF.lciclopresentafactura_id AS LCicloFacturaId,
                AF.dtfechaadd AS FechaRegistro,
                AC.lciclo_id AS LCicloId,
                UPPER(AC.snombre) AS Ciclo,
                ACT.lcontacto_id AS LContactoId,
                UPPER(ACT.snombrecompleto) AS SNombreCompleto,
                AF.lsemana_id AS LSemanaId,
                UPPER(S.lnombre) AS Semana
            FROM administracionciclopresentafactura AF
            INNER JOIN administracionciclo AC ON AC.lciclo_id = AF.lciclo_id
            INNER JOIN administracionsemanaciclo SC ON SC.lsemana_id = AF.lsemana_id
            INNER JOIN administracionsemana S ON S.idsemana = SC.lnrosemana
            INNER JOIN administracioncontacto ACT ON ACT.lcontacto_id = AF.lcontacto_id
            WHERE AC.lciclo_id = @LCicloId
            ORDER BY AF.dtfechaadd DESC
            LIMIT @PageSize OFFSET @Offset;
        ";

        const string queryCount = @"
            SELECT COUNT(*)
            FROM administracionciclopresentafactura AF
            INNER JOIN administracionciclo AC ON AC.lciclo_id = AF.lciclo_id
            INNER JOIN administracionsemanaciclo SC ON SC.lsemana_id = AF.lsemana_id
            INNER JOIN administracionsemana S ON S.idsemana = SC.lnrosemana
            INNER JOIN administracioncontacto ACT ON ACT.lcontacto_id = AF.lcontacto_id
            WHERE AC.lciclo_id = @LCicloId;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [scriptData: {queryData}]");
        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [scriptCount: {queryCount}]");

        try
        {
            using var connection = _context.CreateConnection();

            var parameters = new
            {
                LCicloId = lCicloId,
                PageSize = pageSize,
                Offset = page
            };

            int total = await connection.ExecuteScalarAsync<int>(queryCount, parameters);

            var result = await connection.QueryAsync<ListaAdministracionCicloFactura>(queryData, parameters);

            bool success = result != null && result.Any();
            string mensaje = success ? "Datos obtenidos correctamente." : "No se encontraron registros.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, total:{total}, data:{JsonConvert.SerializeObject(result, Formatting.Indented)}]");

            return (result ?? Enumerable.Empty<ListaAdministracionCicloFactura>(), success, mensaje, total);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (Enumerable.Empty<ListaAdministracionCicloFactura>(), false, $"Error al consultar ciclo facturas: {ex.Message}", 0);
        }
    }
    public async Task<(bool success, string mensaje)> InsertAdministracionCiclofactura(string LogTransaccionId, AdministracionCicloFactura data)
    {
        string nombreMetodo = "InsertAdministracionCiclofactura()";

        const string existeQuery = @"
            SELECT COUNT(*) 
            FROM administracionciclopresentafactura
            WHERE lciclo_id = @LCicloId
            AND lcontacto_id = @LContactoId
            AND lsemana_id = @LSemanaId;
        ";

        const string nextIdQuery = @"
            SELECT IFNULL(MAX(lciclopresentafactura_id), 0) + 1 
            FROM administracionciclopresentafactura;
        ";

        const string insertQuery = @"
            INSERT INTO administracionciclopresentafactura (
                susuarioadd,
                dtfechaadd,
                susuariomod,
                dtfechamod,
                lciclopresentafactura_id,
                lciclo_id,
                lcontacto_id,
                lsemana_id
            ) VALUES (
                @Usuario,
                NOW(),
                @Usuario,
                NOW(),
                @Id,
                @LCicloId,
                @LContactoId,
                @LSemanaId
            );
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [existeQuery: {existeQuery}]");

        try
        {
            using var connection = _context.CreateConnection();

            var existe = await connection.ExecuteScalarAsync<int>(existeQuery, new
            {
                data.LCicloId,
                data.LContactoId,
                data.LSemanaId
            });

            if (existe > 0)
            {
                string mensajeDuplicado = "El registro ya existe para este ciclo, contacto y semana.";
                _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Fin de metodo [mensaje: {mensajeDuplicado}]");
                return (false, mensajeDuplicado);
            }

            var nextId = await connection.ExecuteScalarAsync<int>(nextIdQuery);

            var rowsAffected = await connection.ExecuteAsync(insertQuery, new
            {
                Usuario = data.Usuario,
                Id = nextId,
                data.LCicloId,
                data.LContactoId,
                data.LSemanaId
            });

            bool success = rowsAffected > 0;
            string mensaje = success ? "Registro insertado correctamente." : "No se insertó ningún registro.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, rowsAffected:{rowsAffected}, nextId:{nextId}, data:{JsonConvert.SerializeObject(data, Formatting.Indented)}]");

            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al insertar ciclo factura: {ex.Message}");
        }
    }
    public async Task<(bool succes, string mensaje)> DeleteAdministracionCiclofactura(string LogTransaccionId, int lciclofactura, string? usuario)
    {
        string nombreMetodo = "DeleteAdministracionCiclofactura()";

        if (lciclofactura <= 0)
        {
            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"El ID proporcionado no es válido.");
            return (false, "El ID proporcionado no es válido.");
        }

        const string query = @"
            UPDATE administracionciclopresentafactura 
            SET 
                lciclo_id = (lciclo_id * -1), 
                lcontacto_id = (lcontacto_id * -1),
                susuariomod = @Usuario,
                dtfechamod = NOW()
            WHERE lciclopresentafactura_id = @lciclofactura;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var rowsAffected = await connection.ExecuteAsync(query, new 
            { 
                lciclofactura, 
                Usuario = usuario 
            });

            bool success = rowsAffected > 0;
            string mensaje = success ? "Registro eliminado correctamente." : "No se encontró ningún registro con el ID especificado.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, rowsAffected:{rowsAffected}, usuario:{usuario}]");

            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Ocurrió un error al eliminar el registro: {ex.Message}");
        }
    }
}
