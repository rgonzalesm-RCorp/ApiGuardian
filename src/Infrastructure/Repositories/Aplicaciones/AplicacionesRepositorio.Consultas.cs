namespace ApiGuardian.Infrastructure.Repositories;

public partial class AplicacionesRepositorio
{
    private const string SqlExistsAplicacionesComisionado = """
        SELECT COUNT(1)
        FROM BDQISHUR.dbo.AplicacionesComisionado
        WHERE Ciclo = @Ciclo;
        """;

    private const string SqlExistsAplicacionesComisionPorEmpresa = """
        SELECT COUNT(1)
        FROM BDQISHUR.dbo.AplicacionesComisionPorEmpresa
        WHERE Ciclo = @Ciclo;
        """;

    private const string SqlDeleteAplicacionesProrrateo = """
        DELETE FROM BDQISHUR.dbo.AplicacionesProrrateo
        WHERE Ciclo = @Ciclo;
        """;

    private const string SqlDeleteAplicacionesPagos = """
        DELETE FROM BDQISHUR.dbo.AplicacionesPagos
        WHERE Ciclo = @Ciclo;
        """;

    private const string SqlDeleteAplicacionesComisionado = """
        DELETE FROM BDQISHUR.dbo.AplicacionesComisionado
        WHERE Ciclo = @Ciclo;
        """;

    private const string SqlDeleteAplicacionesComisionPorEmpresa = """
        DELETE FROM BDQISHUR.dbo.AplicacionesComisionPorEmpresa
        WHERE Ciclo = @Ciclo;
        """;

    private const string SqlDeleteGuardianRetencionEmpresa = """
        DELETE FROM tbl_retencionempresa
        WHERE lciclo_id = @Ciclo;
        """;

    private const string SqlDeleteGuardianRetencionEmpresaExterior = """
        DELETE FROM tbl_retencionempresa_exterior
        WHERE lciclo_id = @Ciclo;
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
            @NumeroDocumento,
            @Codigo,
            @Ciclo,
            @Estado,
            @FechaRegistro,
            @ContactoId,
            @NombreCompleto,
            @Observacion,
            @TotalAplicar
        );
        """;

    private const string SqlPendingComisionados = """
        SELECT *
        FROM (
            SELECT
                appComisionado.Id,
                appComisionado.Ciclo Ciclo,
                appComisionado.Lcontacto_id ContactoId,
                appComisionado.Codigo Codigo,
                LTRIM(RTRIM(appComisionado.Carnet)) NumeroDocumento,
                RTRIM(ISNULL(appComisionado.Nombre, '')) NombreCompleto,
                ISNULL(appComisionado.TotalAplicar, 0) TotalAplicar,
                appComisionado.FechaRegistro FechaRegistro,
                appComisionado.Estado Estado,
                RTRIM(ISNULL(appComisionado.Observacion, '')) Observacion,
                ISNULL(datMontoAplicado.monto_aplicado, 0) TotalAplicado,
                ISNULL(appComisionado.TotalAplicar, 0) - ISNULL(datMontoAplicado.monto_aplicado, 0) MontoRestante
            FROM BDQISHUR.dbo.AplicacionesComisionado appComisionado
            LEFT JOIN (
                SELECT
                    LTRIM(RTRIM(p.CI_Cliente)) DocumentoCliente,
                    p.Ciclo,
                    SUM(ISNULL(p.Monto, 0)) monto_aplicado
                FROM BDQISHUR.dbo.AplicacionesPagos p
                WHERE p.Ciclo = @Ciclo
                GROUP BY LTRIM(RTRIM(p.CI_Cliente)), p.Ciclo
            ) datMontoAplicado
                ON LTRIM(RTRIM(appComisionado.Carnet)) = datMontoAplicado.DocumentoCliente
               AND appComisionado.Ciclo = datMontoAplicado.Ciclo
            WHERE appComisionado.Ciclo = @Ciclo
              AND appComisionado.Estado = 0
              AND LTRIM(RTRIM(appComisionado.Carnet)) <> '4823437'
        ) todo
        WHERE ISNULL(todo.MontoRestante, 0) <> 0
          AND ISNULL(todo.Observacion, '') NOT LIKE '%Procesado%'
        ORDER BY todo.Id;
        """;

    private const string SqlMarkComisionadoProcessed = """
        UPDATE BDQISHUR.dbo.AplicacionesComisionado
        SET Observacion = 'Procesado'
        WHERE Ciclo = @Ciclo
          AND LTRIM(RTRIM(Carnet)) = @NumeroDocumento;
        """;

    private const string SqlCompanyTotalsByDocument = """
        SELECT
            LTRIM(RTRIM(Carnet)) NumeroDocumento,
            SUM(ISNULL(Neto, 0)) TotalAplicar
        FROM BDQISHUR.dbo.AplicacionesComisionPorEmpresa
        WHERE Ciclo = @Ciclo
        GROUP BY LTRIM(RTRIM(Carnet));
        """;

    private const string SqlCompanyCommissionByDocument = """
        SELECT
            ROW_NUMBER() OVER(ORDER BY a.Empresa ASC) Id,
            MAX(a.Ciclo) Ciclo,
            MAX(LTRIM(RTRIM(a.Carnet))) NumeroDocumento,
            a.Empresa EmpresaId,
            MAX(ISNULL(c.IDBD_WS, 0)) EmpresaServicioWebId,
            MAX(ISNULL(c.NOMBREBD, '')) NombreBaseDatosEmpresa,
            SUM(ISNULL(a.VentasPersonales, 0)) VentasPersonales,
            SUM(ISNULL(a.VentasGrupales, 0)) VentasGrupales,
            SUM(ISNULL(a.Residual, 0)) Residual,
            SUM(ISNULL(a.MontoComision, 0)) MontoComision,
            SUM(ISNULL(a.Retencion, 0)) MontoRetencion,
            SUM(ISNULL(a.Neto, 0)) MontoNeto,
            SUM(ISNULL(a.Bruto, 0)) MontoBruto,
            SUM(ISNULL(a.Porcentaje13, 0)) MontoTrecePorCiento,
            CASE WHEN SUM(ISNULL(a.Factura, 0)) > 0 THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END RequiereFactura
        FROM BDQISHUR.dbo.AplicacionesComisionPorEmpresa a
        LEFT JOIN BDComisiones.dbo.CNX_BDCOMISIONES c ON c.IDBD = a.Empresa
        WHERE LTRIM(RTRIM(a.Carnet)) = @NumeroDocumento
          AND a.Ciclo = @Ciclo
        GROUP BY a.Empresa
        ORDER BY a.Empresa;
        """;

    private const string SqlCompanyMapping = """
        SELECT
            lempresa_id EmpresaLegadaId,
            IDBD EmpresaId,
            NOMBREBD NombreBaseDatos
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
            @MontoBruto,
            @NumeroDocumento,
            @Ciclo,
            @EmpresaId,
            @IndicadorFactura,
            @MontoComision,
            @MontoNeto,
            @MontoTrecePorCiento,
            @MontoRetencion,
            @Residual,
            @VentasGrupales,
            @VentasPersonales,
            0,
            @EmpresaLegadaId
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
            e.lciclo_id Ciclo,
            e.idempresa EmpresaLegadaId,
            TRIM(e.carnet) NumeroDocumento,
            COALESCE(e.vpers, 0) VentasPersonales,
            COALESCE(e.vgrupo, 0) VentasGrupales,
            COALESCE(e.residual, 0) Residual,
            COALESCE(e.montocomision, 0) MontoComision,
            COALESCE(e.lpresentafactura, 0) IndicadorFactura,
            COALESCE(e.porcentajeret, 0) PorcentajeRetencion,
            COALESCE(e.montoretencion, 0) MontoRetencion,
            COALESCE(e.total_comision, 0) ComisionTotal
        FROM tbl_retencionempresa e
        WHERE e.lciclo_id = @Ciclo

        UNION ALL

        SELECT
            e.lciclo_id Ciclo,
            e.idempresa EmpresaLegadaId,
            TRIM(e.carnet) NumeroDocumento,
            COALESCE(e.vpers, 0) VentasPersonales,
            COALESCE(e.vgrupo, 0) VentasGrupales,
            COALESCE(e.residual, 0) Residual,
            COALESCE(e.montocomision, 0) MontoComision,
            COALESCE(e.lpresentafactura, 0) IndicadorFactura,
            COALESCE(e.porcentajeret, 0) PorcentajeRetencion,
            COALESCE(e.montoretencion, 0) MontoRetencion,
            COALESCE(e.total_comision, 0) ComisionTotal
        FROM tbl_retencionempresa_exterior e
        WHERE e.lciclo_id = @Ciclo;
        """;

    private const string SqlGuardianCommissionAgents = """
        SELECT
            ac.lcontacto_id ContactoId,
            CAST(ac.scodigo AS SIGNED) Codigo,
            TRIM(ac.scedulaidentidad) NumeroDocumento,
            ac.snombrecompleto NombreCompleto,
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
            ) TotalAplicar
        FROM administracioncontacto ac
        LEFT JOIN (
            SELECT COUNT(a.lcontacto_id) contar1, SUM(a.dcomision) sumar1, a.lcontacto_id
            FROM administracionventapersonal a
            INNER JOIN administracioncontrato b ON b.lcontrato_id = a.lcontrato_id
            WHERE a.lciclo_id = @Ciclo
            GROUP BY a.lcontacto_id
        ) vie ON ac.lcontacto_id = vie.lcontacto_id
        LEFT JOIN (
            SELECT COUNT(c.lcontacto_id) contar2, SUM(c.dcomision) sumar2, c.lcontacto_id
            FROM administracionventagrupo c
            INNER JOIN administracioncontrato d ON d.lcontrato_id = c.lcontrato_id
            WHERE c.lciclo_id = @Ciclo
            GROUP BY c.lcontacto_id
        ) mar ON ac.lcontacto_id = mar.lcontacto_id
        LEFT JOIN (
            SELECT COUNT(e.lcontacto_id) contar3, SUM(e.dmonto) sumar3, e.lcontacto_id
            FROM administracionredempresacomplejo e
            WHERE e.lciclo_id = @Ciclo
            GROUP BY e.lcontacto_id
        ) jor ON ac.lcontacto_id = jor.lcontacto_id
        LEFT JOIN (
            SELECT COUNT(c.vendedores_mes_id) contar10, SUM(c.monto) sumar10, c.vendedores_mes_id lcontacto_id
            FROM t_ganadores_bonoliderazgo_empresa_pagar c
            WHERE c.lciclo_id = @Ciclo
            GROUP BY c.vendedores_mes_id
        ) bon ON ac.lcontacto_id = bon.lcontacto_id
        LEFT JOIN (
            SELECT COUNT(c.vendedores_id) contar11, SUM(c.pagar) sumar11, c.vendedores_id lcontacto_id
            FROM t_bono_liderazgo c
            WHERE c.lciclo_id = @Ciclo
            GROUP BY c.vendedores_id
        ) lid ON ac.lcontacto_id = lid.lcontacto_id
        LEFT JOIN (
            SELECT COUNT(c.vendedor_lcontacto_id) contar12, SUM(c.pagar) sumar12, c.vendedor_lcontacto_id lcontacto_id
            FROM t_top_vendedores c
            WHERE c.lciclo_id = @Ciclo
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
                WHERE lciclo_id = @Ciclo
                GROUP BY lcontacto_id

                UNION ALL

                SELECT
                    IFNULL(SUM(montoretencion), 0) MontoRet,
                    lcontacto_id
                FROM tbl_retencionempresa_exterior
                WHERE lciclo_id = @Ciclo
                GROUP BY lcontacto_id
            ) a
            GROUP BY a.lcontacto_id
        ) rete ON rete.lcontacto_id = ac.lcontacto_id
        INNER JOIN administracionventapersonal h ON ac.lcontacto_id = h.lcontacto_id AND h.lciclo_id = @Ciclo
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
            p.IDEMPRESA EmpresaId,
            CAST(p.IDPROYECTO AS INT) ProyectoId,
            CAST(p.IDVENTA AS INT) VentaId,
            ISNULL(p.TOTALVENTA, 0) VentaTotal,
            ISNULL(p.TOTALDEUDA, 0) DeudaTotal,
            p.FECHA FechaVenta,
            LTRIM(RTRIM(p.LOTE)) CodigoLote,
            LTRIM(RTRIM(p.DOCID)) NumeroDocumento,
            RTRIM(ISNULL(p.PROYECTO, '')) NombreProyecto,
            ISNULL(p.CUOTASPENDIENTES, 0) CuotasPendientes,
            ISNULL(p.CUOTASVENCIDAS, 0) CuotasVencidas,
            ISNULL(p.CUOTAS_LOTES_VENCIDAS, 0) CuotasLotesVencidas,
            CAST(ISNULL(p.IDVENDEDOR, 0) AS INT) VendedorId,
            CAST(ISNULL(p.IDCLIENTE, 0) AS INT) ClienteId,
            p.MODFECHA FechaModificacion,
            RTRIM(ISNULL(p.EMPRESA, '')) NombreEmpresa,
            ISNULL(a.prioridad, 1000) Prioridad,
            CONCAT(CAST(ISNULL(p.IDCLIENTE, 0) AS varchar(20)), ':', LTRIM(RTRIM(p.LOTE))) ClaveProducto
        FROM BDComisiones.dbo.vwLOTES_GRL_DOCID p
        LEFT JOIN BDQISHUR.dbo.AplicacionesPrioridad a
            ON a.idEmpresa = p.IDEMPRESA
           AND a.idProyecto = CAST(p.IDPROYECTO AS INT)
        WHERE LTRIM(RTRIM(p.DOCID)) = @NumeroDocumento
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
        WHERE LTRIM(RTRIM(DOCID)) = @NumeroDocumento
          AND LTRIM(RTRIM(LOTE)) = @CodigoLote
          AND ISNULL(TOTALDEUDA, 0) = 0;
        """;

    private const string SqlReprogrammedProducts = """
        SELECT
            LTRIM(RTRIM(r.IDPRODUCTO)) ProductoId,
            CAST(r.IDCLIENTE AS INT) ClienteId
        FROM BDComisiones.dbo.vwLISTAPRODUCTOS_NEW r
        INNER JOIN BDComisiones.dbo.vwLOTES_GRL_DOCID C ON C.IDCLIENTE = R.IDCLIENTE
        WHERE r.GLOSA LIKE '%reprogramacion%' AND C.DOCID = @NumeroDocumento;
        """;

    private const string SqlLetters = """
        SELECT
            LTRIM(RTRIM(carta.ci_cliente_comisionado)) DocumentoComisionado,
            LTRIM(RTRIM(carta.ci_cliente_beneficiario)) DocumentoBeneficiario,
            LTRIM(RTRIM(cartaProd.cod_producto)) CodigoProducto,
            ISNULL(cartaProd.cantidad_cuotas, 0) CuotasAplicar,
            cartaProd.fecha_fin_aplicaciones FechaFin,
            cartaProd.id_venta VentaId,
            cartaProd.cod_empresa EmpresaId,
            cartaProd.id_proyecto ProyectoId
        FROM DBITSIS.dbo.config_solicitud_carta carta
        INNER JOIN DBITSIS.dbo.config_solicitud_carta_producto cartaProd
            ON carta.id = cartaProd.id_config_solicitud
        WHERE carta.activo = 1
          AND cartaProd.habilitado = 1
          AND LTRIM(RTRIM(carta.ci_cliente_comisionado)) = @DocumentoComisionado;
        """;

    private const string SqlActiveDiscounts = """
        SELECT
            d.cod_empresa EmpresaId,
            d.descuento Descripcion,
            CASE WHEN d.es_porcentaje = 1 THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END EsPorcentaje,
            LTRIM(RTRIM(dF.documento_freelancer)) DocumentoComisionado,
            CAST(dF.monto_porcentaje AS decimal(18, 2)) MontoOPorcentaje,
            d.intercompanhia BanderaIntercompania,
            d.id TipoPagoId
        FROM DBITSIS.dbo.aplicaciones_descuentos d
        INNER JOIN DBITSIS.dbo.aplicaciones_descuentos_freelancer dF
            ON d.id = dF.id_aplicaciones_descuento
        WHERE dF.activo = 1
          AND d.activo = 1;
        """;

    private const string SqlCustomerByDocument = """
        SELECT TOP 1
            CAST(c.idCliente AS INT) ClienteId,
            LTRIM(RTRIM(c.DOCID)) NumeroDocumento,
            RTRIM(ISNULL(c.NOMBRE, '')) NombreCompleto
        FROM BDComisiones.dbo.grlCLIENTE c
        WHERE LTRIM(RTRIM(c.DOCID)) = @NumeroDocumento;
        """;

    private const string SqlCompanyDatabase = """
        SELECT TOP 1
            CAST(IDBD AS INT) EmpresaId,
            CAST(ISNULL(IDBD_WS, 0) AS INT) EmpresaServicioWebId,
            RTRIM(ISNULL(NOMBREBD, '')) NombreBaseDatos
        FROM BDComisiones.dbo.CNX_BDCOMISIONES
        WHERE IDBD = @EmpresaId;
        """;

    private const string SqlPaymentsByCycle = """
        SELECT
            CAST(ISNULL(Id, 0) AS INT) Id,
            CAST(ISNULL(Ciclo, 0) AS INT) Ciclo,
            CAST(ISNULL(Id_Empresa, 0) AS INT) EmpresaId,
            CAST(ISNULL(Id_Venta, 0) AS INT) VentaId,
            CAST(ISNULL(Id_Cliente, 0) AS INT) ClienteId,
            LTRIM(RTRIM(ISNULL(CI_Cliente, ''))) DocumentoCliente,
            RTRIM(ISNULL(Id_Producto, '')) ProductoId,
            CAST(ISNULL(Expensa, 0) AS decimal(18, 2)) Expensa,
            CAST(ISNULL(Monto, 0) AS decimal(18, 2)) Monto,
            Fecha FechaCreacion,
            CAST(ISNULL(Id_Recibo, 0) AS INT) ReciboId,
            CAST(ISNULL(Id_Factura, 0) AS INT) FacturaId,
            RTRIM(ISNULL(Observacion, '')) Observacion,
            CAST(ISNULL(TipoPago, 0) AS INT) TipoPagoId,
            CAST(ISNULL(Intercompania, 0) AS INT) BanderaIntercompania
        FROM BDQISHUR.dbo.AplicacionesPagos
        WHERE Ciclo = @Ciclo;
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
            @Ciclo,
            @EmpresaId,
            @VentaId,
            @ClienteId,
            @DocumentoCliente,
            @ProductoId,
            @Expensa,
            @Monto,
            @FechaCreacion,
            @ReciboId,
            @FacturaId,
            @Observacion,
            @TipoPagoId,
            @BanderaIntercompania
        );
        SELECT CAST(SCOPE_IDENTITY() AS int);
        """;

    private const string SqlUpdateInvoice = """
        UPDATE BDQISHUR.dbo.AplicacionesPagos
        SET Id_Factura = @FacturaId,
            Observacion = @Observacion
        WHERE Id_Empresa = @EmpresaId
          AND Id_Venta = @VentaId
          AND Id_Recibo = @ReciboId;
        """;

    private const string SqlCountInvoiceFailures = """
        SELECT COUNT(1)
        FROM BDQISHUR.dbo.AplicacionesPagos
        WHERE Ciclo = @Ciclo
          AND ISNULL(Id_Factura, 0) = -1
          AND ISNULL(Id_Producto, '') <> '';
        """;

    private const string SqlActiveProrations = """
        SELECT
            CAST(ISNULL(Id, 0) AS INT) Id,
            LTRIM(RTRIM(ISNULL(CiCliente, ''))) DocumentoCliente,
            CAST(ISNULL(Ciclo, 0) AS INT) Ciclo,
            CAST(ISNULL(EmpresaPresta, 0) AS INT) EmpresaPrestaId,
            CAST(ISNULL(EmpresaRecibe, 0) AS INT) EmpresaRecibeId,
            CAST(ISNULL(IdCliente, 0) AS INT) ClienteId,
            CAST(ISNULL(IdRecibo, 0) AS INT) ReciboId,
            CAST(ISNULL(Monto, 0) AS decimal(18, 2)) Monto,
            CAST(ISNULL(Habilitado, 0) AS bit) Habilitado,
            CAST(ISNULL(IdComprobante, 0) AS INT) ComprobanteReciboId,
            CAST(ISNULL(Intercompania, 0) AS INT) BanderaIntercompania,
            CAST(ISNULL(TipoPago, 0) AS INT) TipoPagoId
        FROM BDQISHUR.dbo.AplicacionesProrrateo
        WHERE Ciclo = @Ciclo
          AND LTRIM(RTRIM(CiCliente)) = @NumeroDocumento
          AND ISNULL(Habilitado, 0) = 1;
        """;

    private const string SqlDisableProrations = """
        UPDATE BDQISHUR.dbo.AplicacionesProrrateo
        SET Habilitado = 0
        WHERE Ciclo = @Ciclo
          AND LTRIM(RTRIM(CiCliente)) = @NumeroDocumento
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
            @DocumentoCliente,
            @Ciclo,
            @EmpresaPrestaId,
            @EmpresaRecibeId,
            @ClienteId,
            @ReciboId,
            @Monto,
            @Habilitado,
            @ComprobanteReciboId,
            @BanderaIntercompania,
            @TipoPagoId
        );
        SELECT CAST(SCOPE_IDENTITY() AS int);
        """;

    private static string ConstruirConsultaCuotas(string nombreBaseDatos)
    {
        return $"""
            SET DATEFIRST 1;
            SELECT
                NROCUOTA NumeroCuota,
                CAPITAL Capital,
                INTERES Interes,
                INTERES_MORA InteresMora,
                SEGURO Seguro,
                EXPENSA Expensa,
                MULTA Multa,
                IMPORTE_CUOTA ImporteCuota,
                FVENCIMIENTO FechaVencimiento,
                FILA NumeroFila,
                FCALCULO_INTERES FechaCalculoInteres,
                PAGOS_A_CUENTA_DISTRIBUIDO PagosParcialesDistribuidos,
                MONTO_PAGO MontoPago,
                PAGOS_A_CUENTA PagosParciales
            FROM {nombreBaseDatos}.dbo.ffObtenerMontoAPagar(@VentaId, @FechaPago, @CantidadCuotas);
            """;
    }

    private static string ConstruirConsultaPagoSion(string nombreBaseDatos)
    {
        return $"""
            EXEC @MyId = {nombreBaseDatos}.dbo.spPagarCuotasXVenta
                @VentaId,
                @FechaPago,
                @NumeroTransaccionExterna,
                @InstallmentsToPay,
                @MontoPagar,
                @CodigoAgente;
            """;
    }
}

internal sealed class ComisionadoGuardianAplicaciones
{
    public int ContactoId { get; set; }
    public int Codigo { get; set; }
    public string NumeroDocumento { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public decimal TotalAplicar { get; set; }
}

internal class ComisionadoAplicaciones
{
    public int Id { get; set; }
    public int Ciclo { get; set; }
    public int ContactoId { get; set; }
    public int Codigo { get; set; }
    public string NumeroDocumento { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public decimal TotalAplicar { get; set; }
    public DateTime FechaRegistro { get; set; }
    public int Estado { get; set; }
    public string Observacion { get; set; } = string.Empty;
}

internal sealed class ComisionadoPendienteAplicaciones : ComisionadoAplicaciones
{
    public decimal TotalAplicado { get; set; }
    public decimal MontoRestante { get; set; }
}

internal sealed class ComisionEmpresaAplicaciones
{
    public int Id { get; set; }
    public int Ciclo { get; set; }
    public string NumeroDocumento { get; set; } = string.Empty;
    public int EmpresaId { get; set; }
    public int EmpresaServicioWebId { get; set; }
    public string NombreBaseDatosEmpresa { get; set; } = string.Empty;
    public decimal VentasPersonales { get; set; }
    public decimal VentasGrupales { get; set; }
    public decimal Residual { get; set; }
    public decimal MontoComision { get; set; }
    public decimal MontoRetencion { get; set; }
    public decimal MontoNeto { get; set; }
    public decimal MontoBruto { get; set; }
    public decimal MontoTrecePorCiento { get; set; }
    public bool RequiereFactura { get; set; }
}

internal sealed class ProductoCarteraAplicaciones
{
    public int EmpresaId { get; set; }
    public int ProyectoId { get; set; }
    public int VentaId { get; set; }
    public decimal VentaTotal { get; set; }
    public decimal DeudaTotal { get; set; }
    public DateTime? FechaVenta { get; set; }
    public string CodigoLote { get; set; } = string.Empty;
    public string NumeroDocumento { get; set; } = string.Empty;
    public string NombreProyecto { get; set; } = string.Empty;
    public int CuotasPendientes { get; set; }
    public int CuotasVencidas { get; set; }
    public int CuotasLotesVencidas { get; set; }
    public int VendedorId { get; set; }
    public int ClienteId { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public string NombreEmpresa { get; set; } = string.Empty;
    public int Prioridad { get; set; }
    public string ClaveProducto { get; set; } = string.Empty;
}

internal sealed class CuotaAplicaciones
{
    public int NumeroCuota { get; set; }
    public decimal Capital { get; set; }
    public decimal Interes { get; set; }
    public decimal InteresMora { get; set; }
    public decimal Seguro { get; set; }
    public decimal Expensa { get; set; }
    public decimal Multa { get; set; }
    public decimal ImporteCuota { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public int NumeroFila { get; set; }
    public DateTime? FechaCalculoInteres { get; set; }
    public decimal PagosParcialesDistribuidos { get; set; }
    public decimal MontoPago { get; set; }
    public decimal PagosParciales { get; set; }
}

internal sealed class InstruccionCartaAplicaciones
{
    public string DocumentoComisionado { get; set; } = string.Empty;
    public string DocumentoBeneficiario { get; set; } = string.Empty;
    public string CodigoProducto { get; set; } = string.Empty;
    public int CuotasAplicar { get; set; }
    public DateTime FechaFin { get; set; }
    public int VentaId { get; set; }
    public int EmpresaId { get; set; }
    public int ProyectoId { get; set; }
}

internal sealed class DefinicionDescuentoAplicaciones
{
    public int EmpresaId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public bool EsPorcentaje { get; set; }
    public string DocumentoComisionado { get; set; } = string.Empty;
    public decimal MontoOPorcentaje { get; set; }
    public int BanderaIntercompania { get; set; }
    public int TipoPagoId { get; set; }
}

internal sealed class ClienteAplicaciones
{
    public int ClienteId { get; set; }
    public string NumeroDocumento { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
}

internal sealed class BaseDatosEmpresaAplicaciones
{
    public int EmpresaId { get; set; }
    public int EmpresaServicioWebId { get; set; }
    public string NombreBaseDatos { get; set; } = string.Empty;
}

internal sealed class RegistroPagoAplicaciones
{
    public int Id { get; set; }
    public int Ciclo { get; set; }
    public int EmpresaId { get; set; }
    public int VentaId { get; set; }
    public int ClienteId { get; set; }
    public string DocumentoCliente { get; set; } = string.Empty;
    public string ProductoId { get; set; } = string.Empty;
    public decimal Expensa { get; set; }
    public decimal Monto { get; set; }
    public DateTime FechaCreacion { get; set; }
    public int? ReciboId { get; set; }
    public int? FacturaId { get; set; }
    public string Observacion { get; set; } = string.Empty;
    public int TipoPagoId { get; set; }
    public int BanderaIntercompania { get; set; }
}

internal sealed class RegistroProrrateoAplicaciones
{
    public int Id { get; set; }
    public string DocumentoCliente { get; set; } = string.Empty;
    public int Ciclo { get; set; }
    public int EmpresaPrestaId { get; set; }
    public int EmpresaRecibeId { get; set; }
    public int ClienteId { get; set; }
    public int ReciboId { get; set; }
    public decimal Monto { get; set; }
    public bool Habilitado { get; set; }
    public int ComprobanteReciboId { get; set; }
    public int BanderaIntercompania { get; set; }
    public int TipoPagoId { get; set; }
}

internal sealed class MapeoEmpresaAplicaciones
{
    public int EmpresaLegadaId { get; set; }
    public int EmpresaId { get; set; }
    public string NombreBaseDatos { get; set; } = string.Empty;
}

internal sealed class ComisionEmpresaGuardianAplicaciones
{
    public int Ciclo { get; set; }
    public int EmpresaLegadaId { get; set; }
    public string NumeroDocumento { get; set; } = string.Empty;
    public decimal VentasPersonales { get; set; }
    public decimal VentasGrupales { get; set; }
    public decimal Residual { get; set; }
    public decimal MontoComision { get; set; }
    public int IndicadorFactura { get; set; }
    public decimal PorcentajeRetencion { get; set; }
    public decimal MontoRetencion { get; set; }
    public decimal ComisionTotal { get; set; }
}

internal sealed class RegistroComisionEmpresaAplicaciones
{
    public decimal MontoBruto { get; set; }
    public string NumeroDocumento { get; set; } = string.Empty;
    public int Ciclo { get; set; }
    public int EmpresaId { get; set; }
    public int IndicadorFactura { get; set; }
    public decimal MontoComision { get; set; }
    public decimal MontoNeto { get; set; }
    public decimal MontoTrecePorCiento { get; set; }
    public decimal MontoRetencion { get; set; }
    public decimal Residual { get; set; }
    public decimal VentasGrupales { get; set; }
    public decimal VentasPersonales { get; set; }
    public int EmpresaLegadaId { get; set; }
}

internal sealed class TotalComisionAplicaciones
{
    public string NumeroDocumento { get; set; } = string.Empty;
    public decimal TotalAplicar { get; set; }
}

internal sealed class ProductoReprogramadoAplicaciones
{
    public string ProductoId { get; set; } = string.Empty;
    public int ClienteId { get; set; }
}
