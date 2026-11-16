using Dapper;

namespace ApiGuardian.Infrastructure.Repositories.Simulacion;

// F2-T4: altas de contacto y contrato. SQL copiado de AdministracionContactoRepository.InsertContacto
// y AdministracionContratoRepository.InsertContrato, sin reutilizar esos repositorios (R2).
public sealed partial class SimulacionRepositorio
{
    public async Task<(bool Success, string Mensaje)> InsertContactoAsync(string logId, AdministracionContacto data, bool autoCompra)
    {
        const string metodo = nameof(InsertContactoAsync);

        string query = $@"
            INSERT INTO administracioncontacto (
                lcontacto_id, susuarioadd, dtfechaadd, susuariomod, dtfechamod,
                snombrecompleto, scedulaidentidad, stelefonofijo, stelefonomovil, scorreoelectronico,
                sciudad, lpais_id, lpatrocinante_id, lnivel_id, sotro, stelefonooficina, lnit,
                lbanco_id, pvitalicio, pmax, smotivobaja, ltipobaja,
                cbaja, ctienecuenta, lcodigobanco, lcuentabanco,
                lpatrotemp_id, scontrasena,
                snotadescuentolote, ddescuentolote, cpresentafactura, ltipocontacto_id, ccorreo,
                cweb, cradio, cperiodico, ctv, ccena, cpresentacion, cvolante, dproduccion, dlotes, dtfechacalculo,
                cestado, scodigo, dtfechabaja, dtfecharegistro,
                dtfechanacimiento, sdireccion
            )
            SELECT
                IFNULL(MAX_ID, 0) + 1,
                @Usuario, NOW(), @Usuario, NOW(),
                @NombreCompleto, @CedulaIdentidad, @TelefonoFijo, @TelefonoMovil, @CorreoElectronico,
                @Ciudad, @PaisId,
                {(autoCompra ? "IFNULL(MAX_ID, 0) + 1" : "@PatrocinanteId")},
                @NivelId, @Comentario, @TelefonoOficina, @Nit,
                0, 0, 0, '', 0,
                0, 0, 0, 0,
                1, @CedulaIdentidad,
                '', 0, 0, 1, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, now(),
                0, IFNULL(CODIGO, 0) + 1, now(), @FechaRegistro,
                @FechaNacimiento, @Direccion
            FROM (
                SELECT MAX(lcontacto_id) AS MAX_ID, MAX(convert(scodigo, UNSIGNED)) AS CODIGO FROM administracioncontacto
            ) AS sub;";

        try
        {
            using var connection = _context.CreateConnection();
            var rows = await connection.ExecuteAsync(query, new
            {
                data.Usuario,
                data.NombreCompleto,
                data.CedulaIdentidad,
                data.TelefonoFijo,
                data.TelefonoMovil,
                data.CorreoElectronico,
                data.Ciudad,
                data.PaisId,
                data.PatrocinanteId,
                data.NivelId,
                data.Comentario,
                data.TelefonoOficina,
                data.Nit,
                data.FechaRegistro,
                data.FechaNacimiento,
                data.Direccion
            });

            if (rows > 0)
                return (true, "Contacto registrado correctamente.");

            return (false, "No se realizó el guardado del contacto.");
        }
        catch (Exception ex)
        {
            _log.Error(logId, NOMBRE_ARCHIVO, metodo, $"Error al insertar contacto: {ex.Message}");
            return (false, $"Error al insertar contacto: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Mensaje)> InsertContratoAsync(string logId, AdministracionContrato data)
    {
        const string metodo = nameof(InsertContratoAsync);

        const string query = @"
            INSERT INTO administracioncontrato (
                lcontrato_id, dtfecha, lcontacto_id, lcomplejo_id, smanzano, slote, suv, dprecioinicial, dcuota_inicial, dprecio,
                lestado, ltipocontrato_id, lciudad, cespecial, lasesor_id, susuarioadd, susuariomod, dtfechaadd, dtfechamod, snroventa,
                latencion_id, ltramite_id, lreferido_id
            )
            SELECT
                IFNULL(MAX_ID, 0) + 1, @Fecha, @LPropietarioId, @LComplejoId, @Mzno, @Lote, @Uv, @PrecioInicial, @CuotaInicial, @PrecioFinal,
                @LEstadoContratoId, @LTipoContratoId, @LCiudadId, @ContratoEspecial, @LAsesorId, @Usuario, @Usuario, NOW(), NOW(), @NroVenta,
                0, 0, 0
            FROM (
                SELECT MAX(lcontrato_id) AS MAX_ID FROM administracioncontrato
            ) AS sub;";

        try
        {
            using var connection = _context.CreateConnection();
            var rows = await connection.ExecuteAsync(query, new
            {
                data.Fecha,
                data.LPropietarioId,
                LComplejoId = data.LCopmlejoId,
                data.Mzno,
                data.Lote,
                data.Uv,
                data.PrecioInicial,
                data.CuotaInicial,
                data.PrecioFinal,
                data.LEstadoContratoId,
                data.LTipoContratoId,
                data.LCiudadId,
                data.ContratoEspecial,
                data.LAsesorId,
                Usuario = data.Usuario,
                data.NroVenta
            });

            return rows > 0
                ? (true, "Contrato registrado correctamente.")
                : (false, "No se realizó el guardado del contrato.");
        }
        catch (Exception ex)
        {
            _log.Error(logId, NOMBRE_ARCHIVO, metodo, $"Error al insertar contrato: {ex.Message}");
            return (false, $"Error al insertar contrato: {ex.Message}");
        }
    }
}
