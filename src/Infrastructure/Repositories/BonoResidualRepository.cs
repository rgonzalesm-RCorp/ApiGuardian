using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Query.Cnx;

namespace ApiGuardian.Infrastructure.Repositories;

public class BonoResidualRepository : IBonoResidualRepository
{
    private readonly DapperContext _context;
    private readonly DapperContextSqlServer _contextSql;
    private readonly DapperContextSqlServer64 _contextSql64;
    private readonly IConfiguration _configuration;
    private readonly ILogService _log;
    private string NOMBREARCHIVO = "BonoResidualRepository.CS";
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
        string query = $@"insert into T_CARTERA64 
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
            using var connection = _contextSql64.CreateConnection();
            var rows = await connection.ExecuteAsync(query, ListadoCartera);

            bool success = rows > 0;
            string mensaje = success ? "Se registro correctamente la cartera" : "No se realizó el guardado.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, rowsAffected:{rows}, data count:{ListadoCartera.Count}]");

            return (success, mensaje);

        }
        catch (System.Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return(false, $"Error: {ex.Message}");
        }
    }


    public async Task<(IEnumerable<TCuota> ListaCuota, bool Success, string Mensaje, int counter)> GetCuota(string LogTransaccionId,string Usuario, int page, int pageSize, string inicio, string fin)
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

            return (ListaCuota, success, mensaje, ListaCuota.Count());
        }
        catch (Exception ex)
        {
            return(Enumerable.Empty<TCuota>(), false, $"Error al obtener los tipos de descuento: {ex.Message}", 0);
        }
    }
    public async Task<(IEnumerable<TCuota> ListaCuota, bool Success, string Mensaje)> GetCuotaAll(string LogTransaccionId,string Usuario, string inicio, string fin)
    {
        string nombreMetodo = "GetCuotaAll()";
        string query = "EXEC BDQishur.dbo.SP_OBTENER_CUOTAS_FOR_RESIDUAL @inicio, @fin";
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

}
