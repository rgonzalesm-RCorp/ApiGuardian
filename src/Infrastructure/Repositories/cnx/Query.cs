
using Microsoft.Extensions.Configuration;

namespace Query.Cnx
{
    public class ScriptCnx
    {
       
        public static string QueryVentaCnx(IConfiguration configuration, bool IsCasosEspeciales = false)
        {
            List<EmpresaCalculoComision> empresas = configuration.GetSection("EmpresaCalculoComisiones").Get<List<EmpresaCalculoComision>>() ?? new List<EmpresaCalculoComision>();

            string query = @"";

            foreach (var item in empresas)
            {
                query += @$"
                    SELECT {item.EmpresaId} EmpresaId,'{item.Nombre}' Nombre, V.FECHA DFecha
                        , PC.NROMANZANO SManzano, RTRIM(P.CODFABRICA) SLote
                        , V.IDALMACEN LComplejoId, V.IDVENTA IdVenta, RTRIM(VC.LOTES) Lote , PC.UV SUV
                        , ISNULL(VC.PRECIO_LISTA, V.TOTALVENTA) PrecioInicial
                        , V.IDCLIENTE IdCliente, RTRIM(A.DESCRIPCION) Complejo, V.IDVENDEDOR VendedorId
                        , V.IDTIPOVENTA TipoVenta
                        , V.TOTALVENTA DPrecio
                        , CASE WHEN V.IDTIPOVENTA = 1 THEN V.TOTALVENTA * 0.1 ELSE V.CUOTAINICIAL END SCuotaInicial
                        , V.CUOTAINICIAL  SCuotaInicialOriginal
                        , CR.PORC_INICIAL PorcentajeCuotaInicial
                        , CASE WHEN V.IDTIPOVENTA=1 THEN ROUND(VD.PRECIOVENTA * (0.1), 3)
                            WHEN (V.CUOTAINICIAL / VD.PRECIOVENTA)>=0.099 THEN CEILING(ROUND(VD.PRECIOVENTA * (0.1), 2))
                            WHEN (V.CUOTAINICIAL / VD.PRECIOVENTA)>=0.069 and (V.CUOTAINICIAL / VD.PRECIOVENTA)<0.1 THEN ROUND(VD.PRECIOVENTA * (0.07), 0) 
                            WHEN (V.CUOTAINICIAL / VD.PRECIOVENTA)>=0.059 and (V.CUOTAINICIAL / VD.PRECIOVENTA)<0.069 THEN ROUND(VD.PRECIOVENTA * (0.06), 0)
                            WHEN (V.CUOTAINICIAL / VD.PRECIOVENTA)>=0.049 and (V.CUOTAINICIAL / VD.PRECIOVENTA)<0.059 THEN CEILING(ROUND(VD.PRECIOVENTA *(0.05), 2))
                            WHEN (V.CUOTAINICIAL / VD.PRECIOVENTA)>=0.029 and (V.CUOTAINICIAL / VD.PRECIOVENTA)<0.049  THEN CEILING(ROUND(VD.PRECIOVENTA * (0.03),2))       
                            ELSE ROUND(VD.PRECIOVENTA * (0.07), 0) 
                        END AS ValorCi
                        , RTRIM(P.IDSECCION_PROD) SeccionId
                        , RTRIM(V.GLOSA) Glosa
                    FROM {item.DataBase}.dbo.INVENTA V
                    INNER JOIN {item.DataBase}.dbo.INVENTA_CCN VC ON VC.IDVENTA = V.IDVENTA AND VC.COMISIONABLE {(IsCasosEspeciales ? "NOT IN (0, 1)": " = 1")}
                    INNER JOIN {item.DataBase}.dbo.INVENTADETALLE AS VD ON V.IDVENTA = VD.IDVENTA
                    INNER JOIN {item.DataBase}.dbo.INPRODUCTO P ON P.IDPRODUCTO = VC.LOTES
                    INNER JOIN {item.DataBase}.dbo.INPRODUCTO_CCN PC ON PC.IDPRODUCTO = P.IDPRODUCTO 
                    INNER JOIN {item.DataBase}.dbo.INALMACEN A ON A.IDALMACEN = V.IDALMACEN
                    LEFT JOIN BDComisiones.dbo.CO_CFGCREDITOS CR on CR.IDCFG_CRED = VC.IDCFG_CRED                    
                    WHERE V.FECHA BETWEEN @inicio AND @fin AND V.IDESTADO <> 2 { (IsCasosEspeciales ? "": @$"
                    AND
                    (
                        ( VC.IDESTADO_VENTA <> 2 AND (V.NRODOC <> '' OR V.GLOSA LIKE '%upgrade%'))
                        OR
                        (
                            VC.IDESTADO_VENTA = 2
                            AND V.IDVENTA IN (
                                SELECT wVC.IDVENTAORIGINAL
                                FROM {item.DataBase}.dbo.INVENTA wV
                                INNER JOIN {item.DataBase}.dbo.INVENTA_CCN wVC ON wV.IDVENTA = wVC.IDVENTA
                                INNER JOIN {item.DataBase}.dbo.INVENTA wV_1 ON wVC.IDVENTAORIGINAL = wV_1.IDVENTA
                                WHERE wV.GLOSA LIKE '%upgrade%' AND wV.FECHA BETWEEN @inicio AND @fin AND wV_1.FECHA BETWEEN @inicio AND @fin
                            )
                        )
                    )"
                    )}
                    UNION ALL";
                
            }

            query =  query.Substring(0, query.Length - 10);
            return @$"
                    SELECT * FROM (
                     {query}   
                    ) AS SDAT
                    INNER JOIN (
                        SELECT
                            c.FAX TelefonoFijo, c.TELEFONO TelefonoMovil, RTRIM(C.EMAIL) Correo
                            , C.FECHANACIMIENTO FechaNacimiento
                            , RTRIM(C.DIRECCION) Direccion, ISNULL(P.idPaisGuardian, 2) IdPaisResidencia, RTRIM(CIU.DESCRIPCION) SCiudad
                            , c.DOCID SCedulaIdentidad
                            , GETDATE() FechaRegistro, RTRIM(C.NOMBRE) SNombreCompleto
                            , C.FAX STelefonoOficina, C.DOCID SContrasena
                            , C.IDCLIENTE
                        FROM BDComisiones.dbo.grlCLIENTE C
                        INNER JOIN BDComisiones.dbo.grlCLIENTE_CCN CC ON CC.IDCLIENTE = C.IDCLIENTE
                        LEFT JOIN BDComisiones.DBO.PAISCONEXIONGUARDIAN P ON P.idPaisConexion = CC.IDPAIS_RESIDENCIA
                        LEFT JOIN BDComisiones.dbo.PECIUDAD CIU ON CIU.IDCIUDAD = CC.IDCIUDAD_RESIDENCIA
                    ) CL ON CL.IDCLIENTE = SDAT.IdCliente
                    INNER JOIN (
                        SELECT
                            c.FAX TelefonoFijoVendedor, c.TELEFONO TelefonoMovilVendedor, RTRIM(C.EMAIL) CorreoVendedor
                            , C.FECHANACIMIENTO FechaNacimientoVendedor
                            , RTRIM(C.DIRECCION) DireccionVendedor, ISNULL(P.idPaisGuardian, 2) IdPaisResidenciaVendedor, RTRIM(CIU.DESCRIPCION) SCiudadVendedor
                            , c.DOCID SCedulaIdentidadVendedor
                            , GETDATE() FechaReistro, RTRIM(C.NOMBRE) SNombreCompletoVendedor
                            , C.FAX STelefonoOficinaVendedor, C.DOCID SContrasenaVendedor
                            , C.IDCLIENTE
                        FROM BDComisiones.dbo.grlCLIENTE C
                        INNER JOIN BDComisiones.dbo.grlCLIENTE_CCN CC ON CC.IDCLIENTE = C.IDCLIENTE
                        LEFT JOIN BDComisiones.DBO.PAISCONEXIONGUARDIAN P ON P.idPaisConexion = CC.IDPAIS_RESIDENCIA
                        LEFT JOIN BDComisiones.dbo.PECIUDAD CIU ON CIU.IDCIUDAD = CC.IDCIUDAD_RESIDENCIA
                    ) V ON V.IDCLIENTE = SDAT.VendedorId ";
        }
        public static string QueryCllienteDocId()
        {
            return @"SELECT CL.*, V.* FROM  vwLOTES_GRL_DOCID SDAT
            INNER JOIN (
                SELECT
                    c.FAX TelefonoFijo, c.TELEFONO TelefonoMovil, RTRIM(C.EMAIL) Correo
                    , C.FECHANACIMIENTO FechaNacimiento
                    , RTRIM(C.DIRECCION) Direccion, ISNULL(P.idPaisGuardian, 2) IdPaisResidencia, RTRIM(CIU.DESCRIPCION) SCiudad
                    , c.DOCID SCedulaIdentidad
                    , GETDATE() FechaRegistro, RTRIM(C.NOMBRE) SNombreCompleto
                    , C.FAX STelefonoOficina, C.DOCID SContrasena
                    , C.IDCLIENTE
                FROM BDComisiones.dbo.grlCLIENTE C
                INNER JOIN BDComisiones.dbo.grlCLIENTE_CCN CC ON CC.IDCLIENTE = C.IDCLIENTE
                LEFT JOIN BDComisiones.DBO.PAISCONEXIONGUARDIAN P ON P.idPaisConexion = CC.IDPAIS_RESIDENCIA
                LEFT JOIN BDComisiones.dbo.PECIUDAD CIU ON CIU.IDCIUDAD = CC.IDCIUDAD_RESIDENCIA
            ) CL ON CL.IDCLIENTE = SDAT.IdCliente
            INNER JOIN (
                SELECT
                    c.FAX TelefonoFijoVendedor, c.TELEFONO TelefonoMovilVendedor, RTRIM(C.EMAIL) CorreoVendedor
                    , C.FECHANACIMIENTO FechaNacimientoVendedor
                    , RTRIM(C.DIRECCION) DireccionVendedor, ISNULL(P.idPaisGuardian, 2) IdPaisResidenciaVendedor, RTRIM(CIU.DESCRIPCION) SCiudadVendedor
                    , c.DOCID SCedulaIdentidadVendedor
                    , GETDATE() FechaReistro, RTRIM(C.NOMBRE) SNombreCompletoVendedor
                    , C.FAX STelefonoOficinaVendedor, C.DOCID SContrasenaVendedor
                    , C.IDCLIENTE
                FROM BDComisiones.dbo.grlCLIENTE C
                INNER JOIN BDComisiones.dbo.grlCLIENTE_CCN CC ON CC.IDCLIENTE = C.IDCLIENTE
                LEFT JOIN BDComisiones.DBO.PAISCONEXIONGUARDIAN P ON P.idPaisConexion = CC.IDPAIS_RESIDENCIA
                LEFT JOIN BDComisiones.dbo.PECIUDAD CIU ON CIU.IDCIUDAD = CC.IDCIUDAD_RESIDENCIA
            ) V ON V.IDCLIENTE = SDAT.IDVENDEDOR
            where cl.SCedulaIdentidad = @docId ORDER by v.IDCLIENTE ";
        }
        public static string QueryObetnerCuotas(IConfiguration configuration)
        {
            try
            {
                List<EmpresaCalculoComision> empresas = configuration.GetSection("EmpresaCalculoComisiones").Get<List<EmpresaCalculoComision>>() ?? new List<EmpresaCalculoComision>();
                string query = @"";
                foreach (var item in empresas)
                {
                    var proyectosExcluidos = item.migracionCuota.proyectosExcluir;
                    string proyectos = proyectosExcluidos == null ? "" : string.Join(",", proyectosExcluidos);
                    var productosExcluidos = item.migracionCuota.productosExcluir;
                    string pr = productosExcluidos == null ? "" : string.Join(", ", productosExcluidos.Select(x => $"'{x}'"));
                    query += $@"SELECT '{item.Nombre}' Empresa,
                                    RTRIM(VD.IDPRODUCTO) IDPRODUCTO, V.IDALMACEN AS IDPROYECTO, AL.DESCRIPCION AS PROYECTO,
                                    R.IDRECIBO, R.IDVENTA, TP.IDTIPOPAGO,
                                    RTRIM(TP.DESCRIPCION) DESCRIPCION, CLI.IDCLIENTE, RTRIM(CLI.NOMBRE) AS CLIENTE,
                                    CLI.DOCID AS DOCIDCLI, V.IDVENDEDOR, RTRIM(VEN.NOMBRE) AS VENDEDOR,
                                    VEN.DOCID AS DOCIDVEN,
                                    R.MONTO - R.INCREMENTO - R.SEGURO - R.EXPENSA - R.MULTA - ISNULL(PC.MONTO, 0) + R.PAGADOACUENTA AS BONO,
                                    R.AMORTIZACION,
                                    R.MONTO - R.INCREMENTO - R.SEGURO - R.EXPENSA - R.MULTA - ISNULL(PC.MONTO, 0) + R.PAGADOACUENTA AS CAPITAL,
                                    R.INCREMENTO AS INTERES, R.SEGURO, R.EXPENSA,
                                    R.MULTA, V.FECHA AS FECHA_VENTA, R.FECHA AS FECHA_PAGO,
                                    ISNULL(PC.MONTO, 0) AS ACUENTA, R.MONTO AS TOTALPAGO, 0 AS MONTODEUDA,
                                    R.PAGADOACUENTA AS PAGOSACUENTA, R.NROCUOTA
                                FROM {item.DataBase}.dbo.INRECIBO AS R
                                INNER JOIN {item.DataBase}.dbo.INRECIBOTIPOPAGO AS RTP ON RTP.IDRECIBO = R.IDRECIBO AND RTP.NROITEM = 1 AND RTP.IDTIPOPAGO NOT IN (4,5,10,12,15,16,17,18,19)
                                INNER JOIN {item.DataBase}.dbo.INVENTA AS V ON V.IDVENTA = R.IDVENTA
                                INNER JOIN {item.DataBase}.dbo.INVENTADETALLE AS VD ON VD.IDVENTA = V.IDVENTA
                                INNER JOIN {item.DataBase}.dbo.INALMACEN AS AL ON AL.IDALMACEN = V.IDALMACEN
                                INNER JOIN {item.DataBase}.dbo.INTIPOPAGO AS TP ON TP.IDTIPOPAGO = RTP.IDTIPOPAGO
                                INNER JOIN {item.DataBase}.dbo.INCLIENTE AS CLI ON CLI.IDCLIENTE = V.IDCLIENTE
                                INNER JOIN {item.DataBase}.dbo.INCLIENTE AS VEN ON VEN.IDCLIENTE = V.IDVENDEDOR
                                LEFT JOIN {item.DataBase}.dbo.INPAGOACUENTA AS PC ON PC.IDRECIBO = R.IDRECIBO
                                WHERE R.FECHA BETWEEN @inicio AND @fin AND R.IDESTADO <> 2 AND V.IDESTADO <> 2 AND V.IDTIPOVENTA = 2
                                    AND (R.MONTO - R.INCREMENTO - R.SEGURO - R.EXPENSA - R.MULTA - ISNULL(PC.MONTO, 0) + R.PAGADOACUENTA) > 0
                                    AND V.FECHA >= '2016-04-01' AND V.NRODOC <> ''
                                    AND R.IDRECIBO NOT IN (SELECT idRecibo FROM {item.DataBase}.dbo.INRECIBO_UPG AS RG)
                                    {(proyectos.Length > 0 ? $"AND V.IDALMACEN NOT IN ({proyectos})": "")}
                                UNION ALL ";
                }
                query =  query.Substring(0, query.Length - 10);

                query = $@"SELECT 
                            ISNULL(CG.LCOMPLEJO_ID, 0) LComplejoId ,
                            T.*
                        FROM
                        ({query})T
                        LEFT JOIN BDBPMSION.dbo.SolicitudReprogramacion D ON D.IDVENTA =T.IDVENTA AND D.IdEstadoSolicitud IN (2,3,4,5)
                        LEFT JOIN BDComisiones.dbo.T_EMPRESACOMPLEJO CG ON CG.IDPROYECTO = T.IDPROYECTO
                        WHERE
                            T.FECHA_PAGO >= @inicio 
                            AND T.FECHA_PAGO <= @fin 
                            AND T.IDPRODUCTO NOT IN(SELECT 
                                                        E.IDPRODUCTO 
                                                    FROM bdcomisiones.dbo.REPROGRAMACION_ADENDA E 
                                                    WHERE T.IDVENTA=e.IDVENTA and E.fecharepro<'20201001')
                        AND D.IdProducto IS NULL ";

                return query;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        public static string GetQueryVentaResidual (int LCicloId) => @$"
        SELECT 
            CONCAT(y.idventa, '-', y.LOTES) AS NroVenta,
            y.EMPRESA AS Empresa,
            y.IDVENTA AS IdVenta,
            y.FECHA AS Fecha,
            y.IDALMACEN AS IdAlmacen,
            y.PROYECTO AS Proyecto,
            y.LOTES AS Lotes,
            y.IDRECIBO AS IdRecibo,
            y.FECHA_RECIBO AS FechaRecibo,
            y.NROCUOTA AS NroCuota,
            y.NROCUOTASPAGABLES AS NroCuotaPagables,
            y.IMPORTETOTAL AS ImporteTotal,
            y.IDCLIENTE AS IdCliente,
            y.NOMBRE_CLIENTE AS NombreCliente,
            y.CI_CLIENTE AS CiCliente,
            y.IDVENDEDOR AS IdVendedor,
            y.VENDEDOR AS Vendedor,
            y.CI_VENDEDOR AS CiVendedor,
            y.CONCEPTO1 AS Concepto1,
            {LCicloId} AS LcicloId
        FROM vwLISTAVENTAS_RECIBOS y
        WHERE 
            y.FECHA BETWEEN '20240501' AND @Fin
            AND y.FECHA_RECIBO BETWEEN @Inicio AND @Fin
        ORDER BY y.FECHA;
        ";
    }
}