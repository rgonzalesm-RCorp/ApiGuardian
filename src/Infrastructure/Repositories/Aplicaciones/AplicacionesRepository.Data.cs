using System.Data;
using System.Security;
using System.Text;
using System.Xml.Linq;
using ApiGuardian.Application.Interfaces;
using Dapper;

namespace ApiGuardian.Infrastructure.Repositories;

public partial class AplicacionesRepository
{
    private async Task<AplicacionesResultado> ValidateConnectionsAsync()
    {
        var guardian = await ValidateGuardianConnectionAsync();
        if (!guardian.Success)
        {
            return guardian;
        }

        return await ValidateSqlConnectionAsync();
    }

    private async Task<AplicacionesResultado> ValidateGuardianConnectionAsync()
    {
        try
        {
            using var connection = _guardianContext.CreateConnection();
            await connection.ExecuteScalarAsync<int>(
                new CommandDefinition("SELECT 1;", commandTimeout: _settings.CommandTimeoutSeconds)
            );

            return AplicacionesResultado.Ok("Conexion Guardian validada.");
        }
        catch (Exception ex)
        {
            return AplicacionesResultado.Fail($"No existe conexion con la base de datos Guardian: {ex.Message}", true);
        }
    }

    private async Task<AplicacionesResultado> ValidateSqlConnectionAsync()
    {
        try
        {
            using var connection = _sqlContext.CreateConnection();
            await connection.ExecuteScalarAsync<int>(
                new CommandDefinition("SELECT 1;", commandTimeout: _settings.CommandTimeoutSeconds)
            );

            return AplicacionesResultado.Ok("Conexion SQL Server validada.");
        }
        catch (Exception ex)
        {
            return AplicacionesResultado.Fail($"No existe conexion con la base de datos Conexion: {ex.Message}", true);
        }
    }

    private async Task<AplicacionesResultado> LoadLatestCommissionDataAsync()
    {
        try
        {
            using var connection = _guardianContext.CreateConnection();
            await connection.ExecuteAsync(
                new CommandDefinition(
                    "CALL RetencionEmpresa();",
                    commandTimeout: _settings.CommandTimeoutSeconds
                )
            );

            return AplicacionesResultado.Ok("Carga de datos de comision ejecutada correctamente.");
        }
        catch (Exception ex)
        {
            return AplicacionesResultado.Fail($"No se pudo cargar los datos de comision en Guardian: {ex.Message}", true);
        }
    }

    private async Task<AplicacionesResultado> ClearCycleDataAsync(int cycle)
    {
        var clearGuardianResult = await ClearGuardianCycleDataAsync(cycle);
        if (!clearGuardianResult.Success)
        {
            return clearGuardianResult;
        }

        return await ClearBdQishurCycleDataAsync(cycle);
    }

    private async Task<AplicacionesResultado> ClearGuardianCycleDataAsync(int cycle)
    {
        try
        {
            using var connection = _guardianContext.CreateConnection();
            if (connection is IDbConnection dbConnection && dbConnection.State != ConnectionState.Open)
            {
                dbConnection.Open();
            }

            using var transaction = connection.BeginTransaction();
            await connection.ExecuteAsync(
                new CommandDefinition(
                    SqlDeleteGuardianRetencionEmpresaExterior,
                    new { Cycle = cycle },
                    transaction,
                    _settings.CommandTimeoutSeconds
                )
            );
            await connection.ExecuteAsync(
                new CommandDefinition(
                    SqlDeleteGuardianRetencionEmpresa,
                    new { Cycle = cycle },
                    transaction,
                    _settings.CommandTimeoutSeconds
                )
            );
            transaction.Commit();

            return AplicacionesResultado.Ok("Datos por ciclo eliminados de grdsion.");
        }
        catch (Exception ex)
        {
            return AplicacionesResultado.Fail(
                $"No se pudo limpiar la informacion de grdsion para el ciclo {cycle}: {ex.Message}",
                true
            );
        }
    }

    private async Task<AplicacionesResultado> ClearBdQishurCycleDataAsync(int cycle)
    {
        try
        {
            using var connection = _sqlContext.CreateConnection();
            if (connection is IDbConnection dbConnection && dbConnection.State != ConnectionState.Open)
            {
                dbConnection.Open();
            }

            using var transaction = connection.BeginTransaction();
            await connection.ExecuteAsync(
                new CommandDefinition(
                    SqlDeleteAplicacionesProrrateo,
                    new { Cycle = cycle },
                    transaction,
                    _settings.CommandTimeoutSeconds
                )
            );
            await connection.ExecuteAsync(
                new CommandDefinition(
                    SqlDeleteAplicacionesPagos,
                    new { Cycle = cycle },
                    transaction,
                    _settings.CommandTimeoutSeconds
                )
            );
            await connection.ExecuteAsync(
                new CommandDefinition(
                    SqlDeleteAplicacionesComisionado,
                    new { Cycle = cycle },
                    transaction,
                    _settings.CommandTimeoutSeconds
                )
            );
            await connection.ExecuteAsync(
                new CommandDefinition(
                    SqlDeleteAplicacionesComisionPorEmpresa,
                    new { Cycle = cycle },
                    transaction,
                    _settings.CommandTimeoutSeconds
                )
            );
            transaction.Commit();

            return AplicacionesResultado.Ok("Datos por ciclo eliminados de BDQISHUR.");
        }
        catch (Exception ex)
        {
            return AplicacionesResultado.Fail(
                $"No se pudo limpiar la informacion de BDQISHUR para el ciclo {cycle}: {ex.Message}",
                true
            );
        }
    }

    private async Task<AplicacionesResultado> LoadMissingPrioritiesAsync()
    {
        try
        {
            using var connection = _sqlContext.CreateConnection();
            await connection.ExecuteAsync(
                new CommandDefinition(
                    SqlInsertMissingPriorities,
                    commandTimeout: _settings.CommandTimeoutSeconds
                )
            );

            return AplicacionesResultado.Ok("Prioridades faltantes sincronizadas correctamente.");
        }
        catch (Exception ex)
        {
            return AplicacionesResultado.Fail(
                $"No se pudo cargar correctamente las prioridades faltantes: {ex.Message}",
                true
            );
        }
    }

    private async Task<AplicacionesResultado<bool>> ExistsCompanyCommissionsAsync(int cycle)
    {
        try
        {
            using var connection = _sqlContext.CreateConnection();
            var count = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    SqlExistsAplicacionesComisionPorEmpresa,
                    new { Cycle = cycle },
                    commandTimeout: _settings.CommandTimeoutSeconds
                )
            );

            return AplicacionesResultado<bool>.Ok(count > 0);
        }
        catch (Exception ex)
        {
            return AplicacionesResultado<bool>.Fail(
                $"No se pudo verificar AplicacionesComisionPorEmpresa para el ciclo {cycle}: {ex.Message}",
                true
            );
        }
    }

    private async Task<AplicacionesResultado> SyncCompanyCommissionsAsync(int cycle)
    {
        var sourceRowsResult = await BuildCompanyCommissionRowsFromSourceAsync(cycle);
        if (!sourceRowsResult.Success || sourceRowsResult.Data is null)
        {
            return AplicacionesResultado.Fail(sourceRowsResult.Message, sourceRowsResult.IsFatal);
        }

        if (sourceRowsResult.Data.Count == 0)
        {
            return AplicacionesResultado.Fail("Guardian no devolvio comisiones por empresa para sincronizar.", true);
        }

        try
        {
            using var connection = _sqlContext.CreateConnection();
            if (connection is IDbConnection dbConnection && dbConnection.State != ConnectionState.Open)
            {
                dbConnection.Open();
            }

            using var transaction = connection.BeginTransaction();
            await connection.ExecuteAsync(
                new CommandDefinition(
                    SqlInsertCompanyCommission,
                    sourceRowsResult.Data,
                    transaction,
                    _settings.CommandTimeoutSeconds
                )
            );
            transaction.Commit();

            return AplicacionesResultado.Ok("Comisiones por empresa sincronizadas correctamente.");
        }
        catch (Exception ex)
        {
            return AplicacionesResultado.Fail($"No se pudo sincronizar comisiones por empresa: {ex.Message}", true);
        }
    }

    private async Task<AplicacionesResultado<List<AplicacionesCompanyCommissionInsertRow>>> BuildCompanyCommissionRowsFromSourceAsync(int cycle)
    {
        try
        {
            using var sqlConnection = _sqlContext.CreateConnection();
            var mappings = (
                await sqlConnection.QueryAsync<AplicacionesCompanyMappingRow>(
                    new CommandDefinition(
                        SqlCompanyMapping,
                        commandTimeout: _settings.CommandTimeoutSeconds
                    )
                )
            ).ToDictionary(item => item.LegacyCompanyId);

            using var guardianConnection = _guardianContext.CreateConnection();
            var guardianRows = (
                await guardianConnection.QueryAsync<AplicacionesGuardianCompanyCommissionRow>(
                    new CommandDefinition(
                        SqlGuardianCompanyCommission,
                        new { Cycle = cycle },
                        commandTimeout: _settings.CommandTimeoutSeconds
                    )
                )
            ).ToList();

            var records = new List<AplicacionesCompanyCommissionInsertRow>();
            foreach (var row in guardianRows)
            {
                if (!mappings.TryGetValue(row.LegacyCompanyId, out var mapping))
                {
                    return AplicacionesResultado<List<AplicacionesCompanyCommissionInsertRow>>.Fail(
                        $"No existe la empresa de Conexion que asume la empresa Guardian {row.LegacyCompanyId}.",
                        true
                    );
                }

                var thirteenPercent = row.InvoiceFlag == 1
                    ? decimal.Round(row.TotalCommission * 0.13m, 2, MidpointRounding.AwayFromZero)
                    : 0m;

                records.Add(
                    new AplicacionesCompanyCommissionInsertRow
                    {
                        GrossAmount = row.InvoiceFlag == 1 ? row.TotalCommission - thirteenPercent : 0m,
                        DocumentNumber = row.DocumentNumber.Trim(),
                        Cycle = cycle,
                        CompanyId = mapping.CompanyId,
                        InvoiceFlag = row.InvoiceFlag,
                        CommissionAmount = row.CommissionAmount,
                        NetAmount = row.TotalCommission,
                        ThirteenPercentAmount = thirteenPercent,
                        RetentionAmount = row.RetentionAmount,
                        Residual = row.Residual,
                        GroupSales = row.GroupSales,
                        PersonalSales = row.PersonalSales,
                        LegacyCompanyId = row.LegacyCompanyId
                    }
                );
            }

            return AplicacionesResultado<List<AplicacionesCompanyCommissionInsertRow>>.Ok(records);
        }
        catch (Exception ex)
        {
            return AplicacionesResultado<List<AplicacionesCompanyCommissionInsertRow>>.Fail(
                $"No se pudo construir las comisiones por empresa desde Guardian: {ex.Message}",
                true
            );
        }
    }

    private async Task<AplicacionesResultado<List<AplicacionesGuardianCommissionAgent>>> GetGuardianCommissionAgentsAsync(int cycle)
    {
        try
        {
            using var connection = _guardianContext.CreateConnection();
            var rows = (
                await connection.QueryAsync<AplicacionesGuardianCommissionAgent>(
                    new CommandDefinition(
                        SqlGuardianCommissionAgents,
                        new { Cycle = cycle },
                        commandTimeout: _settings.CommandTimeoutSeconds
                    )
                )
            ).ToList();

            return AplicacionesResultado<List<AplicacionesGuardianCommissionAgent>>.Ok(rows);
        }
        catch (Exception ex)
        {
            return AplicacionesResultado<List<AplicacionesGuardianCommissionAgent>>.Fail(
                $"No se pudo obtener el listado de comisionados desde Guardian: {ex.Message}",
                true
            );
        }
    }

    private async Task<AplicacionesResultado<bool>> ExistsCommissionedAgentsAsync(int cycle)
    {
        try
        {
            using var connection = _sqlContext.CreateConnection();
            var count = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    SqlExistsAplicacionesComisionado,
                    new { Cycle = cycle },
                    commandTimeout: _settings.CommandTimeoutSeconds
                )
            );

            return AplicacionesResultado<bool>.Ok(count > 0);
        }
        catch (Exception ex)
        {
            return AplicacionesResultado<bool>.Fail(
                $"No se pudo verificar AplicacionesComisionado para el ciclo {cycle}: {ex.Message}",
                true
            );
        }
    }

    private async Task<AplicacionesResultado> RegisterCommissionedAgentsAsync(IEnumerable<AplicacionesCommissionAgent> agents)
    {
        var rowsToInsert = agents.ToList();

        try
        {
            using var connection = _sqlContext.CreateConnection();
            if (connection is IDbConnection dbConnection && dbConnection.State != ConnectionState.Open)
            {
                dbConnection.Open();
            }

            using var transaction = connection.BeginTransaction();
            var inserted = await connection.ExecuteAsync(
                new CommandDefinition(
                    SqlInsertAplicacionesComisionado,
                    rowsToInsert,
                    transaction,
                    _settings.CommandTimeoutSeconds
                )
            );
            transaction.Commit();

            return inserted == rowsToInsert.Count
                ? AplicacionesResultado.Ok($"Se registraron {inserted} comisionados en AplicacionesComisionado.")
                : AplicacionesResultado.Fail(
                    $"Se registraron {inserted} de {rowsToInsert.Count} comisionados en AplicacionesComisionado.",
                    true
                );
        }
        catch (Exception ex)
        {
            return AplicacionesResultado.Fail($"No se pudo registrar AplicacionesComisionado: {ex.Message}", true);
        }
    }

    private async Task<AplicacionesResultado<List<AplicacionesPendingCommissionAgent>>> GetPendingCommissionedAgentsAsync(int cycle)
    {
        try
        {
            using var connection = _sqlContext.CreateConnection();
            var rows = (
                await connection.QueryAsync<AplicacionesPendingCommissionAgent>(
                    new CommandDefinition(
                        SqlPendingComisionados,
                        new { Cycle = cycle },
                        commandTimeout: _settings.CommandTimeoutSeconds
                    )
                )
            ).ToList();

            return AplicacionesResultado<List<AplicacionesPendingCommissionAgent>>.Ok(rows);
        }
        catch (Exception ex)
        {
            return AplicacionesResultado<List<AplicacionesPendingCommissionAgent>>.Fail(
                $"No se pudo obtener el listado pendiente de pago: {ex.Message}",
                true
            );
        }
    }

    private async Task<AplicacionesResultado> MarkProcessedAsync(int cycle, string documentNumber)
    {
        try
        {
            using var connection = _sqlContext.CreateConnection();
            var rows = await connection.ExecuteAsync(
                new CommandDefinition(
                    SqlMarkComisionadoProcessed,
                    new
                    {
                        Cycle = cycle,
                        DocumentNumber = documentNumber.Trim()
                    },
                    commandTimeout: _settings.CommandTimeoutSeconds
                )
            );

            return rows > 0
                ? AplicacionesResultado.Ok("Comisionado marcado como procesado.")
                : AplicacionesResultado.Fail("No se pudo marcar el comisionado como procesado.", true);
        }
        catch (Exception ex)
        {
            return AplicacionesResultado.Fail($"No se pudo marcar el comisionado como procesado: {ex.Message}", true);
        }
    }

    private async Task<AplicacionesResultado<Dictionary<string, decimal>>> GetCompanyTotalsByDocumentAsync(int cycle)
    {
        try
        {
            using var connection = _sqlContext.CreateConnection();
            var rows = await connection.QueryAsync<AplicacionesCommissionTotalRow>(
                new CommandDefinition(
                    SqlCompanyTotalsByDocument,
                    new { Cycle = cycle },
                    commandTimeout: _settings.CommandTimeoutSeconds
                )
            );

            return AplicacionesResultado<Dictionary<string, decimal>>.Ok(
                rows.ToDictionary(
                    item => item.DocumentNumber.Trim(),
                    item => item.TotalToApply,
                    StringComparer.OrdinalIgnoreCase
                )
            );
        }
        catch (Exception ex)
        {
            return AplicacionesResultado<Dictionary<string, decimal>>.Fail(
                $"No se pudo obtener el total de comisiones por documento: {ex.Message}",
                true
            );
        }
    }

    private async Task<AplicacionesResultado<List<AplicacionesCompanyCommission>>> GetCompanyCommissionsByDocumentAsync(int cycle, string documentNumber)
    {
        try
        {
            using var connection = _sqlContext.CreateConnection();
            var rows = (
                await connection.QueryAsync<AplicacionesCompanyCommission>(
                    new CommandDefinition(
                        SqlCompanyCommissionByDocument,
                        new
                        {
                            Cycle = cycle,
                            DocumentNumber = documentNumber.Trim()
                        },
                        commandTimeout: _settings.CommandTimeoutSeconds
                    )
                )
            ).ToList();

            return AplicacionesResultado<List<AplicacionesCompanyCommission>>.Ok(rows);
        }
        catch (Exception ex)
        {
            return AplicacionesResultado<List<AplicacionesCompanyCommission>>.Fail(
                $"No se pudo obtener el detalle de comisiones por empresa: {ex.Message}",
                true
            );
        }
    }

    private async Task<AplicacionesResultado<List<AplicacionesProductAccount>>> GetProductPortfolioAsync(string documentNumber)
    {
        try
        {
            using var connection = _sqlContext.CreateConnection();
            var rows = (
                await connection.QueryAsync<AplicacionesProductAccount>(
                    new CommandDefinition(
                        SqlPortfolio,
                        new { DocumentNumber = documentNumber.Trim() },
                        commandTimeout: _settings.CommandTimeoutSeconds
                    )
                )
            ).ToList();

            return AplicacionesResultado<List<AplicacionesProductAccount>>.Ok(rows);
        }
        catch (Exception ex)
        {
            return AplicacionesResultado<List<AplicacionesProductAccount>>.Fail(
                $"No se pudo obtener la cartera de productos del documento {documentNumber}: {ex.Message}",
                true
            );
        }
    }

    private async Task<AplicacionesResultado<bool>> IsProductPaidOffAsync(string beneficiaryDocument, string lotCode)
    {
        try
        {
            using var connection = _sqlContext.CreateConnection();
            var count = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    SqlProductPaidOff,
                    new
                    {
                        DocumentNumber = beneficiaryDocument.Trim(),
                        LotCode = lotCode.Trim()
                    },
                    commandTimeout: _settings.CommandTimeoutSeconds
                )
            );

            return AplicacionesResultado<bool>.Ok(count > 0);
        }
        catch (Exception ex)
        {
            return AplicacionesResultado<bool>.Fail(
                $"No se pudo verificar si el producto {lotCode} esta pagado: {ex.Message}",
                true
            );
        }
    }

    private async Task<AplicacionesResultado<HashSet<string>>> GetReprogrammedProductKeysAsync(string documentNumber)
    {
        try
        {
            using var connection = _sqlContext.CreateConnection();
            var rows = await connection.QueryAsync<AplicacionesReprogrammedProductRow>(
                new CommandDefinition(
                    SqlReprogrammedProducts,
                    new { DocumentNumber = documentNumber.Trim() },
                    commandTimeout: _settings.CommandTimeoutSeconds
                )
            );

            return AplicacionesResultado<HashSet<string>>.Ok(
                rows.Select(item => $"{item.ClientId}:{item.ProductId.Trim()}").ToHashSet(StringComparer.OrdinalIgnoreCase)
            );
        }
        catch (Exception ex)
        {
            return AplicacionesResultado<HashSet<string>>.Fail(
                $"No se pudo obtener la lista de productos reprogramados del documento {documentNumber}: {ex.Message}",
                true
            );
        }
    }

    private async Task<AplicacionesResultado<List<AplicacionesInstallmentQuote>>> GetInstallmentQuotesAsync(
        int companyId,
        int saleId,
        DateTime paymentDate,
        int quotaCount
    )
    {
        var companyResult = await GetCompanyDatabaseAsync(companyId);
        if (!companyResult.Success || companyResult.Data is null)
        {
            return AplicacionesResultado<List<AplicacionesInstallmentQuote>>.Fail(
                companyResult.Message,
                companyResult.IsFatal
            );
        }

        try
        {
            var query = BuildInstallmentQuotesQuery(companyResult.Data.DatabaseName);
            using var connection = _sqlContext.CreateConnection();
            var rows = (
                await connection.QueryAsync<AplicacionesInstallmentQuote>(
                    new CommandDefinition(
                        query,
                        new
                        {
                            SaleId = saleId,
                            PaymentDate = paymentDate.ToString("yyyyMMdd"),
                            QuotaCount = quotaCount
                        },
                        commandTimeout: _settings.CommandTimeoutSeconds
                    )
                )
            )
                .Where(item =>
                    item.PaymentAmount != 0
                    && (item.Capital + item.Interest + item.InterestPenalty + item.Insurance + item.Expense + item.Penalty) > 0
                )
                .ToList();

            return AplicacionesResultado<List<AplicacionesInstallmentQuote>>.Ok(rows);
        }
        catch (Exception ex)
        {
            return AplicacionesResultado<List<AplicacionesInstallmentQuote>>.Fail(
                $"No se pudo obtener el detalle de cuotas para la venta {saleId}: {ex.Message}",
                true
            );
        }
    }

    private async Task<AplicacionesResultado<List<AplicacionesLetterInstruction>>> GetLettersAsync(string commissionerDocument)
    {
        try
        {
            using var connection = _sqlContext.CreateConnection();
            var rows = (
                await connection.QueryAsync<AplicacionesLetterInstruction>(
                    new CommandDefinition(
                        SqlLetters,
                        new { CommissionerDocument = commissionerDocument.Trim() },
                        commandTimeout: _settings.CommandTimeoutSeconds
                    )
                )
            ).ToList();

            return AplicacionesResultado<List<AplicacionesLetterInstruction>>.Ok(rows);
        }
        catch (Exception ex)
        {
            return AplicacionesResultado<List<AplicacionesLetterInstruction>>.Fail(
                $"No se pudieron obtener las cartas del comisionado {commissionerDocument}: {ex.Message}",
                true
            );
        }
    }

    private async Task<AplicacionesResultado<List<AplicacionesDiscountDefinition>>> GetActiveDiscountsAsync()
    {
        try
        {
            using var connection = _sqlContext.CreateConnection();
            var rows = (
                await connection.QueryAsync<AplicacionesDiscountDefinition>(
                    new CommandDefinition(
                        SqlActiveDiscounts,
                        commandTimeout: _settings.CommandTimeoutSeconds
                    )
                )
            ).ToList();

            return AplicacionesResultado<List<AplicacionesDiscountDefinition>>.Ok(rows);
        }
        catch (Exception ex)
        {
            return AplicacionesResultado<List<AplicacionesDiscountDefinition>>.Fail(
                $"No se pudieron obtener los descuentos activos: {ex.Message}",
                true
            );
        }
    }

    private async Task<AplicacionesResultado<AplicacionesCustomerRecord>> GetCustomerByDocumentAsync(string documentNumber)
    {
        try
        {
            using var connection = _sqlContext.CreateConnection();
            var row = await connection.QueryFirstOrDefaultAsync<AplicacionesCustomerRecord>(
                new CommandDefinition(
                    SqlCustomerByDocument,
                    new { DocumentNumber = documentNumber.Trim() },
                    commandTimeout: _settings.CommandTimeoutSeconds
                )
            );

            return row is null
                ? AplicacionesResultado<AplicacionesCustomerRecord>.Fail(
                    $"No se encontro el cliente con documento {documentNumber}.",
                    true
                )
                : AplicacionesResultado<AplicacionesCustomerRecord>.Ok(row);
        }
        catch (Exception ex)
        {
            return AplicacionesResultado<AplicacionesCustomerRecord>.Fail(
                $"No se pudo obtener el cliente por documento {documentNumber}: {ex.Message}",
                true
            );
        }
    }

    private async Task<AplicacionesResultado<AplicacionesCompanyDatabase>> GetCompanyDatabaseAsync(int companyId)
    {
        try
        {
            using var connection = _sqlContext.CreateConnection();
            var row = await connection.QueryFirstOrDefaultAsync<AplicacionesCompanyDatabase>(
                new CommandDefinition(
                    SqlCompanyDatabase,
                    new { CompanyId = companyId },
                    commandTimeout: _settings.CommandTimeoutSeconds
                )
            );

            return row is null
                ? AplicacionesResultado<AplicacionesCompanyDatabase>.Fail(
                    $"No se encontro la configuracion de empresa para {companyId}.",
                    true
                )
                : AplicacionesResultado<AplicacionesCompanyDatabase>.Ok(row);
        }
        catch (Exception ex)
        {
            return AplicacionesResultado<AplicacionesCompanyDatabase>.Fail(
                $"No se pudo obtener el catalogo de base de datos para la empresa {companyId}: {ex.Message}",
                true
            );
        }
    }

    private async Task<AplicacionesResultado<List<AplicacionesPaymentRecord>>> GetPaymentsByCycleAsync(int cycle)
    {
        try
        {
            using var connection = _sqlContext.CreateConnection();
            var rows = (
                await connection.QueryAsync<AplicacionesPaymentRecord>(
                    new CommandDefinition(
                        SqlPaymentsByCycle,
                        new { Cycle = cycle },
                        commandTimeout: _settings.CommandTimeoutSeconds
                    )
                )
            ).ToList();

            return AplicacionesResultado<List<AplicacionesPaymentRecord>>.Ok(rows);
        }
        catch (Exception ex)
        {
            return AplicacionesResultado<List<AplicacionesPaymentRecord>>.Fail(
                $"No se pudo obtener AplicacionesPagos del ciclo {cycle}: {ex.Message}",
                true
            );
        }
    }

    private async Task<AplicacionesResultado<int>> RegisterPaymentReceiptAsync(AplicacionesPaymentRecord record)
    {
        try
        {
            using var connection = _sqlContext.CreateConnection();
            var id = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    SqlInsertPaymentReceipt,
                    record,
                    commandTimeout: _settings.CommandTimeoutSeconds
                )
            );

            return AplicacionesResultado<int>.Ok(id);
        }
        catch (Exception ex)
        {
            return AplicacionesResultado<int>.Fail(
                $"No se pudo registrar el recibo en AplicacionesPagos: {ex.Message}",
                true
            );
        }
    }

    private async Task<AplicacionesResultado> UpdateInvoiceAsync(
        int companyId,
        int saleId,
        int receiptId,
        int invoiceId,
        string observation
    )
    {
        try
        {
            using var connection = _sqlContext.CreateConnection();
            await connection.ExecuteAsync(
                new CommandDefinition(
                    SqlUpdateInvoice,
                    new
                    {
                        CompanyId = companyId,
                        SaleId = saleId,
                        ReceiptId = receiptId,
                        InvoiceId = invoiceId,
                        Observation = observation
                    },
                    commandTimeout: _settings.CommandTimeoutSeconds
                )
            );

            return AplicacionesResultado.Ok();
        }
        catch (Exception ex)
        {
            return AplicacionesResultado.Fail($"No se pudo actualizar la factura del recibo {receiptId}: {ex.Message}", true);
        }
    }

    private async Task<AplicacionesResultado<int>> CountInvoiceFailuresAsync(int cycle)
    {
        try
        {
            using var connection = _sqlContext.CreateConnection();
            var count = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    SqlCountInvoiceFailures,
                    new { Cycle = cycle },
                    commandTimeout: _settings.CommandTimeoutSeconds
                )
            );

            return AplicacionesResultado<int>.Ok(count);
        }
        catch (Exception ex)
        {
            return AplicacionesResultado<int>.Fail(
                $"No se pudo contar los errores de facturacion del ciclo {cycle}: {ex.Message}",
                true
            );
        }
    }

    private async Task<AplicacionesResultado<List<AplicacionesProrationEntry>>> GetActiveProrationsAsync(int cycle, string documentNumber)
    {
        try
        {
            using var connection = _sqlContext.CreateConnection();
            var rows = (
                await connection.QueryAsync<AplicacionesProrationEntry>(
                    new CommandDefinition(
                        SqlActiveProrations,
                        new
                        {
                            Cycle = cycle,
                            DocumentNumber = documentNumber.Trim()
                        },
                        commandTimeout: _settings.CommandTimeoutSeconds
                    )
                )
            ).ToList();

            return AplicacionesResultado<List<AplicacionesProrationEntry>>.Ok(rows);
        }
        catch (Exception ex)
        {
            return AplicacionesResultado<List<AplicacionesProrationEntry>>.Fail(
                $"No se pudo obtener el prorrateo activo del documento {documentNumber}: {ex.Message}",
                true
            );
        }
    }

    private async Task<AplicacionesResultado> DisableProrationsAsync(int cycle, string documentNumber)
    {
        try
        {
            using var connection = _sqlContext.CreateConnection();
            await connection.ExecuteAsync(
                new CommandDefinition(
                    SqlDisableProrations,
                    new
                    {
                        Cycle = cycle,
                        DocumentNumber = documentNumber.Trim()
                    },
                    commandTimeout: _settings.CommandTimeoutSeconds
                )
            );

            return AplicacionesResultado.Ok();
        }
        catch (Exception ex)
        {
            return AplicacionesResultado.Fail(
                $"No se pudo deshabilitar el prorrateo activo del documento {documentNumber}: {ex.Message}",
                true
            );
        }
    }

    private async Task<AplicacionesResultado<int>> InsertProrationAsync(AplicacionesProrationEntry entry)
    {
        try
        {
            using var connection = _sqlContext.CreateConnection();
            var id = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    SqlInsertProration,
                    entry,
                    commandTimeout: _settings.CommandTimeoutSeconds
                )
            );

            return AplicacionesResultado<int>.Ok(id);
        }
        catch (Exception ex)
        {
            return AplicacionesResultado<int>.Fail(
                $"No se pudo registrar el prorrateo del documento {entry.ClientDocument}: {ex.Message}"
            );
        }
    }

    private async Task<AplicacionesResultado<int>> ExecuteSionPaymentAsync(
        AplicacionesCompanyDatabase company,
        int saleId,
        DateTime paymentDate,
        string externalTransactionNumber,
        decimal amountToPay
    )
    {
        try
        {
            var query = BuildSionPaymentProcedureQuery(company.DatabaseName);
            var parameters = new DynamicParameters();
            parameters.Add("SaleId", saleId);
            parameters.Add("PaymentDate", paymentDate.ToString("yyyyMMdd"));
            parameters.Add("ExternalTransactionNumber", externalTransactionNumber);
            parameters.Add("InstallmentsToPay", 1);
            parameters.Add("AmountToPay", amountToPay);
            parameters.Add("AgentCode", -13);
            parameters.Add("MyId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            using var connection = _sqlContext.CreateConnection();
            await connection.ExecuteAsync(
                new CommandDefinition(
                    query,
                    parameters,
                    commandTimeout: _settings.PaymentCommandTimeoutSeconds
                )
            );

            var receiptId = parameters.Get<int>("MyId");
            return receiptId <= 0
                ? AplicacionesResultado<int>.Fail("El procedimiento almacenado no devolvio un recibo valido.")
                : AplicacionesResultado<int>.Ok(receiptId);
        }
        catch (Exception ex)
        {
            return AplicacionesResultado<int>.Fail($"No se pudo ejecutar spPagarCuotasXVenta: {ex.Message}");
        }
    }

    private async Task<AplicacionesResultado<AplicacionesInvoiceResult>> GenerateInvoiceAsync(
        int companyWebServiceId,
        int projectId,
        int saleId,
        int receiptId,
        string productId
    )
    {
        if (!_settings.EnableInvoiceGateway)
        {
            return AplicacionesResultado<AplicacionesInvoiceResult>.Ok(
                new AplicacionesInvoiceResult
                {
                    InvoiceId = 0,
                    ServiceCode = 200,
                    ErrorCode = 0,
                    ServiceMessage = "Facturacion deshabilitada por configuracion.",
                    Succeeded = true
                }
            );
        }

        if (string.IsNullOrWhiteSpace(_settings.Facturacion.Endpoint))
        {
            return AplicacionesResultado<AplicacionesInvoiceResult>.Fail("No se configuro el endpoint del servicio de facturacion.");
        }

        var requestXml = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <soap:Envelope
                xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"
                xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
                xmlns:xsd="http://www.w3.org/2001/XMLSchema"
                xmlns:urn="urn:gruposion.com.bo">
              <soap:Body>
                <urn:wsGenerarFacturaRecibo soap:encodingStyle="http://schemas.xmlsoap.org/soap/encoding/">
                  <login xsi:type="xsd:string">{SecurityElement.Escape(_settings.Facturacion.Login)}</login>
                  <password xsi:type="xsd:string">{SecurityElement.Escape(_settings.Facturacion.Password)}</password>
                  <codAgente xsi:type="xsd:string">{SecurityElement.Escape(_settings.Facturacion.AgentCode)}</codAgente>
                  <llaveCnx xsi:type="xsd:string">{SecurityElement.Escape(_settings.Facturacion.ConnectionKey)}</llaveCnx>
                  <codEmpresa xsi:type="xsd:int">{companyWebServiceId}</codEmpresa>
                  <codProyecto xsi:type="xsd:int">{projectId}</codProyecto>
                  <nroVenta xsi:type="xsd:int">{saleId}</nroVenta>
                  <codRecibo xsi:type="xsd:int">{receiptId}</codRecibo>
                  <codProducto xsi:type="xsd:string">{SecurityElement.Escape(productId)}</codProducto>
                </urn:wsGenerarFacturaRecibo>
              </soap:Body>
            </soap:Envelope>
            """;

        using var request = new HttpRequestMessage(HttpMethod.Post, _settings.Facturacion.Endpoint)
        {
            Content = new StringContent(requestXml, Encoding.UTF8, "text/xml")
        };
        request.Headers.Add("SOAPAction", _settings.Facturacion.SoapAction);

        try
        {
            var client = _httpClientFactory.CreateClient(nameof(AplicacionesRepository));
            client.Timeout = TimeSpan.FromSeconds(_settings.Facturacion.TimeoutSeconds);

            using var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return AplicacionesResultado<AplicacionesInvoiceResult>.Fail(
                    $"El servicio de facturacion devolvio {(int)response.StatusCode}: {response.ReasonPhrase}."
                );
            }

            var document = XDocument.Parse(body);
            var values = document
                .Descendants()
                .Where(item =>
                    item.Name.LocalName is "codServicio" or "msgServicio" or "error" or "mensajeError" or "idFactura"
                )
                .GroupBy(item => item.Name.LocalName)
                .ToDictionary(group => group.Key, group => group.First().Value);

            var result = new AplicacionesInvoiceResult
            {
                ServiceCode = TryParseInt(values, "codServicio"),
                ServiceMessage = TryGetValue(values, "msgServicio"),
                ErrorCode = TryParseInt(values, "error"),
                ErrorMessage = TryGetValue(values, "mensajeError"),
                InvoiceId = TryParseInt(values, "idFactura", -1)
            };
            result.Succeeded = result.ErrorCode == 0 && result.InvoiceId >= 0;

            return AplicacionesResultado<AplicacionesInvoiceResult>.Ok(result);
        }
        catch (Exception ex)
        {
            return AplicacionesResultado<AplicacionesInvoiceResult>.Fail(
                $"No se pudo consumir el servicio de facturacion: {ex.Message}"
            );
        }
    }

    private static int TryParseInt(IDictionary<string, string> values, string key, int defaultValue = 0)
    {
        return values.TryGetValue(key, out var raw) && int.TryParse(raw, out var parsed) ? parsed : defaultValue;
    }

    private static string TryGetValue(IDictionary<string, string> values, string key)
    {
        return values.TryGetValue(key, out var raw) ? raw : string.Empty;
    }
}

internal sealed class AplicacionesInvoiceResult
{
    public bool Succeeded { get; set; }
    public int InvoiceId { get; set; }
    public int ServiceCode { get; set; }
    public int ErrorCode { get; set; }
    public string ServiceMessage { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}
