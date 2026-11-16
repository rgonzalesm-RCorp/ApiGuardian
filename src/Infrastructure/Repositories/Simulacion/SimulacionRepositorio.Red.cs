using System.Text;
using ApiGuardian.Domain.Entities;
using Dapper;

namespace ApiGuardian.Infrastructure.Repositories.Simulacion;

// F4-T1 a F4-T3: lecturas y escrituras de Red. SQL copiado de RedesRepository (R2).
// Diferencias: red_comprimida se borra por lciclo_id (R11) en vez de TRUNCATE.
// Tablas temporales (tmp_residual_contacto, tmp_residual_red) no tienen clave de ciclo: TRUNCATE (D-04).
public sealed partial class SimulacionRepositorio
{
    private const string SqlContactosVentasMes = @"SELECT DISTINCT lasesor_id LVendedorId FROM administracioncontrato WHERE dtfecha BETWEEN @Inicio AND @Fin;";

    private const string SqlRedContactos = @"SELECT lcontacto_id Hijo, lpatrocinante_id Padre FROM administracioncontacto;";

    private const string SqlTruncateTmpResidualContacto = "TRUNCATE TABLE tmp_residual_contacto;";

    private const string SqlInsertTmpResidualContacto = @"INSERT INTO tmp_residual_contacto
        SELECT 0, lcontacto_id Hijo, scedulaidentidad, snombrecompleto, scodigo, lpatrocinante_id Padre
        FROM administracioncontacto;";

    private const string SqlDeleteRedComprimida = "DELETE FROM red_comprimida WHERE lciclo_id = @LCicloId;";

    private const string SqlTruncateTmpResidualRed = "TRUNCATE TABLE tmp_residual_red;";

    public async Task<(bool Success, string Mensaje, List<ItemContactoActivo> Lista)> GetContactosVentasMesAsync(string logId, string inicio, string fin)
    {
        const string metodo = nameof(GetContactosVentasMesAsync);
        try
        {
            using var connection = _context.CreateConnection();
            var lista = (await connection.QueryAsync<ItemContactoActivo>(SqlContactosVentasMes, new { Inicio = inicio, Fin = fin })).ToList();
            return (true, "Contactos con venta en el mes obtenidos.", lista);
        }
        catch (Exception ex)
        {
            _log.Error(logId, NOMBRE_ARCHIVO, metodo, $"Error al obtener contactos con venta: {ex.Message}");
            return (false, $"Error al obtener contactos con venta: {ex.Message}", new List<ItemContactoActivo>());
        }
    }

    // Copia de RedesRepository.GetRedCotactoAll: lee la red y recarga tmp_residual_contacto.
    public async Task<(bool Success, string Mensaje, List<RedContacto> Lista)> GetRedContactosAsync(string logId)
    {
        const string metodo = nameof(GetRedContactosAsync);
        try
        {
            using var connection = _context.CreateConnection();
            var lista = (await connection.QueryAsync<RedContacto>(SqlRedContactos)).ToList();
            await connection.ExecuteAsync(SqlTruncateTmpResidualContacto);
            await connection.ExecuteAsync(SqlInsertTmpResidualContacto);
            return (true, "Red de contactos obtenida.", lista);
        }
        catch (Exception ex)
        {
            _log.Error(logId, NOMBRE_ARCHIVO, metodo, $"Error al obtener la red de contactos: {ex.Message}");
            return (false, $"Error al obtener la red de contactos: {ex.Message}", new List<RedContacto>());
        }
    }

    // Red comprimida (RedesRepository.GuardarRedComprimida): borra por ciclo y luego inserta por lotes.
    public async Task<(bool Success, string Mensaje)> ReemplazarRedComprimidaAsync(string logId, string usuario, int lCicloId, List<ItemContactoRed> listado)
    {
        const string metodo = nameof(ReemplazarRedComprimidaAsync);
        const int batchSize = 500;
        try
        {
            using var connection = _context.CreateConnection();
            connection.Open();
            using var transaccion = connection.BeginTransaction();

            await connection.ExecuteAsync(SqlDeleteRedComprimida, new { LCicloId = lCicloId }, transaccion);

            int total = 0;
            for (int i = 0; i < listado.Count; i += batchSize)
            {
                var lote = listado.Skip(i).Take(batchSize).ToList();
                var sql = new StringBuilder(@"
                    INSERT INTO red_comprimida
                    (RedComprimidaId, lcontrato_id, lciclo_id, lcontacto_id, lasesor_id, Nivel, usuario, fecharegistro)
                    VALUES ");
                var parametros = new DynamicParameters();

                for (int j = 0; j < lote.Count; j++)
                {
                    var item = lote[j];
                    sql.Append($"(0, @LContratoId{j}, @LCicloId{j}, @LContactoId{j}, @LPatrocinadorId{j}, @Nivel{j}, @Usuario{j}, NOW())");
                    if (j < lote.Count - 1)
                        sql.Append(',');

                    parametros.Add($"LContratoId{j}", item.LContratoId);
                    parametros.Add($"LCicloId{j}", item.LCicloId);
                    parametros.Add($"LContactoId{j}", item.LContactoId);
                    parametros.Add($"LPatrocinadorId{j}", item.LPatrocinadorId);
                    parametros.Add($"Nivel{j}", item.Nivel);
                    parametros.Add($"Usuario{j}", usuario);
                }
                sql.Append(';');

                total += await connection.ExecuteAsync(sql.ToString(), parametros, transaccion);
            }

            transaccion.Commit();
            return (true, $"Red comprimida guardada. Total insertado: {total}");
        }
        catch (Exception ex)
        {
            _log.Error(logId, NOMBRE_ARCHIVO, metodo, $"Error al guardar red comprimida: {ex.Message}");
            return (false, $"Error al guardar red comprimida: {ex.Message}");
        }
    }

    // Red completa (RedesRepository.GuardarRedContactoTemporal): TRUNCATE de tmp_residual_red y carga por lotes.
    public async Task<(bool Success, string Mensaje)> ReemplazarRedContactoTemporalAsync(string logId, List<ItemRedSieteNiveles> listado)
    {
        const string metodo = nameof(ReemplazarRedContactoTemporalAsync);
        const int batchSize = 1000;
        try
        {
            using var connection = _context.CreateConnection();
            connection.Open();
            using var transaccion = connection.BeginTransaction();

            await connection.ExecuteAsync(SqlTruncateTmpResidualRed, transaction: transaccion);

            int siguiente = 0;
            int total = 0;
            for (int i = 0; i < listado.Count; i += batchSize)
            {
                var lote = listado.Skip(i).Take(batchSize).ToList();
                var sql = new StringBuilder(@"
                    INSERT INTO tmp_residual_red
                    (tmpresidualredId, lcontacto_id, lpatrocinador1g, lpatrocinador2g, lpatrocinador3g, lpatrocinador4g, lpatrocinador5g, lpatrocinador6g, lpatrocinador7g, fecha_registro)
                    VALUES ");
                var parametros = new DynamicParameters();

                for (int j = 0; j < lote.Count; j++)
                {
                    siguiente++;
                    var item = lote[j];
                    sql.Append($"(@Id{j}, @Hijo{j}, @PadreN1{j}, @PadreN2{j}, @PadreN3{j}, @PadreN4{j}, @PadreN5{j}, @PadreN6{j}, @PadreN7{j}, NOW())");
                    if (j < lote.Count - 1)
                        sql.Append(',');

                    parametros.Add($"Id{j}", siguiente);
                    parametros.Add($"Hijo{j}", item.Hijo);
                    parametros.Add($"PadreN1{j}", item.PadreN1);
                    parametros.Add($"PadreN2{j}", item.PadreN2);
                    parametros.Add($"PadreN3{j}", item.PadreN3);
                    parametros.Add($"PadreN4{j}", item.PadreN4);
                    parametros.Add($"PadreN5{j}", item.PadreN5);
                    parametros.Add($"PadreN6{j}", item.PadreN6);
                    parametros.Add($"PadreN7{j}", item.PadreN7);
                }
                sql.Append(';');

                total += await connection.ExecuteAsync(sql.ToString(), parametros, transaccion);
            }

            transaccion.Commit();
            return (true, $"Red completa guardada en tmp_residual_red. Total: {total}");
        }
        catch (Exception ex)
        {
            _log.Error(logId, NOMBRE_ARCHIVO, metodo, $"Error al guardar red completa: {ex.Message}");
            return (false, $"Error al guardar red completa: {ex.Message}");
        }
    }
}
