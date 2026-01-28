using Dapper;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;
using Newtonsoft.Json;

namespace ApiGuardian.Infrastructure.Repositories;

public class AdministracionContactoRepository : IAdministracionContactoRepository
{
    private readonly DapperContext _context;
    private readonly ILogService _log;
    private string NOMBREARCHIVO = "AdministracionContactoRepository.cs";
    public AdministracionContactoRepository(DapperContext context, ILogService log)
    {
        _context = context;
        _log = log;
    }
    public async Task<(ListaAdministracionContacto Data, bool Success, string Mensaje)> GetAdministracionContactoByDocId(string LogTransaccionId, string docId)
    {
        string NombreMetodo = "GetAllAdministracionContactoByDocId()";
        try
        {
            using var connection = _context.CreateConnection();

            var query = @"
                SELECT  
                    A.lcontacto_id AS lContactoId,
                    A.snombrecompleto,
                    A.scedulaidentidad,
                    A.lnit AS lNit,
                    A.stelefonofijo,
                    A.stelefonomovil,
                    A.stelefonooficina,
                    A.sciudad,
                    A.lpatrocinante_id AS lPatrocinanteId,
                    A.lnivel_id AS nNivelId,
                    A.lpais_id AS lPaisId,
                    A.sotro AS Comentario,
                    A.scorreoelectronico SCorreo,
                    A.sdireccion SDireccion,
                    A.dtfechanacimiento FechaNacimiento,
                    A.dtfecharegistro FechaRegistro,
                    A.scodigo SCodigo
                FROM administracioncontacto A
                WHERE   A.scedulaidentidad = @docId
                ORDER BY A.lcontacto_id DESC;
            ";

            var data = await connection.QueryFirstOrDefaultAsync<ListaAdministracionContacto>(query, new {docId});

            return (data, true, "Consulta realizada correctamente.");
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, "Fin de metodo", ex);
            ListaAdministracionContacto  d =  new ListaAdministracionContacto();
            return (d, false, "Error al consultar contactos.");
        }
    }
    public async Task<(IEnumerable<ListaAdministracionContacto> Data, bool Success, string Mensaje, int Total)> GetAllAdministracionContacto(string LogTransaccionId, int page, int pageSize, string? search)
    {
        string NombreMetodo = "GetAllAdministracionContacto()";
        _log.Info(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, $"Inicio de metodo [page:{page}, pageSize:{pageSize}, search:{search}]");

        try
        {
            using var connection = _context.CreateConnection();

            var query = @"
                SELECT  
                    A.lcontacto_id AS lContactoId,
                    A.snombrecompleto,
                    A.scedulaidentidad,
                    A.lnit AS lNit,
                    A.stelefonofijo,
                    A.stelefonomovil,
                    A.stelefonooficina,
                    --A.scorreoelectronico,
                    A.sciudad,
                    A.lpatrocinante_id AS lPatrocinanteId,
                    P.snombrecompleto AS patrocinanteNombre,
                    A.lnivel_id AS nNivelId,
                    UPPER(N.snombre) AS nivelNombre,
                    A.lpais_id AS lPaisId,
                    UPPER(BP.snombre) AS paisNombre,
                    --IFNULL(AB.lbanco_id, 0) AS LBancoId,
                    --IFNULL(AB.snombre, '') AS Banco,
                    A.sotro AS Comentario,
                    --IFNULL(AM.snombre, '') AS Moneda,
                    A.scorreoelectronico SCorreo,
                    A.sdireccion SDireccion,
                    A.dtfechanacimiento FechaNacimiento,
                    A.dtfecharegistro FechaRegistro,
                    A.scodigo SCodigo

                FROM administracioncontacto A
                INNER JOIN administracioncontacto P ON P.lcontacto_id = A.lpatrocinante_id 
                INNER JOIN administracionnivel N ON N.lnivel_id = A.lnivel_id
                INNER JOIN basepais BP ON BP.lpais_id = A.lpais_id
                LEFT JOIN administracionbanco AB ON AB.lbanco_id = A.lbanco_id
                LEFT JOIN administracionmoneda AM ON AM.lmoneda_id = AB.lmoneda_id
                WHERE (@search IS NULL OR A.snombrecompleto LIKE @search OR A.scedulaidentidad LIKE @search) AND A.cbaja = 0
                ORDER BY A.lcontacto_id DESC
                LIMIT @pageSize OFFSET @page;
            ";

            var countQuery = @"
                SELECT COUNT(*)
                 FROM administracioncontacto A
                INNER JOIN administracioncontacto P ON P.lcontacto_id = A.lpatrocinante_id 
                INNER JOIN administracionnivel N ON N.lnivel_id = A.lnivel_id
                INNER JOIN basepais BP ON BP.lpais_id = A.lpais_id
                LEFT JOIN administracionbanco AB ON AB.lbanco_id = A.lbanco_id
                LEFT JOIN administracionmoneda AM ON AM.lmoneda_id = AB.lmoneda_id
                WHERE (@search IS NULL OR A.snombrecompleto LIKE @search OR A.scedulaidentidad LIKE @search) AND A.cbaja = 0;
            ";

            var parameters = new
            {
                search = string.IsNullOrEmpty(search) ? null : $"%{search}%",
                pageSize,
                page
            };

            var data = await connection.QueryAsync<ListaAdministracionContacto>(query, parameters);
            var total = await connection.ExecuteScalarAsync<int>(countQuery, parameters);
            _log.Info(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, $"Fin de metodo [total: {total}, data:{JsonConvert.SerializeObject(data, Formatting.Indented)}]");

            return (data, true, "Consulta realizada correctamente.", total);
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, "Fin de metodo", ex);
            return (Enumerable.Empty<ListaAdministracionContacto>(), false, "Error al consultar contactos.", 0);
        }
    }
    public async Task<(bool Success, string Mensaje)> InsertContacto(string LogTransaccionId, AdministracionContacto data)
    {
        string NombreMetodo = "InsertContacto()";
        _log.Info(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, $"Inicio de metodo");
        try
        {
            string query = @"
                INSERT INTO administracioncontacto (
                    lcontacto_id,
                    susuarioadd,
                    dtfechaadd,
                    susuariomod,
                    dtfechamod,
                    snombrecompleto,
                    scedulaidentidad,
                    stelefonofijo,
                    stelefonomovil,
                    scorreoelectronico,
                    sciudad,
                    lpais_id,
                    lpatrocinante_id,
                    lnivel_id,
                    sotro,
                    stelefonooficina,
                    lnit,
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
                    @Usuario,
                    NOW(),
                    @Usuario,
                    NOW(),
                    @NombreCompleto,
                    @CedulaIdentidad,
                    @TelefonoFijo,
                    @TelefonoMovil,
                    @CorreoElectronico,
                    @Ciudad,
                    @PaisId,
                    @PatrocinanteId,
                    @NivelId,
                    @Comentario,
                    @TelefonoOficina,
                    @Nit,
                    0, 0, 0, '', 0,
                    0, 0, 0, 0,
                    1, @CedulaIdentidad,
                    '', 0, 0, 1, 0,
                    0, 0, 0, 0, 0, 0, 0, 0, 0, now(),
                    0, IFNULL(CODIGO, 0) + 1, now(), @FechaRegistro,
                    @FechaNacimiento, @Direccion
                FROM (
                    SELECT MAX(lcontacto_id) AS MAX_ID, MAX( convert(scodigo, UNSIGNED)) AS CODIGO FROM administracioncontacto
                ) AS sub;
            ";

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
            {
                _log.Info(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, $"Fin de metodo (Contacto registrado correctamente.)");

                return (true, "Contacto registrado correctamente.");
            }
            _log.Warning(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, $"Fin de metodo (No se realizó el guardado.)");
            return (false, "No se realizó el guardado.");
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al insertar contacto: {ex.Message}");
        }
    }
    public async Task<(bool Success, string Mensaje)> UpdateContacto(string LogTransaccionId, AdministracionContacto data)
    {
        string NombreMetodo = "UpdateContacto()";
        _log.Info(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, $"Inicio de metodo");
        try
        {
            string query = @"
                UPDATE administracioncontacto SET
                    snombrecompleto = @NombreCompleto,
                    scedulaidentidad = @CedulaIdentidad,
                    stelefonofijo = @TelefonoFijo,
                    stelefonomovil = @TelefonoMovil,
                    scorreoelectronico = @CorreoElectronico,
                    sciudad = @Ciudad,
                    lpais_id = @PaisId,
                    lpatrocinante_id = @PatrocinanteId,
                    lnivel_id = @NivelId,
                    sotro = @Comentario,
                    stelefonooficina = @TelefonoOficina,
                    lnit = @Nit,
                    dtfecharegistro = @FechaRegistro,
                    dtfechanacimiento = @FechaNacimiento,
                    sdireccion = @Direccion,
                    susuariomod = @Usuario,
                    dtfechamod = NOW()
                WHERE lcontacto_id = @LContactoId;
            ";

            using var connection = _context.CreateConnection();

            var rows = await connection.ExecuteAsync(query, new
            {
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
                data.Direccion,
                data.Usuario,
                data.LContactoId
            });

            if (rows > 0)
            {
                _log.Info(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, $"Fin de metodo (Contacto actualizado correctamente.)");
                return (true, "Contacto actualizado correctamente.");
            }
            _log.Warning(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, $"Fin de metodo (No se realizó la actualización.)");
            return (false, "No se realizó la actualización.");
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, "Fin de metodo", ex);
            return (false, $"Error al actualizar contacto: {ex.Message}");
        }
    }
    public async Task<(bool Success, string Mensaje)> BajaContacto(string LogTransaccionId, AdministracionContactoBaja data)
    {
        string NombreMetodo = "BajaContacto()";
        _log.Info(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, $"Inicio de metodo");
        try
        {
            string query = @"
                UPDATE administracioncontacto SET
                    cbaja = '1',
                    dtfechabaja = NOW(),
                    ltipobaja = @TipoBajaId,
                    smotivobaja = @MotivoBaja,
                    susuariomod = @Usuario,
                    dtfechamod = NOW()
                WHERE lcontacto_id = @LContactoId;
            ";

            using var connection = _context.CreateConnection();

            var rows = await connection.ExecuteAsync(query, new
            {
                data.TipoBajaId,
                data.MotivoBaja,
                data.Usuario,
                data.LContactoId
            });

            if (rows > 0)
            {
                _log.Info(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, $"Fin de metodo (Contacto dado de baja correctamente.)");
                return (true, "Contacto dado de baja correctamente.");
            }
            _log.Warning(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, $"Fin de metodo (No se realizó la baja.)");
            return (false, "No se realizó la baja.");
        }
        catch (Exception ex)
        {
            _log.Error(LogTransaccionId, NOMBREARCHIVO, NombreMetodo, "Fin de metodo", ex);

            return (false, $"Error al dar de baja contacto: {ex.Message}");
        }
    }
}
