
using Microsoft.Extensions.Configuration;

namespace Query.Cnx
{
    public class ScriptCnx
    {
       
        public static string QueryVentaCnx(IConfiguration configuration)
        {
             
            List<EmpresaCalculoComision> empresas = configuration.GetSection("EmpresaCalculoComisiones")
                                    .Get<List<EmpresaCalculoComision>>() ?? new List<EmpresaCalculoComision>();

            string query = @"";

            foreach (var item in empresas)
            {
                query += @$"
                    SELECT {item.EmpresaId} EmpresaId,'{item.Nombre}' Nombre,  V.FECHA DFecha
                        , PC.NROMANZANO SManzano, RTRIM(P.CODFABRICA) SLote, V.TOTALVENTA DPrecio
                        , V.IDALMACEN LComplejoId, V.IDVENTA IdVenta, RTRIM(VC.LOTES) Lote , PC.UV SUV
                        , ISNULL(VC.PRECIO_LISTA, V.TOTALVENTA) PrecioInicial
                        , V.CUOTAINICIAL SCuotaInicial, V.IDCLIENTE IdCliente, RTRIM(A.DESCRIPCION) Complejo, V.IDVENDEDOR VendedorId
                    FROM {item.DataBase}.dbo.INVENTA V
                    INNER JOIN {item.DataBase}.dbo.INVENTA_CCN VC ON VC.IDVENTA = V.IDVENTA
                    INNER JOIN {item.DataBase}.dbo.INPRODUCTO P ON P.IDPRODUCTO = VC.LOTES
                    INNER JOIN {item.DataBase}.dbo.INPRODUCTO_CCN PC ON PC.IDPRODUCTO = P.IDPRODUCTO 
                    INNER JOIN {item.DataBase}.dbo.INALMACEN A ON A.IDALMACEN = V.IDALMACEN
                    WHERE V.FECHA BETWEEN '20260101' AND '20260131' AND V.IDESTADO <> 2 and VC.IDESTADO_VENTA <>2 
                    and (v.NRODOC !='' or V.GLOSA like '%upgrade%') UNION ALL";
                
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
                            , RTRIM(C.DIRECCION) Direccion, ISNULL(P.idPaisGuardian, 2) IdPaisResidencia, '' SCiudad
                            , c.DOCID SCedulaIdentidad
                            , GETDATE() FechaRegistro, RTRIM(C.NOMBRE) SNombreCompleto
                            , C.FAX STelefonoOficina, C.DOCID SContrasena
                            , C.IDCLIENTE
                        FROM BDComisiones.dbo.grlCLIENTE C
                        INNER JOIN BDComisiones.dbo.grlCLIENTE_CCN CC ON CC.IDCLIENTE = C.IDCLIENTE
                        LEFT JOIN BDComisiones.DBO.PAISCONEXIONGUARDIAN P ON P.idPaisConexion = CC.IDCIUDAD_RESIDENCIA
                    ) CL ON CL.IDCLIENTE = SDAT.IdCliente
                    INNER JOIN (
                        SELECT
                            c.FAX TelefonoFijoVendedor, c.TELEFONO TelefonoMovilVendedor, RTRIM(C.EMAIL) CorreoVendedor
                            , C.FECHANACIMIENTO FechaNacimientoVendedor
                            , RTRIM(C.DIRECCION) DireccionVendedor, ISNULL(P.idPaisGuardian, 2) IdPaisResidenciaVendedor, '' SCiudadVendedor
                            , c.DOCID SCedulaIdentidadVendedor
                            , GETDATE() FechaReistro, RTRIM(C.NOMBRE) SNombreCompletoVendedor
                            , C.FAX STelefonoOficinaVendedor, C.DOCID SContrasenaVendedor
                            , C.IDCLIENTE
                        FROM BDComisiones.dbo.grlCLIENTE C
                        INNER JOIN BDComisiones.dbo.grlCLIENTE_CCN CC ON CC.IDCLIENTE = C.IDCLIENTE
                        LEFT JOIN BDComisiones.DBO.PAISCONEXIONGUARDIAN P ON P.idPaisConexion = CC.IDCIUDAD_RESIDENCIA
                    ) V ON V.IDCLIENTE = SDAT.VendedorId

            ";
        }

        public static string QueryCllienteDocId()
        {
            return @"SELECT CL.*, V.* FROM  vwLOTES_GRL_DOCID SDAT
            INNER JOIN (
                SELECT
                    c.FAX TelefonoFijo, c.TELEFONO TelefonoMovil, RTRIM(C.EMAIL) Correo
                    , C.FECHANACIMIENTO FechaNacimiento
                    , RTRIM(C.DIRECCION) Direccion, ISNULL(P.idPaisGuardian, 2) IdPaisResidencia, '' SCiudad
                    , c.DOCID SCedulaIdentidad
                    , GETDATE() FechaRegistro, RTRIM(C.NOMBRE) SNombreCompleto
                    , C.FAX STelefonoOficina, C.DOCID SContrasena
                    , C.IDCLIENTE
                FROM BDComisiones.dbo.grlCLIENTE C
                INNER JOIN BDComisiones.dbo.grlCLIENTE_CCN CC ON CC.IDCLIENTE = C.IDCLIENTE
                LEFT JOIN BDComisiones.DBO.PAISCONEXIONGUARDIAN P ON P.idPaisConexion = CC.IDCIUDAD_RESIDENCIA
            ) CL ON CL.IDCLIENTE = SDAT.IdCliente
            INNER JOIN (
                SELECT
                    c.FAX TelefonoFijoVendedor, c.TELEFONO TelefonoMovilVendedor, RTRIM(C.EMAIL) CorreoVendedor
                    , C.FECHANACIMIENTO FechaNacimientoVendedor
                    , RTRIM(C.DIRECCION) DireccionVendedor, ISNULL(P.idPaisGuardian, 2) IdPaisResidenciaVendedor, '' SCiudadVendedor
                    , c.DOCID SCedulaIdentidadVendedor
                    , GETDATE() FechaReistro, RTRIM(C.NOMBRE) SNombreCompletoVendedor
                    , C.FAX STelefonoOficinaVendedor, C.DOCID SContrasenaVendedor
                    , C.IDCLIENTE
                FROM BDComisiones.dbo.grlCLIENTE C
                INNER JOIN BDComisiones.dbo.grlCLIENTE_CCN CC ON CC.IDCLIENTE = C.IDCLIENTE
                LEFT JOIN BDComisiones.DBO.PAISCONEXIONGUARDIAN P ON P.idPaisConexion = CC.IDCIUDAD_RESIDENCIA
            ) V ON V.IDCLIENTE = SDAT.IDVENDEDOR
            where cl.SCedulaIdentidad = @docId";
        }
    }
}