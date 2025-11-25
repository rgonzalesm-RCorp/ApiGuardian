using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;

namespace ApiGuardian.Infrastructure.Repositories;

public class AdministracionContratoRepository : IAdministracionContratoRepository
{
    private readonly DapperContext _context;

    public AdministracionContratoRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<ListaAdministracionContrato> Data, bool Success, string Mensaje, int Total)> GetAllAdministracionContrato(int page, int pageSize, string? search)
    {
        try
        {
            using var connection = _context.CreateConnection();

            var query = @"
                SELECT 
                    C.lcontrato_id AS lcontratoId,
                    C.dtfechaadd AS fechaRegistro,
                    C.dtfecha AS fecha,
                    C.snroventa AS nroVenta,
                    P.snombrecompleto AS Propietario,
                    AC.snombre AS complejo,
                    C.suv AS Uv,
                    C.smanzano AS nroMnzo,
                    C.slote AS nroLote,
                    C.dprecio AS precio,
                    C.dcuota_inicial AS cuotaInicial,
                    C.lestado AS estado,
                    ATC.snombre AS TipoContrato,
                    A.snombrecompleto AS Asesor,
                    ATC.ltipocontrato_id AS LTipoContratoId,
                    A.lcontacto_id AS LAsesorId,
                    P.lcontacto_id AS LPropietarioId,
                    AC.lcomplejo_id AS LComplejoId,
                    C.dprecioinicial AS DPecioInicial,
                    AEC.snombre AS EstadoContrato,
                    C.cespecial CEspecial
                FROM administracioncontrato C
                INNER JOIN administracioncontacto P ON P.lcontacto_id = C.lcontacto_id
                INNER JOIN administracioncontacto A ON A.lcontacto_id = C.lasesor_id
                INNER JOIN administracioncomplejo AC ON AC.lcomplejo_id = C.lcomplejo_id
                INNER JOIN administraciontipocontrato ATC ON ATC.ltipocontrato_id = C.ltipocontrato_id
                INNER JOIN administracionestadocontrato AEC ON AEC.lestadocontrato_id = C.lestado
                WHERE (@search IS NULL OR P.snombrecompleto LIKE @search OR C.snroventa LIKE @search)
                ORDER BY C.lcontrato_id DESC
                LIMIT @pageSize OFFSET @page;
            ";

            var countQuery = @"
                SELECT COUNT(*)
                FROM administracioncontrato C
                INNER JOIN administracioncontacto P ON P.lcontacto_id = C.lcontacto_id
                INNER JOIN administracioncontacto A ON A.lcontacto_id = C.lasesor_id
                INNER JOIN administracioncomplejo AC ON AC.lcomplejo_id = C.lcomplejo_id
                INNER JOIN administraciontipocontrato ATC ON ATC.ltipocontrato_id = C.ltipocontrato_id
                INNER JOIN administracionestadocontrato AEC ON AEC.lestadocontrato_id = C.lestado
                WHERE (@search IS NULL OR P.snombrecompleto LIKE @search OR C.snroventa LIKE @search);
            ";

            var parameters = new
            {
                search = string.IsNullOrEmpty(search) ? null : $"%{search}%",
                pageSize,
                page
            };

            var data = await connection.QueryAsync<ListaAdministracionContrato>(query, parameters);
            var total = await connection.ExecuteScalarAsync<int>(countQuery, parameters);

            return (data, true, "Consulta realizada correctamente", total);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return (Enumerable.Empty<ListaAdministracionContrato>(), false, "Error al consultar contratos", 0);
        }
    }
    public async Task<(bool Success, string Mensaje)> InsertContrato(AdministracionContrato data)
    {
        try
        {
            string query = @"
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
                    @LCopmlejoId,
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

            using var connection = _context.CreateConnection();

            var rows = await connection.ExecuteAsync(query, new
            {
                data.Fecha,
                data.LPropietarioId,
                data.LCopmlejoId,
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
                data.Usuario,
                data.NroVenta
            });

            if (rows > 0)
            {
                return (true, "Contrato registrado correctamente.");
            }

            return (false, "No se realizó el guardado.");
        }
        catch (Exception ex)
        {
            return (false, $"Error al insertar contrato: {ex.Message}");
        }
    }
    public async Task<(bool Success, string Mensaje)> UpdateContrato(AdministracionContrato data)
    {
        try
        {
            string query = @"
                UPDATE administracioncontrato SET
                    dtfecha = @Fecha,
                    lcontacto_id = @LPropietarioId,
                    lcomplejo_id = @LCopmlejoId,
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

            using var connection = _context.CreateConnection();

            var rows = await connection.ExecuteAsync(query, new
            {
                data.Fecha,
                data.LPropietarioId,
                data.LCopmlejoId,
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
                data.Usuario,
                data.NroVenta,
                data.LContratoId
            });

            if (rows > 0)
            {
                return (true, "Contrato actualizado correctamente.");
            }

            return (false, "No se realizó la actualización.");
        }
        catch (Exception ex)
        {
            return (false, $"Error al actualizar contrato: {ex.Message}");
        }
    }


}
