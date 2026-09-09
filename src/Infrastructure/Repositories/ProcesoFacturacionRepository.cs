using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Infrastructure.Persistence;
using Dapper;

namespace ApiGuardian.Infrastructure.Repositories;

public sealed class ProcesoFacturacionRepository : IProcesoFacturacionRepository
{
    private readonly DapperContext _context;

    public ProcesoFacturacionRepository(DapperContext context) => _context = context;

    public async Task<(bool Success, string Mensaje, ResultadoGuardarAsesoresFacturacion Data)> GuardarAsesoresAsync(
        string logTransaccionId,
        SolicitudGuardarAsesoresFacturacion solicitud
    )
    {
        var resultado = new ResultadoGuardarAsesoresFacturacion();
        var contactos = solicitud.ContactosIds.Where(id => id > 0).Distinct().ToList();
        if (contactos.Count == 0)
            return (false, "Debe seleccionar al menos un asesor.", resultado);

        using var connection = _context.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            const string obtenerPrimeraSemana = @"
                SELECT lsemana_id
                FROM administracionsemanaciclo
                WHERE lciclo_id = @LCicloId
                ORDER BY lnrosemana, lsemana_id
                LIMIT 1;";
            var semanaId = await connection.QueryFirstOrDefaultAsync<int>(
                obtenerPrimeraSemana,
                new { solicitud.LCicloId },
                transaction
            );
            if (semanaId <= 0)
                return (false, "El ciclo seleccionado no tiene semanas configuradas.", resultado);

            const string contactoValido = @"
                SELECT COUNT(*)
                FROM administracioncontacto
                WHERE lcontacto_id = @ContactoId AND cbaja = 0;";
            const string existe = @"
                SELECT COUNT(*)
                FROM administracionciclopresentafactura
                WHERE lciclo_id = @LCicloId AND lcontacto_id = @ContactoId AND lsemana_id = @LSemanaId;";
            const string siguienteId = @"
                SELECT IFNULL(MAX(lciclopresentafactura_id), 0) + 1
                FROM administracionciclopresentafactura;";
            const string insertar = @"
                INSERT INTO administracionciclopresentafactura
                    (susuarioadd, dtfechaadd, susuariomod, dtfechamod, lciclopresentafactura_id, lciclo_id, lcontacto_id, lsemana_id)
                VALUES (@Usuario, NOW(), @Usuario, NOW(), @Id, @LCicloId, @ContactoId, @LSemanaId);";

            foreach (var contactoId in contactos)
            {
                var parametros = new { solicitud.LCicloId, LSemanaId = semanaId, ContactoId = contactoId };
                if (await connection.ExecuteScalarAsync<int>(contactoValido, parametros, transaction) == 0)
                {
                    resultado.Errores.Add($"El asesor {contactoId} no existe o está dado de baja.");
                    continue;
                }
                if (await connection.ExecuteScalarAsync<int>(existe, parametros, transaction) > 0)
                {
                    resultado.Omitidos++;
                    continue;
                }

                var id = await connection.ExecuteScalarAsync<int>(siguienteId, transaction: transaction);
                await connection.ExecuteAsync(insertar, new
                {
                    solicitud.Usuario,
                    solicitud.LCicloId,
                    LSemanaId = semanaId,
                    ContactoId = contactoId,
                    Id = id,
                }, transaction);
                resultado.Insertados++;
            }

            if (resultado.Errores.Count > 0)
            {
                transaction.Rollback();
                return (false, "No se guardó la selección porque existen asesores inválidos.", resultado);
            }

            transaction.Commit();
            return (true, $"Asesores facturadores guardados: {resultado.Insertados}. Omitidos por duplicado: {resultado.Omitidos}.", resultado);
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            resultado.Errores.Add(ex.Message);
            return (false, "No se pudieron guardar los asesores facturadores.", resultado);
        }
    }
}
