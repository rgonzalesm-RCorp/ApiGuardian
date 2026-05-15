using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Query.Cnx;
using DocumentFormat.OpenXml.Bibliography;
using System.Text;

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
    public async Task<(bool Success, string Mensaje)> GuardarCartera(string LogTransaccionId, string Usuario,List<TCartera> ListadoCartera)
    {
        string nombreMetodo = "GuardarCartera()";

        if (ListadoCartera == null || ListadoCartera.Count == 0)
            return (false, "No existen registros para guardar.");

        const int batchSize = 1500;

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
            $"Inicio de metodo [cantidad:{ListadoCartera.Count}]");

        try
        {
            foreach (var item in ListadoCartera)
            {
                item.Fecha = item.Fecha?.Replace("-", "") ?? "";
                item.UltimoPago = item.UltimoPago?.Replace("-", "");
                item.FUltimoVenc = item.FUltimoVenc?.Replace("-", "");
                item.FVencMasAnt = item.FVencMasAnt?.Replace("-", "");
            }

            using var connection = _context.CreateConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();

            await connection.ExecuteAsync(
                SCRIPT_CLEAR_CARTERA,
                transaction: transaction
            );

            int totalInsertados = 0;

            for (int i = 0; i < ListadoCartera.Count; i += batchSize)
            {
                var batch = ListadoCartera
                    .Skip(i)
                    .Take(batchSize)
                    .ToList();

                var sql = new StringBuilder();

                sql.Append(@"
                    INSERT INTO Cartera 
                    (
                        EMPRESA, LOTE, DOCID, CLIENTE, DOCID_VENDEDOR, NOMBRE, IDTIPOVENTA, IDPROYECTO,
                        IDVENTA, CUOTAINICIAL, TOTALVENTA, TOTALDEUDA, FECHA, PROYECTO, CUOTAS_LOTES_VENCIDAS,
                        ULTIMO_PAGO, ESTADO, TRANS, NIT, TEL_CEL, TELEFONO, DIRECCION, EMAIL, UV, MZNO, NRO_LOTE,
                        PRECIO_LISTA, CIUDAD_RESIDENCIA, MONTO_CAPITAL_VENC, MONTO_INTERES_VENC, MONTO_MULTA, MONTO_EXPENSA,
                        F_VENC_MAS_ANT, F_ULTIMO_VENC
                    )
                    VALUES
                ");

                var parameters = new DynamicParameters();

                for (int j = 0; j < batch.Count; j++)
                {
                    var item = batch[j];

                    sql.Append($@"
                    (
                        @Empresa{j}, @Lote{j}, @Docid{j}, @Cliente{j}, @DocidVendedor{j}, @Nombre{j}, @Idtipoventa{j}, @Idproyecto{j},
                        @Idventa{j}, @Cuotainicial{j}, @Totalventa{j}, @Totaldeuda{j}, @Fecha{j}, @Proyecto{j}, @CuotasLotesVencidas{j},
                        @UltimoPago{j}, @Estado{j}, @Trans{j}, @Nit{j}, @TelCel{j}, @Telefono{j}, @Direccion{j}, @Email{j}, @Uv{j}, @Mzno{j}, @NroLote{j},
                        @PrecioLista{j}, @CiudadResidencia{j}, @MontoCapitalVenc{j}, @MontoInteresVenc{j}, @MontoMulta{j}, @MontoExpensa{j},
                        @FVencMasAnt{j}, @FUltimoVenc{j}
                    )");

                    if (j < batch.Count - 1)
                        sql.Append(",");

                    parameters.Add($"Empresa{j}", item.Empresa);
                    parameters.Add($"Lote{j}", item.Lote);
                    parameters.Add($"Docid{j}", item.Docid);
                    parameters.Add($"Cliente{j}", item.Cliente);
                    parameters.Add($"DocidVendedor{j}", item.DocidVendedor);
                    parameters.Add($"Nombre{j}", item.Nombre);
                    parameters.Add($"Idtipoventa{j}", item.Idtipoventa);
                    parameters.Add($"Idproyecto{j}", item.Idproyecto);
                    parameters.Add($"Idventa{j}", item.Idventa);
                    parameters.Add($"Cuotainicial{j}", item.Cuotainicial);
                    parameters.Add($"Totalventa{j}", item.Totalventa);
                    parameters.Add($"Totaldeuda{j}", item.Totaldeuda);
                    parameters.Add($"Fecha{j}", item.Fecha);
                    parameters.Add($"Proyecto{j}", item.Proyecto);
                    parameters.Add($"CuotasLotesVencidas{j}", item.CuotasLotesVencidas);
                    parameters.Add($"UltimoPago{j}", item.UltimoPago);
                    parameters.Add($"Estado{j}", item.Estado);
                    parameters.Add($"Trans{j}", item.Trans);
                    parameters.Add($"Nit{j}", item.Nit);
                    parameters.Add($"TelCel{j}", item.TelCel);
                    parameters.Add($"Telefono{j}", item.Telefono);
                    parameters.Add($"Direccion{j}", item.Direccion);
                    parameters.Add($"Email{j}", item.Email);
                    parameters.Add($"Uv{j}", item.Uv);
                    parameters.Add($"Mzno{j}", item.Mzno);
                    parameters.Add($"NroLote{j}", item.NroLote);
                    parameters.Add($"PrecioLista{j}", item.PrecioLista);
                    parameters.Add($"CiudadResidencia{j}", item.CiudadResidencia);
                    parameters.Add($"MontoCapitalVenc{j}", item.MontoCapitalVenc);
                    parameters.Add($"MontoInteresVenc{j}", item.MontoInteresVenc);
                    parameters.Add($"MontoMulta{j}", item.MontoMulta);
                    parameters.Add($"MontoExpensa{j}", item.MontoExpensa);
                    parameters.Add($"FVencMasAnt{j}", item.FVencMasAnt);
                    parameters.Add($"FUltimoVenc{j}", item.FUltimoVenc);
                }

                sql.Append(";");

                int rows = await connection.ExecuteAsync(
                    sql.ToString(),
                    parameters,
                    transaction
                );

                totalInsertados += rows;
            }

            transaction.Commit();

            string mensaje = $"Se registró correctamente la cartera. Total insertado: {totalInsertados}";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje:{mensaje}, data count:{ListadoCartera.Count}]");

            return (true, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Error al guardar cartera", ex);
            return (false, $"Error: {ex.Message}");
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
    public async Task<(bool Success, string Mensaje)> GuardarCuota(string LogTransaccionId, string Usuario, List<TCuota> ListaCuota, bool excedente = false)
    {
        string nombreMetodo = "GuardarCuota()";

        if (ListaCuota == null || ListaCuota.Count == 0)
            return (false, "No existen registros para guardar.");

        const int batchSize = 500;

        _log.Info(
            LogTransaccionId,
            NOMBREARCHIVO,
            nombreMetodo,
            $"Inicio de metodo [usuario:{Usuario}, cantidad:{ListaCuota.Count}]"
        );

        try
        {
            using var connection = _context.CreateConnection();

            connection.Open();

            using var transaction = connection.BeginTransaction();

            if (!excedente)
            {
                await connection.ExecuteAsync(
                    SCRIPT_CLEAR_CUOTAS,
                    transaction: transaction
                );
            }

            int totalInsertados = 0;

            for (int i = 0; i < ListaCuota.Count; i += batchSize)
            {
                var batch = ListaCuota
                    .Skip(i)
                    .Take(batchSize)
                    .ToList();

                var sql = new StringBuilder();

                sql.Append(@"
                    INSERT INTO T_ACCIONESCUOTASGRL
                    (
                        IDPRODUCTO,
                        IDPROYECTO,
                        PROYECTO,
                        IDRECIBO,
                        IDVENTA,
                        IDTIPOPAGO,
                        DESCRIPCION,
                        IDCLIENTE,
                        CLIENTE,
                        DOCIDCLI,
                        IDVENDEDOR,
                        VENDEDOR,
                        DOCIDVEN,
                        BONO,
                        AMORTIZACION,
                        CAPITAL,
                        INTERES,
                        SEGURO,
                        EXPENSA,
                        MULTA,
                        FECHA_VENTA,
                        FECHA_PAGO,
                        ACUENTA,
                        TOTALPAGO,
                        MONTODEUDA,
                        PAGOSACUENTA,
                        NROCUOTA,
                        empresa,
                        FECHAINS
                    )
                    VALUES
                ");

                var parameters = new DynamicParameters();

                for (int j = 0; j < batch.Count; j++)
                {
                    var item = batch[j];

                    sql.Append($@"
                    (
                        @IDPRODUCTO{j},
                        @IDPROYECTO{j},
                        @PROYECTO{j},
                        @IDRECIBO{j},
                        @IDVENTA{j},
                        @IDTIPOPAGO{j},
                        @DESCRIPCION{j},
                        @IDCLIENTE{j},
                        @CLIENTE{j},
                        @DOCIDCLI{j},
                        @IDVENDEDOR{j},
                        @VENDEDOR{j},
                        @DOCIDVEN{j},
                        @BONO{j},
                        @AMORTIZACION{j},
                        @CAPITAL{j},
                        @INTERES{j},
                        @SEGURO{j},
                        @EXPENSA{j},
                        @MULTA{j},
                        @FECHA_VENTA{j},
                        @FECHA_PAGO{j},
                        @ACUENTA{j},
                        @TOTALPAGO{j},
                        @MONTODEUDA{j},
                        @PAGOSACUENTA{j},
                        @NROCUOTA{j},
                        @Empresa{j},
                        NOW()
                    )");

                    if (j < batch.Count - 1)
                        sql.Append(",");

                    parameters.Add($"IDPRODUCTO{j}", item.Idproducto);
                    parameters.Add($"IDPROYECTO{j}", item.LComplejoId);
                    parameters.Add($"PROYECTO{j}", item.Proyecto);
                    parameters.Add($"IDRECIBO{j}", item.Idrecibo);
                    parameters.Add($"IDVENTA{j}", item.Idventa);
                    parameters.Add($"IDTIPOPAGO{j}", item.Idtipopago);
                    parameters.Add($"DESCRIPCION{j}", item.Descripcion);
                    parameters.Add($"IDCLIENTE{j}", item.Idcliente);
                    parameters.Add($"CLIENTE{j}", item.Cliente);
                    parameters.Add($"DOCIDCLI{j}", item.Docidcli);
                    parameters.Add($"IDVENDEDOR{j}", item.Idvendedor);
                    parameters.Add($"VENDEDOR{j}", item.Vendedor);
                    parameters.Add($"DOCIDVEN{j}", item.Docidven);
                    parameters.Add($"BONO{j}", item.Bono);
                    parameters.Add($"AMORTIZACION{j}", item.Amortizacion);
                    parameters.Add($"CAPITAL{j}", item.Capital);
                    parameters.Add($"INTERES{j}", item.Interes);
                    parameters.Add($"SEGURO{j}", item.Seguro);
                    parameters.Add($"EXPENSA{j}", item.Expensa);
                    parameters.Add($"MULTA{j}", item.Multa);
                    parameters.Add($"FECHA_VENTA{j}", item.Fecha_Venta);
                    parameters.Add($"FECHA_PAGO{j}", item.Fecha_Pago);
                    parameters.Add($"ACUENTA{j}", item.Acuenta);
                    parameters.Add($"TOTALPAGO{j}", item.Totalpago);
                    parameters.Add($"MONTODEUDA{j}", item.Montodeuda);
                    parameters.Add($"PAGOSACUENTA{j}", item.Pagosacuenta);
                    parameters.Add($"NROCUOTA{j}", item.Nrocuota);
                    parameters.Add($"Empresa{j}", item.Empresa);
                }

                sql.Append(";");

                int rows = await connection.ExecuteAsync(
                    sql.ToString(),
                    parameters,
                    transaction
                );

                totalInsertados += rows;
            }

            transaction.Commit();

            string mensaje = $"Se registró correctamente la cuota. Total insertado: {totalInsertados}";

            _log.Info(
                LogTransaccionId,
                NOMBREARCHIVO,
                nombreMetodo,
                $"Fin de metodo [mensaje:{mensaje}, data count:{ListaCuota.Count}]"
            );

            return (true, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(
                LogTransaccionId,
                NOMBREARCHIVO,
                nombreMetodo,
                "Fin de metodo",
                ex
            );

            return (false, $"Error: {ex.Message}");
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
                                    , lpatrocinante_id LPatrocinanteId
                                from tmp_residual_contacto";
        string queryContactosActivos = @"select DISTINCT lcontacto_id LContactoId from administracionventapersonal where lciclo_id = @LCicloId";

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
