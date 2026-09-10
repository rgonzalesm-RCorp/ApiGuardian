using Dapper;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Infrastructure.Persistence;

namespace ApiGuardian.Infrastructure.Repositories;

public class CasosObservadosRepository : ICasosObservadosRepository
{
    private readonly DapperContext _context;
    private readonly ILogService _log;
    private const string NOMBREARCHIVO = "CasosObservadosRepository.cs";

    public CasosObservadosRepository(DapperContext context, ILogService log)
    {
        _context = context;
        _log = log;
    }

    public async Task<(IEnumerable<ItemCasoObservado> Data, CasosObservadosResumen Resumen, bool Success, string Mensaje)> GetCasosObservados(
        string LogTransaccionId,
        string Usuario,
        int LCicloId,
        DateTime fechaInicio,
        DateTime fechaFin
    )
    {
        const string nombreMetodo = "GetCasosObservados()";
        const string queryContratosPeriodo = @"
            SELECT
                ctr.lcontrato_id AS LContratoId,
                ctr.snroventa AS NroVenta,
                ctr.dtfecha AS FechaVenta,
                cto.lcontacto_id AS ClienteId,
                cto.snombrecompleto AS Cliente,
                cto.scedulaidentidad AS ClienteDocId,
                cto.cbaja AS ClienteBaja,
                cto.dtfecharegistro AS ClienteFechaRegistro,
                cto.scodigo AS ClienteCodigo,
                pr.lcontacto_id AS PatrocinadorId,
                pr.snombrecompleto AS Patrocinador,
                pr.scedulaidentidad AS PatrocinadorDocId,
                pr.cbaja AS PatrocinadorBaja,
                ven.lcontacto_id AS VendedorId,
                ven.snombrecompleto AS Vendedor,
                ven.scedulaidentidad AS VendedorDocId,
                ven.cbaja AS VendedorBaja,
                ven.dtfecharegistro AS VendedorFechaRegistro,
                ven.scodigo AS VendedorCodigo,
                cto.lpatrocinante_id AS ClientePatrocinadorId,
                ctr.lasesor_id AS ContratoAsesorId
            FROM administracioncontrato ctr
            INNER JOIN administracioncontacto cto ON cto.lcontacto_id = ctr.lcontacto_id
            INNER JOIN administracioncontacto ven ON ven.lcontacto_id = ctr.lasesor_id
            LEFT JOIN administracioncontacto pr ON pr.lcontacto_id = cto.lpatrocinante_id
            WHERE ctr.dtfecha >= @FechaInicio
              AND ctr.dtfecha < @FechaFinExclusiva;
        ";

        const string queryVendedoresHistoricos = @"
            SELECT DISTINCT lasesor_id
            FROM administracioncontrato
            WHERE lasesor_id IN @Ids
              AND dtfecha >= @FechaHistoricaInicio
              AND dtfecha < @FechaHistoricaFinExclusiva
              AND ltipocontrato_id IN (1, 2);
        ";

        const string queryClientesHistoricos = @"
            SELECT DISTINCT lcontacto_id
            FROM administracioncontrato
            WHERE lcontacto_id IN @Ids
              AND dtfecha >= @FechaHistoricaInicio
              AND dtfecha < @FechaHistoricaFinExclusiva
              AND ltipocontrato_id IN (1, 2);
        ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
            $"Inicio de metodo [Usuario:{Usuario}, LCicloId:{LCicloId}, FechaInicio:{fechaInicio:yyyy-MM-dd}, FechaFin:{fechaFin:yyyy-MM-dd}]");

        try
        {
            using var connection = _context.CreateConnection();

            var parameters = new
            {
                FechaInicio = fechaInicio.Date,
                FechaFinExclusiva = fechaFin.Date.AddDays(1),
                FechaHistoricaInicio = fechaInicio.Date.AddYears(-1),
                FechaHistoricaFinExclusiva = fechaInicio.Date
            };

            var contratosPeriodo = (
                await connection.QueryAsync<ItemCasoObservado>(queryContratosPeriodo, parameters)
            ).ToList();

            var vendedoresDadosBaja = contratosPeriodo
                .Where(item => EsBaja(item.VendedorBaja) && item.VendedorId.HasValue)
                .ToList();
            var clientesDadosBaja = contratosPeriodo
                .Where(item => EsBaja(item.ClienteBaja) && item.ClienteId.HasValue)
                .ToList();

            var vendedorIds = vendedoresDadosBaja
                .Select(item => item.VendedorId!.Value)
                .Distinct()
                .ToArray();
            var clienteIds = clientesDadosBaja
                .Select(item => item.ClienteId!.Value)
                .Distinct()
                .ToArray();

            var vendedoresConVentasHistoricas = vendedorIds.Length == 0
                ? new HashSet<int>()
                : (
                    await connection.QueryAsync<int>(
                        queryVendedoresHistoricos,
                        new
                        {
                            Ids = vendedorIds,
                            parameters.FechaHistoricaInicio,
                            parameters.FechaHistoricaFinExclusiva
                        }
                    )
                ).ToHashSet();

            var clientesConComprasHistoricas = clienteIds.Length == 0
                ? new HashSet<int>()
                : (
                    await connection.QueryAsync<int>(
                        queryClientesHistoricos,
                        new
                        {
                            Ids = clienteIds,
                            parameters.FechaHistoricaInicio,
                            parameters.FechaHistoricaFinExclusiva
                        }
                    )
                ).ToHashSet();

            var casos = new List<ItemCasoObservado>();
            foreach (var contrato in contratosPeriodo)
            {
                var esDoblePatrocinio = contrato.ClientePatrocinadorId.HasValue
                    && contrato.ContratoAsesorId.HasValue
                    && contrato.ClientePatrocinadorId != contrato.ContratoAsesorId
                    && !string.IsNullOrWhiteSpace(contrato.VendedorDocId)
                    && !string.IsNullOrWhiteSpace(contrato.ClienteDocId)
                    && !string.Equals(
                        contrato.VendedorDocId,
                        contrato.ClienteDocId,
                        StringComparison.OrdinalIgnoreCase
                    );

                if (esDoblePatrocinio)
                {
                    casos.Add(CrearCaso(
                        contrato,
                        "DOBLE_PATROCINIO",
                        "Venta con patrocinador diferente al vendedor"
                    ));
                }

                if (EsBaja(contrato.VendedorBaja))
                {
                    casos.Add(CrearCaso(
                        contrato,
                        "VENDEDOR_DADO_BAJA",
                        "Venta realizada por un vendedor dado de baja"
                    ));
                }

                if (EsBaja(contrato.ClienteBaja))
                {
                    casos.Add(CrearCaso(
                        contrato,
                        "CLIENTE_DADO_BAJA",
                        "Compra realizada por un cliente dado de baja"
                    ));
                }
            }

            foreach (var vendedor in vendedoresDadosBaja
                .GroupBy(item => item.VendedorId!.Value)
                .Select(grupo => grupo.First())
                .Where(item => !vendedoresConVentasHistoricas.Contains(item.VendedorId!.Value)))
            {
                casos.Add(CrearCaso(
                    vendedor,
                    "VENDEDOR_SIN_VENTAS_UN_ANIO",
                    "Vendedor dado de baja sin ventas en los doce meses anteriores al ciclo",
                    incluirVenta: false,
                    incluirCliente: false,
                    incluirPatrocinador: false
                ));
            }

            foreach (var cliente in clientesDadosBaja
                .GroupBy(item => item.ClienteId!.Value)
                .Select(grupo => grupo.First())
                .Where(item => !clientesConComprasHistoricas.Contains(item.ClienteId!.Value)))
            {
                casos.Add(CrearCaso(
                    cliente,
                    "CLIENTE_SIN_COMPRAS_UN_ANIO",
                    "Cliente dado de baja sin compras en los doce meses anteriores al ciclo",
                    incluirVenta: false
                ));
            }

            casos = casos
                .OrderByDescending(item => item.FechaVenta.HasValue)
                .ThenByDescending(item => item.FechaVenta)
                .ThenBy(item => item.NroVenta)
                .ThenBy(item => item.TipoCaso)
                .ToList();

            for (var index = 0; index < casos.Count; index++)
            {
                casos[index].CasoObservadoId = index + 1;
                casos[index].LCicloId = LCicloId;
            }

            var resumen = new CasosObservadosResumen
            {
                TotalCasos = casos.Count,
                CasosPendientes = casos.Count(item => item.Estado == "PENDIENTE"),
                CasosRevisados = casos.Count(item => item.Estado == "REVISADO"),
                DoblePatrocinio = casos.Count(item => item.TipoCaso == "DOBLE_PATROCINIO"),
                VendedoresDadosBaja = casos.Count(item => item.TipoCaso == "VENDEDOR_DADO_BAJA"),
                ClientesDadosBaja = casos.Count(item => item.TipoCaso == "CLIENTE_DADO_BAJA"),
                VendedoresSinVentasUnAnio = casos.Count(item => item.TipoCaso == "VENDEDOR_SIN_VENTAS_UN_ANIO"),
                ClientesSinComprasUnAnio = casos.Count(item => item.TipoCaso == "CLIENTE_SIN_COMPRAS_UN_ANIO")
            };

            const string mensaje = "Casos observados obtenidos correctamente.";
            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje:{mensaje}, total:{resumen.TotalCasos}]");

            return (casos, resumen, true, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (
                Enumerable.Empty<ItemCasoObservado>(),
                new CasosObservadosResumen(),
                false,
                $"Error al consultar casos observados: {ex.Message}"
            );
        }
    }

    private static bool EsBaja(string? valor) => string.Equals(valor?.Trim(), "1", StringComparison.OrdinalIgnoreCase);

    private static ItemCasoObservado CrearCaso(
        ItemCasoObservado origen,
        string tipoCaso,
        string motivo,
        bool incluirVenta = true,
        bool incluirCliente = true,
        bool incluirPatrocinador = true
    )
    {
        return new ItemCasoObservado
        {
            TipoCaso = tipoCaso,
            LContratoId = incluirVenta ? origen.LContratoId : null,
            NroVenta = incluirVenta ? origen.NroVenta : string.Empty,
            FechaVenta = incluirVenta ? origen.FechaVenta : null,
            ClienteId = incluirCliente ? origen.ClienteId : null,
            Cliente = incluirCliente ? origen.Cliente : string.Empty,
            ClienteDocId = incluirCliente ? origen.ClienteDocId : string.Empty,
            ClienteBaja = incluirCliente ? origen.ClienteBaja : string.Empty,
            ClienteFechaRegistro = incluirCliente ? origen.ClienteFechaRegistro : null,
            ClienteCodigo = incluirCliente ? origen.ClienteCodigo : string.Empty,
            ClientePatrocinadorId = incluirCliente ? origen.ClientePatrocinadorId : null,
            PatrocinadorId = incluirPatrocinador ? origen.PatrocinadorId : null,
            Patrocinador = incluirPatrocinador ? origen.Patrocinador : string.Empty,
            PatrocinadorDocId = incluirPatrocinador ? origen.PatrocinadorDocId : string.Empty,
            PatrocinadorBaja = incluirPatrocinador ? origen.PatrocinadorBaja : string.Empty,
            VendedorId = origen.VendedorId,
            Vendedor = origen.Vendedor,
            VendedorDocId = origen.VendedorDocId,
            VendedorBaja = origen.VendedorBaja,
            VendedorFechaRegistro = origen.VendedorFechaRegistro,
            VendedorCodigo = origen.VendedorCodigo,
            ContratoAsesorId = origen.ContratoAsesorId,
            Motivo = motivo,
            Estado = "PENDIENTE"
        };
    }

    public Task<(bool Success, string Mensaje)> ProcesarCasosObservados(string LogTransaccionId, string Usuario, int LCicloId)
    {
        string nombreMetodo = "ProcesarCasosObservados()";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [Usuario:{Usuario}, LCicloId:{LCicloId}]");

        // Punto de extension para conectar la logica real de casos observados.
        const string mensaje = "Paso de casos observados procesado correctamente.";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Fin de metodo [mensaje:{mensaje}]");

        return Task.FromResult((true, mensaje));
    }
}
