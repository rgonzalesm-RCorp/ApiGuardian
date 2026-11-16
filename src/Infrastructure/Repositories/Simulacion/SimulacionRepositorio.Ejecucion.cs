using Dapper;

namespace ApiGuardian.Infrastructure.Repositories.Simulacion;

// F1-T6: candado por ciclo con la tabla comisiones_light_ejecucion (D-21, D-25).
// La revisión y el registro se hacen en una transaccion. El SELECT ... FOR UPDATE bloquea
// las filas EN_CURSO del ciclo, para que dos corridas simultaneas no pasen la revision a la vez.
public sealed partial class SimulacionRepositorio
{
    private const string TablaEjecucion = "comisiones_light_ejecucion";

    // Una EN_CURSO vieja se cierra como ERROR (corrida caida). El limite viene de configuracion (entero).
    private static string SqlCerrarAbandonadas(int horas) => $@"
        UPDATE {TablaEjecucion}
        SET estado = 'ERROR',
            fecha_fin = NOW(),
            mensaje_error = 'Corrida abandonada: sin finalizar despues de {horas} horas.'
        WHERE lciclo_id = @LCicloId
          AND estado = 'EN_CURSO'
          AND fecha_inicio < NOW() - INTERVAL {horas} HOUR;";

    private static readonly string SqlBuscarEnCurso = $@"
        SELECT id FROM {TablaEjecucion}
        WHERE lciclo_id = @LCicloId AND estado = 'EN_CURSO'
        LIMIT 1
        FOR UPDATE;";

    private static readonly string SqlInsertarOmitida = $@"
        INSERT INTO {TablaEjecucion} (lciclo_id, fecha_inicio, fecha_fin, estado, usuario)
        VALUES (@LCicloId, NOW(), NOW(), 'OMITIDA', @Usuario);";

    private static readonly string SqlInsertarEnCurso = $@"
        INSERT INTO {TablaEjecucion} (lciclo_id, fecha_inicio, fecha_fin, estado, usuario)
        VALUES (@LCicloId, NOW(), NULL, 'EN_CURSO', @Usuario);";

    private static readonly string SqlUltimoId = "SELECT LAST_INSERT_ID();";

    private static readonly string SqlTerminar = $@"
        UPDATE {TablaEjecucion}
        SET estado = @Estado,
            fecha_fin = NOW(),
            paso_fallido = @PasoFallido,
            mensaje_error = @MensajeError
        WHERE id = @Id;";

    public async Task<(bool Iniciada, long Id, string Mensaje)> IniciarCorridaAsync(string logId, int lCicloId, string usuario, int horasMaximasEnCurso)
    {
        const string metodo = nameof(IniciarCorridaAsync);

        if (lCicloId <= 0 || horasMaximasEnCurso <= 0)
            return (false, 0, "Parámetros de corrida no válidos.");

        using var connection = _context.CreateConnection();
        connection.Open();
        using var transaccion = connection.BeginTransaction();

        try
        {
            var parametros = new { LCicloId = lCicloId, Usuario = usuario };

            await connection.ExecuteAsync(SqlCerrarAbandonadas(horasMaximasEnCurso), parametros, transaccion);

            var enCurso = await connection.QueryFirstOrDefaultAsync<long?>(SqlBuscarEnCurso, parametros, transaccion);
            if (enCurso.HasValue)
            {
                await connection.ExecuteAsync(SqlInsertarOmitida, parametros, transaccion);
                transaccion.Commit();
                _log.Warning(logId, NOMBRE_ARCHIVO, metodo, $"Corrida omitida: ciclo {lCicloId} en curso (id {enCurso.Value}).");
                return (false, enCurso.Value, $"Corrida omitida: ya hay una corrida en curso para el ciclo {lCicloId}.");
            }

            await connection.ExecuteAsync(SqlInsertarEnCurso, parametros, transaccion);
            var id = await connection.ExecuteScalarAsync<long>(SqlUltimoId, transaction: transaccion);
            transaccion.Commit();

            _log.Info(logId, NOMBRE_ARCHIVO, metodo, $"Corrida iniciada: ciclo {lCicloId}, id {id}.");
            return (true, id, "Corrida iniciada.");
        }
        catch (Exception ex)
        {
            transaccion.Rollback();
            _log.Error(logId, NOMBRE_ARCHIVO, metodo, $"Error al iniciar la corrida del ciclo {lCicloId}: {ex.Message}");
            return (false, 0, $"Error al iniciar la corrida: {ex.Message}");
        }
    }

    public async Task<bool> TerminarCorridaAsync(string logId, long id, string estado, string? pasoFallido, string? mensajeError)
    {
        const string metodo = nameof(TerminarCorridaAsync);

        if (estado != "OK" && estado != "ERROR")
            return false;

        try
        {
            using var connection = _context.CreateConnection();
            var filas = await connection.ExecuteAsync(SqlTerminar, new
            {
                Id = id,
                Estado = estado,
                PasoFallido = pasoFallido,
                MensajeError = mensajeError
            });
            _log.Info(logId, NOMBRE_ARCHIVO, metodo, $"Corrida {id} terminada con estado {estado}.");
            return filas > 0;
        }
        catch (Exception ex)
        {
            _log.Error(logId, NOMBRE_ARCHIVO, metodo, $"Error al terminar la corrida {id}: {ex.Message}");
            return false;
        }
    }
}
