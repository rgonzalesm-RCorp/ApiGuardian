using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using Newtonsoft.Json;
using System.Text;

namespace ApiGuardian.Infrastructure.Repositories;

public class RedesRepository : IRedesRepository
{
    private readonly DapperContext _context;
    private readonly ILogService _log;
    private string NOMBREARCHIVO = "ControlProcesoRepository.CS";
    public RedesRepository(DapperContext context, ILogService log)
    {
        _context = context;
        _log = log;
    }
    public async Task<(bool Success, string Mensaje, IEnumerable<ItemContactoActivo> ListadoContactosActivos)> GetObetenerContactoVentasMes(string LogTransaccionId, string Usuario, string Inicio, string Fin)
    {
        string nombreMetodo = "GetObetenerContactoVentasMes()";

        string query = $@" select DISTINCT lasesor_id  LVendedorId from administracioncontrato where dtfecha BETWEEN @Inicio and @Fin ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var Lista = await connection.QueryAsync<ItemContactoActivo>(query, new {Inicio, Fin});

            bool success = true;
            string mensaje = success ? "Tipos de descuento obtenidos correctamente." : "No se encontraron tipos de descuento.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Fin de metodo [mensaje: {mensaje}]");

            return ( success, mensaje, Lista ?? new List<ItemContactoActivo>());
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al obtener los tipos de descuento: {ex.Message}", Enumerable.Empty<ItemContactoActivo>());
        }
    }
    public async Task<(bool Success, string Mensaje)> GuardarRedComprimida(string LogTransaccionId, string Usuario, List<ItemContactoRed> Listado)
    {
        string nombreMetodo = "GuardarRedComprimida()";

        if (Listado == null || Listado.Count == 0)
            return (false, "No existen registros para guardar.");

        const int batchSize = 500;

        try
        {
            using var connection = _context.CreateConnection();

            connection.Open();

            using var transaction = connection.BeginTransaction();

            await connection.ExecuteAsync(
                "TRUNCATE TABLE red_comprimida;",
                transaction: transaction
            );

            int totalInsertados = 0;

            for (int i = 0; i < Listado.Count; i += batchSize)
            {
                var batch = Listado
                    .Skip(i)
                    .Take(batchSize)
                    .ToList();

                var sql = new StringBuilder();

                sql.Append(@"
                    INSERT INTO red_comprimida
                    (
                        RedComprimidaId,
                        lcontrato_id,
                        lciclo_id,
                        lcontacto_id,
                        lasesor_id,
                        Nivel,
                        usuario,
                        fecharegistro
                    )
                    VALUES
                ");

                var parameters = new DynamicParameters();

                for (int j = 0; j < batch.Count; j++)
                {
                    var item = batch[j];

                    sql.Append($@"
                    (
                        0,
                        @LContratoId{j},
                        @LCicloId{j},
                        @LContactoId{j},
                        @LPatrocinadorId{j},
                        @Nivel{j},
                        @Usuario{j},
                        NOW()
                    )");

                    if (j < batch.Count - 1)
                        sql.Append(",");

                    parameters.Add($"LContratoId{j}", item.LContratoId);
                    parameters.Add($"LCicloId{j}", item.LCicloId);
                    parameters.Add($"LContactoId{j}", item.LContactoId);
                    parameters.Add($"LPatrocinadorId{j}", item.LPatrocinadorId);
                    parameters.Add($"Nivel{j}", item.Nivel);
                    parameters.Add($"Usuario{j}", Usuario);
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

            string mensaje = $"Red comprimida guardada correctamente. Total insertado: {totalInsertados}";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Fin de metodo [mensaje:{mensaje}, registros insertados:{totalInsertados}]");

            return (true, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Error al guardar red comprimida", ex);

            return (false, $"Error al guardar red comprimida: {ex.Message}");
        }
    }
    public async Task<(bool Success, string Mensaje, int PatrocinadorId)> GetObetenerPatrocinador(string LogTransaccionId, string Usuario, int LContactoId)
    {
        string nombreMetodo = "GetObetenerPatrocinador()";

        string query = $@" select DISTINCT lpatrocinante_id LContactoId from administracioncontacto where lcontacto_id = @LContactoId";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var PatrocinadorId = await connection.ExecuteScalarAsync<int>(query, new {LContactoId});

            bool success = true;
            string mensaje = success ? "PatrocinadoId obtenidos correctamente." : "No se encontraron el PatrocinadorId.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Fin de metodo [mensaje: {mensaje}]");

            return ( success, mensaje, PatrocinadorId);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al obtener el PatrocinadorId: {ex.Message}", 0);
        }
    }
    
    public async Task<(bool Success, string Mensaje, IEnumerable<ItemCuotasRed> ListadoContactosCuotas, IEnumerable<BrContacto> ListaContacto)> GetObtnerClientesCuotas(string LogTransaccionId, string Usuario)
    {
        string nombreMetodo = "GetObtnerClientesCuotas()";

        string query = $@"SELECT 
                        DISTINCT DOCID DocId,
                        CLIENTE Cliente,
                        ACT.lcontacto_id LContactoId,
                        ACT.scedulaidentidad ScedulaIdentidad,
                        ACT.lpatrocinante_id LPatrocinanteId 
                    FROM cartera C
                    INNER JOIN administracioncontacto ACT on ACT.scedulaidentidad = C.DOCID";
        string queryContacto = @"select 
                                    tmpresidualcontactoId TmpResidualContactoId
                                    , lcontacto_id LContactoId
                                    , scedulaidentidad SCedulaIdentidad
                                    , snombrecompleto SNombreCompleto
                                    , scodigo Codigo 
                                    , lpatrocinante_id LPatrocinanteId
                                from tmp_residual_contacto";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var Lista = await connection.QueryAsync<ItemCuotasRed>(query);
            var ListaContacto = await connection.QueryAsync<BrContacto>(queryContacto);

            bool success = true;
            string mensaje = success ? "Clientes obtenidos correctamente." : "No se encontraron clientes.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Fin de metodo [mensaje: {mensaje}]");

            return ( success, mensaje, Lista ?? new List<ItemCuotasRed>(), ListaContacto ?? new List<BrContacto>());
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al obtener los clientes: {ex.Message}", Enumerable.Empty<ItemCuotasRed>(), Enumerable.Empty<BrContacto>());
        }
    }
    
    public async Task<(bool Success, string Mensaje, List<RedContacto> ListadoContactosCuotas)> GetRedCotactoAll(string LogTransaccionId, string Usuario)
    {
        string nombreMetodo = "GetRedCotactoAll()";

        string query = $@"select lcontacto_id Hijo, lpatrocinante_id Padre from administracioncontacto ";
        string queryContacto = @"TRUNCATE TABLE tmp_residual_contacto;
                                            insert into tmp_residual_contacto
                                            select 0, lcontacto_id Hijo, scedulaidentidad, snombrecompleto, scodigo, lpatrocinante_id Padre
                                            from administracioncontacto ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();

            var Lista = await connection.QueryAsync<RedContacto>(query);
            await connection.ExecuteAsync(queryContacto);

            bool success = true;
            string mensaje = success ? "Registros obtenidos correctamente." : "No se encontraron registros.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Fin de metodo [mensaje: {mensaje}]");

            return ( success, mensaje, Lista.ToList());
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al obtener los tipos de descuento: {ex.Message}", new List<RedContacto>());
        }
    }
    public async Task<(bool Success, string Mensaje)> GuardarRedContactoTemporal(string logTransaccionId, string usuario, List<ItemRedSieteNiveles> listado)
    {
        string nombreMetodo = "GuardarRedContactoTemporal";

        if (listado == null || listado.Count == 0)
            return (false, "No existen registros para guardar.");

        const int batchSize = 1000;

        const string queryClear = @"truncate table tmp_residual_red;";

        try
        {
            using var connection = _context.CreateConnection();

            connection.Open();

            using var transaction = connection.BeginTransaction();

            int nextId = 0 ; 
            await connection.ExecuteScalarAsync(queryClear,transaction: transaction);

            int totalInsertados = 0;

            for (int i = 0; i < listado.Count; i += batchSize)
            {
                var batch = listado.Skip(i).Take(batchSize).ToList();

                var sql = new StringBuilder();

                sql.Append(@"
                    INSERT INTO tmp_residual_red
                    (tmpresidualredId, lcontacto_id, lpatrocinador1g, lpatrocinador2g, lpatrocinador3g, lpatrocinador4g, lpatrocinador5g, lpatrocinador6g, lpatrocinador7g)
                    VALUES
                ");

                var parameters = new DynamicParameters();

                for (int j = 0; j < batch.Count; j++)
                {
                    nextId++;

                    var item = batch[j];

                    sql.Append($@"(@Id{j}, @Hijo{j}, @PadreN1{j}, @PadreN2{j}, @PadreN3{j}, @PadreN4{j}, @PadreN5{j}, @PadreN6{j}, @PadreN7{j})");

                    if (j < batch.Count - 1)
                        sql.Append(",");

                    parameters.Add($"Id{j}", nextId);
                    parameters.Add($"Hijo{j}", item.Hijo);
                    parameters.Add($"PadreN1{j}", item.PadreN1);
                    parameters.Add($"PadreN2{j}", item.PadreN2);
                    parameters.Add($"PadreN3{j}", item.PadreN3);
                    parameters.Add($"PadreN4{j}", item.PadreN4);
                    parameters.Add($"PadreN5{j}", item.PadreN5);
                    parameters.Add($"PadreN6{j}", item.PadreN6);
                    parameters.Add($"PadreN7{j}", item.PadreN7);
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

            string mensaje = $"Registros insertados correctamente. Total: {totalInsertados}";

            _log.Info(logTransaccionId, NOMBREARCHIVO, nombreMetodo, mensaje);

            return (true, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(logTransaccionId, NOMBREARCHIVO, nombreMetodo, "Error al guardar red temporal", ex);

            return (false, $"Error al insertar la red: {ex.Message}");
        }
    }

}
