using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text.Json;

namespace ApiGuardian.Infrastructure.Repositories;

public partial class AplicacionesRepository : IAplicacionesRepository
{
    private readonly DapperContext _guardianContext;
    private readonly DapperContextSqlServer _sqlContext;
    private readonly ILogService _log;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AplicacionesSettings _settings;
    private const string NombreArchivo = "AplicacionesRepository.cs";

    public AplicacionesRepository(
        DapperContext guardianContext,
        DapperContextSqlServer sqlContext,
        ILogService log,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory
    )
    {
        _guardianContext = guardianContext;
        _sqlContext = sqlContext;
        _log = log;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _settings = _configuration.GetSection("Aplicaciones").Get<AplicacionesSettings>() ?? new AplicacionesSettings();
    }

    public async Task<(AplicacionesPreviewResponse Data, bool Success, string Mensaje)> Preview(string logTransaccionId, int lCicloId)
    {
        var result = await RunProcessAsync(logTransaccionId, lCicloId, previewOnly: true);
        var response = BuildPreviewResponse(result.Data ?? new AplicacionesRunState(lCicloId, true));
        return (response, result.Success, result.Message);
    }

    public async Task<(AplicacionesApplyResponse Data, bool Success, string Mensaje)> Apply(string logTransaccionId, int lCicloId)
    {
        var result = await RunProcessAsync(logTransaccionId, lCicloId, previewOnly: false);
        var response = BuildApplyResponse(result.Data ?? new AplicacionesRunState(lCicloId, false));
        return (response, result.Success, result.Message);
    }

    private async Task<AplicacionesResultado<AplicacionesRunState>> RunProcessAsync(
        string logTransaccionId,
        int cycle,
        bool previewOnly
    )
    {
        var state = new AplicacionesRunState(cycle, previewOnly);
        var metodo = previewOnly ? "Preview" : "Apply";

        try
        {
            _log.Info(logTransaccionId, NombreArchivo, metodo, $"Inicio proceso aplicaciones. ciclo:{cycle}, preview:{previewOnly}");

            var validationResult = await ValidateConnectionsAsync();
            if (!validationResult.Success)
            {
                return FailState(state, validationResult.Message, validationResult.IsFatal);
            }

            if (previewOnly)
            {
                state.Notas.Add(
                    "El preview no ejecuta RetencionEmpresa(), no inserta prioridades ni sincroniza tablas; usa el estado actual y simulacion en memoria."
                );
            }
            else
            {
                var clearCycleDataResult = await ClearCycleDataAsync(cycle);
                if (!clearCycleDataResult.Success)
                {
                    return FailState(state, clearCycleDataResult.Message, clearCycleDataResult.IsFatal);
                }

                state.Notas.Add(
                    "Antes de ejecutar apply se limpiaron por ciclo las tablas derivadas de BDQISHUR y grdsion."
                );

                var preparationResult = await LoadLatestCommissionDataAsync();
                if (!preparationResult.Success)
                {
                    return FailState(state, preparationResult.Message, preparationResult.IsFatal);
                }

                preparationResult = await LoadMissingPrioritiesAsync();
                if (!preparationResult.Success)
                {
                    return FailState(state, preparationResult.Message, preparationResult.IsFatal);
                }
            }

            var sessionPaymentsResult = await GetPaymentsByCycleAsync(cycle);
            if (!sessionPaymentsResult.Success || sessionPaymentsResult.Data is null)
            {
                return FailState(state, sessionPaymentsResult.Message, sessionPaymentsResult.IsFatal);
            }

            state.SessionPayments = sessionPaymentsResult.Data;

            var companyCommissionsExistResult = await ExistsCompanyCommissionsAsync(cycle);
            if (!companyCommissionsExistResult.Success)
            {
                return FailState(state, companyCommissionsExistResult.Message, companyCommissionsExistResult.IsFatal);
            }

            state.CompanyCommissionsExist = companyCommissionsExistResult.Data;

            if (!previewOnly && !state.CompanyCommissionsExist)
            {
                var syncResult = await SyncCompanyCommissionsAsync(cycle);
                if (!syncResult.Success)
                {
                    return FailState(state, syncResult.Message, syncResult.IsFatal);
                }

                state.CompanyCommissionsExist = true;
            }
            else if (previewOnly && !state.CompanyCommissionsExist)
            {
                state.Notas.Add(
                    "AplicacionesComisionPorEmpresa no existe para este ciclo; el preview construye los montos en memoria desde Guardian."
                );
            }

            var guardianAgentsResult = await GetGuardianCommissionAgentsAsync(cycle);
            if (!guardianAgentsResult.Success || guardianAgentsResult.Data is null)
            {
                return FailState(state, guardianAgentsResult.Message, guardianAgentsResult.IsFatal);
            }

            state.TotalComisionadosGuardian = guardianAgentsResult.Data.Count;

            var totalsResult = await ResolveCompanyTotalsAsync(cycle, previewOnly);
            if (!totalsResult.Success || totalsResult.Data is null)
            {
                return FailState(state, totalsResult.Message, totalsResult.IsFatal);
            }

            var commissionedAgentsExistResult = await ExistsCommissionedAgentsAsync(cycle);
            if (!commissionedAgentsExistResult.Success)
            {
                return FailState(state, commissionedAgentsExistResult.Message, commissionedAgentsExistResult.IsFatal);
            }

            state.AplicacionesComisionadoExiste = commissionedAgentsExistResult.Data;

            List<AplicacionesPendingCommissionAgent> pendingAgents;
            if (state.AplicacionesComisionadoExiste)
            {
                var pendingResult = await GetPendingCommissionedAgentsAsync(cycle);
                if (!pendingResult.Success || pendingResult.Data is null)
                {
                    return FailState(state, pendingResult.Message, pendingResult.IsFatal);
                }

                pendingAgents = pendingResult.Data;
            }
            else
            {
                state.RequiereRegistrarComisionados = true;

                var buildAgentsResult = BuildCommissionedAgentsToRegister(
                    cycle,
                    guardianAgentsResult.Data,
                    totalsResult.Data
                );
                if (!buildAgentsResult.Success || buildAgentsResult.Data is null)
                {
                    return FailState(state, buildAgentsResult.Message, buildAgentsResult.IsFatal);
                }

                if (previewOnly)
                {
                    pendingAgents = BuildPreviewPendingAgents(buildAgentsResult.Data, state.SessionPayments);
                    state.Notas.Add("El preview no inserta en AplicacionesComisionado; el listado pendiente es simulado.");
                }
                else
                {
                    var registerResult = await RegisterCommissionedAgentsAsync(buildAgentsResult.Data);
                    if (!registerResult.Success)
                    {
                        return FailState(state, registerResult.Message, registerResult.IsFatal);
                    }

                    var pendingResult = await GetPendingCommissionedAgentsAsync(cycle);
                    if (!pendingResult.Success || pendingResult.Data is null)
                    {
                        return FailState(state, pendingResult.Message, pendingResult.IsFatal);
                    }

                    pendingAgents = pendingResult.Data;
                    state.AplicacionesComisionadoExiste = true;
                }
            }

            state.TotalPendientes = pendingAgents.Count;
            state.TotalPendienteAplicar = pendingAgents.Sum(item => item.RemainingAmount);
            int counter = 0;
            foreach (var agent in pendingAgents)
            {
                var processResult = await ProcessAgentAsync(logTransaccionId, agent, state);
                if (!processResult.Success)
                {
                    state.TotalErrores++;
                    if (processResult.IsFatal)
                    {
                        return FailState(state, processResult.Message, true);
                    }
                }
                _log.Info(logTransaccionId, NombreArchivo, metodo, $"Fin proceso aplicaciones. ciclo:{cycle}, agent:{JsonSerializer.Serialize(agent)}, contador:{counter}");
                counter++;
            }

            state.Notas.Add(previewOnly
                ? "Preview finalizado. No se escribio informacion en base de datos."
                : "Fin del proceso, se dejara listo el envio futuro del informe.");

            _log.Info(logTransaccionId, NombreArchivo, metodo, $"Fin proceso aplicaciones. ciclo:{cycle}, preview:{previewOnly}");
            return AplicacionesResultado<AplicacionesRunState>.Ok(state, "Proceso de aplicaciones ejecutado correctamente.");
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId, NombreArchivo, metodo, "Error en proceso de aplicaciones", ex);
            return FailState(state, ex.Message, true);
        }
    }

    private async Task<AplicacionesResultado<Dictionary<string, decimal>>> ResolveCompanyTotalsAsync(int cycle, bool previewOnly)
    {
        if (!previewOnly)
        {
            return await GetCompanyTotalsByDocumentAsync(cycle);
        }

        var existsResult = await ExistsCompanyCommissionsAsync(cycle);
        if (!existsResult.Success)
        {
            return AplicacionesResultado<Dictionary<string, decimal>>.Fail(existsResult.Message, existsResult.IsFatal);
        }

        if (existsResult.Data)
        {
            return await GetCompanyTotalsByDocumentAsync(cycle);
        }

        var sourceRowsResult = await BuildCompanyCommissionRowsFromSourceAsync(cycle);
        if (!sourceRowsResult.Success || sourceRowsResult.Data is null)
        {
            return AplicacionesResultado<Dictionary<string, decimal>>.Fail(
                sourceRowsResult.Message,
                sourceRowsResult.IsFatal
            );
        }

        var totals = sourceRowsResult.Data
            .GroupBy(item => item.DocumentNumber.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(item => item.NetAmount),
                StringComparer.OrdinalIgnoreCase
            );

        return AplicacionesResultado<Dictionary<string, decimal>>.Ok(totals);
    }

    private AplicacionesResultado<List<AplicacionesCommissionAgent>> BuildCommissionedAgentsToRegister(
        int cycle,
        IReadOnlyCollection<AplicacionesGuardianCommissionAgent> guardianAgents,
        IReadOnlyDictionary<string, decimal> totalsByDocument
    )
    {
        if (_settings.RequireCommissionCountMatch && totalsByDocument.Count != guardianAgents.Count)
        {
            return AplicacionesResultado<List<AplicacionesCommissionAgent>>.Fail(
                $"La cantidad de comisionados de Guardian ({guardianAgents.Count}) no coincide con los totales por empresa ({totalsByDocument.Count}) para el ciclo {cycle}.",
                true
            );
        }

        var agents = new List<AplicacionesCommissionAgent>();
        foreach (var guardianAgent in guardianAgents)
        {
            if (!totalsByDocument.TryGetValue(guardianAgent.DocumentNumber.Trim(), out var totalToApply))
            {
                return AplicacionesResultado<List<AplicacionesCommissionAgent>>.Fail(
                    $"No se encontro TotalAplicar para el comisionado {guardianAgent.DocumentNumber}.",
                    true
                );
            }

            agents.Add(
                new AplicacionesCommissionAgent
                {
                    Cycle = cycle,
                    ContactId = guardianAgent.ContactId,
                    Code = guardianAgent.Code,
                    DocumentNumber = guardianAgent.DocumentNumber.Trim(),
                    FullName = guardianAgent.FullName,
                    TotalToApply = totalToApply,
                    RegisteredAt = DateTime.Now,
                    Status = 0,
                    Observation = string.Empty
                }
            );
        }

        return AplicacionesResultado<List<AplicacionesCommissionAgent>>.Ok(agents);
    }

    private static List<AplicacionesPendingCommissionAgent> BuildPreviewPendingAgents(
        IEnumerable<AplicacionesCommissionAgent> agents,
        IEnumerable<AplicacionesPaymentRecord> existingPayments
    )
    {
        var appliedTotals = existingPayments
            .GroupBy(item => item.ClientDocument.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Amount), StringComparer.OrdinalIgnoreCase);

        return agents
            .Select(item =>
            {
                appliedTotals.TryGetValue(item.DocumentNumber.Trim(), out var totalApplied);
                return new AplicacionesPendingCommissionAgent
                {
                    Id = item.Id,
                    Cycle = item.Cycle,
                    ContactId = item.ContactId,
                    Code = item.Code,
                    DocumentNumber = item.DocumentNumber,
                    FullName = item.FullName,
                    TotalToApply = item.TotalToApply,
                    RegisteredAt = item.RegisteredAt,
                    Status = item.Status,
                    Observation = item.Observation,
                    TotalAppliedAmount = totalApplied,
                    RemainingAmount = item.TotalToApply - totalApplied
                };
            })
            .Where(item => item.RemainingAmount != 0)
            .Where(item => !item.DocumentNumber.Equals("4823437", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private async Task<AplicacionesResultado> ProcessAgentAsync(
        string logTransaccionId,
        AplicacionesPendingCommissionAgent agent,
        AplicacionesRunState state
    )
    {
        var agentResult = new AplicacionesAgentResult
        {
            Carnet = agent.DocumentNumber,
            NombreCompleto = agent.FullName,
            TotalAplicar = agent.TotalToApply,
            SaldoInicial = agent.RemainingAmount,
            SaldoFinal = agent.RemainingAmount
        };
        state.Comisionados.Add(agentResult);

        var groupSionResult = await ApplyGroupSionAsync(logTransaccionId, agent, state, agentResult);
        if (!groupSionResult.Success)
        {
            agentResult.ErrorGrave = groupSionResult.IsFatal;
            agentResult.Mensaje = groupSionResult.Message;
            return groupSionResult;
        }

        var cartaResult = await ApplyCartaAsync(logTransaccionId, agent, state, agentResult);
        if (!cartaResult.Success)
        {
            agentResult.ErrorGrave = cartaResult.IsFatal;
            agentResult.Mensaje = cartaResult.Message;
            return cartaResult;
        }

        var discountResult = await ApplyDiscountsAsync(logTransaccionId, agent, state, agentResult);
        if (!discountResult.Success)
        {
            agentResult.ErrorGrave = discountResult.IsFatal;
            agentResult.Mensaje = discountResult.Message;
            return discountResult;
        }

        var prorationResult = await ApplyProrationAsync(logTransaccionId, agent, state, agentResult);
        if (!prorationResult.Success)
        {
            agentResult.ErrorGrave = prorationResult.IsFatal;
            agentResult.Mensaje = prorationResult.Message;
            return prorationResult;
        }

        if (state.PreviewOnly)
        {
            agentResult.Operaciones.Add(
                new AplicacionesOperation
                {
                    Paso = "Procesado",
                    Estado = "Planificado",
                    Observacion = "El comisionado seria marcado como procesado al ejecutar apply."
                }
            );
            agentResult.Mensaje = "Preview generado correctamente.";
        }
        else
        {
            var markResult = await MarkProcessedAsync(agent.Cycle, agent.DocumentNumber);
            if (!markResult.Success)
            {
                agentResult.ErrorGrave = markResult.IsFatal;
                agentResult.Mensaje = markResult.Message;
                return markResult;
            }

            agentResult.Operaciones.Add(
                new AplicacionesOperation
                {
                    Paso = "Procesado",
                    Estado = "Aplicado",
                    Observacion = "Comisionado marcado como procesado."
                }
            );
            agentResult.Procesado = true;
            state.TotalProcesados++;
            agentResult.Mensaje = "Comisionado procesado correctamente.";
        }

        agentResult.SaldoFinal = agent.RemainingAmount;
        return AplicacionesResultado.Ok();
    }

    private async Task<AplicacionesResultado> ApplyGroupSionAsync(
        string logTransaccionId,
        AplicacionesPendingCommissionAgent agent,
        AplicacionesRunState state,
        AplicacionesAgentResult agentResult
    )
    {
        var remaining = agent.RemainingAmount;
        var firstIteration = true;
        var shouldContinue = true;
        var forgivenExpenses = false;

        var reprogrammedKeysResult = await GetReprogrammedProductKeysAsync(agent.DocumentNumber);
        if (!reprogrammedKeysResult.Success || reprogrammedKeysResult.Data is null)
        {
            return AplicacionesResultado.Fail(reprogrammedKeysResult.Message, reprogrammedKeysResult.IsFatal);
        }

        while (remaining > 0 && shouldContinue)
        {
            var productsResult = await GetProductPortfolioAsync(agent.DocumentNumber);
            if (!productsResult.Success || productsResult.Data is null)
            {
                return AplicacionesResultado.Fail(productsResult.Message, productsResult.IsFatal);
            }

            var products = FilterEligibleProducts(productsResult.Data, logTransaccionId, "GrupoSion", agent.DocumentNumber);
            if (products.Count == 0)
            {
                agent.RemainingAmount = remaining;
                return AplicacionesResultado.Ok("El comisionado no tiene productos elegibles en Grupo Sion.");
            }

            var hasOverdueProducts = products.Any(item => item.OverdueInstallments > 0);
            if (firstIteration && hasOverdueProducts)
            {
                foreach (var product in products.Where(item => item.OverdueInstallments > 0))
                {
                    var quotesResult = await GetInstallmentQuotesAsync(product.CompanyId, product.SaleId, DateTime.Now, product.OverdueInstallments);
                    if (!quotesResult.Success || quotesResult.Data is null)
                    {
                        return AplicacionesResultado.Fail(quotesResult.Message, quotesResult.IsFatal);
                    }

                    var firstQuote = quotesResult.Data.FirstOrDefault();
                    if (firstQuote is null)
                    {
                        continue;
                    }

                    var applyResult = await ApplyQuotaAsync(logTransaccionId, "GrupoSion", product, firstQuote, remaining, agent, state, agentResult);
                    if (!applyResult.Success)
                    {
                        return applyResult;
                    }

                    remaining = applyResult.Data;
                    if (remaining <= 0)
                    {
                        break;
                    }
                }

                firstIteration = false;
                continue;
            }

            if (hasOverdueProducts)
            {
                var overdueCandidatesResult = await BuildOverdueCandidatesAsync(products);
                if (!overdueCandidatesResult.Success || overdueCandidatesResult.Data is null)
                {
                    return AplicacionesResultado.Fail(overdueCandidatesResult.Message, overdueCandidatesResult.IsFatal);
                }

                if (overdueCandidatesResult.Data.Count == 0)
                {
                    forgivenExpenses = true;
                }

                var overdueApplyResult = await ApplyCandidatesAsync(
                    logTransaccionId,
                    "GrupoSion",
                    overdueCandidatesResult.Data,
                    remaining,
                    agent,
                    state,
                    agentResult
                );
                if (!overdueApplyResult.Success)
                {
                    return AplicacionesResultado.Fail(overdueApplyResult.Message, overdueApplyResult.IsFatal);
                }

                remaining = overdueApplyResult.Data;
            }

            var clientIsUpToDate = !products.Any(item => item.OverdueInstallments > 0) || forgivenExpenses;
            if (remaining > 0 && clientIsUpToDate)
            {
                var currentMonthResult = await BuildSingleQuoteCandidatesAsync(
                    products,
                    quote => quote.DueDate.Month == DateTime.Now.Month && quote.DueDate.Year == DateTime.Now.Year
                );
                if (!currentMonthResult.Success || currentMonthResult.Data is null)
                {
                    return AplicacionesResultado.Fail(currentMonthResult.Message, currentMonthResult.IsFatal);
                }

                var currentMonthApplyResult = await ApplyCandidatesAsync(
                    logTransaccionId,
                    "GrupoSion",
                    currentMonthResult.Data,
                    remaining,
                    agent,
                    state,
                    agentResult
                );
                if (!currentMonthApplyResult.Success)
                {
                    return AplicacionesResultado.Fail(currentMonthApplyResult.Message, currentMonthApplyResult.IsFatal);
                }

                remaining = currentMonthApplyResult.Data;

                var nextMonthResult = await BuildSingleQuoteCandidatesAsync(
                    products,
                    quote => quote.InstallmentNumber == 2 && IsNextMonth(quote.DueDate, DateTime.Now)
                );
                if (!nextMonthResult.Success || nextMonthResult.Data is null)
                {
                    return AplicacionesResultado.Fail(nextMonthResult.Message, nextMonthResult.IsFatal);
                }

                var nextMonthApplyResult = await ApplyCandidatesAsync(
                    logTransaccionId,
                    "GrupoSion",
                    nextMonthResult.Data,
                    remaining,
                    agent,
                    state,
                    agentResult
                );
                if (!nextMonthApplyResult.Success)
                {
                    return AplicacionesResultado.Fail(nextMonthApplyResult.Message, nextMonthApplyResult.IsFatal);
                }

                remaining = nextMonthApplyResult.Data;

                var reprogrammedProducts = products
                    .Where(item => reprogrammedKeysResult.Data.Contains(item.ProductKey))
                    .ToList();
                var reprogrammedResult = await BuildSingleQuoteCandidatesAsync(
                    reprogrammedProducts,
                    quote => quote.DueDate.Date <= DateTime.Now.Date
                );
                if (!reprogrammedResult.Success || reprogrammedResult.Data is null)
                {
                    return AplicacionesResultado.Fail(reprogrammedResult.Message, reprogrammedResult.IsFatal);
                }

                var reprogrammedApplyResult = await ApplyCandidatesAsync(
                    logTransaccionId,
                    "GrupoSion",
                    reprogrammedResult.Data,
                    remaining,
                    agent,
                    state,
                    agentResult
                );
                if (!reprogrammedApplyResult.Success)
                {
                    return AplicacionesResultado.Fail(reprogrammedApplyResult.Message, reprogrammedApplyResult.IsFatal);
                }

                remaining = reprogrammedApplyResult.Data;
                shouldContinue = currentMonthResult.Data.Count > 0;

                if (!shouldContinue && remaining > 0 && remaining < _settings.MinimumAmountForPaymentOnAccount)
                {
                    var onAccountResult = await BuildSingleQuoteCandidatesAsync(
                        products,
                        quote => quote.DueDate >= DateTime.Now.AddMonths(1) || quote.InstallmentNumber == 3
                    );
                    if (!onAccountResult.Success || onAccountResult.Data is null)
                    {
                        return AplicacionesResultado.Fail(onAccountResult.Message, onAccountResult.IsFatal);
                    }

                    var onAccountApplyResult = await ApplyCandidatesAsync(
                        logTransaccionId,
                        "GrupoSion",
                        onAccountResult.Data,
                        remaining,
                        agent,
                        state,
                        agentResult
                    );
                    if (!onAccountApplyResult.Success)
                    {
                        return AplicacionesResultado.Fail(onAccountApplyResult.Message, onAccountApplyResult.IsFatal);
                    }

                    remaining = onAccountApplyResult.Data;
                }
            }
            else
            {
                shouldContinue = false;
            }
        }

        agent.RemainingAmount = remaining;
        return AplicacionesResultado.Ok();
    }

    private async Task<AplicacionesResultado> ApplyCartaAsync(
        string logTransaccionId,
        AplicacionesPendingCommissionAgent agent,
        AplicacionesRunState state,
        AplicacionesAgentResult agentResult
    )
    {
        var remaining = agent.RemainingAmount;
        if (remaining <= 0)
        {
            agent.RemainingAmount = remaining;
            return AplicacionesResultado.Ok();
        }

        var lettersResult = await GetLettersAsync(agent.DocumentNumber);
        if (!lettersResult.Success || lettersResult.Data is null)
        {
            return AplicacionesResultado.Fail(lettersResult.Message, lettersResult.IsFatal);
        }

        var letters = lettersResult.Data
            .Where(item => item.EndDate.Date >= DateTime.Now.Date)
            .Where(item => item.InstallmentsToApply != 0)
            .OrderByDescending(item => item.InstallmentsToApply)
            .ToList();

        foreach (var letter in letters)
        {
            if (remaining <= 0)
            {
                break;
            }

            var unlimited = letter.InstallmentsToApply < 0;
            var iterations = 0;

            while (remaining > 0 && (unlimited || iterations < letter.InstallmentsToApply))
            {
                var quoteResult = await GetInstallmentQuotesAsync(letter.CompanyId, letter.SaleId, DateTime.Now, 1);
                if (!quoteResult.Success || quoteResult.Data is null)
                {
                    return AplicacionesResultado.Fail(quoteResult.Message, quoteResult.IsFatal);
                }

                var quote = quoteResult.Data.FirstOrDefault();
                if (quote is null)
                {
                    break;
                }

                var beneficiaryResult = await GetCustomerByDocumentAsync(letter.BeneficiaryDocument);
                if (!beneficiaryResult.Success || beneficiaryResult.Data is null)
                {
                    return AplicacionesResultado.Fail(beneficiaryResult.Message, beneficiaryResult.IsFatal);
                }

                if (ShouldSkipAlreadyRegisteredCarta(state.SessionPayments, agent.DocumentNumber, letter, quote.PaymentAmount))
                {
                    break;
                }

                var executionResult = await ExecutePaymentAsync(
                    logTransaccionId,
                    "Carta",
                    new AplicacionesProductAccount
                    {
                        CompanyId = letter.CompanyId,
                        ProjectId = letter.ProjectId,
                        SaleId = letter.SaleId,
                        ClientId = beneficiaryResult.Data.ClientId,
                        DocumentNumber = beneficiaryResult.Data.DocumentNumber,
                        LotCode = letter.ProductCode
                    },
                    quote,
                    remaining,
                    new AplicacionesPaymentExecutionContext
                    {
                        Cycle = agent.Cycle,
                        BeneficiaryClientId = beneficiaryResult.Data.ClientId,
                        BeneficiaryDocument = beneficiaryResult.Data.DocumentNumber,
                        ProductId = letter.ProductCode,
                        CommissionerDocumentForLedger = agent.DocumentNumber,
                        Observation =
                            $"Carta de C.I.: {agent.DocumentNumber} a favor de {beneficiaryResult.Data.FullName.Trim()} con C.I.: {letter.BeneficiaryDocument}",
                        PaymentTypeId = -2,
                        IntercompanyFlag = 1
                    },
                    state,
                    agentResult
                );

                if (!executionResult.Success)
                {
                    if (executionResult.IsFatal)
                    {
                        return AplicacionesResultado.Fail(executionResult.Message, true);
                    }

                    break;
                }

                remaining = executionResult.Data!.RemainingBalance;

                var paidOffResult = await IsProductPaidOffAsync(letter.BeneficiaryDocument, letter.ProductCode);
                if (!paidOffResult.Success)
                {
                    return AplicacionesResultado.Fail(paidOffResult.Message, paidOffResult.IsFatal);
                }

                if (paidOffResult.Data)
                {
                    break;
                }

                iterations++;
            }
        }

        agent.RemainingAmount = remaining;
        return AplicacionesResultado.Ok();
    }

    private async Task<AplicacionesResultado> ApplyDiscountsAsync(
        string logTransaccionId,
        AplicacionesPendingCommissionAgent agent,
        AplicacionesRunState state,
        AplicacionesAgentResult agentResult
    )
    {
        var remaining = agent.RemainingAmount;
        if (remaining <= 0)
        {
            agent.RemainingAmount = remaining;
            return AplicacionesResultado.Ok();
        }

        var discountsResult = await GetActiveDiscountsAsync();
        if (!discountsResult.Success || discountsResult.Data is null)
        {
            return AplicacionesResultado.Fail(discountsResult.Message, discountsResult.IsFatal);
        }

        var customerResult = await GetCustomerByDocumentAsync(agent.DocumentNumber);
        if (!customerResult.Success || customerResult.Data is null)
        {
            return AplicacionesResultado.Fail(customerResult.Message, customerResult.IsFatal);
        }

        var orderedDiscounts = OrderDiscounts(discountsResult.Data)
            .Where(item => string.Equals(item.CommissionerDocument.Trim(), agent.DocumentNumber.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var discount in orderedDiscounts)
        {
            if (remaining <= 0)
            {
                break;
            }

            var amount = discount.IsPercentage
                ? decimal.Round(agent.TotalToApply * discount.AmountOrPercent / 100m, 2, MidpointRounding.AwayFromZero)
                : discount.AmountOrPercent;

            var observation = $"Descuento por {discount.Description}";
            if (remaining < amount)
            {
                amount = remaining;
                observation = $"{observation}- A cuenta";
            }

            if (amount <= 0)
            {
                continue;
            }

            var alreadyRegistered = state.SessionPayments.Any(item =>
                item.CompanyId == discount.CompanyId
                && string.Equals(item.ClientDocument.Trim(), agent.DocumentNumber.Trim(), StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(item.ProductId)
                && item.Amount == amount
            );

            if (alreadyRegistered)
            {
                continue;
            }

            var paymentRecord = new AplicacionesPaymentRecord
            {
                Cycle = agent.Cycle,
                CompanyId = discount.CompanyId,
                SaleId = 0,
                ClientId = customerResult.Data.ClientId,
                ClientDocument = agent.DocumentNumber,
                ProductId = string.Empty,
                Expense = 0,
                Amount = amount,
                CreatedAt = DateTime.Now,
                ReceiptId = 0,
                InvoiceId = -1,
                Observation = observation,
                PaymentTypeId = discount.PaymentTypeId,
                IntercompanyFlag = discount.IntercompanyFlag
            };

            if (state.PreviewOnly)
            {
                agentResult.Operaciones.Add(
                    new AplicacionesOperation
                    {
                        Paso = "Descuento",
                        Estado = "Planificado",
                        EmpresaId = discount.CompanyId,
                        Monto = amount,
                        Observacion = observation
                    }
                );
                state.SessionPayments.Add(ClonePaymentRecord(paymentRecord));
            }
            else
            {
                var registerResult = await RegisterPaymentReceiptAsync(paymentRecord);
                if (!registerResult.Success || registerResult.Data <= 0)
                {
                    return AplicacionesResultado.Fail(
                        registerResult.Message.Length > 0 ? registerResult.Message : "No se pudo registrar el descuento en la bitacora.",
                        true
                    );
                }

                paymentRecord.Id = registerResult.Data;
                state.SessionPayments.Add(ClonePaymentRecord(paymentRecord));
                agentResult.Operaciones.Add(
                    new AplicacionesOperation
                    {
                        Paso = "Descuento",
                        Estado = "Aplicado",
                        EmpresaId = discount.CompanyId,
                        Monto = amount,
                        Observacion = observation,
                        ReciboId = 0,
                        FacturaId = -1
                    }
                );
            }

            remaining = Math.Max(remaining - amount, 0);
        }

        agent.RemainingAmount = remaining;
        return AplicacionesResultado.Ok();
    }

    private async Task<AplicacionesResultado> ApplyProrationAsync(
        string logTransaccionId,
        AplicacionesPendingCommissionAgent agent,
        AplicacionesRunState state,
        AplicacionesAgentResult agentResult
    )
    {
        var existingResult = await GetActiveProrationsAsync(agent.Cycle, agent.DocumentNumber);
        if (!existingResult.Success || existingResult.Data is null)
        {
            return AplicacionesResultado.Fail(existingResult.Message, existingResult.IsFatal);
        }

        if (existingResult.Data.Any(item => item.ReceiptVoucherId != 0))
        {
            agentResult.Operaciones.Add(
                new AplicacionesOperation
                {
                    Paso = "Prorrateo",
                    Estado = "Omitido",
                    Observacion = "El prorrateo ya fue procesado previamente."
                }
            );
            return AplicacionesResultado.Ok();
        }

        if (existingResult.Data.Count > 0 && !state.PreviewOnly)
        {
            var disableResult = await DisableProrationsAsync(agent.Cycle, agent.DocumentNumber);
            if (!disableResult.Success)
            {
                return AplicacionesResultado.Fail(disableResult.Message, true);
            }
        }

        var companyCommissionsResult = await GetCompanyCommissionsByDocumentAsync(agent.Cycle, agent.DocumentNumber);
        if (!companyCommissionsResult.Success || companyCommissionsResult.Data is null)
        {
            return AplicacionesResultado.Fail(companyCommissionsResult.Message, companyCommissionsResult.IsFatal);
        }

        var applications = state.SessionPayments
            .Where(item => string.Equals(item.ClientDocument.Trim(), agent.DocumentNumber.Trim(), StringComparison.OrdinalIgnoreCase))
            .Select(ClonePaymentRecord)
            .ToList();

        if (applications.Count == 0)
        {
            agentResult.Operaciones.Add(
                new AplicacionesOperation
                {
                    Paso = "Prorrateo",
                    Estado = "Omitido",
                    Observacion = "No existen aplicaciones para prorratear."
                }
            );
            return AplicacionesResultado.Ok();
        }

        var commissions = companyCommissionsResult.Data
            .Select(CloneCompanyCommission)
            .ToList();

        foreach (var application in applications.ToList())
        {
            foreach (var commission in commissions.ToList())
            {
                if (application.Amount <= 0 || commission.NetAmount <= 0)
                {
                    continue;
                }

                if (commission.CompanyId != application.CompanyId || commission.NetAmount < application.Amount)
                {
                    continue;
                }

                var amount = application.Amount;
                application.Amount -= amount;
                commission.NetAmount -= amount;

                await RegisterProrationOperationAsync(logTransaccionId, state, agentResult, agent, application, commission, amount);
            }
        }

        applications = applications.Where(item => item.Amount > 0).ToList();
        commissions = commissions.Where(item => item.NetAmount > 0).ToList();

        foreach (var application in applications)
        {
            while (application.Amount > 0 && commissions.Any(item => item.NetAmount > 0))
            {
                var lender = commissions.OrderByDescending(item => item.NetAmount).First();
                var amount = Math.Min(lender.NetAmount, application.Amount);

                application.Amount -= amount;
                lender.NetAmount -= amount;

                await RegisterProrationOperationAsync(logTransaccionId, state, agentResult, agent, application, lender, amount);
            }
        }

        return AplicacionesResultado.Ok();
    }

    private async Task RegisterProrationOperationAsync(
        string logTransaccionId,
        AplicacionesRunState state,
        AplicacionesAgentResult agentResult,
        AplicacionesPendingCommissionAgent agent,
        AplicacionesPaymentRecord application,
        AplicacionesCompanyCommission commission,
        decimal amount
    )
    {
        var entry = new AplicacionesProrationEntry
        {
            ClientDocument = agent.DocumentNumber,
            Cycle = agent.Cycle,
            LendingCompanyId = commission.CompanyId,
            ReceivingCompanyId = application.CompanyId,
            ClientId = application.ClientId,
            ReceiptId = application.ReceiptId ?? 0,
            Amount = amount,
            Enabled = true,
            ReceiptVoucherId = 0,
            IntercompanyFlag = application.IntercompanyFlag,
            PaymentTypeId = application.PaymentTypeId
        };

        if (state.PreviewOnly)
        {
            agentResult.Operaciones.Add(
                new AplicacionesOperation
                {
                    Paso = "Prorrateo",
                    Estado = "Planificado",
                    EmpresaId = commission.CompanyId,
                    VentaId = application.SaleId,
                    Monto = amount,
                    Observacion = $"Empresa presta:{commission.CompanyId}, empresa recibe:{application.CompanyId}"
                }
            );
            return;
        }

        var insertResult = await InsertProrationAsync(entry);
        if (insertResult.Success)
        {
            agentResult.Operaciones.Add(
                new AplicacionesOperation
                {
                    Paso = "Prorrateo",
                    Estado = "Aplicado",
                    EmpresaId = commission.CompanyId,
                    VentaId = application.SaleId,
                    Monto = amount,
                    Observacion = $"Empresa presta:{commission.CompanyId}, empresa recibe:{application.CompanyId}"
                }
            );
            return;
        }

        _log.Error(logTransaccionId, NombreArchivo, "Prorrateo", insertResult.Message, new Exception(insertResult.Message));
        agentResult.Operaciones.Add(
            new AplicacionesOperation
            {
                Paso = "Prorrateo",
                Estado = "Error",
                EmpresaId = commission.CompanyId,
                VentaId = application.SaleId,
                Monto = amount,
                Observacion = insertResult.Message
            }
        );
    }

    private async Task<AplicacionesResultado<List<AplicacionesPaymentCandidate>>> BuildOverdueCandidatesAsync(
        IReadOnlyCollection<AplicacionesProductAccount> products
    )
    {
        var candidates = new List<AplicacionesPaymentCandidate>();

        foreach (var product in products.Where(item => item.OverdueInstallments > 0))
        {
            var quotesResult = await GetInstallmentQuotesAsync(product.CompanyId, product.SaleId, DateTime.Now, product.OverdueInstallments);
            if (!quotesResult.Success || quotesResult.Data is null)
            {
                return AplicacionesResultado<List<AplicacionesPaymentCandidate>>.Fail(
                    quotesResult.Message,
                    quotesResult.IsFatal
                );
            }

            foreach (var quote in quotesResult.Data
                         .Where(item => item.DueDate.Date <= DateTime.Now.Date)
                         .OrderBy(item => item.DueDate)
                         .ThenBy(item => item.InstallmentNumber))
            {
                candidates.Add(new AplicacionesPaymentCandidate(product, quote));
            }
        }

        return AplicacionesResultado<List<AplicacionesPaymentCandidate>>.Ok(
            candidates.OrderBy(item => item.Product.Priority)
                .ThenBy(item => item.Quote.DueDate)
                .ThenBy(item => item.Quote.InstallmentNumber)
                .ToList()
        );
    }

    private async Task<AplicacionesResultado<List<AplicacionesPaymentCandidate>>> BuildSingleQuoteCandidatesAsync(
        IReadOnlyCollection<AplicacionesProductAccount> products,
        Func<AplicacionesInstallmentQuote, bool> predicate
    )
    {
        var candidates = new List<AplicacionesPaymentCandidate>();

        foreach (var product in products)
        {
            var quotesResult = await GetInstallmentQuotesAsync(product.CompanyId, product.SaleId, DateTime.Now, 1);
            if (!quotesResult.Success || quotesResult.Data is null)
            {
                return AplicacionesResultado<List<AplicacionesPaymentCandidate>>.Fail(
                    quotesResult.Message,
                    quotesResult.IsFatal
                );
            }

            var quote = quotesResult.Data.FirstOrDefault(predicate);
            if (quote is null)
            {
                continue;
            }

            candidates.Add(new AplicacionesPaymentCandidate(product, quote));
        }

        return AplicacionesResultado<List<AplicacionesPaymentCandidate>>.Ok(
            candidates.OrderBy(item => item.Product.Priority)
                .ThenBy(item => item.Quote.DueDate)
                .ThenBy(item => item.Quote.InstallmentNumber)
                .ToList()
        );
    }

    private async Task<AplicacionesResultado<decimal>> ApplyCandidatesAsync(
        string logTransaccionId,
        string step,
        IReadOnlyCollection<AplicacionesPaymentCandidate> candidates,
        decimal remaining,
        AplicacionesPendingCommissionAgent agent,
        AplicacionesRunState state,
        AplicacionesAgentResult agentResult
    )
    {
        foreach (var candidate in candidates)
        {
            if (remaining <= 0)
            {
                break;
            }

            var applyResult = await ApplyQuotaAsync(
                logTransaccionId,
                step,
                candidate.Product,
                candidate.Quote,
                remaining,
                agent,
                state,
                agentResult
            );

            if (!applyResult.Success)
            {
                return applyResult;
            }

            remaining = applyResult.Data;
        }

        return AplicacionesResultado<decimal>.Ok(remaining);
    }

    private async Task<AplicacionesResultado<decimal>> ApplyQuotaAsync(
        string logTransaccionId,
        string step,
        AplicacionesProductAccount product,
        AplicacionesInstallmentQuote quote,
        decimal remaining,
        AplicacionesPendingCommissionAgent agent,
        AplicacionesRunState state,
        AplicacionesAgentResult agentResult
    )
    {
        var invoiceLimitResult = await ValidateInvoiceFailureLimitAsync(agent.Cycle);
        if (!invoiceLimitResult.Success)
        {
            return AplicacionesResultado<decimal>.Fail(invoiceLimitResult.Message, invoiceLimitResult.IsFatal);
        }

        var paymentResult = await ExecutePaymentAsync(
            logTransaccionId,
            step,
            product,
            quote,
            remaining,
            new AplicacionesPaymentExecutionContext
            {
                Cycle = agent.Cycle,
                BeneficiaryClientId = product.ClientId,
                BeneficiaryDocument = product.DocumentNumber,
                ProductId = product.LotCode,
                Observation = "Pago de Cuota",
                PaymentTypeId = -1,
                IntercompanyFlag = 1
            },
            state,
            agentResult
        );

        if (!paymentResult.Success)
        {
            if (paymentResult.IsFatal)
            {
                return AplicacionesResultado<decimal>.Fail(paymentResult.Message, true);
            }

            return AplicacionesResultado<decimal>.Ok(remaining, paymentResult.Message);
        }

        return AplicacionesResultado<decimal>.Ok(paymentResult.Data!.RemainingBalance, paymentResult.Message);
    }

    private async Task<AplicacionesResultado> ValidateInvoiceFailureLimitAsync(int cycle)
    {
        var failuresResult = await CountInvoiceFailuresAsync(cycle);
        if (!failuresResult.Success)
        {
            return AplicacionesResultado.Fail(failuresResult.Message, failuresResult.IsFatal);
        }

        return failuresResult.Data >= _settings.InvoiceFailureLimit
            ? AplicacionesResultado.Fail(
                $"Se llego al limite de errores de facturacion para el ciclo {cycle}. Total errores: {failuresResult.Data}.",
                true
            )
            : AplicacionesResultado.Ok();
    }

    private async Task<AplicacionesResultado<AplicacionesPaymentExecutionOutcome>> ExecutePaymentAsync(
        string logTransaccionId,
        string step,
        AplicacionesProductAccount product,
        AplicacionesInstallmentQuote quote,
        decimal availableAmount,
        AplicacionesPaymentExecutionContext context,
        AplicacionesRunState state,
        AplicacionesAgentResult agentResult
    )
    {
        if (availableAmount <= 0)
        {
            return AplicacionesResultado<AplicacionesPaymentExecutionOutcome>.Ok(
                new AplicacionesPaymentExecutionOutcome { RemainingBalance = availableAmount },
                "No hay saldo disponible para aplicar."
            );
        }

        var decision = DecidePayment(quote, availableAmount, DateTime.Now);
        if (decision.AmountToPay <= 0)
        {
            return AplicacionesResultado<AplicacionesPaymentExecutionOutcome>.Ok(
                new AplicacionesPaymentExecutionOutcome { RemainingBalance = availableAmount },
                "La cuota no pudo convertirse en una decision de pago valida."
            );
        }

        var companyResult = await GetCompanyDatabaseAsync(product.CompanyId);
        if (!companyResult.Success || companyResult.Data is null)
        {
            return AplicacionesResultado<AplicacionesPaymentExecutionOutcome>.Fail(
                companyResult.Message,
                companyResult.IsFatal
            );
        }

        if (state.PreviewOnly)
        {
            var previewLedgerClient = await ResolveLedgerClientAsync(context);
            if (!previewLedgerClient.Success || previewLedgerClient.Data is null)
            {
                return AplicacionesResultado<AplicacionesPaymentExecutionOutcome>.Fail(
                    previewLedgerClient.Message,
                    previewLedgerClient.IsFatal
                );
            }

            var previewObservation = BuildFinalObservation(context.Observation, decision.ObservationSuffix);
            state.SessionPayments.Add(
                new AplicacionesPaymentRecord
                {
                    Cycle = context.Cycle,
                    CompanyId = product.CompanyId,
                    SaleId = product.SaleId,
                    ClientId = previewLedgerClient.Data.ClientId,
                    ClientDocument = previewLedgerClient.Data.DocumentNumber,
                    ProductId = context.ProductId,
                    Expense = quote.Expense,
                    Amount = decision.AmountToPay,
                    CreatedAt = DateTime.Now,
                    ReceiptId = 0,
                    InvoiceId = 0,
                    Observation = previewObservation,
                    PaymentTypeId = context.PaymentTypeId,
                    IntercompanyFlag = context.IntercompanyFlag
                }
            );

            agentResult.Operaciones.Add(
                new AplicacionesOperation
                {
                    Paso = step,
                    Estado = "Planificado",
                    EmpresaId = product.CompanyId,
                    VentaId = product.SaleId,
                    ProductoId = context.ProductId,
                    Monto = decision.AmountToPay,
                    Observacion = previewObservation,
                    TipoPago = decision.Mode,
                    TiempoPago = decision.Timing
                }
            );

            return AplicacionesResultado<AplicacionesPaymentExecutionOutcome>.Ok(
                new AplicacionesPaymentExecutionOutcome
                {
                    RemainingBalance = Math.Max(availableAmount - decision.AmountToPay, 0),
                    ReceiptId = 0,
                    InvoiceId = 0,
                    Observation = previewObservation
                },
                "Pago simulado correctamente."
            );
        }

        var paymentResult = await ExecuteSionPaymentAsync(
            companyResult.Data,
            product.SaleId,
            decision.EffectivePaymentDate,
            BuildExternalTransactionNumber(),
            decision.AmountToPay
        );
        if (!paymentResult.Success || paymentResult.Data <= 0)
        {
            return AplicacionesResultado<AplicacionesPaymentExecutionOutcome>.Fail(
                paymentResult.Message.Length > 0 ? paymentResult.Message : "No se pudo ejecutar el pago en Sion.",
                paymentResult.IsFatal
            );
        }

        var ledgerClient = await ResolveLedgerClientAsync(context);
        if (!ledgerClient.Success || ledgerClient.Data is null)
        {
            return AplicacionesResultado<AplicacionesPaymentExecutionOutcome>.Fail(
                ledgerClient.Message,
                ledgerClient.IsFatal
            );
        }

        var observation = BuildFinalObservation(context.Observation, decision.ObservationSuffix);
        var paymentRecord = new AplicacionesPaymentRecord
        {
            Cycle = context.Cycle,
            CompanyId = product.CompanyId,
            SaleId = product.SaleId,
            ClientId = ledgerClient.Data.ClientId,
            ClientDocument = ledgerClient.Data.DocumentNumber,
            ProductId = context.ProductId,
            Expense = quote.Expense,
            Amount = decision.AmountToPay,
            CreatedAt = DateTime.Now,
            ReceiptId = paymentResult.Data,
            InvoiceId = -1,
            Observation = observation,
            PaymentTypeId = context.PaymentTypeId,
            IntercompanyFlag = context.IntercompanyFlag
        };

        var ledgerInsertResult = await RegisterPaymentReceiptAsync(paymentRecord);
        if (!ledgerInsertResult.Success || ledgerInsertResult.Data <= 0)
        {
            return AplicacionesResultado<AplicacionesPaymentExecutionOutcome>.Fail(
                ledgerInsertResult.Message.Length > 0
                    ? ledgerInsertResult.Message
                    : "No se pudo registrar el recibo en AplicacionesPagos.",
                true
            );
        }

        paymentRecord.Id = ledgerInsertResult.Data;

        var invoiceId = 0;
        var finalObservation = observation;
        var invoiceResult = await GenerateInvoiceAsync(
            companyResult.Data.WebServiceCompanyId,
            product.ProjectId,
            product.SaleId,
            paymentResult.Data,
            context.ProductId
        );

        if (invoiceResult.Success && invoiceResult.Data is not null)
        {
            invoiceId = invoiceResult.Data.Succeeded ? invoiceResult.Data.InvoiceId : -1;
            if (!invoiceResult.Data.Succeeded && !string.IsNullOrWhiteSpace(invoiceResult.Data.ErrorMessage))
            {
                finalObservation = $"{observation} - Error En Facturacion= {invoiceResult.Data.ErrorMessage}";
            }
        }
        else
        {
            invoiceId = -1;
            finalObservation = $"{observation} - Error En Facturacion= {invoiceResult.Message}";
        }

        var updateResult = await UpdateInvoiceAsync(product.CompanyId, product.SaleId, paymentResult.Data, invoiceId, finalObservation);
        if (!updateResult.Success)
        {
            return AplicacionesResultado<AplicacionesPaymentExecutionOutcome>.Fail(updateResult.Message, updateResult.IsFatal);
        }

        paymentRecord.InvoiceId = invoiceId;
        paymentRecord.Observation = finalObservation;
        state.SessionPayments.Add(ClonePaymentRecord(paymentRecord));

        agentResult.Operaciones.Add(
            new AplicacionesOperation
            {
                Paso = step,
                Estado = "Aplicado",
                EmpresaId = product.CompanyId,
                VentaId = product.SaleId,
                ProductoId = context.ProductId,
                Monto = decision.AmountToPay,
                Observacion = finalObservation,
                ReciboId = paymentResult.Data,
                FacturaId = invoiceId,
                TipoPago = decision.Mode,
                TiempoPago = decision.Timing
            }
        );

        return AplicacionesResultado<AplicacionesPaymentExecutionOutcome>.Ok(
            new AplicacionesPaymentExecutionOutcome
            {
                RemainingBalance = Math.Max(availableAmount - decision.AmountToPay, 0),
                ReceiptId = paymentResult.Data,
                InvoiceId = invoiceId,
                Observation = finalObservation
            },
            "Pago aplicado correctamente."
        );
    }

    private async Task<AplicacionesResultado<AplicacionesCustomerRecord>> ResolveLedgerClientAsync(
        AplicacionesPaymentExecutionContext context
    )
    {
        if (string.IsNullOrWhiteSpace(context.CommissionerDocumentForLedger))
        {
            return AplicacionesResultado<AplicacionesCustomerRecord>.Ok(
                new AplicacionesCustomerRecord
                {
                    ClientId = context.BeneficiaryClientId,
                    DocumentNumber = context.BeneficiaryDocument,
                    FullName = string.Empty
                }
            );
        }

        return await GetCustomerByDocumentAsync(context.CommissionerDocumentForLedger);
    }

    private static AplicacionesPaymentDecision DecidePayment(
        AplicacionesInstallmentQuote quote,
        decimal availableAmount,
        DateTime now
    )
    {
        var amountToPay = Math.Min(quote.PaymentAmount, availableAmount);
        var fullPayment = availableAmount >= quote.PaymentAmount;
        var valueDate = CanPayAtValueDate(quote.DueDate, now);

        return new AplicacionesPaymentDecision
        {
            AmountToPay = amountToPay,
            EffectivePaymentDate = valueDate ? quote.DueDate : now,
            Mode = fullPayment ? "Completo" : "A Cuenta",
            Timing = valueDate ? "Fecha Valor" : "Normal",
            ObservationSuffix = BuildObservationSuffix(fullPayment, valueDate)
        };
    }

    private static bool CanPayAtValueDate(DateTime dueDate, DateTime now)
    {
        var firstDayPreviousMonth = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
        return dueDate.Date >= firstDayPreviousMonth.Date && dueDate.Date <= now.Date;
    }

    private static string BuildObservationSuffix(bool fullPayment, bool valueDate)
    {
        if (fullPayment && !valueDate)
        {
            return string.Empty;
        }

        if (fullPayment && valueDate)
        {
            return " A Fecha Valor";
        }

        if (!fullPayment && !valueDate)
        {
            return " A Cuenta";
        }

        return " A Cuenta - A Fecha Valor";
    }

    private static string BuildFinalObservation(string baseObservation, string suffix)
    {
        return string.IsNullOrWhiteSpace(suffix) ? baseObservation.Trim() : $"{baseObservation.Trim()} {suffix.Trim()}".Trim();
    }

    private static List<AplicacionesProductAccount> FilterEligibleProducts(
        IReadOnlyCollection<AplicacionesProductAccount> products,
        string logTransaccionId,
        string step,
        string documentNumber
    )
    {
        var filtered = products
            .Where(item => !(item.TotalDebt == 0 && item.PendingInstallments == 0))
            .ToList();

        return filtered
            .Where(item => item.CompanyId != 17 && item.CompanyId != 21)
            .ToList();
    }

    private static bool IsNextMonth(DateTime dueDate, DateTime now)
    {
        var next = now.AddMonths(1);
        return dueDate.Month == next.Month && dueDate.Year == next.Year;
    }

    private static IEnumerable<AplicacionesDiscountDefinition> OrderDiscounts(IEnumerable<AplicacionesDiscountDefinition> discounts)
    {
        var royal = discounts.Where(item => item.Description.Contains("Royal", StringComparison.OrdinalIgnoreCase));
        var cards = discounts.Where(item => item.Description.Contains("Tarjeta", StringComparison.OrdinalIgnoreCase));
        var others = discounts.Where(item =>
            !item.Description.Contains("Royal", StringComparison.OrdinalIgnoreCase)
            && !item.Description.Contains("Tarjeta", StringComparison.OrdinalIgnoreCase)
        );

        return royal.Concat(cards).Concat(others);
    }

    private static bool ShouldSkipAlreadyRegisteredCarta(
        IReadOnlyCollection<AplicacionesPaymentRecord> existingPayments,
        string commissionerDocument,
        AplicacionesLetterInstruction letter,
        decimal amount
    )
    {
        return existingPayments.Any(item =>
            item.CompanyId == letter.CompanyId
            && string.Equals(item.ClientDocument.Trim(), commissionerDocument.Trim(), StringComparison.OrdinalIgnoreCase)
            && item.SaleId == letter.SaleId
            && item.Amount == amount
            && item.Observation.Contains("Carta", StringComparison.OrdinalIgnoreCase)
        );
    }

    private static AplicacionesPaymentRecord ClonePaymentRecord(AplicacionesPaymentRecord source)
    {
        return new AplicacionesPaymentRecord
        {
            Id = source.Id,
            Cycle = source.Cycle,
            CompanyId = source.CompanyId,
            SaleId = source.SaleId,
            ClientId = source.ClientId,
            ClientDocument = source.ClientDocument,
            ProductId = source.ProductId,
            Expense = source.Expense,
            Amount = source.Amount,
            CreatedAt = source.CreatedAt,
            ReceiptId = source.ReceiptId,
            InvoiceId = source.InvoiceId,
            Observation = source.Observation,
            PaymentTypeId = source.PaymentTypeId,
            IntercompanyFlag = source.IntercompanyFlag
        };
    }

    private static AplicacionesCompanyCommission CloneCompanyCommission(AplicacionesCompanyCommission source)
    {
        return new AplicacionesCompanyCommission
        {
            Id = source.Id,
            Cycle = source.Cycle,
            DocumentNumber = source.DocumentNumber,
            CompanyId = source.CompanyId,
            CompanyWebServiceId = source.CompanyWebServiceId,
            CompanyDatabaseName = source.CompanyDatabaseName,
            PersonalSales = source.PersonalSales,
            GroupSales = source.GroupSales,
            Residual = source.Residual,
            CommissionAmount = source.CommissionAmount,
            RetentionAmount = source.RetentionAmount,
            NetAmount = source.NetAmount,
            GrossAmount = source.GrossAmount,
            ThirteenPercentAmount = source.ThirteenPercentAmount,
            RequiresInvoice = source.RequiresInvoice
        };
    }

    private static string BuildExternalTransactionNumber()
    {
        return DateTime.Now.ToString("HHmmssffffff");
    }

    private static AplicacionesPreviewResponse BuildPreviewResponse(AplicacionesRunState state)
    {
        return new AplicacionesPreviewResponse
        {
            LCicloId = state.Cycle,
            Preview = true,
            AplicacionesComisionadoExiste = state.AplicacionesComisionadoExiste,
            CompanyCommissionsExist = state.CompanyCommissionsExist,
            RequiereRegistrarComisionados = state.RequiereRegistrarComisionados,
            ErrorGrave = state.ErrorGrave,
            ErrorGraveMensaje = state.ErrorGraveMensaje,
            TotalComisionadosGuardian = state.TotalComisionadosGuardian,
            TotalPendientes = state.TotalPendientes,
            TotalPendienteAplicar = state.TotalPendienteAplicar,
            Notas = state.Notas,
            Comisionados = state.Comisionados
        };
    }

    private static AplicacionesApplyResponse BuildApplyResponse(AplicacionesRunState state)
    {
        return new AplicacionesApplyResponse
        {
            LCicloId = state.Cycle,
            Preview = false,
            AplicacionesComisionadoExiste = state.AplicacionesComisionadoExiste,
            CompanyCommissionsExist = state.CompanyCommissionsExist,
            RequiereRegistrarComisionados = state.RequiereRegistrarComisionados,
            ErrorGrave = state.ErrorGrave,
            ErrorGraveMensaje = state.ErrorGraveMensaje,
            TotalComisionadosGuardian = state.TotalComisionadosGuardian,
            TotalPendientes = state.TotalPendientes,
            TotalPendienteAplicar = state.TotalPendienteAplicar,
            TotalProcesados = state.TotalProcesados,
            TotalErrores = state.TotalErrores,
            Notas = state.Notas,
            Comisionados = state.Comisionados
        };
    }

    private static AplicacionesResultado<AplicacionesRunState> FailState(
        AplicacionesRunState state,
        string message,
        bool isFatal
    )
    {
        state.ErrorGrave = isFatal;
        state.ErrorGraveMensaje = message;
        return new AplicacionesResultado<AplicacionesRunState>
        {
            Success = false,
            IsFatal = isFatal,
            Message = message,
            Data = state
        };
    }
}

internal sealed class AplicacionesRunState
{
    public AplicacionesRunState(int cycle, bool previewOnly)
    {
        Cycle = cycle;
        PreviewOnly = previewOnly;
    }

    public int Cycle { get; }
    public bool PreviewOnly { get; }
    public bool AplicacionesComisionadoExiste { get; set; }
    public bool CompanyCommissionsExist { get; set; }
    public bool RequiereRegistrarComisionados { get; set; }
    public bool ErrorGrave { get; set; }
    public string ErrorGraveMensaje { get; set; } = string.Empty;
    public int TotalComisionadosGuardian { get; set; }
    public int TotalPendientes { get; set; }
    public decimal TotalPendienteAplicar { get; set; }
    public int TotalProcesados { get; set; }
    public int TotalErrores { get; set; }
    public List<string> Notas { get; set; } = new();
    public List<AplicacionesAgentResult> Comisionados { get; set; } = new();
    public List<AplicacionesPaymentRecord> SessionPayments { get; set; } = new();
}

internal sealed class AplicacionesPaymentCandidate
{
    public AplicacionesPaymentCandidate(AplicacionesProductAccount product, AplicacionesInstallmentQuote quote)
    {
        Product = product;
        Quote = quote;
    }

    public AplicacionesProductAccount Product { get; }
    public AplicacionesInstallmentQuote Quote { get; }
}

internal sealed class AplicacionesPaymentDecision
{
    public decimal AmountToPay { get; set; }
    public DateTime EffectivePaymentDate { get; set; }
    public string Mode { get; set; } = string.Empty;
    public string Timing { get; set; } = string.Empty;
    public string ObservationSuffix { get; set; } = string.Empty;
}

internal sealed class AplicacionesPaymentExecutionContext
{
    public int Cycle { get; set; }
    public int BeneficiaryClientId { get; set; }
    public string BeneficiaryDocument { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public string CommissionerDocumentForLedger { get; set; } = string.Empty;
    public string Observation { get; set; } = string.Empty;
    public int PaymentTypeId { get; set; }
    public int IntercompanyFlag { get; set; }
}

internal sealed class AplicacionesPaymentExecutionOutcome
{
    public decimal RemainingBalance { get; set; }
    public int ReceiptId { get; set; }
    public int InvoiceId { get; set; }
    public string Observation { get; set; } = string.Empty;
}

internal class AplicacionesResultado
{
    public bool Success { get; init; }
    public bool IsFatal { get; init; }
    public string Message { get; init; } = string.Empty;

    public static AplicacionesResultado Ok(string message = "")
    {
        return new AplicacionesResultado
        {
            Success = true,
            Message = message
        };
    }

    public static AplicacionesResultado Fail(string message, bool isFatal = false)
    {
        return new AplicacionesResultado
        {
            Success = false,
            IsFatal = isFatal,
            Message = message
        };
    }
}

internal class AplicacionesResultado<T> : AplicacionesResultado
{
    public T? Data { get; init; }

    public static AplicacionesResultado<T> Ok(T data, string message = "")
    {
        return new AplicacionesResultado<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    public static new AplicacionesResultado<T> Fail(string message, bool isFatal = false)
    {
        return new AplicacionesResultado<T>
        {
            Success = false,
            IsFatal = isFatal,
            Message = message
        };
    }
}
