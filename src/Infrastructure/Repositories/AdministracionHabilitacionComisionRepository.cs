using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using Newtonsoft.Json;

namespace ApiGuardian.Infrastructure.Repositories;

public class AdministracionHabilitacionComisionRepository : IAdministracionHabilitacionComisionRepository
{
    private readonly DapperContext _context;
    private readonly ILogService _log;
    private readonly string NOMBREARCHIVO = "AdministracionHabilitacionComisionRepository.cs";

    public AdministracionHabilitacionComisionRepository(DapperContext context, ILogService log)
    {
        _context = context;
        _log = log;
    }

    public async Task<(IEnumerable<ItemHabilitacionComision> Data, bool Success, string Mensaje)> GetHabilitaciones(
        string LogTransaccionId,
        string Usuario,
        int LCicloId
    )
    {
        string nombreMetodo = "GetHabilitaciones()";

        const string query = @"
            SELECT
                AHC.lhabilitacion_id LHabilitacionId,
                AHC.lcontacto_id LContactoId,
                AHC.lciclo_id LCicloId,
                AHC.monto_venta MontoVenta,
                IFNULL(AHC.observacion, '') Observacion,
                IFNULL(AHC.genera_comisiones, 1) GeneraComisiones,
                AHC.estado Estado,
                AHC.usuario_creacion UsuarioCreacion,
                AHC.fecha_creacion FechaCreacion,
                IFNULL(AHC.usuario_modificacion, '') UsuarioModificacion,
                AHC.fecha_modificacion FechaModificacion,
                IFNULL(AC.snombrecompleto, '') NombreAsesor,
                IFNULL(AC.scedulaidentidad, '') DocumentoAsesor
            FROM administracionhabilitacioncomision AHC
            LEFT JOIN administracioncontacto AC ON AC.lcontacto_id = AHC.lcontacto_id
            WHERE AHC.lciclo_id = @LCicloId
              AND AHC.estado = 1
            ORDER BY AHC.lhabilitacion_id DESC;
        ";

        _log.Info(
            LogTransaccionId,
            NOMBREARCHIVO,
            nombreMetodo,
            $"Inicio de metodo [Usuario:{Usuario}, LCicloId:{LCicloId}, script:{query}]"
        );

        try
        {
            using var connection = _context.CreateConnection();

            var listado = (await connection.QueryAsync<ItemHabilitacionComision>(query, new { LCicloId })).ToList();

            string mensaje = listado.Count > 0
                ? "Habilitaciones obtenidas correctamente."
                : "No se encontraron habilitaciones registradas para el ciclo.";

            _log.Info(
                LogTransaccionId,
                NOMBREARCHIVO,
                nombreMetodo,
                $"Fin de metodo [mensaje:{mensaje}, total:{listado.Count}]"
            );

            return (listado, true, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (Enumerable.Empty<ItemHabilitacionComision>(), false, $"Error al obtener habilitaciones: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Mensaje)> SaveHabilitaciones(
        string LogTransaccionId,
        string Usuario,
        int LCicloId,
        List<ItemHabilitacionComision> Listado
    )
    {
        string nombreMetodo = "SaveHabilitaciones()";

        if (Listado == null)
        {
            return (false, "Debe enviar una lista válida de habilitaciones.");
        }

        if (LCicloId <= 0)
        {
            return (false, "El ciclo enviado no es válido.");
        }

        if (Listado.Any(item => item.LContactoId <= 0))
        {
            return (false, "Todas las habilitaciones deben tener un asesor válido.");
        }

        if (Listado.Any(item => item.MontoVenta <= 0))
        {
            return (false, "Todas las habilitaciones deben tener un monto de venta mayor a 0.");
        }

        if (Listado
            .GroupBy(item => item.LContactoId)
            .Any(group => group.Key > 0 && group.Count() > 1))
        {
            return (false, "No puede registrar la misma persona más de una vez en el mismo ciclo.");
        }

        const string deleteQuery = @"
            DELETE FROM administracionhabilitacioncomision
            WHERE lciclo_id = @LCicloId;
        ";

        const string nextIdQuery = @"
            SELECT IFNULL(MAX(lhabilitacion_id), 0)
            FROM administracionhabilitacioncomision;
        ";

        const string insertQuery = @"
            INSERT INTO administracionhabilitacioncomision
            (
                lhabilitacion_id,
                lcontacto_id,
                lciclo_id,
                monto_venta,
                observacion,
                genera_comisiones,
                estado,
                usuario_creacion,
                fecha_creacion,
                usuario_modificacion,
                fecha_modificacion
            )
            VALUES
            (
                @LHabilitacionId,
                @LContactoId,
                @LCicloId,
                @MontoVenta,
                @Observacion,
                @GeneraComisiones,
                @Estado,
                @UsuarioCreacion,
                @FechaCreacion,
                @UsuarioModificacion,
                @FechaModificacion
            );
        ";

        _log.Info(
            LogTransaccionId,
            NOMBREARCHIVO,
            nombreMetodo,
            $"Inicio de metodo [Usuario:{Usuario}, LCicloId:{LCicloId}, total:{Listado.Count}, script:{insertQuery}]"
        );

        try
        {
            using var connection = _context.CreateConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();

            await connection.ExecuteAsync(deleteQuery, new { LCicloId }, transaction);

            if (Listado.Count == 0)
            {
                transaction.Commit();
                return (true, "Habilitaciones actualizadas correctamente.");
            }

            int nextId = await connection.ExecuteScalarAsync<int>(nextIdQuery, transaction: transaction);
            DateTime fechaActual = DateTime.Now;

            foreach (var item in Listado)
            {
                nextId++;
                item.LHabilitacionId = nextId;
                item.LCicloId = LCicloId;
                item.Estado = 1;
                item.Observacion = item.Observacion?.Trim() ?? string.Empty;
                item.GeneraComisiones = item.GeneraComisiones;
                item.UsuarioCreacion = Usuario;
                item.FechaCreacion = fechaActual;
                item.UsuarioModificacion = Usuario;
                item.FechaModificacion = fechaActual;
            }

            int rowsAffected = await connection.ExecuteAsync(insertQuery, Listado, transaction);

            if (rowsAffected <= 0)
            {
                transaction.Rollback();
                return (false, "No se guardó ninguna habilitación.");
            }

            var contactosBloqueados = HabilitacionComisionHelper
                .GetContactosBloqueadosParaComision(Listado)
                .ToList();

            if (contactosBloqueados.Count > 0)
            {
                await LimpiarComisionesNoPermitidasAsync(
                    connection,
                    transaction,
                    LCicloId,
                    contactosBloqueados
                );
            }

            transaction.Commit();

            string mensaje = rowsAffected == Listado.Count
                ? "Habilitaciones registradas correctamente."
                : "Las habilitaciones se guardaron parcialmente.";

            _log.Info(
                LogTransaccionId,
                NOMBREARCHIVO,
                nombreMetodo,
                $"Fin de metodo [mensaje:{mensaje}, rowsAffected:{rowsAffected}, data:{JsonConvert.SerializeObject(Listado, Formatting.Indented)}]"
            );

            return (true, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al guardar habilitaciones: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Mensaje)> UpdateHabilitacion(
        string LogTransaccionId,
        string Usuario,
        ItemHabilitacionComision Data
    )
    {
        string nombreMetodo = "UpdateHabilitacion()";

        if (Data.LHabilitacionId <= 0)
        {
            return (false, "La habilitación enviada no es válida.");
        }

        if (Data.LContactoId <= 0)
        {
            return (false, "Debe seleccionar un asesor válido.");
        }

        if (Data.LCicloId <= 0)
        {
            return (false, "El ciclo enviado no es válido.");
        }

        if (Data.MontoVenta <= 0)
        {
            return (false, "El monto de venta debe ser mayor a 0.");
        }

        const string query = @"
            UPDATE administracionhabilitacioncomision
            SET
                lcontacto_id = @LContactoId,
                lciclo_id = @LCicloId,
                monto_venta = @MontoVenta,
                observacion = @Observacion,
                genera_comisiones = @GeneraComisiones,
                usuario_modificacion = @UsuarioModificacion,
                fecha_modificacion = @FechaModificacion
            WHERE lhabilitacion_id = @LHabilitacionId
              AND estado = 1;
        ";

        Data.Observacion = Data.Observacion?.Trim() ?? string.Empty;
        Data.UsuarioModificacion = Usuario;
        Data.FechaModificacion = DateTime.Now;

        _log.Info(
            LogTransaccionId,
            NOMBREARCHIVO,
            nombreMetodo,
            $"Inicio de metodo [Usuario:{Usuario}, script:{query}, data:{JsonConvert.SerializeObject(Data, Formatting.Indented)}]"
        );

        try
        {
            using var connection = _context.CreateConnection();

            int rowsAffected = await connection.ExecuteAsync(query, Data);

            bool success = rowsAffected > 0;
            string mensaje = success
                ? "Habilitación actualizada correctamente."
                : "No se encontró la habilitación a actualizar.";

            _log.Info(
                LogTransaccionId,
                NOMBREARCHIVO,
                nombreMetodo,
                $"Fin de metodo [mensaje:{mensaje}, rowsAffected:{rowsAffected}]"
            );

            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al actualizar la habilitación: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Mensaje)> DeleteHabilitacion(
        string LogTransaccionId,
        string Usuario,
        int LHabilitacionId
    )
    {
        string nombreMetodo = "DeleteHabilitacion()";

        if (LHabilitacionId <= 0)
        {
            return (false, "La habilitación enviada no es válida.");
        }

        const string query = @"
            DELETE FROM administracionhabilitacioncomision
            WHERE lhabilitacion_id = @LHabilitacionId;
        ";

        _log.Info(
            LogTransaccionId,
            NOMBREARCHIVO,
            nombreMetodo,
            $"Inicio de metodo [Usuario:{Usuario}, LHabilitacionId:{LHabilitacionId}, script:{query}]"
        );

        try
        {
            using var connection = _context.CreateConnection();

            int rowsAffected = await connection.ExecuteAsync(query, new { LHabilitacionId });

            bool success = rowsAffected > 0;
            string mensaje = success
                ? "Habilitación eliminada correctamente."
                : "No se encontró la habilitación a eliminar.";

            _log.Info(
                LogTransaccionId,
                NOMBREARCHIVO,
                nombreMetodo,
                $"Fin de metodo [mensaje:{mensaje}, rowsAffected:{rowsAffected}]"
            );

            return (success, mensaje);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al eliminar la habilitación: {ex.Message}");
        }
    }

    private static async Task LimpiarComisionesNoPermitidasAsync(
        System.Data.IDbConnection connection,
        System.Data.IDbTransaction transaction,
        int LCicloId,
        List<int> contactosBloqueados
    )
    {
        if (contactosBloqueados.Count == 0)
        {
            return;
        }

        const string deleteBonoParDetalle = @"
            DELETE BPD
            FROM bonopardetalle BPD
            INNER JOIN bonopar BP ON BP.id = BPD.bonopar_id
            WHERE BP.lciclo_id = @LCicloId
              AND BP.l_contacto_ganador_id IN @ContactosBloqueados;
        ";

        const string deleteBonoPar = @"
            DELETE FROM bonopar
            WHERE lciclo_id = @LCicloId
              AND l_contacto_ganador_id IN @ContactosBloqueados;
        ";

        const string deleteBonoResidual = @"
            DELETE FROM administracionbonoresidual
            WHERE lciclo_id = @LCicloId
              AND lcontacto_id IN @ContactosBloqueados;
        ";

        const string deleteBonoCompleto = @"
            DELETE FROM t_bonocompleto
            WHERE lciclo_id = @LCicloId
              AND lcontacto_id IN @ContactosBloqueados;
        ";

        const string deleteRedEmpresaComplejo = @"
            DELETE FROM administracionredempresacomplejo
            WHERE lciclo_id = @LCicloId
              AND lcontacto_id IN @ContactosBloqueados;
        ";

        const string deleteVentaGrupo = @"
            DELETE FROM administracionventagrupo
            WHERE lciclo_id = @LCicloId
              AND lcontacto_id IN @ContactosBloqueados;
        ";

        const string deleteVentaPersonal = @"
            DELETE FROM administracionventapersonal
            WHERE lciclo_id = @LCicloId
              AND lcontacto_id IN @ContactosBloqueados;
        ";

        const string deleteBonoCarrera = @"
            DELETE FROM administracionbonocarrera
            WHERE lciclo_id = @LCicloId
              AND lcontacto_id IN @ContactosBloqueados;
        ";

        var parameters = new
        {
            LCicloId,
            ContactosBloqueados = contactosBloqueados
        };

        await connection.ExecuteAsync(deleteBonoParDetalle, parameters, transaction);
        await connection.ExecuteAsync(deleteBonoPar, parameters, transaction);
        await connection.ExecuteAsync(deleteBonoResidual, parameters, transaction);
        await connection.ExecuteAsync(deleteBonoCompleto, parameters, transaction);
        await connection.ExecuteAsync(deleteRedEmpresaComplejo, parameters, transaction);
        await connection.ExecuteAsync(deleteVentaGrupo, parameters, transaction);
        await connection.ExecuteAsync(deleteVentaPersonal, parameters, transaction);
        await connection.ExecuteAsync(deleteBonoCarrera, parameters, transaction);
    }
}
