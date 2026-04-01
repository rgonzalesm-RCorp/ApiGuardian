using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Query.Cnx;
using DocumentFormat.OpenXml.Bibliography;

namespace ApiGuardian.Infrastructure.Repositories;

public class BonoResidualRepository : IBonoResidualRepository
{
    private readonly DapperContext _context;
    private readonly DapperContextSqlServer _contextSql;
    private readonly DapperContextSqlServer64 _contextSql64;
    private readonly IConfiguration _configuration;
    private readonly ILogService _log;
    private string NOMBREARCHIVO = "BonoResidualRepository.CS";

    #region "Script"
    private readonly string SCRIPT_CLEAR_CUOTAS = "TRUNCATE TABLE T_ACCIONESCUOTASGRL";
    private readonly string SCRIPT_CLEAR_CARTERA = "TRUNCATE TABLE Cartera";
    #endregion
    public BonoResidualRepository(DapperContext context, ILogService log, DapperContextSqlServer contextSql, DapperContextSqlServer64 contextSql64, IConfiguration configuration)
    {
        _context = context;
        _contextSql = contextSql;
        _contextSql64 = contextSql64;
        _log = log;
        _configuration = configuration;
    }
    public async Task<(IEnumerable<TCartera> ListaCartera, bool Success, string Mensaje, int counter)> GetCartera(string LogTransaccionId, string Usuario, int page, int pageSize)
    {
        string nombreMetodo = "GetCartera()";

        string query = $@"SELECT EMPRESA AS Empresa,
                        LOTE AS Lote,
                        DOCID AS Docid,
                        CLIENTE AS Cliente,
                        DOCID_VENDEDOR AS DocidVendedor,
                        NOMBRE AS Nombre,
                        IDTIPOVENTA AS Idtipoventa,
                        IDPROYECTO AS Idproyecto,
                        IDVENTA AS Idventa,
                        CUOTAINICIAL AS Cuotainicial,
                        TOTALVENTA AS Totalventa,
                        TOTALDEUDA AS Totaldeuda,
                        FECHA AS Fecha,
                        PROYECTO AS Proyecto,
                        CUOTAS_LOTES_VENCIDAS AS CuotasLotesVencidas,
                        ULTIMO_PAGO AS UltimoPago,
                        ESTADO AS Estado,
                        TRANS AS Trans,
                        NIT AS Nit,
                        TEL_CEL AS TelCel,
                        TELEFONO AS Telefono,
                        DIRECCION AS Direccion,
                        EMAIL AS Email,
                        UV AS Uv,
                        MZNO AS Mzno,
                        NRO_LOTE AS NroLote,
                        PRECIO_LISTA AS PrecioLista,
                        CIUDAD_RESIDENCIA AS CiudadResidencia,
                        MONTO_CAPITAL_VENC AS MontoCapitalVenc,
                        MONTO_INTERES_VENC AS MontoInteresVenc,
                        MONTO_MULTA AS MontoMulta,
                        MONTO_EXPENSA AS MontoExpensa,
                        F_VENC_MAS_ANT AS FVencMasAnt,
                        F_ULTIMO_VENC AS FUltimoVenc 
            FROM BDComisiones.DBO.T_CARTERA
            ORDER BY IDVENTA
            OFFSET @page * @pageSize ROWS
            FETCH NEXT @pageSize ROWS ONLY";
        string queryCount = $@"SELECT COUNT(*) FROM BDComisiones.DBO.T_CARTERA";
        int counter = 0;
        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _contextSql.CreateConnection();

            var ListaCartera = await connection.QueryAsync<TCartera>(query, new{ page, pageSize});
            counter = await connection.ExecuteScalarAsync<int>(queryCount);

            bool success = true;
            string mensaje = success ? "Cartera obtenidos correctamente." : "No se encontraron lista de Cartera.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, total registro:{counter}]");

            return (ListaCartera, success, mensaje, counter);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (Enumerable.Empty<TCartera>(), false, $"Error al obtener los tipos de descuento: {ex.Message}", 0);
        }
    }
    public async Task<(IEnumerable<TCartera> ListaCartera, bool Success, string Mensaje, int counter)> GetCarteraAll(string LogTransaccionId, string Usuario)
    {
        string nombreMetodo = "GetCarteraAll()";

        string query = $@"SELECT EMPRESA AS Empresa,
                        LOTE AS Lote,
                        DOCID AS Docid,
                        CLIENTE AS Cliente,
                        DOCID_VENDEDOR AS DocidVendedor,
                        NOMBRE AS Nombre,
                        IDTIPOVENTA AS Idtipoventa,
                        IDPROYECTO AS Idproyecto,
                        IDVENTA AS Idventa,
                        CUOTAINICIAL AS Cuotainicial,
                        TOTALVENTA AS Totalventa,
                        TOTALDEUDA AS Totaldeuda,
                        FECHA AS Fecha,
                        PROYECTO AS Proyecto,
                        CUOTAS_LOTES_VENCIDAS AS CuotasLotesVencidas,
                        ULTIMO_PAGO AS UltimoPago,
                        ESTADO AS Estado,
                        TRANS AS Trans,
                        NIT AS Nit,
                        TEL_CEL AS TelCel,
                        TELEFONO AS Telefono,
                        DIRECCION AS Direccion,
                        EMAIL AS Email,
                        UV AS Uv,
                        MZNO AS Mzno,
                        NRO_LOTE AS NroLote,
                        PRECIO_LISTA AS PrecioLista,
                        CIUDAD_RESIDENCIA AS CiudadResidencia,
                        MONTO_CAPITAL_VENC AS MontoCapitalVenc,
                        MONTO_INTERES_VENC AS MontoInteresVenc,
                        MONTO_MULTA AS MontoMulta,
                        MONTO_EXPENSA AS MontoExpensa,
                        F_VENC_MAS_ANT AS FVencMasAnt,
                        F_ULTIMO_VENC AS FUltimoVenc 
            FROM BDComisiones.DBO.T_CARTERA";
        int counter = 0;
        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _contextSql.CreateConnection();

            var ListaCartera = await connection.QueryAsync<TCartera>(query);

            bool success = true;
            string mensaje = success ? "Cartera obtenidos correctamente." : "No se encontraron lista de Cartera.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, total registro:{counter}]");

            return (ListaCartera, success, mensaje, counter);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (Enumerable.Empty<TCartera>(), false, $"Error al obtener los tipos de descuento: {ex.Message}", 0);
        }
    }
    public async Task<(bool Success, string Mensaje)> GuardarCartera(string LogTransaccionId,string Usuario, List<TCartera> ListadoCartera)
    {
        string nombreMetodo = "GuardarCartera()";
        string query = $@"insert into Cartera 
                            (
                                EMPRESA, LOTE, DOCID, CLIENTE, DOCID_VENDEDOR, NOMBRE, IDTIPOVENTA, IDPROYECTO,
                                IDVENTA, CUOTAINICIAL, TOTALVENTA, TOTALDEUDA, FECHA, PROYECTO, CUOTAS_LOTES_VENCIDAS,
                                ULTIMO_PAGO, ESTADO, TRANS, NIT, TEL_CEL, TELEFONO, DIRECCION, EMAIL, UV, MZNO, NRO_LOTE,
                                PRECIO_LISTA, CIUDAD_RESIDENCIA, MONTO_CAPITAL_VENC, MONTO_INTERES_VENC, MONTO_MULTA, MONTO_EXPENSA,
                                F_VENC_MAS_ANT, F_ULTIMO_VENC
                            )
                            values
                            (
                                @Empresa, @Lote, @Docid, @Cliente, @DocidVendedor, @Nombre, @Idtipoventa, @Idproyecto,
                                @Idventa, @Cuotainicial, @Totalventa, @Totaldeuda, @Fecha, @Proyecto, @CuotasLotesVencidas,
                                @UltimoPago, @Estado, @Trans, @Nit, @TelCel, @Telefono, @Direccion, @Email, @Uv, @Mzno, @NroLote,
                                @PrecioLista, @CiudadResidencia, @MontoCapitalVenc, @MontoInteresVenc, @MontoMulta, @MontoExpensa,
                                @FVencMasAnt, @FUltimoVenc
                            )";
        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");
        try
        {
            foreach (var item in ListadoCartera)
            {
                item.Fecha = item.Fecha.Replace("-", "");
                item.UltimoPago = item.UltimoPago != null ? item.UltimoPago.Replace("-", ""): null;                    
                item.FUltimoVenc = item.FUltimoVenc == null ? null: item.FUltimoVenc.Replace("-", "");
                item.FVencMasAnt = item.FVencMasAnt == null ? null: item.FVencMasAnt.Replace("-", "");
            }
            
            using var connection = _context.CreateConnection();
            await connection.ExecuteAsync(SCRIPT_CLEAR_CARTERA);
            var rows = await connection.ExecuteAsync(query, ListadoCartera);
            bool success = rows > 0;
            string mensaje = success ? "Se registro correctamente la cartera" : "No se realizó el guardado.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,$"Fin de metodo [mensaje: {mensaje}, rowsAffected:{rows}, data count:{ListadoCartera.Count}]");

            return (success, mensaje);

        }
        catch (System.Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return(false, $"Error: {ex.Message}");
        }
    }


    public async Task<(IEnumerable<TCuota> ListaCuota, bool Success, string Mensaje)> GetCuota(string LogTransaccionId,string Usuario,  string inicio, string fin)
    {
        string nombreMetodo = "GetCuota()";
        var query = ScriptCnx.QueryObetnerCuotas(_configuration);

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _contextSql.CreateConnection();

            var ListaCuota = await connection.QueryAsync<TCuota>(query, new{ inicio, fin});

            bool success = true;
            string mensaje = success ? "Cuota obtenidos correctamente." : "No se encontraron lista de Cuota.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, total registro:{ListaCuota.Count()}]");

            return (ListaCuota, success, mensaje);
        }
        catch (Exception ex)
        {
            return(Enumerable.Empty<TCuota>(), false, $"Error al obtener los tipos de descuento: {ex.Message}");
        }
    }
    public async Task<(bool Success, string Mensaje)> GuardarCuota(string LogTransaccionId,string Usuario, List<TCuota> ListaCuota, bool excedente = false)
    {
        string nombreMetodo = "GuardarCuota()";
        string query = $@"insert into T_ACCIONESCUOTASGRL (
                                IDPRODUCTO,	IDPROYECTO,	PROYECTO, IDRECIBO,	IDVENTA, IDTIPOPAGO, DESCRIPCION, IDCLIENTE, CLIENTE
                                , DOCIDCLI, IDVENDEDOR, VENDEDOR, DOCIDVEN, BONO, AMORTIZACION, CAPITAL, INTERES, SEGURO, EXPENSA
                                , MULTA, FECHA_VENTA, FECHA_PAGO, ACUENTA, TOTALPAGO, MONTODEUDA, PAGOSACUENTA,	NROCUOTA, empresa) 
                            values (
                                @IDPRODUCTO, @LComplejoId, @PROYECTO, @IDRECIBO, @IDVENTA, @IDTIPOPAGO, @DESCRIPCION, @IDCLIENTE, @CLIENTE
                                , @DOCIDCLI, @IDVENDEDOR, @VENDEDOR, @DOCIDVEN, @BONO, @AMORTIZACION, @CAPITAL, @INTERES, @SEGURO, @EXPENSA
                                , @MULTA, @FECHA_VENTA, @FECHA_PAGO, @ACUENTA, @TOTALPAGO, @MONTODEUDA, @PAGOSACUENTA, @NROCUOTA, @Empresa)";
        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}, usuario = {Usuario}]");
        try
        {
            using var connection = _context.CreateConnection();
            if (!excedente)
                await connection.ExecuteAsync(SCRIPT_CLEAR_CUOTAS);
            
            var rows = await connection.ExecuteAsync(query, ListaCuota);

            bool success = rows > 0;
            string mensaje = success ? "Se registro correctamente la cuota" : "No se realizó el guardado.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, rowsAffected:{rows}, data count:{ListaCuota.Count}]");

            return (success, mensaje);

        }
        catch (System.Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return(false, $"Error: {ex.Message}");
        }
    }


    public async Task<(IEnumerable<Excedente> ListaCuota, bool Success, string Mensaje)> GetExcedente(string LogTransaccionId,string Usuario, string inicio, string fin)
    {
        
        string nombreMetodo = "GetExcedente()";
        var query = ScriptCnx.QueryVentaCnx(_configuration);

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}, usuario: {Usuario}]");

        try
        {
            using var connection = _contextSql.CreateConnection();

            var ListaCuota = await connection.QueryAsync<Excedente>(query, new{ inicio, fin});

            bool success = true;
            string mensaje = success ? "Excedentes obtenidos correctamente." : "No se encontraron lista de excedentes.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, total registro:{ListaCuota.Count()}]");

            return (ListaCuota, success, mensaje);
        }
        catch (Exception ex)
        {
            return(Enumerable.Empty<Excedente>(), false, $"Error al obtener los tipos de descuento: {ex.Message}");
        }
    }
    public async Task<(IEnumerable<BrCuotaRed> ListaCuotaRed, IEnumerable<BrContacto> ListaContacto, IEnumerable<BrContactoActivos> ListaContactosActivos, bool Success, string Mensaje)> GetDataCalculoBonoResidual(string LogTransaccionId,string Usuario, int LCicloId)
    {
        string nombreMetodo = "GetDataCalculoBonoResidual()";
        string queryCuota = @"select 
                                ct.Id , ct.IDPRODUCTO ProductoId
                                , ct.IDPROYECTO ProyectoId , CT.PROYECTO Proyecto
                                , CT.IDRECIBO ReciboId , CT.IDVENTA VentaId
                                , CT.IDTIPOPAGO TipoPagoId , CT.DESCRIPCION TipoPago
                                , CT.IDCLIENTE ClienteId , CT.CLIENTE Cliente
                                , CT.DOCIDCLI DocumentoCliente , CT.IDVENDEDOR VendedorId
                                , CT.VENDEDOR Vendedor , CT.DOCIDVEN DocumentoVendedor
                                , CT.BONO Bono , C.lcontacto_id LContactoId
                                , r.lpatrocinador1g LPatrocinado1 , r.lpatrocinador2g LPatrocinado2
                                , r.lpatrocinador3g LPatrocinado3 , r.lpatrocinador4g LPatrocinado4
                                , r.lpatrocinador5g LPatrocinado5 , r.lpatrocinador6g LPatrocinado6
                                , r.lpatrocinador7g LPatrocinado7
                                , ct.empresa Empresa
                            from T_ACCIONESCUOTASGRL ct 
                            inner JOIN tmp_residual_contacto c on ct.docidcli = c.scedulaidentidad
                            inner join tmp_residual_red r on r.lcontacto_id = c.lcontacto_id ";
        string queryContacto = @"select 
                                    tmpresidualcontactoId TmpResidualContactoId
                                    , lcontacto_id LContactoId
                                    , scedulaidentidad SCedulaIdentidad
                                    , snombrecompleto SNombreCompleto
                                    , scodigo Codigo 
                                from tmp_residual_contacto";
        string queryContactosActivos = @"select DISTINCT lcontacto_id from administracionventapersonal where lciclo_id = @LCicloId";
        
        string queryInsertTempRed = @"
                                        TRUNCATE TABLE tmp_residual_red;
                                        insert into tmp_residual_red
                                        SELECT 0,
                                                    r.lcontacto_id
                                                    , r.lpatrocinador1g
                                                    , r.lpatrocinador2g
                                                    , r.lpatrocinador3g
                                                    , r.lpatrocinador4g
                                                    , r.lpatrocinador5g
                                                    , r.lpatrocinador6g
                                                    , r.lpatrocinador7g
                                                    FROM administracionred R  
                                                    where r.lciclo_id = @LCicloId";
        string queryInsertTempContacto = @"TRUNCATE TABLE tmp_residual_contacto;
                                            insert into tmp_residual_contacto
                                            select 0, lcontacto_id, scedulaidentidad, snombrecompleto, scodigo 
                                            from administracioncontacto ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [queryCuota: {queryCuota}, queryContacto: {queryContacto}, usuario: {Usuario}]");

        try
        {
            using var connection = _context.CreateConnection();
            //await connection.QueryAsync(queryInsertTempRed, new {LCicloId});
            //await connection.QueryAsync(queryInsertTempContacto);

            var ListaCuota = await connection.QueryAsync<BrCuotaRed>(queryCuota);
            var ListaContacto = await connection.QueryAsync<BrContacto>(queryContacto);
            var ListaContactoActivos = await connection.QueryAsync<BrContactoActivos>(queryContactosActivos, new {LCicloId});

            bool success = true;
            string mensaje = success ? "Datos obtenidos correctamente." : "No se encontraron los datos.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, total registro:{ListaCuota.Count()}]");

            return (ListaCuota, ListaContacto, ListaContactoActivos, success, mensaje);
        }
        catch (Exception ex)
        {
            return(Enumerable.Empty<BrCuotaRed>(), Enumerable.Empty<BrContacto>(), Enumerable.Empty<BrContactoActivos>(), false, $"Error al obtener los tipos de descuento: {ex.Message}");
        }
    }


}
