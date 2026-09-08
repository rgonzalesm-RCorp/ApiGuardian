using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Infrastructure.Persistence;
using Dapper;

namespace ApiGuardian.Infrastructure.Repositories;

public sealed class RetencionEmpresaRepository : IRetencionEmpresaRepository
{
    private readonly DapperContext _context;
    private readonly ILogService _log;
    private const string NombreArchivo = "RetencionEmpresaRepository.cs";

    public RetencionEmpresaRepository(DapperContext context, ILogService log)
    {
        _context = context;
        _log = log;
    }

    public async Task<(bool Success, string Mensaje, List<RetencionEmpresaItem> Data)> ObtenerAsync(
        string logTransaccionId,
        int cicloId
    )
    {
        const string sql = """
            SELECT
                @CicloId AS LCicloId,
                componentes.empresa_id AS EmpresaId,
                componentes.lcontacto_id AS ContactoId,
                TRIM(COALESCE(contacto.scedulaidentidad, '')) AS Carnet,
                SUM(componentes.vpers) AS VPers,
                SUM(componentes.vgrupo) AS VGrupo,
                SUM(componentes.residual) AS Residual,
                SUM(componentes.vpers + componentes.vgrupo + componentes.residual) AS MontoComision,
                CASE WHEN factura.lcontacto_id IS NULL THEN 0 ELSE 1 END AS LPresentaFactura
            FROM (
                SELECT
                    venta.lcontacto_id,
                    empresaComplejo.empresa_id,
                    SUM(COALESCE(venta.dcomision, 0)) AS vpers,
                    0 AS vgrupo,
                    0 AS residual
                FROM administracionventapersonal venta
                INNER JOIN administracioncontrato contrato
                    ON contrato.lcontrato_id = venta.lcontrato_id
                INNER JOIN administracioncontacto contacto
                    ON contacto.lcontacto_id = venta.lcontacto_id
                   AND contacto.cbaja = 0
                   AND contacto.lcontacto_id <> 6474
                INNER JOIN (
                    SELECT DISTINCT complejo_id, empresa_id
                    FROM empresa_complejo
                ) empresaComplejo
                    ON empresaComplejo.complejo_id = contrato.lcomplejo_id
                WHERE venta.lciclo_id = @CicloId
                  AND venta.lcontacto_id > 3
                GROUP BY venta.lcontacto_id, empresaComplejo.empresa_id

                UNION ALL

                SELECT
                    venta.lcontacto_id,
                    empresaComplejo.empresa_id,
                    0 AS vpers,
                    SUM(COALESCE(venta.dcomision, 0)) AS vgrupo,
                    0 AS residual
                FROM administracionventagrupo venta
                INNER JOIN administracioncontrato contrato
                    ON contrato.lcontrato_id = venta.lcontrato_id
                INNER JOIN administracioncontacto contacto
                    ON contacto.lcontacto_id = venta.lcontacto_id
                   AND contacto.cbaja = 0
                   AND contacto.lcontacto_id <> 6474
                INNER JOIN (
                    SELECT DISTINCT complejo_id, empresa_id
                    FROM empresa_complejo
                ) empresaComplejo
                    ON empresaComplejo.complejo_id = contrato.lcomplejo_id
                WHERE venta.lciclo_id = @CicloId
                  AND venta.lcontacto_id > 3
                GROUP BY venta.lcontacto_id, empresaComplejo.empresa_id

                UNION ALL

                SELECT
                    redEmpresaComplejo.lcontacto_id,
                    empresaComplejo.empresa_id,
                    0 AS vpers,
                    0 AS vgrupo,
                    SUM(COALESCE(redEmpresaComplejo.dmonto, 0)) AS residual
                FROM administracionredempresacomplejo redEmpresaComplejo
                INNER JOIN administracioncontacto contacto
                    ON contacto.lcontacto_id = redEmpresaComplejo.lcontacto_id
                   AND contacto.cbaja = 0
                   AND contacto.lcontacto_id <> 6474
                INNER JOIN (
                    SELECT DISTINCT empresa_id, complejo_id
                    FROM empresa_complejo
                ) empresaComplejo
                    ON empresaComplejo.complejo_id = redEmpresaComplejo.lcomplejo_id
                WHERE redEmpresaComplejo.lciclo_id = @CicloId
                  AND redEmpresaComplejo.lcontacto_id > 3
                GROUP BY redEmpresaComplejo.lcontacto_id, empresaComplejo.empresa_id

                UNION ALL

                SELECT
                    bonoPar.l_contacto_ganador_id AS lcontacto_id,
                    empresaComplejo.empresa_id,
                    0 AS vpers,
                    0 AS vgrupo,
                    SUM(COALESCE(bonoPar.bono / NULLIF(bonoPar.cantidad_venta, 0), 0)) AS residual
                FROM bonopar bonoPar
                INNER JOIN administracioncontacto contacto
                    ON contacto.lcontacto_id = bonoPar.l_contacto_ganador_id
                   AND contacto.cbaja = 0
                   AND contacto.lcontacto_id <> 6474
                INNER JOIN bonopardetalle detalle
                    ON detalle.bonopar_id = bonoPar.id
                INNER JOIN administracioncontrato contrato
                    ON contrato.lcontrato_id = detalle.l_contrato_id
                INNER JOIN (
                    SELECT DISTINCT complejo_id, empresa_id
                    FROM empresa_complejo
                ) empresaComplejo
                    ON empresaComplejo.complejo_id = contrato.lcomplejo_id
                WHERE bonoPar.lciclo_id = @CicloId
                  AND bonoPar.l_contacto_ganador_id > 3
                GROUP BY bonoPar.l_contacto_ganador_id, empresaComplejo.empresa_id
            ) componentes
            INNER JOIN administracionempresa empresa
                ON empresa.lempresa_id = componentes.empresa_id
            LEFT JOIN administracioncontacto contacto
                ON contacto.lcontacto_id = componentes.lcontacto_id
            LEFT JOIN (
                SELECT DISTINCT lcontacto_id
                FROM administracionciclopresentafactura
                WHERE lciclo_id = @CicloId
            ) factura ON factura.lcontacto_id = componentes.lcontacto_id
            GROUP BY componentes.empresa_id, componentes.lcontacto_id, contacto.scedulaidentidad, factura.lcontacto_id;
            """;

        try
        {
            using var connection = _context.CreateConnection();
            var data = (await connection.QueryAsync<RetencionEmpresaItem>(sql, new { CicloId = cicloId })).ToList();
            return (true, $"Se encontraron {data.Count} retenciones por persona y empresa.", data);
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId, NombreArchivo, nameof(ObtenerAsync), "Error al consultar retenciones.", ex);
            return (false, $"Error al consultar retenciones: {ex.Message}", new List<RetencionEmpresaItem>());
        }
    }

    public async Task<(bool Success, string Mensaje, int Insertados)> InsertarAsync(
        string logTransaccionId,
        string usuario,
        List<RetencionEmpresaItem> retenciones
    )
    {
        const string sql = """
            INSERT INTO tbl_retencionempresa
            (
                susuarioadd, dtfechaadd, susuariomod, dtfechamod,
                idtblretencion, lciclo_id, idempresa, lcontacto_id, carnet,
                vpers, vgrupo, residual, montocomision,
                lpresentafactura, largentina_id, porcentajeret,
                montoretencion, total_comision
            )
            SELECT
                @Usuario, NOW(), @Usuario, NOW(),
                (
                    SELECT COALESCE(MAX(actual.idtblretencion), 0) + 1
                    FROM tbl_retencionempresa actual
                ),
                @LCicloId, @EmpresaId, @ContactoId, @Carnet,
                @VPers, @VGrupo, @Residual, @MontoComision,
                @LPresentaFactura, 0, @PorcentajeRetencion,
                @MontoRetencion, @TotalComision
            FROM DUAL
            WHERE NOT EXISTS
            (
                SELECT 1
                FROM tbl_retencionempresa existente
                WHERE existente.lciclo_id = @LCicloId
                  AND existente.idempresa = @EmpresaId
                  AND existente.lcontacto_id = @ContactoId
            );
            """;

        try
        {
            using var connection = _context.CreateConnection();
            var insertados = await connection.ExecuteAsync(sql, retenciones.Select(item => new
            {
                Usuario = usuario,
                item.LCicloId,
                item.EmpresaId,
                item.ContactoId,
                item.Carnet,
                item.VPers,
                item.VGrupo,
                item.Residual,
                item.MontoComision,
                item.LPresentaFactura,
                item.PorcentajeRetencion,
                item.MontoRetencion,
                item.TotalComision,
            }));
            return (true, $"Se insertaron {insertados} retenciones. No se actualizó ni eliminó ningún registro.", insertados);
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId, NombreArchivo, nameof(InsertarAsync), "Error al insertar retenciones.", ex);
            return (false, $"Error al insertar retenciones: {ex.Message}", 0);
        }
    }
}
