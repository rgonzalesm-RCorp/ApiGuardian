namespace ApiGuardian.Infrastructure.Repositories;

public partial class AplicacionesRepository
{
    private const string SqlExistsAplicacionesComisionado = """
        SELECT COUNT(1)
        FROM BDQISHUR.dbo.AplicacionesComisionado
        WHERE Ciclo = @Cycle;
        """;

    private const string SqlExistsAplicacionesComisionPorEmpresa = """
        SELECT COUNT(1)
        FROM BDQISHUR.dbo.AplicacionesComisionPorEmpresa
        WHERE Ciclo = @Cycle;
        """;

    private const string SqlDeleteAplicacionesProrrateo = """
        DELETE FROM BDQISHUR.dbo.AplicacionesProrrateo
        WHERE Ciclo = @Cycle;
        """;

    private const string SqlDeleteAplicacionesPagos = """
        DELETE FROM BDQISHUR.dbo.AplicacionesPagos
        WHERE Ciclo = @Cycle;
        """;

    private const string SqlDeleteAplicacionesComisionado = """
        DELETE FROM BDQISHUR.dbo.AplicacionesComisionado
        WHERE Ciclo = @Cycle;
        """;

    private const string SqlDeleteAplicacionesComisionPorEmpresa = """
        DELETE FROM BDQISHUR.dbo.AplicacionesComisionPorEmpresa
        WHERE Ciclo = @Cycle;
        """;

    private const string SqlDeleteGuardianRetencionEmpresa = """
        DELETE FROM tbl_retencionempresa
        WHERE lciclo_id = @Cycle;
        """;

    private const string SqlDeleteGuardianRetencionEmpresaExterior = """
        DELETE FROM tbl_retencionempresa_exterior
        WHERE lciclo_id = @Cycle;
        """;

    private const string SqlInsertAplicacionesComisionado = """
        INSERT INTO BDQISHUR.dbo.AplicacionesComisionado
        (
            Carnet,
            Codigo,
            Ciclo,
            Estado,
            FechaRegistro,
            Lcontacto_id,
            Nombre,
            Observacion,
            TotalAplicar
        )
        VALUES
        (
            @DocumentNumber,
            @Code,
            @Cycle,
            @Status,
            @RegisteredAt,
            @ContactId,
            @FullName,
            @Observation,
            @TotalToApply
        );
        """;

    private const string SqlPendingComisionados = """
        SELECT *
        FROM (
            SELECT
                appComisionado.Id,
                appComisionado.Ciclo Cycle,
                appComisionado.Lcontacto_id ContactId,
                appComisionado.Codigo Code,
                LTRIM(RTRIM(appComisionado.Carnet)) DocumentNumber,
                RTRIM(ISNULL(appComisionado.Nombre, '')) FullName,
                ISNULL(appComisionado.TotalAplicar, 0) TotalToApply,
                appComisionado.FechaRegistro RegisteredAt,
                appComisionado.Estado Status,
                RTRIM(ISNULL(appComisionado.Observacion, '')) Observation,
                ISNULL(datMontoAplicado.monto_aplicado, 0) TotalAppliedAmount,
                ISNULL(appComisionado.TotalAplicar, 0) - ISNULL(datMontoAplicado.monto_aplicado, 0) RemainingAmount
            FROM BDQISHUR.dbo.AplicacionesComisionado appComisionado
            LEFT JOIN (
                SELECT
                    LTRIM(RTRIM(p.CI_Cliente)) ClientDocument,
                    p.Ciclo,
                    SUM(ISNULL(p.Monto, 0)) monto_aplicado
                FROM BDQISHUR.dbo.AplicacionesPagos p
                WHERE p.Ciclo = @Cycle
                GROUP BY LTRIM(RTRIM(p.CI_Cliente)), p.Ciclo
            ) datMontoAplicado
                ON LTRIM(RTRIM(appComisionado.Carnet)) = datMontoAplicado.ClientDocument
               AND appComisionado.Ciclo = datMontoAplicado.Ciclo
            WHERE appComisionado.Ciclo = @Cycle
              AND appComisionado.Estado = 0
              AND LTRIM(RTRIM(appComisionado.Carnet)) <> '4823437'
        ) todo
        WHERE ISNULL(todo.RemainingAmount, 0) <> 0
          AND ISNULL(todo.Observation, '') NOT LIKE '%Procesado%'
        ORDER BY todo.Id;
        """;

    private const string SqlMarkComisionadoProcessed = """
        UPDATE BDQISHUR.dbo.AplicacionesComisionado
        SET Observacion = 'Procesado'
        WHERE Ciclo = @Cycle
          AND LTRIM(RTRIM(Carnet)) = @DocumentNumber;
        """;

    private const string SqlCompanyTotalsByDocument = """
        SELECT
            LTRIM(RTRIM(Carnet)) DocumentNumber,
            SUM(ISNULL(Neto, 0)) TotalToApply
        FROM BDQISHUR.dbo.AplicacionesComisionPorEmpresa
        WHERE Ciclo = @Cycle
        GROUP BY LTRIM(RTRIM(Carnet));
        """;

    private const string SqlCompanyCommissionByDocument = """
        SELECT
            ROW_NUMBER() OVER(ORDER BY a.Empresa ASC) Id,
            MAX(a.Ciclo) Cycle,
            MAX(LTRIM(RTRIM(a.Carnet))) DocumentNumber,
            a.Empresa CompanyId,
            MAX(ISNULL(c.IDBD_WS, 0)) CompanyWebServiceId,
            MAX(ISNULL(c.NOMBREBD, '')) CompanyDatabaseName,
            SUM(ISNULL(a.VentasPersonales, 0)) PersonalSales,
            SUM(ISNULL(a.VentasGrupales, 0)) GroupSales,
            SUM(ISNULL(a.Residual, 0)) Residual,
            SUM(ISNULL(a.MontoComision, 0)) CommissionAmount,
            SUM(ISNULL(a.Retencion, 0)) RetentionAmount,
            SUM(ISNULL(a.Neto, 0)) NetAmount,
            SUM(ISNULL(a.Bruto, 0)) GrossAmount,
            SUM(ISNULL(a.Porcentaje13, 0)) ThirteenPercentAmount,
            CASE WHEN SUM(ISNULL(a.Factura, 0)) > 0 THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END RequiresInvoice
        FROM BDQISHUR.dbo.AplicacionesComisionPorEmpresa a
        LEFT JOIN BDComisiones.dbo.CNX_BDCOMISIONES c ON c.IDBD = a.Empresa
        WHERE LTRIM(RTRIM(a.Carnet)) = @DocumentNumber
          AND a.Ciclo = @Cycle
        GROUP BY a.Empresa
        ORDER BY a.Empresa;
        """;

    private const string SqlCompanyMapping = """
        SELECT
            lempresa_id LegacyCompanyId,
            IDBD CompanyId,
            NOMBREBD DatabaseName
        FROM BDQISHUR.dbo.AplicacionesEmpresaGuardianAsumeSion;
        """;

    private const string SqlInsertCompanyCommission = """
        INSERT INTO BDQISHUR.dbo.AplicacionesComisionPorEmpresa
        (
            Bruto,
            Carnet,
            Ciclo,
            Empresa,
            Factura,
            MontoComision,
            Neto,
            Porcentaje13,
            Retencion,
            Residual,
            VentasGrupales,
            VentasPersonales,
            IdComprobante,
            IdEmpresaGuardianGeneroComision
        )
        VALUES
        (
            @GrossAmount,
            @DocumentNumber,
            @Cycle,
            @CompanyId,
            @InvoiceFlag,
            @CommissionAmount,
            @NetAmount,
            @ThirteenPercentAmount,
            @RetentionAmount,
            @Residual,
            @GroupSales,
            @PersonalSales,
            0,
            @LegacyCompanyId
        );
        """;

    private const string SqlInsertMissingPriorities = """
        INSERT INTO BDQISHUR.dbo.AplicacionesPrioridad
        (
            idEmpresa,
            idProyecto,
            prioridad
        )
        SELECT
            p.IDEMPRESA,
            CAST(p.IDALMACEN AS INT),
            1000
        FROM BDComisiones.dbo.vwPROYECTOS_ALL p
        LEFT JOIN BDQISHUR.dbo.AplicacionesPrioridad ap
            ON ap.idEmpresa = p.IDEMPRESA
           AND ap.idProyecto = CAST(p.IDALMACEN AS INT)
        WHERE ap.id IS NULL;
        """;

    private const string SqlGuardianCompanyCommission = """
        SELECT
            e.lciclo_id Cycle,
            e.idempresa LegacyCompanyId,
            TRIM(e.carnet) DocumentNumber,
            COALESCE(e.vpers, 0) PersonalSales,
            COALESCE(e.vgrupo, 0) GroupSales,
            COALESCE(e.residual, 0) Residual,
            COALESCE(e.montocomision, 0) CommissionAmount,
            COALESCE(e.lpresentafactura, 0) InvoiceFlag,
            COALESCE(e.porcentajeret, 0) RetentionPercentage,
            COALESCE(e.montoretencion, 0) RetentionAmount,
            COALESCE(e.total_comision, 0) TotalCommission
        FROM tbl_retencionempresa e
        WHERE e.lciclo_id = @Cycle

        UNION ALL

        SELECT
            e.lciclo_id Cycle,
            e.idempresa LegacyCompanyId,
            TRIM(e.carnet) DocumentNumber,
            COALESCE(e.vpers, 0) PersonalSales,
            COALESCE(e.vgrupo, 0) GroupSales,
            COALESCE(e.residual, 0) Residual,
            COALESCE(e.montocomision, 0) CommissionAmount,
            COALESCE(e.lpresentafactura, 0) InvoiceFlag,
            COALESCE(e.porcentajeret, 0) RetentionPercentage,
            COALESCE(e.montoretencion, 0) RetentionAmount,
            COALESCE(e.total_comision, 0) TotalCommission
        FROM tbl_retencionempresa_exterior e
        WHERE e.lciclo_id = @Cycle;
        """;

    private const string SqlGuardianCommissionAgents = """
        SELECT
            ac.lcontacto_id ContactId,
            CAST(ac.scodigo AS SIGNED) Code,
            TRIM(ac.scedulaidentidad) DocumentNumber,
            ac.snombrecompleto FullName,
            ROUND(
                (
                    IFNULL(vie.sumar1, 0)
                    + IFNULL(mar.sumar2, 0)
                    + IFNULL(jor.sumar3, 0)
                    + IFNULL(bon.sumar10, 0)
                    + IFNULL(lid.sumar11, 0)
                    + IFNULL(top.sumar12, 0)
                ) - IFNULL(rete.MontoRet, 0),
                2
            ) TotalToApply
        FROM administracioncontacto ac
        LEFT JOIN (
            SELECT COUNT(a.lcontacto_id) contar1, SUM(a.dcomision) sumar1, a.lcontacto_id
            FROM administracionventapersonal a
            INNER JOIN administracioncontrato b ON b.lcontrato_id = a.lcontrato_id
            WHERE a.lciclo_id = @Cycle
            GROUP BY a.lcontacto_id
        ) vie ON ac.lcontacto_id = vie.lcontacto_id
        LEFT JOIN (
            SELECT COUNT(c.lcontacto_id) contar2, SUM(c.dcomision) sumar2, c.lcontacto_id
            FROM administracionventagrupo c
            INNER JOIN administracioncontrato d ON d.lcontrato_id = c.lcontrato_id
            WHERE c.lciclo_id = @Cycle
            GROUP BY c.lcontacto_id
        ) mar ON ac.lcontacto_id = mar.lcontacto_id
        LEFT JOIN (
            SELECT COUNT(e.lcontacto_id) contar3, SUM(e.dmonto) sumar3, e.lcontacto_id
            FROM administracionredempresacomplejo e
            WHERE e.lciclo_id = @Cycle
            GROUP BY e.lcontacto_id
        ) jor ON ac.lcontacto_id = jor.lcontacto_id
        LEFT JOIN (
            SELECT COUNT(c.vendedores_mes_id) contar10, SUM(c.monto) sumar10, c.vendedores_mes_id lcontacto_id
            FROM t_ganadores_bonoliderazgo_empresa_pagar c
            WHERE c.lciclo_id = @Cycle
            GROUP BY c.vendedores_mes_id
        ) bon ON ac.lcontacto_id = bon.lcontacto_id
        LEFT JOIN (
            SELECT COUNT(c.vendedores_id) contar11, SUM(c.pagar) sumar11, c.vendedores_id lcontacto_id
            FROM t_bono_liderazgo c
            WHERE c.lciclo_id = @Cycle
            GROUP BY c.vendedores_id
        ) lid ON ac.lcontacto_id = lid.lcontacto_id
        LEFT JOIN (
            SELECT COUNT(c.vendedor_lcontacto_id) contar12, SUM(c.pagar) sumar12, c.vendedor_lcontacto_id lcontacto_id
            FROM t_top_vendedores c
            WHERE c.lciclo_id = @Cycle
            GROUP BY c.vendedor_lcontacto_id
        ) top ON ac.lcontacto_id = top.lcontacto_id
        LEFT JOIN (
            SELECT
                a.lcontacto_id,
                SUM(a.MontoRet) MontoRet
            FROM (
                SELECT
                    IFNULL(SUM(montoretencion), 0) MontoRet,
                    lcontacto_id
                FROM tbl_retencionempresa
                WHERE lciclo_id = @Cycle
                GROUP BY lcontacto_id

                UNION ALL

                SELECT
                    IFNULL(SUM(montoretencion), 0) MontoRet,
                    lcontacto_id
                FROM tbl_retencionempresa_exterior
                WHERE lciclo_id = @Cycle
                GROUP BY lcontacto_id
            ) a
            GROUP BY a.lcontacto_id
        ) rete ON rete.lcontacto_id = ac.lcontacto_id
        INNER JOIN administracionventapersonal h ON ac.lcontacto_id = h.lcontacto_id AND h.lciclo_id = @Cycle
        WHERE ac.scedulaidentidad <> '4823437'
        GROUP BY
            ac.lcontacto_id,
            ac.scodigo,
            ac.scedulaidentidad,
            ac.snombrecompleto,
            vie.sumar1,
            mar.sumar2,
            jor.sumar3,
            bon.sumar10,
            lid.sumar11,
            top.sumar12,
            rete.MontoRet
        HAVING ROUND(
            (
                IFNULL(vie.sumar1, 0)
                + IFNULL(mar.sumar2, 0)
                + IFNULL(jor.sumar3, 0)
                + IFNULL(bon.sumar10, 0)
                + IFNULL(lid.sumar11, 0)
                + IFNULL(top.sumar12, 0)
            ) - IFNULL(rete.MontoRet, 0),
            2
        ) > 0
        ORDER BY ac.snombrecompleto;
        """;

    private const string SqlPortfolio = """
        SELECT
            p.IDEMPRESA CompanyId,
            CAST(p.IDPROYECTO AS INT) ProjectId,
            CAST(p.IDVENTA AS INT) SaleId,
            ISNULL(p.TOTALVENTA, 0) TotalSale,
            ISNULL(p.TOTALDEUDA, 0) TotalDebt,
            p.FECHA SaleDate,
            LTRIM(RTRIM(p.LOTE)) LotCode,
            LTRIM(RTRIM(p.DOCID)) DocumentNumber,
            RTRIM(ISNULL(p.PROYECTO, '')) ProjectName,
            ISNULL(p.CUOTASPENDIENTES, 0) PendingInstallments,
            ISNULL(p.CUOTASVENCIDAS, 0) OverdueInstallments,
            ISNULL(p.CUOTAS_LOTES_VENCIDAS, 0) MaturedInstallments,
            CAST(ISNULL(p.IDVENDEDOR, 0) AS INT) SalespersonId,
            CAST(ISNULL(p.IDCLIENTE, 0) AS INT) ClientId,
            p.MODFECHA LastModifiedAt,
            RTRIM(ISNULL(p.EMPRESA, '')) CompanyName,
            ISNULL(a.prioridad, 1000) Priority,
            CONCAT(CAST(ISNULL(p.IDCLIENTE, 0) AS varchar(20)), ':', LTRIM(RTRIM(p.LOTE))) ProductKey
        FROM BDComisiones.dbo.vwLOTES_GRL_DOCID p
        LEFT JOIN BDQISHUR.dbo.AplicacionesPrioridad a
            ON a.idEmpresa = p.IDEMPRESA
           AND a.idProyecto = CAST(p.IDPROYECTO AS INT)
        WHERE LTRIM(RTRIM(p.DOCID)) = @DocumentNumber
          AND CAST(p.IDPROYECTO AS INT) <> 51
          AND CAST(p.IDPROYECTO AS INT) <> 46
          AND p.IDEMPRESA <> 13
          /*AND NOT EXISTS (
              SELECT 1
              FROM BDQISHUR.dbo.AplicacionesProyectosExcluidos e
              WHERE e.idEntidad = 1
                AND e.idProyecto = CAST(p.IDPROYECTO AS INT)
          )*/
        ORDER BY ISNULL(a.prioridad, 1000) ASC, p.CUOTASVENCIDAS DESC;
        """;

    private const string SqlProductPaidOff = """
        SELECT COUNT(1)
        FROM BDComisiones.dbo.vwLOTES_GRL_DOCID
        WHERE LTRIM(RTRIM(DOCID)) = @DocumentNumber
          AND LTRIM(RTRIM(LOTE)) = @LotCode
          AND ISNULL(TOTALDEUDA, 0) = 0;
        """;

    private const string SqlReprogrammedProducts = """
        SELECT
            LTRIM(RTRIM(r.IDPRODUCTO)) ProductId,
            CAST(r.IDCLIENTE AS INT) ClientId
        FROM BDComisiones.dbo.vwLISTAPRODUCTOS_NEW r
        INNER JOIN BDComisiones.dbo.vwLOTES_GRL_DOCID C ON C.IDCLIENTE = R.IDCLIENTE
        WHERE r.GLOSA LIKE '%reprogramacion%' AND C.DOCID = @DocumentNumber;
        """;

    private const string SqlLetters = """
        SELECT
            LTRIM(RTRIM(carta.ci_cliente_comisionado)) CommissionerDocument,
            LTRIM(RTRIM(carta.ci_cliente_beneficiario)) BeneficiaryDocument,
            LTRIM(RTRIM(cartaProd.cod_producto)) ProductCode,
            ISNULL(cartaProd.cantidad_cuotas, 0) InstallmentsToApply,
            cartaProd.fecha_fin_aplicaciones EndDate,
            cartaProd.id_venta SaleId,
            cartaProd.cod_empresa CompanyId,
            cartaProd.id_proyecto ProjectId
        FROM DBITSIS.dbo.config_solicitud_carta carta
        INNER JOIN DBITSIS.dbo.config_solicitud_carta_producto cartaProd
            ON carta.id = cartaProd.id_config_solicitud
        WHERE carta.activo = 1
          AND cartaProd.habilitado = 1
          AND LTRIM(RTRIM(carta.ci_cliente_comisionado)) = @CommissionerDocument;
        """;

    private const string SqlActiveDiscounts = """
        SELECT
            d.cod_empresa CompanyId,
            d.descuento Description,
            CASE WHEN d.es_porcentaje = 1 THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END IsPercentage,
            LTRIM(RTRIM(dF.documento_freelancer)) CommissionerDocument,
            CAST(dF.monto_porcentaje AS decimal(18, 2)) AmountOrPercent,
            d.intercompanhia IntercompanyFlag,
            d.id PaymentTypeId
        FROM DBITSIS.dbo.aplicaciones_descuentos d
        INNER JOIN DBITSIS.dbo.aplicaciones_descuentos_freelancer dF
            ON d.id = dF.id_aplicaciones_descuento
        WHERE dF.activo = 1
          AND d.activo = 1;
        """;

    private const string SqlCustomerByDocument = """
        SELECT TOP 1
            CAST(c.idCliente AS INT) ClientId,
            LTRIM(RTRIM(c.DOCID)) DocumentNumber,
            RTRIM(ISNULL(c.NOMBRE, '')) FullName
        FROM BDComisiones.dbo.grlCLIENTE c
        WHERE LTRIM(RTRIM(c.DOCID)) = @DocumentNumber;
        """;

    private const string SqlCompanyDatabase = """
        SELECT TOP 1
            CAST(IDBD AS INT) CompanyId,
            CAST(ISNULL(IDBD_WS, 0) AS INT) WebServiceCompanyId,
            RTRIM(ISNULL(NOMBREBD, '')) DatabaseName
        FROM BDComisiones.dbo.CNX_BDCOMISIONES
        WHERE IDBD = @CompanyId;
        """;

    private const string SqlPaymentsByCycle = """
        SELECT
            CAST(ISNULL(Id, 0) AS INT) Id,
            CAST(ISNULL(Ciclo, 0) AS INT) Cycle,
            CAST(ISNULL(Id_Empresa, 0) AS INT) CompanyId,
            CAST(ISNULL(Id_Venta, 0) AS INT) SaleId,
            CAST(ISNULL(Id_Cliente, 0) AS INT) ClientId,
            LTRIM(RTRIM(ISNULL(CI_Cliente, ''))) ClientDocument,
            RTRIM(ISNULL(Id_Producto, '')) ProductId,
            CAST(ISNULL(Expensa, 0) AS decimal(18, 2)) Expense,
            CAST(ISNULL(Monto, 0) AS decimal(18, 2)) Amount,
            Fecha CreatedAt,
            CAST(ISNULL(Id_Recibo, 0) AS INT) ReceiptId,
            CAST(ISNULL(Id_Factura, 0) AS INT) InvoiceId,
            RTRIM(ISNULL(Observacion, '')) Observation,
            CAST(ISNULL(TipoPago, 0) AS INT) PaymentTypeId,
            CAST(ISNULL(Intercompania, 0) AS INT) IntercompanyFlag
        FROM BDQISHUR.dbo.AplicacionesPagos
        WHERE Ciclo = @Cycle;
        """;

    private const string SqlInsertPaymentReceipt = """
        INSERT INTO BDQISHUR.dbo.AplicacionesPagos
        (
            Ciclo,
            Id_Empresa,
            Id_Venta,
            Id_Cliente,
            CI_Cliente,
            Id_Producto,
            Expensa,
            Monto,
            Fecha,
            Id_Recibo,
            Id_Factura,
            Observacion,
            TipoPago,
            Intercompania
        )
        VALUES
        (
            @Cycle,
            @CompanyId,
            @SaleId,
            @ClientId,
            @ClientDocument,
            @ProductId,
            @Expense,
            @Amount,
            @CreatedAt,
            @ReceiptId,
            @InvoiceId,
            @Observation,
            @PaymentTypeId,
            @IntercompanyFlag
        );
        SELECT CAST(SCOPE_IDENTITY() AS int);
        """;

    private const string SqlUpdateInvoice = """
        UPDATE BDQISHUR.dbo.AplicacionesPagos
        SET Id_Factura = @InvoiceId,
            Observacion = @Observation
        WHERE Id_Empresa = @CompanyId
          AND Id_Venta = @SaleId
          AND Id_Recibo = @ReceiptId;
        """;

    private const string SqlCountInvoiceFailures = """
        SELECT COUNT(1)
        FROM BDQISHUR.dbo.AplicacionesPagos
        WHERE Ciclo = @Cycle
          AND ISNULL(Id_Factura, 0) = -1
          AND ISNULL(Id_Producto, '') <> '';
        """;

    private const string SqlActiveProrations = """
        SELECT
            CAST(ISNULL(Id, 0) AS INT) Id,
            LTRIM(RTRIM(ISNULL(CiCliente, ''))) ClientDocument,
            CAST(ISNULL(Ciclo, 0) AS INT) Cycle,
            CAST(ISNULL(EmpresaPresta, 0) AS INT) LendingCompanyId,
            CAST(ISNULL(EmpresaRecibe, 0) AS INT) ReceivingCompanyId,
            CAST(ISNULL(IdCliente, 0) AS INT) ClientId,
            CAST(ISNULL(IdRecibo, 0) AS INT) ReceiptId,
            CAST(ISNULL(Monto, 0) AS decimal(18, 2)) Amount,
            CAST(ISNULL(Habilitado, 0) AS bit) Enabled,
            CAST(ISNULL(IdComprobante, 0) AS INT) ReceiptVoucherId,
            CAST(ISNULL(Intercompania, 0) AS INT) IntercompanyFlag,
            CAST(ISNULL(TipoPago, 0) AS INT) PaymentTypeId
        FROM BDQISHUR.dbo.AplicacionesProrrateo
        WHERE Ciclo = @Cycle
          AND LTRIM(RTRIM(CiCliente)) = @DocumentNumber
          AND ISNULL(Habilitado, 0) = 1;
        """;

    private const string SqlDisableProrations = """
        UPDATE BDQISHUR.dbo.AplicacionesProrrateo
        SET Habilitado = 0
        WHERE Ciclo = @Cycle
          AND LTRIM(RTRIM(CiCliente)) = @DocumentNumber
          AND ISNULL(Habilitado, 0) = 1;
        """;

    private const string SqlInsertProration = """
        INSERT INTO BDQISHUR.dbo.AplicacionesProrrateo
        (
            CiCliente,
            Ciclo,
            EmpresaPresta,
            EmpresaRecibe,
            IdCliente,
            IdRecibo,
            Monto,
            Habilitado,
            IdComprobante,
            Intercompania,
            TipoPago
        )
        VALUES
        (
            @ClientDocument,
            @Cycle,
            @LendingCompanyId,
            @ReceivingCompanyId,
            @ClientId,
            @ReceiptId,
            @Amount,
            @Enabled,
            @ReceiptVoucherId,
            @IntercompanyFlag,
            @PaymentTypeId
        );
        SELECT CAST(SCOPE_IDENTITY() AS int);
        """;

    private static string BuildInstallmentQuotesQuery(string databaseName)
    {
        return $"""
            SET DATEFIRST 1;
            SELECT
                NROCUOTA InstallmentNumber,
                CAPITAL Capital,
                INTERES Interest,
                INTERES_MORA InterestPenalty,
                SEGURO Insurance,
                EXPENSA Expense,
                MULTA Penalty,
                IMPORTE_CUOTA InstallmentAmount,
                FVENCIMIENTO DueDate,
                FILA RowNumber,
                FCALCULO_INTERES InterestCalculationDate,
                PAGOS_A_CUENTA_DISTRIBUIDO DistributedPartialPayments,
                MONTO_PAGO PaymentAmount,
                PAGOS_A_CUENTA PartialPayments
            FROM {databaseName}.dbo.ffObtenerMontoAPagar(@SaleId, @PaymentDate, @QuotaCount);
            """;
    }

    private static string BuildSionPaymentProcedureQuery(string databaseName)
    {
        return $"""
            EXEC @MyId = {databaseName}.dbo.spPagarCuotasXVenta
                @SaleId,
                @PaymentDate,
                @ExternalTransactionNumber,
                @InstallmentsToPay,
                @AmountToPay,
                @AgentCode;
            """;
    }
}

internal sealed class AplicacionesGuardianCommissionAgent
{
    public int ContactId { get; set; }
    public int Code { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public decimal TotalToApply { get; set; }
}

internal class AplicacionesCommissionAgent
{
    public int Id { get; set; }
    public int Cycle { get; set; }
    public int ContactId { get; set; }
    public int Code { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public decimal TotalToApply { get; set; }
    public DateTime RegisteredAt { get; set; }
    public int Status { get; set; }
    public string Observation { get; set; } = string.Empty;
}

internal sealed class AplicacionesPendingCommissionAgent : AplicacionesCommissionAgent
{
    public decimal TotalAppliedAmount { get; set; }
    public decimal RemainingAmount { get; set; }
}

internal sealed class AplicacionesCompanyCommission
{
    public int Id { get; set; }
    public int Cycle { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public int CompanyId { get; set; }
    public int CompanyWebServiceId { get; set; }
    public string CompanyDatabaseName { get; set; } = string.Empty;
    public decimal PersonalSales { get; set; }
    public decimal GroupSales { get; set; }
    public decimal Residual { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal RetentionAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal ThirteenPercentAmount { get; set; }
    public bool RequiresInvoice { get; set; }
}

internal sealed class AplicacionesProductAccount
{
    public int CompanyId { get; set; }
    public int ProjectId { get; set; }
    public int SaleId { get; set; }
    public decimal TotalSale { get; set; }
    public decimal TotalDebt { get; set; }
    public DateTime? SaleDate { get; set; }
    public string LotCode { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public int PendingInstallments { get; set; }
    public int OverdueInstallments { get; set; }
    public int MaturedInstallments { get; set; }
    public int SalespersonId { get; set; }
    public int ClientId { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string ProductKey { get; set; } = string.Empty;
}

internal sealed class AplicacionesInstallmentQuote
{
    public int InstallmentNumber { get; set; }
    public decimal Capital { get; set; }
    public decimal Interest { get; set; }
    public decimal InterestPenalty { get; set; }
    public decimal Insurance { get; set; }
    public decimal Expense { get; set; }
    public decimal Penalty { get; set; }
    public decimal InstallmentAmount { get; set; }
    public DateTime DueDate { get; set; }
    public int RowNumber { get; set; }
    public DateTime? InterestCalculationDate { get; set; }
    public decimal DistributedPartialPayments { get; set; }
    public decimal PaymentAmount { get; set; }
    public decimal PartialPayments { get; set; }
}

internal sealed class AplicacionesLetterInstruction
{
    public string CommissionerDocument { get; set; } = string.Empty;
    public string BeneficiaryDocument { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public int InstallmentsToApply { get; set; }
    public DateTime EndDate { get; set; }
    public int SaleId { get; set; }
    public int CompanyId { get; set; }
    public int ProjectId { get; set; }
}

internal sealed class AplicacionesDiscountDefinition
{
    public int CompanyId { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsPercentage { get; set; }
    public string CommissionerDocument { get; set; } = string.Empty;
    public decimal AmountOrPercent { get; set; }
    public int IntercompanyFlag { get; set; }
    public int PaymentTypeId { get; set; }
}

internal sealed class AplicacionesCustomerRecord
{
    public int ClientId { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

internal sealed class AplicacionesCompanyDatabase
{
    public int CompanyId { get; set; }
    public int WebServiceCompanyId { get; set; }
    public string DatabaseName { get; set; } = string.Empty;
}

internal sealed class AplicacionesPaymentRecord
{
    public int Id { get; set; }
    public int Cycle { get; set; }
    public int CompanyId { get; set; }
    public int SaleId { get; set; }
    public int ClientId { get; set; }
    public string ClientDocument { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public decimal Expense { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? ReceiptId { get; set; }
    public int? InvoiceId { get; set; }
    public string Observation { get; set; } = string.Empty;
    public int PaymentTypeId { get; set; }
    public int IntercompanyFlag { get; set; }
}

internal sealed class AplicacionesProrationEntry
{
    public int Id { get; set; }
    public string ClientDocument { get; set; } = string.Empty;
    public int Cycle { get; set; }
    public int LendingCompanyId { get; set; }
    public int ReceivingCompanyId { get; set; }
    public int ClientId { get; set; }
    public int ReceiptId { get; set; }
    public decimal Amount { get; set; }
    public bool Enabled { get; set; }
    public int ReceiptVoucherId { get; set; }
    public int IntercompanyFlag { get; set; }
    public int PaymentTypeId { get; set; }
}

internal sealed class AplicacionesCompanyMappingRow
{
    public int LegacyCompanyId { get; set; }
    public int CompanyId { get; set; }
    public string DatabaseName { get; set; } = string.Empty;
}

internal sealed class AplicacionesGuardianCompanyCommissionRow
{
    public int Cycle { get; set; }
    public int LegacyCompanyId { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public decimal PersonalSales { get; set; }
    public decimal GroupSales { get; set; }
    public decimal Residual { get; set; }
    public decimal CommissionAmount { get; set; }
    public int InvoiceFlag { get; set; }
    public decimal RetentionPercentage { get; set; }
    public decimal RetentionAmount { get; set; }
    public decimal TotalCommission { get; set; }
}

internal sealed class AplicacionesCompanyCommissionInsertRow
{
    public decimal GrossAmount { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public int Cycle { get; set; }
    public int CompanyId { get; set; }
    public int InvoiceFlag { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal ThirteenPercentAmount { get; set; }
    public decimal RetentionAmount { get; set; }
    public decimal Residual { get; set; }
    public decimal GroupSales { get; set; }
    public decimal PersonalSales { get; set; }
    public int LegacyCompanyId { get; set; }
}

internal sealed class AplicacionesCommissionTotalRow
{
    public string DocumentNumber { get; set; } = string.Empty;
    public decimal TotalToApply { get; set; }
}

internal sealed class AplicacionesReprogrammedProductRow
{
    public string ProductId { get; set; } = string.Empty;
    public int ClientId { get; set; }
}
