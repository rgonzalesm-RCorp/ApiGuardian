using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using Newtonsoft.Json;

namespace ApiGuardian.Infrastructure.Repositories;

public class AdministracionContratoRepository : IAdministracionContratoRepository
{
    private readonly DapperContext _context;
    private readonly ILogService _log;
    private string NOMBREARCHIVO = "AdministracionContratoRepository.cs";
    public AdministracionContratoRepository(DapperContext context, ILogService log)
    {
        _context = context;
        _log = log;
    }

    public async Task<(IEnumerable<ListaAdministracionContrato> Data, bool Success, string Mensaje, int Total)> GetAllAdministracionContrato(string LogTransaccionId, int page, int pageSize, string? search)
    {
        string nombreMetodo = "GetAllAdministracionContrato()";

        const string queryData = @"
            SELECT 
                C.lcontrato_id AS LContratoId,
                C.dtfechaadd AS FechaRegistro,
                C.dtfecha AS Fecha,
                C.snroventa AS NroVenta,
                P.snombrecompleto AS Propietario,
                AC.snombre AS Complejo,
                C.suv AS Uv,
                C.smanzano AS NroMnzo,
                C.slote AS NroLote,
                C.dprecio AS Precio,
                C.dcuota_inicial AS CuotaInicial,
                C.lestado AS Estado,
                ATC.snombre AS TipoContrato,
                A.snombrecompleto AS Asesor,
                ATC.ltipocontrato_id AS LTipoContratoId,
                A.lcontacto_id AS LAsesorId,
                P.lcontacto_id AS LPropietarioId,
                AC.lcomplejo_id AS LComplejoId,
                C.dprecioinicial AS DPrecioInicial,
                AEC.snombre AS EstadoContrato,
                C.cespecial AS CEspecial
            FROM administracioncontrato C
            INNER JOIN administracioncontacto P ON P.lcontacto_id = C.lcontacto_id
            INNER JOIN administracioncontacto A ON A.lcontacto_id = C.lasesor_id
            INNER JOIN administracioncomplejo AC ON AC.lcomplejo_id = C.lcomplejo_id
            INNER JOIN administraciontipocontrato ATC ON ATC.ltipocontrato_id = C.ltipocontrato_id
            INNER JOIN administracionestadocontrato AEC ON AEC.lestadocontrato_id = C.lestado
            WHERE (@Search IS NULL OR P.snombrecompleto LIKE @Search OR C.snroventa LIKE @Search)
            ORDER BY C.lcontrato_id DESC
            LIMIT @PageSize OFFSET @Page;
        ";

        const string queryCount = @"
            SELECT COUNT(*)
            FROM administracioncontrato C
            INNER JOIN administracioncontacto P ON P.lcontacto_id = C.lcontacto_id
            INNER JOIN administracioncontacto A ON A.lcontacto_id = C.lasesor_id
            INNER JOIN administracioncomplejo AC ON AC.lcomplejo_id = C.lcomplejo_id
            INNER JOIN administraciontipocontrato ATC ON ATC.ltipocontrato_id = C.ltipocontrato_id
            INNER JOIN administracionestadocontrato AEC ON AEC.lestadocontrato_id = C.lestado
            WHERE (@Search IS NULL OR P.snombrecompleto LIKE @Search OR C.snroventa LIKE @Search);
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [scriptData: {queryData}]");
        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [scriptCount: {queryCount}]");

        try
        {
            using var connection = _context.CreateConnection();

            var parameters = new
            {
                Search = string.IsNullOrEmpty(search) ? null : $"%{search}%",
                PageSize = pageSize,
                Page = page
            };

            var data = await connection.QueryAsync<ListaAdministracionContrato>(queryData, parameters);
            var total = await connection.ExecuteScalarAsync<int>(queryCount, parameters);

            bool success = data != null && data.Any();
            string mensaje = success ? "Contratos obtenidos correctamente." : "No se encontraron contratos.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, total:{total}, data:{JsonConvert.SerializeObject(data, Formatting.Indented)}]");

            return (data ?? Enumerable.Empty<ListaAdministracionContrato>(), success, mensaje, total);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (Enumerable.Empty<ListaAdministracionContrato>(), false, $"Error al consultar contratos: {ex.Message}", 0);
        }
    }
    public async Task<(bool Success, string Mensaje)> InsertContrato(string LogTransaccionId, AdministracionContrato data)
    {
        string nombreMetodo = "InsertContrato()";

        const string query = @"
            INSERT INTO administracioncontrato (
                lcontrato_id,
                dtfecha,
                lcontacto_id,
                lcomplejo_id,
                smanzano,
                slote,
                suv,
                dprecioinicial,
                dcuota_inicial,
                dprecio,
                lestado,
                ltipocontrato_id,
                lciudad,
                cespecial,
                lasesor_id,
                susuarioadd,
                susuariomod,
                dtfechaadd,
                dtfechamod,
                snroventa,
                latencion_id,
                ltramite_id,
                lreferido_id
            )
            SELECT
                IFNULL(MAX_ID, 0) + 1,
                @Fecha,
                @LPropietarioId,
                @LComplejoId,
                @Mzno,
                @Lote,
                @Uv,
                @PrecioInicial,
                @CuotaInicial,
                @PrecioFinal,
                @LEstadoContratoId,
                @LTipoContratoId,
                @LCiudadId,
                @ContratoEspecial,
                @LAsesorId,
                @Usuario,
                @Usuario,
                NOW(),
                NOW(),
                @NroVenta,
                0,
                0,
                0
            FROM (
                SELECT MAX(lcontrato_id) AS MAX_ID FROM administracioncontrato
            ) AS sub;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var rows = await connection.ExecuteAsync(query, new
            {
                data.Fecha,
                data.LPropietarioId,
                LComplejoId = data.LCopmlejoId, // corregido alias
                data.Mzno,
                data.Lote,
                data.Uv,
                data.PrecioInicial,
                data.CuotaInicial,
                data.PrecioFinal,
                data.LEstadoContratoId,
                data.LTipoContratoId,
                data.LCiudadId,
                data.ContratoEspecial,
                data.LAsesorId,
                Usuario = data.Usuario,
                data.NroVenta
            });

            bool success = rows > 0;
            string mensaje = success ? "Contrato registrado correctamente." : "No se realizó el guardado.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, rowsAffected:{rows}, data:{JsonConvert.SerializeObject(data, Formatting.Indented)}]");

            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al insertar contrato: {ex.Message}");
        }
    }
    public async Task<(bool Success, string Mensaje)> UpdateContrato(string LogTransaccionId, AdministracionContrato data)
    {
        string nombreMetodo = "UpdateContrato()";

        const string query = @"
            UPDATE administracioncontrato 
            SET
                dtfecha = @Fecha,
                lcontacto_id = @LPropietarioId,
                lcomplejo_id = @LComplejoId,
                smanzano = @Mzno,
                slote = @Lote,
                suv = @Uv,
                dprecioinicial = @PrecioInicial,
                dcuota_inicial = @CuotaInicial,
                dprecio = @PrecioFinal,
                lestado = @LEstadoContratoId,
                ltipocontrato_id = @LTipoContratoId,
                lciudad = @LCiudadId,
                cespecial = @ContratoEspecial,
                lasesor_id = @LAsesorId,
                susuariomod = @Usuario,
                dtfechamod = NOW(),
                snroventa = @NroVenta
            WHERE lcontrato_id = @LContratoId;
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var rows = await connection.ExecuteAsync(query, new
            {
                data.Fecha,
                data.LPropietarioId,
                LComplejoId = data.LCopmlejoId, // corregido alias
                data.Mzno,
                data.Lote,
                data.Uv,
                data.PrecioInicial,
                data.CuotaInicial,
                data.PrecioFinal,
                data.LEstadoContratoId,
                data.LTipoContratoId,
                data.LCiudadId,
                data.ContratoEspecial,
                data.LAsesorId,
                Usuario = data.Usuario,
                data.NroVenta,
                data.LContratoId
            });

            bool success = rows > 0;
            string mensaje = success ? "Contrato actualizado correctamente." : "No se realizó la actualización.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, rowsAffected:{rows}, data:{JsonConvert.SerializeObject(data, Formatting.Indented)}]");

            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al actualizar contrato: {ex.Message}");
        }
    }
}
