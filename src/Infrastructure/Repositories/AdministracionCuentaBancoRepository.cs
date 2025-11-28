using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using Newtonsoft.Json;

namespace ApiGuardian.Infrastructure.Repositories;

public class AdministracionCuentaBancoRepository : IAdministracionCuentaBancoRepository
{
    private readonly DapperContext _context;
    private readonly ILogService _log;
    private string NOMBREARCHIVO = "AdministracionCuentaBancoRepository.CS";
    public AdministracionCuentaBancoRepository(DapperContext context, ILogService log)
    {
        _context = context;
        _log = log;
    }
    public async Task<(CuentaBanco Data, bool Success, string Mensaje)> GetCuentaBanco(string LogTransaccionId, int lContactoId)
    {
        string nombreMetodo = "GetCuentaBanco()";

        const string query = @"
            SELECT 
                lcontacto_id AS LContactoId, 
                ctienecuenta AS CTieneCuenta, 
                lcuentabanco AS LCuentaBanco, 
                lcodigobanco AS LCodigoBanco, 
                cbaja AS CBaja, 
                snombrecompleto AS SNombreCompleto, 
                dtfecharegistro AS FechaRegistro, 
                scedulaidentidad AS SCedulaIdentidad, 
                lnit AS LNit 
            FROM administracioncontacto 
            WHERE lcontacto_id = @LContactoId;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var data = await connection.QueryFirstOrDefaultAsync<CuentaBanco>(query, new { LContactoId = lContactoId });

            bool success = data != null;
            string mensaje = success ? "Datos obtenidos correctamente." : "No se encontró el contacto solicitado.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, data:{JsonConvert.SerializeObject(data, Formatting.Indented)}]");

            return (data ?? new CuentaBanco(), success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (new CuentaBanco(), false, $"Error al obtener cuenta bancaria: {ex.Message}");
        }
    }
    public async Task<(IEnumerable<CuentaBanco> Data, bool Success, string Mensaje, int Total)> GetAllCuentaBanco(string LogTransaccionId, int page, int pageSize, string? search)
    {
        string nombreMetodo = "GetAllCuentaBanco()";

        search ??= "";

        const string queryData = @"
            SELECT 
                AC.lcontacto_id AS LContactoId, 
                AC.ctienecuenta AS CTieneCuenta, 
                AC.lcuentabanco AS LCuentaBanco, 
                AC.lcodigobanco AS LCodigoBanco, 
                AC.cbaja AS CBaja, 
                AC.snombrecompleto AS SNombreCompleto, 
                AC.dtfecharegistro AS FechaRegistro, 
                AC.scedulaidentidad AS SCedulaIdentidad, 
                AC.lnit AS LNit,
                IFNULL(AB.lbanco_id, 0) AS LBancoId,
                IFNULL(AB.snombre, '') AS Banco,
                AC.sotro AS Comentario,
                IFNULL(AM.snombre, '') AS Moneda
            FROM administracioncontacto AC
            LEFT JOIN administracionbanco AB ON AB.lbanco_id = AC.lbanco_id
            LEFT JOIN administracionmoneda AM ON AM.lmoneda_id = AB.lmoneda_id
            WHERE (AC.snombrecompleto LIKE @Search OR AC.scedulaidentidad LIKE @Search) 
            AND AC.lcontacto_id > 0 
            AND AC.cbaja = 0
            LIMIT @PageSize OFFSET @Page;
        ";

        const string queryTotal = @"
            SELECT COUNT(*) 
            FROM administracioncontacto AC
            LEFT JOIN administracionbanco AB ON AB.lbanco_id = AC.lbanco_id
            LEFT JOIN administracionmoneda AM ON AM.lmoneda_id = AB.lmoneda_id
            WHERE (AC.snombrecompleto LIKE @Search OR AC.scedulaidentidad LIKE @Search) 
            AND AC.lcontacto_id > 0 
            AND AC.cbaja = 0;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [scriptData: {queryData}]");
        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [scriptTotal: {queryTotal}]");

        try
        {
            using var connection = _context.CreateConnection();

            var parameters = new
            {
                Search = $"%{search}%",
                PageSize = pageSize,
                Page = page
            };

            var data = await connection.QueryAsync<CuentaBanco>(queryData, parameters);
            var total = await connection.ExecuteScalarAsync<int>(queryTotal, parameters);

            bool success = data != null && data.Any();
            string mensaje = success ? "Datos obtenidos correctamente." : "No se encontraron registros.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, total:{total}, data:{JsonConvert.SerializeObject(data, Formatting.Indented)}]");

            return (data ?? Enumerable.Empty<CuentaBanco>(), success, mensaje, total);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (Enumerable.Empty<CuentaBanco>(), false, $"Error al obtener cuentas bancarias: {ex.Message}", 0);
        }
    }
    public async Task<(bool Success, string Mensaje)> UpdateCuentaBanco(string LogTransaccionId, DataCuentaBanco data)
    {
        string nombreMetodo = "UpdateCuentaBanco()";

        const string query = @"
            UPDATE administracioncontacto
            SET
                ctienecuenta = @CTieneCuenta,
                lcuentabanco = @LCuentaBanco,
                lcodigobanco = @LCodigoBanco,
                cbaja = @CBaja,
                snombrecompleto = @SNombreCompleto,
                dtfecharegistro = @FechaRegistro,
                scedulaidentidad = @SCedulaIdentidad,
                lnit = @LNit,
                susuariomod = @Usuario,
                dtfechamod = NOW(),
                lbanco_id = @LBancoId,
                sotro = @Comentario
            WHERE lcontacto_id = @LContactoId;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var rows = await connection.ExecuteAsync(query, new
            {
                data.LContactoId,
                data.CTieneCuenta,
                data.LCuentaBanco,
                data.LCodigoBanco,
                data.CBaja,
                data.SNombreCompleto,
                data.FechaRegistro,
                data.SCedulaIdentidad,
                data.LNit,
                Usuario = data.usuario,
                data.LBancoId,
                data.Comentario
            });

            bool success = rows > 0;
            string mensaje = success ? "Registro actualizado correctamente." : "No se encontró el registro o no se realizaron cambios.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, rowsAffected:{rows}, data:{JsonConvert.SerializeObject(data, Formatting.Indented)}]");

            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al actualizar cuenta bancaria: {ex.Message}");
        }
    }
}
