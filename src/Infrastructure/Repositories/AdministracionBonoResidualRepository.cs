using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using Newtonsoft.Json;

namespace ApiGuardian.Infrastructure.Repositories;

public class AdministracionBonoResidualRepository : IAdministracionBonoResidualRepository
{
    private readonly DapperContext _context;
    private readonly ILogService _log;
    private string NOMBREARCHIVO = "AdministracionVentaPersonalRepository.cs";
    public AdministracionBonoResidualRepository(DapperContext context, ILogService log)
    {
        _context = context;
        _log = log;
    }

    public async Task<( bool Success, string Mensaje)> SaveAdministracionBonoResidual(string LogTransaccionId, string Usuario, List<ItemAdministracionBonoResidual> data)
    {
        string nombreMetodo = "SaveAdministracionBonoResidual()";
        const string nextIdQuery = @"SELECT IFNULL(MAX(lbonoresidual_id), 0) FROM administracionbonoresidual;";

        const string query = @"INSERT INTO administracionbonoresidual (
                                    susuarioadd, dtfechaadd, susuariomod, dtfechamod,
                                    lbonoresidual_id, dtfechacalculo, lciclo_id, lcontacto_id,
                                    ltipobono, dmontolote, lmora1g, lporcentajemora1g,
                                    lterrenos1g, lmisterrenosconmora, ltotalterrenossinmora, scondicion1,
                                    scondicion2, scondicion3, dtotalbono, dtotalpagados_licencia,
                                    dtotal_bonolicencia, lnrosemana, lsemana_id
                                )VALUES (
                                    @Usuario, NOW(), @Usuario, NOW()
                                    , @LBonoResidualId, NOW(), @LCicloId, @LContactoId
                                    , @LTipoBono, @DMontoLote, @LMoraG1, @LPorcentajeMoraG1
                                    , @LTerrenoG1, @LMisTerrenosConMora, @LTotalTerrenosSinMora, NULL
                                    , NULL, NULL, @DTotalBono, @DTotalPagadosLicencia
                                    , @DTotalBonoLicencia, @LNroSemana, @LSemanaId
                                )";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();
            var nextId = await connection.ExecuteScalarAsync<int>(nextIdQuery);

            foreach (var item in data)
            {
                nextId++;
                item.LBonoResidualId = nextId;
            }

            var rows = await connection.ExecuteAsync(query, data);

            bool success = rows > 0;
            string mensaje = success ? "Registrados guardados correctamente." : "No se realizó el guardado.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, rowsAffected:{rows}]");

            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al insertar las comisiones: {ex.Message}");
        }
    }
    public async Task<( bool Success, string Mensaje)> SaveAdministracionBonoCompleto(string LogTransaccionId, string Usuario, List<ItemBonoCompleto> data)
    {
        string nombreMetodo = "SaveAdministracionBonoCompleto()";
        const string nextIdQuery = @"SELECT IFNULL(MAX(id), 0) FROM t_bonocompleto;";

        const string query = @"INSERT INTO t_bonocompleto (
                                    id, lbonocompleto, fecha, generacion, padre_lcontacto_id, lciclo_id,
                                    lcontacto_id, cedulaidentidad, proyecto, bono, porcentaje, pagar, cantidad
                                )VALUES (
                                    @Id, 0, now(), @Nivel, @LContactoId, @LCicloId,
                                    @LContactoIdHijo, @DocumentoHijo, @LComplejoId, @TotalBono, 1, @TotalPago, @Cantidad
                                )";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();
            var nextId = await connection.ExecuteScalarAsync<int>(nextIdQuery);

            foreach (var item in data)
            {
                nextId++;
                item.Id = nextId;
            }

            var rows = await connection.ExecuteAsync(query, data);

            bool success = rows > 0;
            string mensaje = success ? "Registrados guardados correctamente." : "No se realizó el guardado.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, rowsAffected:{rows}]");

            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al insertar las comisiones: {ex.Message}");
        }
    }
    public async Task<( bool Success, string Mensaje)> SaveAdministracionRedEmpresaComplejo(string LogTransaccionId, string Usuario, List<ItemRedEmpresaComplejo> data)
    {
        string nombreMetodo = "SaveAdministracionRedEmpresaComplejo()";
        const string nextIdQuery = @"SELECT IFNULL(MAX(lredempresacomplejo_id), 0) FROM administracionredempresacomplejo;";

        const string query = @"INSERT INTO administracionredempresacomplejo (
                                    susuarioadd, dtfechaadd, susuariomod, dtfechamod,
                                    lredempresacomplejo_id, lciclo_id, lcontacto_id, lcomplejo_id, dmonto
                                )VALUES (
                                    @Usuario, now(), @Usuario, now(), 
                                    @LRedEmpresaComplejoId, @LCicloId, @LContactoId, @LComplejoId, @DMonto
                                )";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [script: {query}]");

        try
        {
            using var connection = _context.CreateConnection();
            var nextId = await connection.ExecuteScalarAsync<int>(nextIdQuery);

            foreach (var item in data)
            {
                nextId++;
                item.LRedEmpresaComplejoId = nextId;
                item.Usuario = Usuario;
            }

            var rows = await connection.ExecuteAsync(query, data);

            bool success = rows > 0;
            string mensaje = success ? "Registrados guardados correctamente." : "No se realizó el guardado.";

            _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo,
                $"Fin de metodo [mensaje: {mensaje}, rowsAffected:{rows}]");

            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al insertar las comisiones: {ex.Message}");
        }
    }
}
