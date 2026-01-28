using ApiGuardian.Application.Interfaces;
using Quartz;

public class MiCronJob : IJob
{
    private readonly ILogger<MiCronJob> _logger;
    private readonly IVentasCnxRepository _ventasCnxRepository;
    private readonly IAdministracionContactoRepository _administracionContactoRepository;
    private readonly IAdministracionContratoRepository _administracionContratoRepository;
    public MiCronJob(ILogger<MiCronJob> logger, IVentasCnxRepository ventasCnxRepository, IAdministracionContactoRepository administracionContactoRepository, IAdministracionContratoRepository administracionContratoRepository)
    {
        _logger = logger;
        _ventasCnxRepository = ventasCnxRepository;
        _administracionContactoRepository = administracionContactoRepository;
        _administracionContratoRepository = administracionContratoRepository;
    }
    private async Task<AdministracionContacto> objPatrocinante(ItemVentaCnx vtaCnx, long lPatrocinante)
    {
        return new AdministracionContacto
                    {
                        Usuario = ".Net",
                        NombreCompleto = vtaCnx.SNombreCompletoVendedor,
                        CedulaIdentidad = vtaCnx.SCedulaIdentidadVendedor,
                        TelefonoFijo = vtaCnx.TelefonoFijoVendedor,
                        TelefonoMovil = vtaCnx.TelefonoMovilVendedor,
                        CorreoElectronico = vtaCnx.CorreoVendedor,
                        Ciudad = "",
                        PaisId = vtaCnx.IdPaisResidenciaVendedor,
                        PatrocinanteId = lPatrocinante,
                        NivelId = 0,
                        Comentario = "",
                        TelefonoOficina = vtaCnx.STelefonoOficinaVendedor,
                        Direccion = vtaCnx.DireccionVendedor,
                        Nit = 0,
                        FechaRegistro = DateTime.Now,
                        FechaNacimiento = vtaCnx.FechaNacimientoVendedor,
                        LContactoId = 0,
                    };
        
    }
    private async Task<AdministracionContacto> objCliennte(ItemVentaCnx vtaCnx, long lPatrocinante)
    {
        return new AdministracionContacto
                    {
                        Usuario = ".Net",
                    NombreCompleto = vtaCnx.SNombreCompleto,
                    CedulaIdentidad = vtaCnx.SCedulaIdentidad,
                    TelefonoFijo = vtaCnx.TelefonoFijo,
                    TelefonoMovil = vtaCnx.TelefonoMovil,
                    CorreoElectronico = vtaCnx.Correo,
                    Ciudad = "",
                    PaisId = vtaCnx.IdPaisResidencia,
                    PatrocinanteId = lPatrocinante,
                    NivelId = 0,
                    Comentario = "",
                    TelefonoOficina = vtaCnx.STelefonoOficina,
                    Direccion = vtaCnx.Direccion,
                    Nit = 0,
                    FechaRegistro = DateTime.Now,
                    FechaNacimiento = vtaCnx.FechaNacimiento,
                    LContactoId = 0,
                    };
        
    }
    private async Task<long> EnsureContactoExisteAsync(string LogTransaccionId, string ci, ItemVentaCnx item)
    {
        // 1️⃣ ¿Existe en GRD?
        var responseGrd = await _administracionContactoRepository.GetAdministracionContactoByDocId(LogTransaccionId, ci);

        if (!string.IsNullOrEmpty(responseGrd.Data?.SCedulaIdentidad))
            return responseGrd.Data.LContactoId;

        // 2️⃣ Buscar en CNX
        var responseCnx = await _ventasCnxRepository.GetClienteDocId(LogTransaccionId, ci);

        long padreId = 0;

        // 3️⃣ Procesar padre (recursivo)
        
        if ( responseCnx.Data != null)
        {
            responseGrd = await _administracionContactoRepository.GetAdministracionContactoByDocId(LogTransaccionId, responseCnx.Data.SCedulaIdentidadVendedor ?? "");
            if (responseGrd.Data == null)
            {
                padreId = await EnsureContactoExisteAsync(LogTransaccionId, responseCnx.Data.SCedulaIdentidadVendedor ?? "", item);
            }
            else
            {
                padreId = responseGrd.Data.LContactoId;
            }
        }else
        {
            padreId = 1;
        }

        // 4️⃣ Crear contacto en GRD
        AdministracionContacto contacto = await objPatrocinante(item, padreId);

        responseGrd = await _administracionContactoRepository.GetAdministracionContactoByDocId(LogTransaccionId, contacto.CedulaIdentidad ?? "");
        if (responseGrd.Data == null)
        {
            var insert = await _administracionContactoRepository.InsertContacto(LogTransaccionId, contacto);
            var responseCliente = await _administracionContactoRepository.GetAdministracionContactoByDocId(LogTransaccionId, contacto.CedulaIdentidad ?? "");

            return responseCliente.Data.LContactoId;
        }else
        {
            return responseGrd.Data.LContactoId;
        }
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var  vtaCnx = await _ventasCnxRepository.GetVentaCnx("","","");

        foreach (var item in vtaCnx.Data)
        {
            // 🔹 Asegurar vendedor (árbol completo)
            var vendedorId = await EnsureContactoExisteAsync("", item.SCedulaIdentidadVendedor ?? "", item);

            // 🔹 Asegurar cliente (depende del vendedor)
            var responseCliente = await _administracionContactoRepository.GetAdministracionContactoByDocId("", item.SCedulaIdentidad ?? "");

            if (string.IsNullOrEmpty(responseCliente.Data?.SCedulaIdentidad))
            {
                AdministracionContacto cliente = await objCliennte(item, vendedorId);

                await _administracionContactoRepository.InsertContacto("", cliente);
                responseCliente = await _administracionContactoRepository.GetAdministracionContactoByDocId("", item.SCedulaIdentidad ?? "");

            }
            AdministracionContrato data = new AdministracionContrato
            {
                LContratoId = 0,
                Fecha = item.DFecha,
                NroVenta = $"{item.IdVenta}-{item.Lote}",
                LPropietarioId = (int)responseCliente.Data.LContactoId,
                LCopmlejoId = 29, //Obtener la equivalecia
                Mzno = item.SManzano,
                Lote = $"{item.IdVenta}-{item.Lote}" ,
                Uv = item.SUV,
                PrecioInicial = item.PrecioInicial,
                CuotaInicial = item.SCuotaInicial,
                PrecioFinal = item.DPrecio,
                LEstadoContratoId = 0,
                LTipoContratoId = 1, //revisas
                LCiudadId =  0, //item.SCiudad,
                ContratoEspecial = 0, // revisar 
                LAsesorId = (int)vendedorId,
                Usuario = ".Net"
            };
            var respSaveContrato = await _administracionContratoRepository.InsertContrato("", data);
            Console.WriteLine(item.Lote);
        }
        _logger.LogInformation("Quartz Job ejecutado: {time}", DateTime.Now);

        await Procesar();
    }

    private Task Procesar()
    {
        return Task.CompletedTask;
    }
}
