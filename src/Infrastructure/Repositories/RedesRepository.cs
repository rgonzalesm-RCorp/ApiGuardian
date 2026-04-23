using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using Newtonsoft.Json;

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

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}]");

            return ( success, mensaje, Lista ?? new List<ItemContactoActivo>());
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al obtener los tipos de descuento: {ex.Message}", Enumerable.Empty<ItemContactoActivo>());
        }
    }
    public async Task<(bool Success, string Mensaje)> GuardarRedComprimida(string LogTransaccionId, string Usuario,  List<ItemContactoRed> Listado)
    {
        string nombreMetodo = "GuardarRedComprimida()";

        string query = $@"insert into red_comprimida 
                        (RedComprimidaId,lcontrato_id, lciclo_id, lcontacto_id, lasesor_id, Nivel, usuario, fecharegistro)
                        VALUES
                        (0, @LContratoId, @LCicloId, @LContactoId, @LPatrocinadorId, @Nivel, @Usuario,  NOW()) ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}, Usuario: {Usuario}]");

        try
        {
            using var connection = _context.CreateConnection();

            var response = await connection.ExecuteAsync(query, Listado);

            bool success = true;
            string mensaje = success ? "Red comprimida guardado correctamente." : "No see pudo guardar la red comprimida.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Fin de metodo [mensaje: {mensaje}, registro insertado: {response}]");

            return ( success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al obtener los tipos de descuento: {ex.Message}");
        }
    }
    public async Task<(bool Success, string Mensaje)> GuardarRedCompletaCuotas(string LogTransaccionId, string Usuario,  List<ItemContactoRed> Listado)
    {
        string nombreMetodo = "GuardarRedCompletaCuotas()";

        string query = $@"insert into red_completa_cuotas 
                        (RedComprimidaId,lcontrato_id, lciclo_id, lcontacto_id, lasesor_id, Nivel, usuario, fecharegistro)
                        VALUES
                        (0, @LContratoId, @LCicloId, @LContactoId, @LPatrocinadorId, @Nivel, @Usuario,  NOW()) ";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}, Usuario: {Usuario}]");

        try
        {
            using var connection = _context.CreateConnection();

            var response = await connection.ExecuteAsync(query, Listado);

            bool success = true;
            string mensaje = success ? "Red comprimida guardado correctamente." : "No see pudo guardar la red comprimida.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Fin de metodo [mensaje: {mensaje}, registro insertado: {response}]");

            return ( success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al obtener los tipos de descuento: {ex.Message}");
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

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}]");

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

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}]");

            return ( success, mensaje, Lista ?? new List<ItemCuotasRed>(), ListaContacto ?? new List<BrContacto>());
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al obtener los clientes: {ex.Message}", Enumerable.Empty<ItemCuotasRed>(), Enumerable.Empty<BrContacto>());
        }
    }
    /*
    SELECT DISTINCT DOCID, CLIENTE, ACT.lcontacto_id, ACT.scedulaidentidad, ACT.lpatrocinante_id FROM cartera C
inner JOIN administracioncontacto ACT on ACT.scedulaidentidad = C.DOCID
    */
}
