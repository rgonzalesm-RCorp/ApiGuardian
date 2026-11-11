using System.Data;
using System.Text;
using ApiGuardian.Domain.Entities;
using Dapper;

namespace ApiGuardian.Infrastructure.Repositories.Simulacion;

// F7-T2: Bono Residual. Lectura y escrituras copiadas de BonoResidualRepository y AdministracionBonoResidualRepository (R2).
// Diferencia: la consulta de cuotas filtra por FECHA_PAGO del ciclo. El original no filtra porque TRUNCATE deja solo
// las cuotas del ciclo; el light (D-32) borra por rango, así que sin el filtro contaría cuotas de ciclos anteriores.
public sealed partial class SimulacionRepositorio
{
    private const string SqlCuotasRedCiclo = @"select
            ct.Id, ct.IDPRODUCTO ProductoId,
            ct.IDPROYECTO ProyectoId, CT.PROYECTO Proyecto,
            CT.IDRECIBO ReciboId, CT.IDVENTA VentaId,
            CT.IDTIPOPAGO TipoPagoId, CT.DESCRIPCION TipoPago,
            CT.IDCLIENTE ClienteId, CT.CLIENTE Cliente,
            CT.DOCIDCLI DocumentoCliente, CT.IDVENDEDOR VendedorId,
            CT.VENDEDOR Vendedor, CT.DOCIDVEN DocumentoVendedor,
            CT.BONO Bono, C.lcontacto_id LContactoId,
            r.lpatrocinador1g LPatrocinado1, r.lpatrocinador2g LPatrocinado2,
            r.lpatrocinador3g LPatrocinado3, r.lpatrocinador4g LPatrocinado4,
            r.lpatrocinador5g LPatrocinado5, r.lpatrocinador6g LPatrocinado6,
            r.lpatrocinador7g LPatrocinado7,
            ct.empresa Empresa,
            ct.FECHA_PAGO FechaPago
        from T_ACCIONESCUOTASGRL ct
        inner JOIN tmp_residual_contacto c on ct.docidcli = c.scedulaidentidad
        inner join tmp_residual_red r on r.lcontacto_id = c.lcontacto_id
        WHERE ct.FECHA_PAGO BETWEEN @Inicio AND @Fin;";

    private const string SqlContactosTmp = @"select
            tmpresidualcontactoId TmpResidualContactoId, lcontacto_id LContactoId,
            scedulaidentidad SCedulaIdentidad, snombrecompleto SNombreCompleto,
            scodigo Codigo, lpatrocinante_id LPatrocinanteId
        from tmp_residual_contacto;";

    private const string SqlContactosActivosCiclo = @"
        SELECT DISTINCT VP.lcontacto_id LContactoId
        FROM administracionventapersonal VP
        INNER JOIN administracioncontrato AC ON AC.lcontrato_id = VP.lcontrato_id
        WHERE VP.lciclo_id = @LCicloId;";

    private const string SqlEmpresaComplejo = "SELECT * FROM empresa_complejo;";

    public async Task<(bool Success, string Mensaje, List<BrCuotaRed> Cuotas, List<BrContacto> Contactos, List<BrContactoActivos> Activos)>
        GetDatosCalculoResidualAsync(string logId, int lCicloId, string inicio, string fin)
    {
        const string metodo = nameof(GetDatosCalculoResidualAsync);
        try
        {
            using var connection = _context.CreateConnection();
            var cuotas = (await connection.QueryAsync<BrCuotaRed>(SqlCuotasRedCiclo, new { Inicio = inicio, Fin = fin })).ToList();
            var contactos = (await connection.QueryAsync<BrContacto>(SqlContactosTmp)).ToList();
            var activos = (await connection.QueryAsync<BrContactoActivos>(SqlContactosActivosCiclo, new { LCicloId = lCicloId })).ToList();
            return (true, "Datos del cálculo residual obtenidos.", cuotas, contactos, activos);
        }
        catch (Exception ex)
        {
            _log.Error(logId, NOMBRE_ARCHIVO, metodo, $"Error al obtener datos del cálculo residual: {ex.Message}");
            return (false, $"Error al obtener datos del cálculo residual: {ex.Message}", new(), new(), new());
        }
    }

    // Copia de AdministracionBonoResidualRepository.SaveAdministracionBonoResidual (maestro y detalle).
    public async Task<(bool Success, string Mensaje)> InsertBonoResidualAsync(string logId, string usuario, List<ItemAdministracionBonoResidual> data)
    {
        const string metodo = nameof(InsertBonoResidualAsync);
        if (data == null || data.Count == 0)
            return (false, "No existen registros para guardar.");

        const int batchSize = 1000;
        IDbConnection? connection = null;
        IDbTransaction? transaction = null;
        int lCicloId = data.FirstOrDefault()?.LCicloId ?? 0;

        try
        {
            connection = _context.CreateConnection();
            connection.Open();
            transaction = connection.BeginTransaction();

            int nextId = await connection.ExecuteScalarAsync<int>(
                "SELECT IFNULL(MAX(lbonoresidual_id), 0) FROM administracionbonoresidual;", transaction: transaction);

            var complejos = (await connection.QueryAsync<EmpresaComplejoCatalogo>(SqlEmpresaComplejo, transaction: transaction)).ToList();
            var mapaComplejos = complejos
                .Where(c => c.id_almacen_conexion.HasValue)
                .GroupBy(c => c.id_almacen_conexion!.Value)
                .ToDictionary(g => g.Key, g => g.First());

            int totalInsertados = 0;
            int totalDetalles = 0;
            var detallesPendientes = new List<(int BonoResidualId, AdministracionBonoResidualDetalle Detalle)>();

            for (int i = 0; i < data.Count; i += batchSize)
            {
                var lote = data.Skip(i).Take(batchSize).ToList();
                var sql = new StringBuilder(@"
                    INSERT INTO administracionbonoresidual
                    (
                        susuarioadd, dtfechaadd, susuariomod, dtfechamod, lbonoresidual_id, dtfechacalculo,
                        lciclo_id, lcontacto_id, ltipobono, dmontolote, lmora1g, lporcentajemora1g, lterrenos1g,
                        lmisterrenosconmora, ltotalterrenossinmora, scondicion1, scondicion2, scondicion3,
                        dtotalbono, dtotalpagados_licencia, dtotal_bonolicencia, lnrosemana, lsemana_id
                    )
                    VALUES ");
                var parametros = new DynamicParameters();

                for (int j = 0; j < lote.Count; j++)
                {
                    nextId++;
                    var item = lote[j];
                    int bonoResidualId = nextId;

                    sql.Append($@"(
                        @Usuario{j}, NOW(), @Usuario{j}, NOW(), @LBonoResidualId{j}, NOW(),
                        @LCicloId{j}, @LContactoId{j}, @LTipoBono{j}, @DMontoLote{j}, @LMoraG1{j}, @LPorcentajeMoraG1{j}, @LTerrenoG1{j},
                        @LMisTerrenosConMora{j}, @LTotalTerrenosSinMora{j}, NULL, NULL, NULL,
                        @DTotalBono{j}, @DTotalPagadosLicencia{j}, @DTotalBonoLicencia{j}, @LNroSemana{j}, @LSemanaId{j})");
                    if (j < lote.Count - 1)
                        sql.Append(',');

                    parametros.Add($"Usuario{j}", usuario);
                    parametros.Add($"LBonoResidualId{j}", bonoResidualId);
                    parametros.Add($"LCicloId{j}", item.LCicloId);
                    parametros.Add($"LContactoId{j}", item.LContactoId);
                    parametros.Add($"LTipoBono{j}", item.LTipoBono);
                    parametros.Add($"DMontoLote{j}", item.DMontoLote);
                    parametros.Add($"LMoraG1{j}", item.LMoraG1);
                    parametros.Add($"LPorcentajeMoraG1{j}", item.LPorcentajeMoraG1);
                    parametros.Add($"LTerrenoG1{j}", item.LTerrenoG1);
                    parametros.Add($"LMisTerrenosConMora{j}", item.LMisTerrenosConMora);
                    parametros.Add($"LTotalTerrenosSinMora{j}", item.LTotalTerrenosSinMora);
                    parametros.Add($"DTotalBono{j}", item.DTotalBono);
                    parametros.Add($"DTotalPagadosLicencia{j}", item.DTotalPagadosLicencia);
                    parametros.Add($"DTotalBonoLicencia{j}", item.DTotalBonoLicencia);
                    parametros.Add($"LNroSemana{j}", item.LNroSemana);
                    parametros.Add($"LSemanaId{j}", item.LSemanaId);

                    if (item.Detalle?.Count > 0)
                        foreach (var detalle in item.Detalle)
                            detallesPendientes.Add((bonoResidualId, detalle));
                }
                sql.Append(';');
                totalInsertados += await connection.ExecuteAsync(sql.ToString(), parametros, transaction);
            }

            // Reglas de empresa/complejo del INSERT de detalle original (subconsultas CASE).
            var detallesResueltos = detallesPendientes.Select(x =>
            {
                mapaComplejos.TryGetValue(x.Detalle.LcomplejoId, out var complejo);
                int empresa = x.Detalle.LempresaId > 0 ? x.Detalle.LempresaId : (complejo?.empresa_id ?? 0);
                int complejoResuelto = x.Detalle.LcomplejoId > 0 ? (complejo?.complejo_id ?? x.Detalle.LcomplejoId) : 0;
                return (x.BonoResidualId, LEmpresaId: empresa, LComplejoId: complejoResuelto, x.Detalle);
            }).ToList();

            for (int i = 0; i < detallesResueltos.Count; i += batchSize)
            {
                var lote = detallesResueltos.Skip(i).Take(batchSize).ToList();
                var sql = new StringBuilder(@"
                    INSERT INTO administracionbonoresidual_detalle
                    (
                        lbonoresidual_id, lempresa_id, lcomplejos_id, nivel, producto, monto,
                        porcentaje_comision, comision, lciclo_id, fechapago, fecha_registro
                    )
                    VALUES ");
                var parametros = new DynamicParameters();

                for (int j = 0; j < lote.Count; j++)
                {
                    var (bonoResidualId, lEmpresaId, lComplejoId, detalle) = lote[j];
                    sql.Append($@"(
                        @LBonoResidualDetalleId{j}, @LEmpresaId{j}, @LComplejoId{j}, @Nivel{j}, @Producto{j}, @Monto{j},
                        @PorcentajeComision{j}, @Comision{j}, @LCicloId{j}, @FechaPago{j}, NOW())");
                    if (j < lote.Count - 1)
                        sql.Append(',');

                    parametros.Add($"LBonoResidualDetalleId{j}", bonoResidualId);
                    parametros.Add($"LEmpresaId{j}", lEmpresaId);
                    parametros.Add($"LComplejoId{j}", lComplejoId);
                    parametros.Add($"Nivel{j}", detalle.Nivel);
                    parametros.Add($"Producto{j}", detalle.Producto);
                    parametros.Add($"Monto{j}", detalle.Monto);
                    parametros.Add($"PorcentajeComision{j}", detalle.PorcentajeComision);
                    parametros.Add($"Comision{j}", detalle.Comision);
                    parametros.Add($"LCicloId{j}", lCicloId);
                    parametros.Add($"FechaPago{j}", detalle.FechaPago);
                }
                sql.Append(';');
                totalDetalles += await connection.ExecuteAsync(sql.ToString(), parametros, transaction);
            }

            transaction.Commit();
            return (true, $"Bono residual guardado. Maestro: {totalInsertados}, detalle: {totalDetalles}.");
        }
        catch (Exception ex)
        {
            try { if (transaction != null && connection?.State == ConnectionState.Open) transaction.Rollback(); } catch { }
            _log.Error(logId, NOMBRE_ARCHIVO, metodo, $"Error al guardar bono residual: {ex.Message}");
            return (false, $"Error al guardar bono residual: {ex.Message}");
        }
        finally
        {
            transaction?.Dispose();
            connection?.Dispose();
        }
    }

    // Copia de AdministracionBonoResidualRepository.SaveAdministracionBonoCompleto (t_bonocompleto).
    public async Task<(bool Success, string Mensaje)> InsertBonoCompletoAsync(string logId, List<ItemBonoCompleto> data)
    {
        const string metodo = nameof(InsertBonoCompletoAsync);
        if (data == null || data.Count == 0)
            return (false, "No existen registros para guardar.");

        const int batchSize = 1000;
        try
        {
            using var connection = _context.CreateConnection();
            connection.Open();
            using var transaccion = connection.BeginTransaction();

            int nextId = await connection.ExecuteScalarAsync<int>("SELECT IFNULL(MAX(id), 0) FROM t_bonocompleto;", transaction: transaccion);
            int total = 0;

            for (int i = 0; i < data.Count; i += batchSize)
            {
                var lote = data.Skip(i).Take(batchSize).ToList();
                var sql = new StringBuilder(@"
                    INSERT INTO t_bonocompleto
                    (
                        id, lbonocompleto, fecha, generacion, padre_lcontacto_id, lciclo_id, lcontacto_id,
                        cedulaidentidad, proyecto, bono, porcentaje, pagar, cantidad
                    )
                    VALUES ");
                var parametros = new DynamicParameters();

                for (int j = 0; j < lote.Count; j++)
                {
                    nextId++;
                    var item = lote[j];
                    sql.Append($@"(
                        @Id{j}, 0, NOW(), @Nivel{j}, @LContactoId{j}, @LCicloId{j}, @LContactoIdHijo{j}, @DocumentoHijo{j},
                        COALESCE((SELECT complejo_id FROM empresa_complejo WHERE id_almacen_conexion = @LComplejoId{j} LIMIT 1),0),
                        @TotalBono{j}, 1, @TotalPago{j}, @Cantidad{j})");
                    if (j < lote.Count - 1)
                        sql.Append(',');

                    parametros.Add($"Id{j}", nextId);
                    parametros.Add($"Nivel{j}", item.Nivel);
                    parametros.Add($"LContactoId{j}", item.LContactoId);
                    parametros.Add($"LCicloId{j}", item.LCicloId);
                    parametros.Add($"LContactoIdHijo{j}", item.LContactoIdHijo);
                    parametros.Add($"DocumentoHijo{j}", item.DocumentoHijo);
                    parametros.Add($"LComplejoId{j}", item.LComplejoId);
                    parametros.Add($"TotalBono{j}", item.TotalBono);
                    parametros.Add($"TotalPago{j}", item.TotalPago);
                    parametros.Add($"Cantidad{j}", item.Cantidad);
                }
                sql.Append(';');
                total += await connection.ExecuteAsync(sql.ToString(), parametros, transaccion);
            }

            transaccion.Commit();
            return (true, $"Bono completo guardado. Total: {total}.");
        }
        catch (Exception ex)
        {
            _log.Error(logId, NOMBRE_ARCHIVO, metodo, $"Error al guardar bono completo: {ex.Message}");
            return (false, $"Error al guardar bono completo: {ex.Message}");
        }
    }

    // Copia de AdministracionBonoResidualRepository.SaveAdministracionRedEmpresaComplejo.
    public async Task<(bool Success, string Mensaje)> InsertRedEmpresaComplejoAsync(string logId, string usuario, List<ItemRedEmpresaComplejo> data)
    {
        const string metodo = nameof(InsertRedEmpresaComplejoAsync);
        if (data == null || data.Count == 0)
            return (false, "No existen registros para guardar.");

        const int batchSize = 1000;
        try
        {
            using var connection = _context.CreateConnection();
            connection.Open();
            using var transaccion = connection.BeginTransaction();

            int nextId = await connection.ExecuteScalarAsync<int>(
                "SELECT IFNULL(MAX(lredempresacomplejo_id), 0) FROM administracionredempresacomplejo;", transaction: transaccion);
            int total = 0;

            for (int i = 0; i < data.Count; i += batchSize)
            {
                var lote = data.Skip(i).Take(batchSize).ToList();
                var sql = new StringBuilder(@"
                    INSERT INTO administracionredempresacomplejo
                    (
                        susuarioadd, dtfechaadd, susuariomod, dtfechamod, lredempresacomplejo_id,
                        lciclo_id, lcontacto_id, lcomplejo_id, dmonto
                    )
                    VALUES ");
                var parametros = new DynamicParameters();

                for (int j = 0; j < lote.Count; j++)
                {
                    nextId++;
                    var item = lote[j];
                    sql.Append($@"(
                        @Usuario{j}, NOW(), @Usuario{j}, NOW(), @LRedEmpresaComplejoId{j},
                        @LCicloId{j}, @LContactoId{j},
                        COALESCE((SELECT complejo_id FROM empresa_complejo WHERE id_almacen_conexion = @LComplejoId{j} LIMIT 1),0),
                        @DMonto{j})");
                    if (j < lote.Count - 1)
                        sql.Append(',');

                    parametros.Add($"Usuario{j}", usuario);
                    parametros.Add($"LRedEmpresaComplejoId{j}", nextId);
                    parametros.Add($"LCicloId{j}", item.LCicloId);
                    parametros.Add($"LContactoId{j}", item.LContactoId);
                    parametros.Add($"LComplejoId{j}", item.LComplejoId);
                    parametros.Add($"DMonto{j}", item.DMonto);
                }
                sql.Append(';');
                total += await connection.ExecuteAsync(sql.ToString(), parametros, transaccion);
            }

            transaccion.Commit();
            return (true, $"Red empresa-complejo guardada. Total: {total}.");
        }
        catch (Exception ex)
        {
            _log.Error(logId, NOMBRE_ARCHIVO, metodo, $"Error al guardar red empresa-complejo: {ex.Message}");
            return (false, $"Error al guardar red empresa-complejo: {ex.Message}");
        }
    }

    // Catálogo de empresa_complejo (clase del repositorio original, reutilizada solo como tipo de datos).
    public sealed class EmpresaComplejoCatalogo
    {
        public int? id_almacen_conexion { get; set; }
        public int? empresa_id { get; set; }
        public int? complejo_id { get; set; }
    }
}
