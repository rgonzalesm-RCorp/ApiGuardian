using System.Data;
using System.Security;
using System.Text;
using System.Xml.Linq;
using ApiGuardian.Application.Interfaces;
using Dapper;

namespace ApiGuardian.Infrastructure.Repositories;

public partial class AplicacionesRepositorio
{
    private async Task<ResultadoAplicaciones> ValidarConexionesAsync()
    {
        var guardian = await ValidarConexionGuardianAsync();
        if (!guardian.Exito)
        {
            return guardian;
        }

        return await ValidarConexionSqlAsync();
    }

    private async Task<ResultadoAplicaciones> ValidarConexionGuardianAsync()
    {
        try
        {
            using var conexion = _guardianContext.CreateConnection();
            await conexion.ExecuteScalarAsync<int>(
                new CommandDefinition("SELECT 1;", commandTimeout: _configuracionAplicaciones.TiempoEsperaComandoSegundos)
            );

            return ResultadoAplicaciones.Ok("Conexion Guardian validada.");
        }
        catch (Exception ex)
        {
            return ResultadoAplicaciones.Fail($"No existe conexion con la base de datos Guardian: {ex.Message}", true);
        }
    }

    private async Task<ResultadoAplicaciones> ValidarConexionSqlAsync()
    {
        try
        {
            using var conexion = _sqlContext.CreateConnection();
            await conexion.ExecuteScalarAsync<int>(
                new CommandDefinition("SELECT 1;", commandTimeout: _configuracionAplicaciones.TiempoEsperaComandoSegundos)
            );

            return ResultadoAplicaciones.Ok("Conexion SQL Server validada.");
        }
        catch (Exception ex)
        {
            return ResultadoAplicaciones.Fail($"No existe conexion con la base de datos Conexion: {ex.Message}", true);
        }
    }

    private async Task<ResultadoAplicaciones> CargarUltimosDatosComisionAsync()
    {
        try
        {
            using var conexion = _guardianContext.CreateConnection();
            await conexion.ExecuteAsync(
                new CommandDefinition(
                    "CALL RetencionEmpresa();",
                    commandTimeout: _configuracionAplicaciones.TiempoEsperaComandoSegundos
                )
            );

            return ResultadoAplicaciones.Ok("Carga de datos de comision ejecutada correctamente.");
        }
        catch (Exception ex)
        {
            return ResultadoAplicaciones.Fail($"No se pudo cargar los datos de comision en Guardian: {ex.Message}", true);
        }
    }

    private async Task<ResultadoAplicaciones> LimpiarDatosCicloAsync(int ciclo)
    {
        var clearGuardianResult = await LimpiarDatosCicloGuardianAsync(ciclo);
        if (!clearGuardianResult.Exito)
        {
            return clearGuardianResult;
        }

        return await LimpiarDatosCicloBdQishurAsync(ciclo);
    }

    private async Task<ResultadoAplicaciones> LimpiarDatosCicloGuardianAsync(int ciclo)
    {
        try
        {
            using var conexion = _guardianContext.CreateConnection();
            if (conexion is IDbConnection dbConnection && dbConnection.State != ConnectionState.Open)
            {
                dbConnection.Open();
            }

            using var transaccion = conexion.BeginTransaction();
            await conexion.ExecuteAsync(
                new CommandDefinition(
                    SqlDeleteGuardianRetencionEmpresaExterior,
                    new { Ciclo = ciclo },
                    transaccion,
                    _configuracionAplicaciones.TiempoEsperaComandoSegundos
                )
            );
            await conexion.ExecuteAsync(
                new CommandDefinition(
                    SqlDeleteGuardianRetencionEmpresa,
                    new { Ciclo = ciclo },
                    transaccion,
                    _configuracionAplicaciones.TiempoEsperaComandoSegundos
                )
            );
            transaccion.Commit();

            return ResultadoAplicaciones.Ok("Datos por ciclo eliminados de grdsion.");
        }
        catch (Exception ex)
        {
            return ResultadoAplicaciones.Fail(
                $"No se pudo limpiar la informacion de grdsion para el ciclo {ciclo}: {ex.Message}",
                true
            );
        }
    }

    private async Task<ResultadoAplicaciones> LimpiarDatosCicloBdQishurAsync(int ciclo)
    {
        try
        {
            using var conexion = _sqlContext.CreateConnection();
            if (conexion is IDbConnection dbConnection && dbConnection.State != ConnectionState.Open)
            {
                dbConnection.Open();
            }

            using var transaccion = conexion.BeginTransaction();
            await conexion.ExecuteAsync(
                new CommandDefinition(
                    SqlDeleteAplicacionesProrrateo,
                    new { Ciclo = ciclo },
                    transaccion,
                    _configuracionAplicaciones.TiempoEsperaComandoSegundos
                )
            );
            await conexion.ExecuteAsync(
                new CommandDefinition(
                    SqlDeleteAplicacionesPagos,
                    new { Ciclo = ciclo },
                    transaccion,
                    _configuracionAplicaciones.TiempoEsperaComandoSegundos
                )
            );
            await conexion.ExecuteAsync(
                new CommandDefinition(
                    SqlDeleteAplicacionesComisionado,
                    new { Ciclo = ciclo },
                    transaccion,
                    _configuracionAplicaciones.TiempoEsperaComandoSegundos
                )
            );
            await conexion.ExecuteAsync(
                new CommandDefinition(
                    SqlDeleteAplicacionesComisionPorEmpresa,
                    new { Ciclo = ciclo },
                    transaccion,
                    _configuracionAplicaciones.TiempoEsperaComandoSegundos
                )
            );
            transaccion.Commit();

            return ResultadoAplicaciones.Ok("Datos por ciclo eliminados de BDQISHUR.");
        }
        catch (Exception ex)
        {
            return ResultadoAplicaciones.Fail(
                $"No se pudo limpiar la informacion de BDQISHUR para el ciclo {ciclo}: {ex.Message}",
                true
            );
        }
    }

    private async Task<ResultadoAplicaciones> CargarPrioridadesFaltantesAsync()
    {
        try
        {
            using var conexion = _sqlContext.CreateConnection();
            await conexion.ExecuteAsync(
                new CommandDefinition(
                    SqlInsertMissingPriorities,
                    commandTimeout: _configuracionAplicaciones.TiempoEsperaComandoSegundos
                )
            );

            return ResultadoAplicaciones.Ok("Prioridades faltantes sincronizadas correctamente.");
        }
        catch (Exception ex)
        {
            return ResultadoAplicaciones.Fail(
                $"No se pudo cargar correctamente las prioridades faltantes: {ex.Message}",
                true
            );
        }
    }

    private async Task<ResultadoAplicaciones<bool>> ExistenComisionesEmpresaAsync(int ciclo)
    {
        try
        {
            using var conexion = _sqlContext.CreateConnection();
            var cantidad = await conexion.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    SqlExistsAplicacionesComisionPorEmpresa,
                    new { Ciclo = ciclo },
                    commandTimeout: _configuracionAplicaciones.TiempoEsperaComandoSegundos
                )
            );

            return ResultadoAplicaciones<bool>.Ok(cantidad > 0);
        }
        catch (Exception ex)
        {
            return ResultadoAplicaciones<bool>.Fail(
                $"No se pudo verificar AplicacionesComisionPorEmpresa para el ciclo {ciclo}: {ex.Message}",
                true
            );
        }
    }

    private async Task<ResultadoAplicaciones> SincronizarComisionesEmpresaAsync(int ciclo)
    {
        var resultadoFilasOrigen = await ConstruirRegistrosComisionEmpresaDesdeOrigenAsync(ciclo);
        if (!resultadoFilasOrigen.Exito || resultadoFilasOrigen.Datos is null)
        {
            return ResultadoAplicaciones.Fail(resultadoFilasOrigen.Mensaje, resultadoFilasOrigen.EsFatal);
        }

        if (resultadoFilasOrigen.Datos.Count == 0)
        {
            return ResultadoAplicaciones.Fail("Guardian no devolvio comisiones por empresa para sincronizar.", true);
        }

        try
        {
            using var conexion = _sqlContext.CreateConnection();
            if (conexion is IDbConnection dbConnection && dbConnection.State != ConnectionState.Open)
            {
                dbConnection.Open();
            }

            using var transaccion = conexion.BeginTransaction();
            await conexion.ExecuteAsync(
                new CommandDefinition(
                    SqlInsertCompanyCommission,
                    resultadoFilasOrigen.Datos,
                    transaccion,
                    _configuracionAplicaciones.TiempoEsperaComandoSegundos
                )
            );
            transaccion.Commit();

            return ResultadoAplicaciones.Ok("Comisiones por empresa sincronizadas correctamente.");
        }
        catch (Exception ex)
        {
            return ResultadoAplicaciones.Fail($"No se pudo sincronizar comisiones por empresa: {ex.Message}", true);
        }
    }

    private async Task<ResultadoAplicaciones<List<RegistroComisionEmpresaAplicaciones>>> ConstruirRegistrosComisionEmpresaDesdeOrigenAsync(int ciclo)
    {
        try
        {
            using var sqlConnection = _sqlContext.CreateConnection();
            var mappings = (
                await sqlConnection.QueryAsync<MapeoEmpresaAplicaciones>(
                    new CommandDefinition(
                        SqlCompanyMapping,
                        commandTimeout: _configuracionAplicaciones.TiempoEsperaComandoSegundos
                    )
                )
            ).ToDictionary(item => item.EmpresaLegadaId);

            using var guardianConnection = _guardianContext.CreateConnection();
            var guardianRows = (
                await guardianConnection.QueryAsync<ComisionEmpresaGuardianAplicaciones>(
                    new CommandDefinition(
                        SqlGuardianCompanyCommission,
                        new { Ciclo = ciclo },
                        commandTimeout: _configuracionAplicaciones.TiempoEsperaComandoSegundos
                    )
                )
            ).ToList();

            var records = new List<RegistroComisionEmpresaAplicaciones>();
            foreach (var fila in guardianRows)
            {
                if (!mappings.TryGetValue(fila.EmpresaLegadaId, out var mapeo))
                {
                    return ResultadoAplicaciones<List<RegistroComisionEmpresaAplicaciones>>.Fail(
                        $"No existe la empresa de Conexion que asume la empresa Guardian {fila.EmpresaLegadaId}.",
                        true
                    );
                }

                var thirteenPercent = fila.IndicadorFactura == 1
                    ? decimal.Round(fila.ComisionTotal * 0.13m, 2, MidpointRounding.AwayFromZero)
                    : 0m;

                records.Add(
                    new RegistroComisionEmpresaAplicaciones
                    {
                        MontoBruto = fila.IndicadorFactura == 1 ? fila.ComisionTotal - thirteenPercent : 0m,
                        NumeroDocumento = fila.NumeroDocumento.Trim(),
                        Ciclo = ciclo,
                        EmpresaId = mapeo.EmpresaId,
                        IndicadorFactura = fila.IndicadorFactura,
                        MontoComision = fila.MontoComision,
                        MontoNeto = fila.ComisionTotal,
                        MontoTrecePorCiento = thirteenPercent,
                        MontoRetencion = fila.MontoRetencion,
                        Residual = fila.Residual,
                        VentasGrupales = fila.VentasGrupales,
                        VentasPersonales = fila.VentasPersonales,
                        EmpresaLegadaId = fila.EmpresaLegadaId
                    }
                );
            }

            return ResultadoAplicaciones<List<RegistroComisionEmpresaAplicaciones>>.Ok(records);
        }
        catch (Exception ex)
        {
            return ResultadoAplicaciones<List<RegistroComisionEmpresaAplicaciones>>.Fail(
                $"No se pudo construir las comisiones por empresa desde Guardian: {ex.Message}",
                true
            );
        }
    }

    private async Task<ResultadoAplicaciones<List<ComisionadoGuardianAplicaciones>>> ObtenerComisionadosGuardianAsync(int ciclo)
    {
        try
        {
            using var conexion = _guardianContext.CreateConnection();
            var filas = (
                await conexion.QueryAsync<ComisionadoGuardianAplicaciones>(
                    new CommandDefinition(
                        SqlGuardianCommissionAgents,
                        new { Ciclo = ciclo },
                        commandTimeout: _configuracionAplicaciones.TiempoEsperaComandoSegundos
                    )
                )
            ).ToList();

            return ResultadoAplicaciones<List<ComisionadoGuardianAplicaciones>>.Ok(filas);
        }
        catch (Exception ex)
        {
            return ResultadoAplicaciones<List<ComisionadoGuardianAplicaciones>>.Fail(
                $"No se pudo obtener el listado de comisionados desde Guardian: {ex.Message}",
                true
            );
        }
    }

    private async Task<ResultadoAplicaciones<bool>> ExistenComisionadosRegistradosAsync(int ciclo)
    {
        try
        {
            using var conexion = _sqlContext.CreateConnection();
            var cantidad = await conexion.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    SqlExistsAplicacionesComisionado,
                    new { Ciclo = ciclo },
                    commandTimeout: _configuracionAplicaciones.TiempoEsperaComandoSegundos
                )
            );

            return ResultadoAplicaciones<bool>.Ok(cantidad > 0);
        }
        catch (Exception ex)
        {
            return ResultadoAplicaciones<bool>.Fail(
                $"No se pudo verificar AplicacionesComisionado para el ciclo {ciclo}: {ex.Message}",
                true
            );
        }
    }

    private async Task<ResultadoAplicaciones> RegistrarComisionadosAsync(IEnumerable<ComisionadoAplicaciones> comisionados)
    {
        var rowsToInsert = comisionados.ToList();

        try
        {
            using var conexion = _sqlContext.CreateConnection();
            if (conexion is IDbConnection dbConnection && dbConnection.State != ConnectionState.Open)
            {
                dbConnection.Open();
            }

            using var transaccion = conexion.BeginTransaction();
            var inserted = await conexion.ExecuteAsync(
                new CommandDefinition(
                    SqlInsertAplicacionesComisionado,
                    rowsToInsert,
                    transaccion,
                    _configuracionAplicaciones.TiempoEsperaComandoSegundos
                )
            );
            transaccion.Commit();

            return inserted == rowsToInsert.Count
                ? ResultadoAplicaciones.Ok($"Se registraron {inserted} comisionados en AplicacionesComisionado.")
                : ResultadoAplicaciones.Fail(
                    $"Se registraron {inserted} de {rowsToInsert.Count} comisionados en AplicacionesComisionado.",
                    true
                );
        }
        catch (Exception ex)
        {
            return ResultadoAplicaciones.Fail($"No se pudo registrar AplicacionesComisionado: {ex.Message}", true);
        }
    }

    private async Task<ResultadoAplicaciones<List<ComisionadoPendienteAplicaciones>>> ObtenerComisionadosPendientesAsync(int ciclo)
    {
        try
        {
            using var conexion = _sqlContext.CreateConnection();
            var filas = (
                await conexion.QueryAsync<ComisionadoPendienteAplicaciones>(
                    new CommandDefinition(
                        SqlPendingComisionados,
                        new { Ciclo = ciclo },
                        commandTimeout: _configuracionAplicaciones.TiempoEsperaComandoSegundos
                    )
                )
            ).ToList();

            return ResultadoAplicaciones<List<ComisionadoPendienteAplicaciones>>.Ok(filas);
        }
        catch (Exception ex)
        {
            return ResultadoAplicaciones<List<ComisionadoPendienteAplicaciones>>.Fail(
                $"No se pudo obtener el listado pendiente de pago: {ex.Message}",
                true
            );
        }
    }

    private async Task<ResultadoAplicaciones> MarcarProcesadoAsync(int ciclo, string numeroDocumento)
    {
        try
        {
            using var conexion = _sqlContext.CreateConnection();
            var filas = await conexion.ExecuteAsync(
                new CommandDefinition(
                    SqlMarkComisionadoProcessed,
                    new
                    {
                        Ciclo = ciclo,
                        NumeroDocumento = numeroDocumento.Trim()
                    },
                    commandTimeout: _configuracionAplicaciones.TiempoEsperaComandoSegundos
                )
            );

            return filas > 0
                ? ResultadoAplicaciones.Ok("Comisionado marcado como procesado.")
                : ResultadoAplicaciones.Fail("No se pudo marcar el comisionado como procesado.", true);
        }
        catch (Exception ex)
        {
            return ResultadoAplicaciones.Fail($"No se pudo marcar el comisionado como procesado: {ex.Message}", true);
        }
    }

    private async Task<ResultadoAplicaciones<Dictionary<string, decimal>>> ObtenerTotalesEmpresaPorDocumentoAsync(int ciclo)
    {
        try
        {
            using var conexion = _sqlContext.CreateConnection();
            var filas = await conexion.QueryAsync<TotalComisionAplicaciones>(
                new CommandDefinition(
                    SqlCompanyTotalsByDocument,
                    new { Ciclo = ciclo },
                    commandTimeout: _configuracionAplicaciones.TiempoEsperaComandoSegundos
                )
            );

            return ResultadoAplicaciones<Dictionary<string, decimal>>.Ok(
                filas.ToDictionary(
                    item => item.NumeroDocumento.Trim(),
                    item => item.TotalAplicar,
                    StringComparer.OrdinalIgnoreCase
                )
            );
        }
        catch (Exception ex)
        {
            return ResultadoAplicaciones<Dictionary<string, decimal>>.Fail(
                $"No se pudo obtener el total de comisiones por documento: {ex.Message}",
                true
            );
        }
    }

    private async Task<ResultadoAplicaciones<List<ComisionEmpresaAplicaciones>>> ObtenerComisionesEmpresaPorDocumentoAsync(int ciclo, string numeroDocumento)
    {
        try
        {
            using var conexion = _sqlContext.CreateConnection();
            var filas = (
                await conexion.QueryAsync<ComisionEmpresaAplicaciones>(
                    new CommandDefinition(
                        SqlCompanyCommissionByDocument,
                        new
                        {
                            Ciclo = ciclo,
                            NumeroDocumento = numeroDocumento.Trim()
                        },
                        commandTimeout: _configuracionAplicaciones.TiempoEsperaComandoSegundos
                    )
                )
            ).ToList();

            return ResultadoAplicaciones<List<ComisionEmpresaAplicaciones>>.Ok(filas);
        }
        catch (Exception ex)
        {
            return ResultadoAplicaciones<List<ComisionEmpresaAplicaciones>>.Fail(
                $"No se pudo obtener el detalle de comisiones por empresa: {ex.Message}",
                true
            );
        }
    }

    private async Task<ResultadoAplicaciones<List<ProductoCarteraAplicaciones>>> ObtenerCarteraProductosAsync(string numeroDocumento)
    {
        try
        {
            using var conexion = _sqlContext.CreateConnection();
            var filas = (
                await conexion.QueryAsync<ProductoCarteraAplicaciones>(
                    new CommandDefinition(
                        SqlPortfolio,
                        new { NumeroDocumento = numeroDocumento.Trim() },
                        commandTimeout: _configuracionAplicaciones.TiempoEsperaComandoSegundos
                    )
                )
            ).ToList();

            return ResultadoAplicaciones<List<ProductoCarteraAplicaciones>>.Ok(filas);
        }
        catch (Exception ex)
        {
            return ResultadoAplicaciones<List<ProductoCarteraAplicaciones>>.Fail(
                $"No se pudo obtener la cartera de productos del documento {numeroDocumento}: {ex.Message}",
                true
            );
        }
    }

    private async Task<ResultadoAplicaciones<bool>> EstaProductoPagadoAsync(string documentoBeneficiario, string codigoLote)
    {
        try
        {
            using var conexion = _sqlContext.CreateConnection();
            var cantidad = await conexion.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    SqlProductPaidOff,
                    new
                    {
                        NumeroDocumento = documentoBeneficiario.Trim(),
                        CodigoLote = codigoLote.Trim()
                    },
                    commandTimeout: _configuracionAplicaciones.TiempoEsperaComandoSegundos
                )
            );

            return ResultadoAplicaciones<bool>.Ok(cantidad > 0);
        }
        catch (Exception ex)
        {
            return ResultadoAplicaciones<bool>.Fail(
                $"No se pudo verificar si el producto {codigoLote} esta pagado: {ex.Message}",
                true
            );
        }
    }

    private async Task<ResultadoAplicaciones<HashSet<string>>> ObtenerClavesProductosReprogramadosAsync(string numeroDocumento)
    {
        try
        {
            using var conexion = _sqlContext.CreateConnection();
            var filas = await conexion.QueryAsync<ProductoReprogramadoAplicaciones>(
                new CommandDefinition(
                    SqlReprogrammedProducts,
                    new { NumeroDocumento = numeroDocumento.Trim() },
                    commandTimeout: _configuracionAplicaciones.TiempoEsperaComandoSegundos
                )
            );

            return ResultadoAplicaciones<HashSet<string>>.Ok(
                filas.Select(item => $"{item.ClienteId}:{item.ProductoId.Trim()}").ToHashSet(StringComparer.OrdinalIgnoreCase)
            );
        }
        catch (Exception ex)
        {
            return ResultadoAplicaciones<HashSet<string>>.Fail(
                $"No se pudo obtener la lista de productos reprogramados del documento {numeroDocumento}: {ex.Message}",
                true
            );
        }
    }

    private async Task<ResultadoAplicaciones<List<CuotaAplicaciones>>> ObtenerCuotasAsync(
        int empresaId,
        int ventaId,
        DateTime fechaPago,
        int cantidadCuotas
    )
    {
        var resultadoEmpresa = await ObtenerBaseDatosEmpresaAsync(empresaId);
        if (!resultadoEmpresa.Exito || resultadoEmpresa.Datos is null)
        {
            return ResultadoAplicaciones<List<CuotaAplicaciones>>.Fail(
                resultadoEmpresa.Mensaje,
                resultadoEmpresa.EsFatal
            );
        }

        try
        {
            var consulta = ConstruirConsultaCuotas(resultadoEmpresa.Datos.NombreBaseDatos);
            using var conexion = _sqlContext.CreateConnection();
            var filas = (
                await conexion.QueryAsync<CuotaAplicaciones>(
                    new CommandDefinition(
                        consulta,
                        new
                        {
                            VentaId = ventaId,
                            FechaPago = fechaPago.ToString("yyyyMMdd"),
                            CantidadCuotas = cantidadCuotas
                        },
                        commandTimeout: _configuracionAplicaciones.TiempoEsperaComandoSegundos
                    )
                )
            )
                .Where(item =>
                    item.MontoPago != 0
                    && (item.Capital + item.Interes + item.InteresMora + item.Seguro + item.Expensa + item.Multa) > 0
                )
                .ToList();

            return ResultadoAplicaciones<List<CuotaAplicaciones>>.Ok(filas);
        }
        catch (Exception ex)
        {
            return ResultadoAplicaciones<List<CuotaAplicaciones>>.Fail(
                $"No se pudo obtener el detalle de cuotas para la venta {ventaId}: {ex.Message}",
                true
            );
        }
    }

    private async Task<ResultadoAplicaciones<List<InstruccionCartaAplicaciones>>> ObtenerCartasAsync(string documentoComisionado)
    {
        try
        {
            using var conexion = _sqlContext.CreateConnection();
            var filas = (
                await conexion.QueryAsync<InstruccionCartaAplicaciones>(
                    new CommandDefinition(
                        SqlLetters,
                        new { DocumentoComisionado = documentoComisionado.Trim() },
                        commandTimeout: _configuracionAplicaciones.TiempoEsperaComandoSegundos
                    )
                )
            ).ToList();

            return ResultadoAplicaciones<List<InstruccionCartaAplicaciones>>.Ok(filas);
        }
        catch (Exception ex)
        {
            return ResultadoAplicaciones<List<InstruccionCartaAplicaciones>>.Fail(
                $"No se pudieron obtener las cartas del comisionado {documentoComisionado}: {ex.Message}",
                true
            );
        }
    }

    private async Task<ResultadoAplicaciones<List<DefinicionDescuentoAplicaciones>>> ObtenerDescuentosActivosAsync()
    {
        try
        {
            using var conexion = _sqlContext.CreateConnection();
            var filas = (
                await conexion.QueryAsync<DefinicionDescuentoAplicaciones>(
                    new CommandDefinition(
                        SqlActiveDiscounts,
                        commandTimeout: _configuracionAplicaciones.TiempoEsperaComandoSegundos
                    )
                )
            ).ToList();

            return ResultadoAplicaciones<List<DefinicionDescuentoAplicaciones>>.Ok(filas);
        }
        catch (Exception ex)
        {
            return ResultadoAplicaciones<List<DefinicionDescuentoAplicaciones>>.Fail(
                $"No se pudieron obtener los descuentos activos: {ex.Message}",
                true
            );
        }
    }

    private async Task<ResultadoAplicaciones<ClienteAplicaciones>> ObtenerClientePorDocumentoAsync(string numeroDocumento)
    {
        try
        {
            using var conexion = _sqlContext.CreateConnection();
            var fila = await conexion.QueryFirstOrDefaultAsync<ClienteAplicaciones>(
                new CommandDefinition(
                    SqlCustomerByDocument,
                    new { NumeroDocumento = numeroDocumento.Trim() },
                    commandTimeout: _configuracionAplicaciones.TiempoEsperaComandoSegundos
                )
            );

            return fila is null
                ? ResultadoAplicaciones<ClienteAplicaciones>.Fail(
                    $"No se encontro el cliente con documento {numeroDocumento}.",
                    true
                )
                : ResultadoAplicaciones<ClienteAplicaciones>.Ok(fila);
        }
        catch (Exception ex)
        {
            return ResultadoAplicaciones<ClienteAplicaciones>.Fail(
                $"No se pudo obtener el cliente por documento {numeroDocumento}: {ex.Message}",
                true
            );
        }
    }

    private async Task<ResultadoAplicaciones<BaseDatosEmpresaAplicaciones>> ObtenerBaseDatosEmpresaAsync(int empresaId)
    {
        try
        {
            using var conexion = _sqlContext.CreateConnection();
            var fila = await conexion.QueryFirstOrDefaultAsync<BaseDatosEmpresaAplicaciones>(
                new CommandDefinition(
                    SqlCompanyDatabase,
                    new { EmpresaId = empresaId },
                    commandTimeout: _configuracionAplicaciones.TiempoEsperaComandoSegundos
                )
            );

            return fila is null
                ? ResultadoAplicaciones<BaseDatosEmpresaAplicaciones>.Fail(
                    $"No se encontro la configuracion de empresa para {empresaId}.",
                    true
                )
                : ResultadoAplicaciones<BaseDatosEmpresaAplicaciones>.Ok(fila);
        }
        catch (Exception ex)
        {
            return ResultadoAplicaciones<BaseDatosEmpresaAplicaciones>.Fail(
                $"No se pudo obtener el catalogo de base de datos para la empresa {empresaId}: {ex.Message}",
                true
            );
        }
    }

    private async Task<ResultadoAplicaciones<List<RegistroPagoAplicaciones>>> ObtenerPagosPorCicloAsync(int ciclo)
    {
        try
        {
            using var conexion = _sqlContext.CreateConnection();
            var filas = (
                await conexion.QueryAsync<RegistroPagoAplicaciones>(
                    new CommandDefinition(
                        SqlPaymentsByCycle,
                        new { Ciclo = ciclo },
                        commandTimeout: _configuracionAplicaciones.TiempoEsperaComandoSegundos
                    )
                )
            ).ToList();

            return ResultadoAplicaciones<List<RegistroPagoAplicaciones>>.Ok(filas);
        }
        catch (Exception ex)
        {
            return ResultadoAplicaciones<List<RegistroPagoAplicaciones>>.Fail(
                $"No se pudo obtener AplicacionesPagos del ciclo {ciclo}: {ex.Message}",
                true
            );
        }
    }

    private async Task<ResultadoAplicaciones<int>> RegistrarReciboPagoAsync(RegistroPagoAplicaciones registroPago)
    {
        try
        {
            using var conexion = _sqlContext.CreateConnection();
            var id = await conexion.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    SqlInsertPaymentReceipt,
                    registroPago,
                    commandTimeout: _configuracionAplicaciones.TiempoEsperaComandoSegundos
                )
            );

            return ResultadoAplicaciones<int>.Ok(id);
        }
        catch (Exception ex)
        {
            return ResultadoAplicaciones<int>.Fail(
                $"No se pudo registrar el recibo en AplicacionesPagos: {ex.Message}",
                true
            );
        }
    }

    private async Task<ResultadoAplicaciones> ActualizarFacturaAsync(
        int empresaId,
        int ventaId,
        int reciboId,
        int facturaId,
        string observacion
    )
    {
        try
        {
            using var conexion = _sqlContext.CreateConnection();
            await conexion.ExecuteAsync(
                new CommandDefinition(
                    SqlUpdateInvoice,
                    new
                    {
                        EmpresaId = empresaId,
                        VentaId = ventaId,
                        ReciboId = reciboId,
                        FacturaId = facturaId,
                        Observacion = observacion
                    },
                    commandTimeout: _configuracionAplicaciones.TiempoEsperaComandoSegundos
                )
            );

            return ResultadoAplicaciones.Ok();
        }
        catch (Exception ex)
        {
            return ResultadoAplicaciones.Fail($"No se pudo actualizar la factura del recibo {reciboId}: {ex.Message}", true);
        }
    }

    private async Task<ResultadoAplicaciones<int>> ContarErroresFacturacionAsync(int ciclo)
    {
        try
        {
            using var conexion = _sqlContext.CreateConnection();
            var cantidad = await conexion.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    SqlCountInvoiceFailures,
                    new { Ciclo = ciclo },
                    commandTimeout: _configuracionAplicaciones.TiempoEsperaComandoSegundos
                )
            );

            return ResultadoAplicaciones<int>.Ok(cantidad);
        }
        catch (Exception ex)
        {
            return ResultadoAplicaciones<int>.Fail(
                $"No se pudo contar los errores de facturacion del ciclo {ciclo}: {ex.Message}",
                true
            );
        }
    }

    private async Task<ResultadoAplicaciones<List<RegistroProrrateoAplicaciones>>> ObtenerProrrateosActivosAsync(int ciclo, string numeroDocumento)
    {
        try
        {
            using var conexion = _sqlContext.CreateConnection();
            var filas = (
                await conexion.QueryAsync<RegistroProrrateoAplicaciones>(
                    new CommandDefinition(
                        SqlActiveProrations,
                        new
                        {
                            Ciclo = ciclo,
                            NumeroDocumento = numeroDocumento.Trim()
                        },
                        commandTimeout: _configuracionAplicaciones.TiempoEsperaComandoSegundos
                    )
                )
            ).ToList();

            return ResultadoAplicaciones<List<RegistroProrrateoAplicaciones>>.Ok(filas);
        }
        catch (Exception ex)
        {
            return ResultadoAplicaciones<List<RegistroProrrateoAplicaciones>>.Fail(
                $"No se pudo obtener el prorrateo activo del documento {numeroDocumento}: {ex.Message}",
                true
            );
        }
    }

    private async Task<ResultadoAplicaciones> DeshabilitarProrrateosAsync(int ciclo, string numeroDocumento)
    {
        try
        {
            using var conexion = _sqlContext.CreateConnection();
            await conexion.ExecuteAsync(
                new CommandDefinition(
                    SqlDisableProrations,
                    new
                    {
                        Ciclo = ciclo,
                        NumeroDocumento = numeroDocumento.Trim()
                    },
                    commandTimeout: _configuracionAplicaciones.TiempoEsperaComandoSegundos
                )
            );

            return ResultadoAplicaciones.Ok();
        }
        catch (Exception ex)
        {
            return ResultadoAplicaciones.Fail(
                $"No se pudo deshabilitar el prorrateo activo del documento {numeroDocumento}: {ex.Message}",
                true
            );
        }
    }

    private async Task<ResultadoAplicaciones<int>> InsertarProrrateoAsync(RegistroProrrateoAplicaciones registro)
    {
        try
        {
            using var conexion = _sqlContext.CreateConnection();
            var id = await conexion.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    SqlInsertProration,
                    registro,
                    commandTimeout: _configuracionAplicaciones.TiempoEsperaComandoSegundos
                )
            );

            return ResultadoAplicaciones<int>.Ok(id);
        }
        catch (Exception ex)
        {
            return ResultadoAplicaciones<int>.Fail(
                $"No se pudo registrar el prorrateo del documento {registro.DocumentoCliente}: {ex.Message}"
            );
        }
    }

    private async Task<ResultadoAplicaciones<int>> EjecutarPagoSionAsync(
        BaseDatosEmpresaAplicaciones empresa,
        int ventaId,
        DateTime fechaPago,
        string numeroTransaccionExterna,
        decimal montoPagar
    )
    {
        try
        {
            var consulta = ConstruirConsultaPagoSion(empresa.NombreBaseDatos);
            var parametros = new DynamicParameters();
            parametros.Add("VentaId", ventaId);
            parametros.Add("FechaPago", fechaPago.ToString("yyyyMMdd"));
            parametros.Add("NumeroTransaccionExterna", numeroTransaccionExterna);
            parametros.Add("InstallmentsToPay", 1);
            parametros.Add("MontoPagar", montoPagar);
            parametros.Add("CodigoAgente", -13);
            parametros.Add("MyId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            using var conexion = _sqlContext.CreateConnection();
            await conexion.ExecuteAsync(
                new CommandDefinition(
                    consulta,
                    parametros,
                    commandTimeout: _configuracionAplicaciones.TiempoEsperaPagoSegundos
                )
            );

            var reciboId = parametros.Get<int>("MyId");
            return reciboId <= 0
                ? ResultadoAplicaciones<int>.Fail("El procedimiento almacenado no devolvio un recibo valido.")
                : ResultadoAplicaciones<int>.Ok(reciboId);
        }
        catch (Exception ex)
        {
            return ResultadoAplicaciones<int>.Fail($"No se pudo ejecutar spPagarCuotasXVenta: {ex.Message}");
        }
    }

    private async Task<ResultadoAplicaciones<ResultadoFacturaAplicaciones>> GenerarFacturaAsync(
        int empresaServicioWebId,
        int proyectoId,
        int ventaId,
        int reciboId,
        string productoId
    )
    {
        if (!_configuracionAplicaciones.HabilitarPasarelaFacturacion)
        {
            return ResultadoAplicaciones<ResultadoFacturaAplicaciones>.Ok(
                new ResultadoFacturaAplicaciones
                {
                    FacturaId = 0,
                    CodigoServicio = 200,
                    CodigoError = 0,
                    MensajeServicio = "Facturacion deshabilitada por configuracion.",
                    EjecutadoCorrectamente = true
                }
            );
        }

        if (string.IsNullOrWhiteSpace(_configuracionAplicaciones.Facturacion.PuntoFinal))
        {
            return ResultadoAplicaciones<ResultadoFacturaAplicaciones>.Fail("No se configuro el endpoint del servicio de facturacion.");
        }

        var xmlSolicitud = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <soap:Envelope
                xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"
                xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
                xmlns:xsd="http://www.w3.org/2001/XMLSchema"
                xmlns:urn="urn:gruposion.com.bo">
              <soap:Body>
                <urn:wsGenerarFacturaRecibo soap:encodingStyle="http://schemas.xmlsoap.org/soap/encoding/">
                  <login xsi:type="xsd:string">{SecurityElement.Escape(_configuracionAplicaciones.Facturacion.Usuario)}</login>
                  <password xsi:type="xsd:string">{SecurityElement.Escape(_configuracionAplicaciones.Facturacion.Contrasena)}</password>
                  <codAgente xsi:type="xsd:string">{SecurityElement.Escape(_configuracionAplicaciones.Facturacion.CodigoAgente)}</codAgente>
                  <llaveCnx xsi:type="xsd:string">{SecurityElement.Escape(_configuracionAplicaciones.Facturacion.LlaveConexion)}</llaveCnx>
                  <codEmpresa xsi:type="xsd:int">{empresaServicioWebId}</codEmpresa>
                  <codProyecto xsi:type="xsd:int">{proyectoId}</codProyecto>
                  <nroVenta xsi:type="xsd:int">{ventaId}</nroVenta>
                  <codRecibo xsi:type="xsd:int">{reciboId}</codRecibo>
                  <codProducto xsi:type="xsd:string">{SecurityElement.Escape(productoId)}</codProducto>
                </urn:wsGenerarFacturaRecibo>
              </soap:Body>
            </soap:Envelope>
            """;

        using var solicitud = new HttpRequestMessage(HttpMethod.Post, _configuracionAplicaciones.Facturacion.PuntoFinal)
        {
            Content = new StringContent(xmlSolicitud, Encoding.UTF8, "text/xml")
        };
        solicitud.Headers.Add("SOAPAction", _configuracionAplicaciones.Facturacion.AccionSoap);

        try
        {
            var clienteHttp = _fabricaHttpClient.CreateClient(nameof(AplicacionesRepositorio));
            clienteHttp.Timeout = TimeSpan.FromSeconds(_configuracionAplicaciones.Facturacion.TiempoEsperaSegundos);

            using var respuesta = await clienteHttp.SendAsync(solicitud);
            var cuerpo = await respuesta.Content.ReadAsStringAsync();

            if (!respuesta.IsSuccessStatusCode)
            {
                return ResultadoAplicaciones<ResultadoFacturaAplicaciones>.Fail(
                    $"El servicio de facturacion devolvio {(int)respuesta.StatusCode}: {respuesta.ReasonPhrase}."
                );
            }

            var documentoXml = XDocument.Parse(cuerpo);
            var valores = documentoXml
                .Descendants()
                .Where(item =>
                    item.Name.LocalName is "codServicio" or "msgServicio" or "error" or "mensajeError" or "idFactura"
                )
                .GroupBy(item => item.Name.LocalName)
                .ToDictionary(group => group.Key, group => group.First().Value);

            var resultado = new ResultadoFacturaAplicaciones
            {
                CodigoServicio = IntentarConvertirEntero(valores, "codServicio"),
                MensajeServicio = IntentarObtenerValor(valores, "msgServicio"),
                CodigoError = IntentarConvertirEntero(valores, "error"),
                MensajeError = IntentarObtenerValor(valores, "mensajeError"),
                FacturaId = IntentarConvertirEntero(valores, "idFactura", -1)
            };
            resultado.EjecutadoCorrectamente = resultado.CodigoError == 0 && resultado.FacturaId >= 0;

            return ResultadoAplicaciones<ResultadoFacturaAplicaciones>.Ok(resultado);
        }
        catch (Exception ex)
        {
            return ResultadoAplicaciones<ResultadoFacturaAplicaciones>.Fail(
                $"No se pudo consumir el servicio de facturacion: {ex.Message}"
            );
        }
    }

    private static int IntentarConvertirEntero(IDictionary<string, string> valores, string llave, int valorPredeterminado = 0)
    {
        return valores.TryGetValue(llave, out var valorCrudo) && int.TryParse(valorCrudo, out var valorConvertido) ? valorConvertido : valorPredeterminado;
    }

    private static string IntentarObtenerValor(IDictionary<string, string> valores, string llave)
    {
        return valores.TryGetValue(llave, out var valorCrudo) ? valorCrudo : string.Empty;
    }
}

internal sealed class ResultadoFacturaAplicaciones
{
    public bool EjecutadoCorrectamente { get; set; }
    public int FacturaId { get; set; }
    public int CodigoServicio { get; set; }
    public int CodigoError { get; set; }
    public string MensajeServicio { get; set; } = string.Empty;
    public string MensajeError { get; set; } = string.Empty;
}
