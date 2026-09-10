
using Microsoft.Extensions.Configuration;

namespace Query.Grd
{
    public class ScriptGrd
    {
       
        public static string QueryBonoPar()
        {
             
            string query = $@"
                        SELECT * FROM(
                            SELECT 
                                CT.lcontacto_id LContctoGanadorId
                                , CT.SNombreCompleto SNombreGanador
                                , CT.scedulaidentidad SCedulaIdentidadGanador
                                , COUNT(*)PersonaQueVendieron
                                , case 
                                    when  TRUNCATE(COUNT(*), 0) BETWEEN 2 AND 3 THEN 200
                                    when  TRUNCATE(COUNT(*), 0) BETWEEN 4 AND 5 THEN 700
                                    when  TRUNCATE(COUNT(*), 0) BETWEEN 6 AND 7 THEN 1100
                                    when  TRUNCATE(COUNT(*), 0) BETWEEN 8 AND 9 THEN 1400
                                    when  TRUNCATE(COUNT(*), 0) > 10 THEN 2000
                                    ELSE 0
                                END Bono
                                , sum(DAT.CantVta) CantidadVenta
                                , GROUP_CONCAT(DAT.VId)VendedoresId
                                , GROUP_CONCAT(DAT.LContratoId)LContratoId
                                , GROUP_CONCAT(DAT.SNroVenta)SNroVenta
                                , sum(DAT.MontoVentas)MontoVentas
                                , sum(DAT.CuotasIniciales)CuotasIniciales
                            FROM (
                                SELECT
                                    V.lpatrocinante_id GId
                                    , V.lcontacto_id VId 
                                    , GROUP_CONCAT(ACT.snroventa) SNroVenta
                                    , GROUP_CONCAT(ACT.lcontrato_id) LContratoId
                                    , GROUP_CONCAT(ACT.dtfecha) Fecha
                                    , COUNT(*) CantVta
                                    , sum(act.dprecio) MontoVentas
                                    , sum(act.dcuota_inicial) CuotasIniciales
                                FROM administracioncontrato ACT
                                INNER JOIN administracioncontacto V ON v.lcontacto_id = ACT.lasesor_id
                                WHERE ACT.dtfecha BETWEEN @Inicio AND @Fin and 
                                    CASE WHEN (ACT.dcuota_inicial * 100 / ACT.dprecio) BETWEEN 2.9999 AND 4.98 THEN 
                                        CASE WHEN  ACT.dprecio >= 10000 THEN 1 ELSE 0 END
                                    ELSE 1 END = 1 and ACT.ltipocontrato_id in (1,2) and ACT.lcontacto_id != ACT.lasesor_id
                                    AND ACT.snroventa not like '%KTRB5%'
                                GROUP BY V.lpatrocinante_id, V.lcontacto_id 
                            )DAT 
                            INNER JOIN administracioncontacto CT ON CT.lcontacto_id = DAT.GId
                            GROUP BY CT.lcontacto_id, CT.SNombreCompleto, CT.scedulaidentidad
                        ) DATG where DATG.Bono > 0 ORDER BY SNombreGanador";
            return query;
        }
        public static string QueryDetalleBonoPar(string LcontratoId)
        {
                string queryDetalle =$@" SELECT
                                V.lpatrocinante_id LContactoGanadorId 
                                , V.lcontacto_id LContactoVendedorId
                                , V.snombrecompleto SNombreVendedor
                                , V.scedulaidentidad SCedulaIdentidadVendedor
                                , C.lcontacto_id LContactoClienteId
                                , C.snombrecompleto SNombreCliente
                                , C.scedulaidentidad SCedulaCliente
                                , ACT.lcontrato_id LContratoId
                                , ACT.dtfecha DtFecha
                                , ACT.snroventa SNroVenta
                                , ACT.dprecio DPrecio
                                , ACT.dcuota_inicial DCuotaInicial
                                FROM administracioncontrato ACT
                                INNER JOIN administracioncontacto V on V.lcontacto_id = ACT.lasesor_id
                                INNER JOIN administracioncontacto C ON C.lcontacto_id = ACT.lcontacto_id
                                WHERE ACT.lcontrato_id in ({LcontratoId}) ORDER BY V.snombrecompleto";
     

            return queryDetalle;
        }
   }
}