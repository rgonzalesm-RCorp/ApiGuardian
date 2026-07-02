using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using Newtonsoft.Json;
using System.Text;

namespace ApiGuardian.Infrastructure.Repositories;

public class AdministracionBonoResidualRepository : IAdministracionBonoResidualRepository
{
    private readonly DapperContext _context;
    private readonly ILogService _log;
    private string NOMBREARCHIVO = "AdministracionVentaPersonalRepository.cs";
    public AdministracionBonoResidualRepository(DapperContext context, ILogService log)
    {
        _context = context;
        _log = log;
    }

    public async Task<(bool Success, string Mensaje)> SaveAdministracionBonoResidual(string LogTransaccionId, string Usuario, List<ItemAdministracionBonoResidual> data)
    {
        string nombreMetodo = "SaveAdministracionBonoResidual()";

        if (data == null || data.Count == 0)
            return (false, "No existen registros para guardar.");

        const int batchSize = 1000;

        const string nextIdQuery = @"
            SELECT IFNULL(MAX(lbonoresidual_id), 0) 
            FROM administracionbonoresidual;
        ";

        System.Data.IDbConnection? connection = null;
        System.Data.IDbTransaction? transaction = null;
        int LCicloId = data.FirstOrDefault()?.LCicloId ?? 0;

        try
        {
            connection = _context.CreateConnection();

            connection.Open();

            transaction = connection.BeginTransaction();

            int nextId = await connection.ExecuteScalarAsync<int>(
                nextIdQuery,
                transaction: transaction
            );

            int totalInsertados = 0;
            int totalDetallesInsertados = 0;
            var detallesPendientes = new List<(int BonoResidualId, AdministracionBonoResidualDetalle Detalle)>();

            for (int i = 0; i < data.Count; i += batchSize)
            {
                var batch = data
                    .Skip(i)
                    .Take(batchSize)
                    .ToList();

                var sql = new StringBuilder();

                sql.Append(@"
                    INSERT INTO administracionbonoresidual
                    (
                        susuarioadd,
                        dtfechaadd,
                        susuariomod,
                        dtfechamod,
                        lbonoresidual_id,
                        dtfechacalculo,
                        lciclo_id,
                        lcontacto_id,
                        ltipobono,
                        dmontolote,
                        lmora1g,
                        lporcentajemora1g,
                        lterrenos1g,
                        lmisterrenosconmora,
                        ltotalterrenossinmora,
                        scondicion1,
                        scondicion2,
                        scondicion3,
                        dtotalbono,
                        dtotalpagados_licencia,
                        dtotal_bonolicencia,
                        lnrosemana,
                        lsemana_id
                    )
                    VALUES
                ");

                var parameters = new DynamicParameters();

                for (int j = 0; j < batch.Count; j++)
                {
                    nextId++;

                    var item = batch[j];
                    int bonoResidualId = nextId;

                    sql.Append($@"
                    (
                        @Usuario{j},
                        NOW(),
                        @Usuario{j},
                        NOW(),
                        @LBonoResidualId{j},
                        NOW(),
                        @LCicloId{j},
                        @LContactoId{j},
                        @LTipoBono{j},
                        @DMontoLote{j},
                        @LMoraG1{j},
                        @LPorcentajeMoraG1{j},
                        @LTerrenoG1{j},
                        @LMisTerrenosConMora{j},
                        @LTotalTerrenosSinMora{j},
                        NULL,
                        NULL,
                        NULL,
                        @DTotalBono{j},
                        @DTotalPagadosLicencia{j},
                        @DTotalBonoLicencia{j},
                        @LNroSemana{j},
                        @LSemanaId{j}
                    )");

                    if (j < batch.Count - 1)
                        sql.Append(",");

                    parameters.Add($"Usuario{j}", Usuario);
                    parameters.Add($"LBonoResidualId{j}", bonoResidualId);
                    parameters.Add($"LCicloId{j}", item.LCicloId);
                    parameters.Add($"LContactoId{j}", item.LContactoId);
                    parameters.Add($"LTipoBono{j}", item.LTipoBono);
                    parameters.Add($"DMontoLote{j}", item.DMontoLote);
                    parameters.Add($"LMoraG1{j}", item.LMoraG1);
                    parameters.Add($"LPorcentajeMoraG1{j}", item.LPorcentajeMoraG1);
                    parameters.Add($"LTerrenoG1{j}", item.LTerrenoG1);
                    parameters.Add($"LMisTerrenosConMora{j}", item.LMisTerrenosConMora);
                    parameters.Add($"LTotalTerrenosSinMora{j}", item.LTotalTerrenosSinMora);
                    parameters.Add($"DTotalBono{j}", item.DTotalBono);
                    parameters.Add($"DTotalPagadosLicencia{j}", item.DTotalPagadosLicencia);
                    parameters.Add($"DTotalBonoLicencia{j}", item.DTotalBonoLicencia);
                    parameters.Add($"LNroSemana{j}", item.LNroSemana);
                    parameters.Add($"LSemanaId{j}", item.LSemanaId);

                    if (item.Detalle?.Count > 0)
                    {
                        foreach (var detalle in item.Detalle)
                        {
                            detallesPendientes.Add((bonoResidualId, detalle));
                        }
                    }
                }

                sql.Append(";");

                int rows = await connection.ExecuteAsync(
                    sql.ToString(),
                    parameters,
                    transaction
                );

                totalInsertados += rows;
            }

            for (int i = 0; i < detallesPendientes.Count; i += batchSize)
            {
                var batchDetalle = detallesPendientes
                    .Skip(i)
                    .Take(batchSize)
                    .ToList();

                var sqlDetalle = new StringBuilder();

                sqlDetalle.Append(@"
                    INSERT INTO administracionbonoresidual_detalle
                    (
                        lbonoresidual_id,
                        lempresa_id,
                        lcomplejos_id,
                        nivel,
                        producto,
                        monto,
                        porcentaje_comision,
                        comision,
                        lciclo_id
                    )
                    VALUES
                ");

                var parametersDetalle = new DynamicParameters();

                for (int j = 0; j < batchDetalle.Count; j++)
                {
                    var (bonoResidualId, detalle) = batchDetalle[j];

                    sqlDetalle.Append($@"
                    (
                        @LBonoResidualDetalleId{j},
                        CASE
                            WHEN @LEmpresaId{j} > 0 THEN @LEmpresaId{j}
                            ELSE COALESCE(
                                (SELECT empresa_id
                                 FROM empresa_complejo
                                 WHERE id_almacen_conexion = @LComplejoId{j}
                                 LIMIT 1), 0)
                        END,
                        CASE
                            WHEN @LComplejoId{j} > 0 THEN COALESCE(
                                (SELECT complejo_id
                                 FROM empresa_complejo
                                 WHERE id_almacen_conexion = @LComplejoId{j}
                                 LIMIT 1), @LComplejoId{j})
                            ELSE 0
                        END,
                        @Nivel{j},
                        @Producto{j},
                        @Monto{j},
                        @PorcentajeComision{j},
                        @Comision{j},
                        @LCicloId{j}
                    )");

                    if (j < batchDetalle.Count - 1)
                        sqlDetalle.Append(",");

                    parametersDetalle.Add($"LBonoResidualDetalleId{j}", bonoResidualId);
                    parametersDetalle.Add($"LEmpresaId{j}", detalle.LempresaId);
                    parametersDetalle.Add($"LComplejoId{j}", detalle.LcomplejoId);
                    parametersDetalle.Add($"Nivel{j}", detalle.Nivel);
                    parametersDetalle.Add($"Producto{j}", detalle.Producto);
                    parametersDetalle.Add($"Monto{j}", detalle.Monto);
                    parametersDetalle.Add($"PorcentajeComision{j}", detalle.PorcentajeComision);
                    parametersDetalle.Add($"Comision{j}", detalle.Comision);
                    parametersDetalle.Add($"LCicloId{j}", LCicloId);
                }

                sqlDetalle.Append(";");

                int rowsDetalle = await connection.ExecuteAsync(
                    sqlDetalle.ToString(),
                    parametersDetalle,
                    transaction
                );

                totalDetallesInsertados += rowsDetalle;
            }

            transaction.Commit();

            string mensaje = $"Registros guardados correctamente. Total maestro: {totalInsertados}, total detalle: {totalDetallesInsertados}";

            _log.Info(
                LogTransaccionId,
                NOMBREARCHIVO,
                nombreMetodo,
                $"Fin de metodo [mensaje:{mensaje}, rowsAffected:{totalInsertados}, detalleRowsAffected:{totalDetallesInsertados}]"
            );

            return (true, mensaje);
        }
        catch (Exception ex)
        {
            try
            {
                if (transaction != null && connection?.State == System.Data.ConnectionState.Open)
                    transaction.Rollback();
            }
            catch
            {
            }

            _log.Error(
                LogTransaccionId,
                NOMBREARCHIVO,
                nombreMetodo,
                "Error al insertar las comisiones",
                ex
            );

            return (false, $"Error al insertar las comisiones: {ex.Message}");
        }
        finally
        {
            transaction?.Dispose();
            connection?.Dispose();
        }
    }
        
    public async Task<(bool Success, string Mensaje)> SaveAdministracionBonoCompleto(string LogTransaccionId, string Usuario, List<ItemBonoCompleto> data)
    {
        string nombreMetodo = "SaveAdministracionBonoCompleto()";

        if (data == null || data.Count == 0)
            return (false, "No existen registros para guardar.");

        const int batchSize = 1000;

        const string nextIdQuery = @"
            SELECT IFNULL(MAX(id), 0) 
            FROM t_bonocompleto;
        ";

        _log.Info(
            LogTransaccionId,
            NOMBREARCHIVO,
            nombreMetodo,
            $"Inicio de metodo [usuario:{Usuario}, cantidad:{data.Count}]"
        );

        try
        {
            using var connection = _context.CreateConnection();

            connection.Open();

            using var transaction = connection.BeginTransaction();

            int nextId = await connection.ExecuteScalarAsync<int>(
                nextIdQuery,
                transaction: transaction
            );

            int totalInsertados = 0;

            for (int i = 0; i < data.Count; i += batchSize)
            {
                var batch = data
                    .Skip(i)
                    .Take(batchSize)
                    .ToList();

                var sql = new StringBuilder();

                sql.Append(@"
                    INSERT INTO t_bonocompleto
                    (
                        id,
                        lbonocompleto,
                        fecha,
                        generacion,
                        padre_lcontacto_id,
                        lciclo_id,
                        lcontacto_id,
                        cedulaidentidad,
                        proyecto,
                        bono,
                        porcentaje,
                        pagar,
                        cantidad
                    )
                    VALUES
                ");

                var parameters = new DynamicParameters();

                for (int j = 0; j < batch.Count; j++)
                {
                    nextId++;

                    var item = batch[j];

                    sql.Append($@"
                    (
                        @Id{j},
                        0,
                        NOW(),
                        @Nivel{j},
                        @LContactoId{j},
                        @LCicloId{j},
                        @LContactoIdHijo{j},
                        @DocumentoHijo{j},
                        COALESCE((SELECT complejo_id FROM empresa_complejo WHERE id_almacen_conexion = @LComplejoId{j} LIMIT 1),0),
                        @TotalBono{j},
                        1,
                        @TotalPago{j},
                        @Cantidad{j}
                    )");

                    if (j < batch.Count - 1)
                        sql.Append(",");

                    parameters.Add($"Id{j}", nextId);
                    parameters.Add($"Nivel{j}", item.Nivel);
                    parameters.Add($"LContactoId{j}", item.LContactoId);
                    parameters.Add($"LCicloId{j}", item.LCicloId);
                    parameters.Add($"LContactoIdHijo{j}", item.LContactoIdHijo);
                    parameters.Add($"DocumentoHijo{j}", item.DocumentoHijo);
                    parameters.Add($"LComplejoId{j}", item.LComplejoId);
                    parameters.Add($"TotalBono{j}", item.TotalBono);
                    parameters.Add($"TotalPago{j}", item.TotalPago);
                    parameters.Add($"Cantidad{j}", item.Cantidad);
                }

                sql.Append(";");

                int rows = await connection.ExecuteAsync(
                    sql.ToString(),
                    parameters,
                    transaction
                );

                totalInsertados += rows;
            }

            transaction.Commit();

            string mensaje = $"Registros guardados correctamente. Total insertado: {totalInsertados}";

            _log.Info(
                LogTransaccionId,
                NOMBREARCHIVO,
                nombreMetodo,
                $"Fin de metodo [mensaje:{mensaje}, rowsAffected:{totalInsertados}]"
            );

            return (true, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(
                LogTransaccionId,
                NOMBREARCHIVO,
                nombreMetodo,
                "Error al insertar las comisiones",
                ex
            );

            return (false, $"Error al insertar las comisiones: {ex.Message}");
        }
    }
    public async Task<(bool Success, string Mensaje)> SaveAdministracionRedEmpresaComplejo(
        string LogTransaccionId,
        string Usuario,
        List<ItemRedEmpresaComplejo> data)
    {
        string nombreMetodo = "SaveAdministracionRedEmpresaComplejo()";

        if (data == null || data.Count == 0)
            return (false, "No existen registros para guardar.");

        const int batchSize = 1000;

        const string nextIdQuery = @"
            SELECT IFNULL(MAX(lredempresacomplejo_id), 0) 
            FROM administracionredempresacomplejo;
        ";

        try
        {
            using var connection = _context.CreateConnection();

            connection.Open();

            using var transaction = connection.BeginTransaction();

            int nextId = await connection.ExecuteScalarAsync<int>(
                nextIdQuery,
                transaction: transaction
            );

            int totalInsertados = 0;

            for (int i = 0; i < data.Count; i += batchSize)
            {
                var batch = data
                    .Skip(i)
                    .Take(batchSize)
                    .ToList();

                var sql = new StringBuilder();

                sql.Append(@"
                    INSERT INTO administracionredempresacomplejo
                    (
                        susuarioadd,
                        dtfechaadd,
                        susuariomod,
                        dtfechamod,
                        lredempresacomplejo_id,
                        lciclo_id,
                        lcontacto_id,
                        lcomplejo_id,
                        dmonto
                    )
                    VALUES
                ");

                var parameters = new DynamicParameters();

                for (int j = 0; j < batch.Count; j++)
                {
                    nextId++;

                    var item = batch[j];

                    sql.Append($@"
                    (
                        @Usuario{j},
                        NOW(),
                        @Usuario{j},
                        NOW(),
                        @LRedEmpresaComplejoId{j},
                        @LCicloId{j},
                        @LContactoId{j},
                        COALESCE((SELECT complejo_id FROM empresa_complejo WHERE id_almacen_conexion = @LComplejoId{j} LIMIT 1),0),
                        @DMonto{j}
                    )");

                    if (j < batch.Count - 1)
                        sql.Append(",");

                    parameters.Add($"Usuario{j}", Usuario);
                    parameters.Add($"LRedEmpresaComplejoId{j}", nextId);
                    parameters.Add($"LCicloId{j}", item.LCicloId);
                    parameters.Add($"LContactoId{j}", item.LContactoId);
                    parameters.Add($"LComplejoId{j}", item.LComplejoId);
                    parameters.Add($"DMonto{j}", item.DMonto);
                }

                sql.Append(";");

                int rows = await connection.ExecuteAsync(
                    sql.ToString(),
                    parameters,
                    transaction
                );

                totalInsertados += rows;
            }

            transaction.Commit();

            string mensaje = $"Registros guardados correctamente. Total insertado: {totalInsertados}";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Fin de metodo [mensaje:{mensaje}, rowsAffected:{totalInsertados}]");

            return (true, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(
                LogTransaccionId,
                NOMBREARCHIVO,
                nombreMetodo,
                "Error al insertar las comisiones",
                ex
            );

            return (false, $"Error al insertar las comisiones: {ex.Message}");
        }
    }
}
