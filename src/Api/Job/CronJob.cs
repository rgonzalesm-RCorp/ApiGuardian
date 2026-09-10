using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Repositories;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using Newtonsoft.Json;
using Quartz;

public class MiCronJob : IJob
{
    private readonly ILogger<MiCronJob> _logger;
    private readonly IVentasCnxRepository _ventasCnxRepository;
    private readonly IAdministracionContactoRepository _administracionContactoRepository;
    private readonly IAdministracionContratoRepository _administracionContratoRepository;
    private readonly IProcesoComisionesRepository _procesoComisionesRepository;
    private readonly IAdministracionComplejoRepository _administracionComplejoRepository;
    private readonly IControlProcesoRepository _controlProcesoRepository;
    private readonly IAdministracionCicloRepository _administracionCicloRepository;
    public MiCronJob(ILogger<MiCronJob> logger, IVentasCnxRepository ventasCnxRepository, IAdministracionContactoRepository administracionContactoRepository, IAdministracionContratoRepository administracionContratoRepository, IProcesoComisionesRepository procesoComisionesRepository, IAdministracionComplejoRepository administracionComplejoRepository, IControlProcesoRepository controlProcesoRepository, IAdministracionCicloRepository administracionCicloRepository)
    {
        _logger = logger;
        _ventasCnxRepository = ventasCnxRepository;
        _administracionContactoRepository = administracionContactoRepository;
        _administracionContratoRepository = administracionContratoRepository;
        _procesoComisionesRepository = procesoComisionesRepository;
        _administracionComplejoRepository = administracionComplejoRepository;
        _controlProcesoRepository = controlProcesoRepository;
        _administracionCicloRepository = administracionCicloRepository;
    }
    private async Task<AdministracionContacto> objPatrocinante(ItemVentaCnx vtaCnx, long lPatrocinante, string Usuario)
    {
        return new AdministracionContacto
                    {
                        Usuario = Usuario,
                        NombreCompleto = vtaCnx.SNombreCompletoVendedor,
                        CedulaIdentidad = vtaCnx.SCedulaIdentidadVendedor,
                        TelefonoFijo = vtaCnx.TelefonoFijoVendedor,
                        TelefonoMovil = vtaCnx.TelefonoMovilVendedor,
                        CorreoElectronico = vtaCnx.CorreoVendedor,
                        Ciudad = vtaCnx.SCiudad,
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
    private async Task<AdministracionContacto> objCliennte(ItemVentaCnx vtaCnx, long lPatrocinante, string Usuario)
    {
        return new AdministracionContacto
                    {
                        Usuario = Usuario,
                        NombreCompleto = vtaCnx.SNombreCompleto,
                        CedulaIdentidad = vtaCnx.SCedulaIdentidad,
                        TelefonoFijo = vtaCnx.TelefonoFijo,
                        TelefonoMovil = vtaCnx.TelefonoMovil,
                        CorreoElectronico = vtaCnx.Correo,
                        Ciudad = vtaCnx.SCiudad,
                        PaisId = vtaCnx.IdPaisResidencia,
                        PatrocinanteId = lPatrocinante,
                        NivelId = 1,
                        Comentario = "",
                        TelefonoOficina = vtaCnx.STelefonoOficina,
                        Direccion = vtaCnx.Direccion,
                        Nit = 0,
                        FechaRegistro = DateTime.Now,
                        FechaNacimiento = vtaCnx.FechaNacimiento,
                        LContactoId = 0,
                    };
        
    }
    private async Task<long> EnsureContactoExisteAsync(string LogTransaccionId, string ci, ItemVentaCnx item, string Usuario)
    {
        // 1️⃣ ¿Existe en GRD?
        var responseGrd = await _administracionContactoRepository.GetAdministracionContactoByDocId(LogTransaccionId, ci);
        //verificar autocompra


        if (!string.IsNullOrEmpty(responseGrd.Data?.SCedulaIdentidad))
            return responseGrd.Data.LContactoId;

        // 2️⃣ Buscar en CNX
        var responseCnx = await _ventasCnxRepository.GetClienteDocId(LogTransaccionId, ci);

        long padreId = 0;

        if (item.SCedulaIdentidad == item.SCedulaIdentidadVendedor)
        {
            AdministracionContacto contactoAutoCompra = await objPatrocinante(item, padreId, Usuario);
            var insert = await _administracionContactoRepository.InsertContacto(LogTransaccionId, contactoAutoCompra, true);
            var responseCliente = await _administracionContactoRepository.GetAdministracionContactoByDocId(LogTransaccionId, contactoAutoCompra.CedulaIdentidad ?? "");

            return responseCliente.Data.LContactoId;
        }

        // 3️⃣ Procesar padre (recursivo)
        
        if ( responseCnx.Data != null)
        {
            //AUTOCOMPRA
            if (responseCnx.Data.SCedulaIdentidad == responseCnx.Data.SCedulaIdentidadVendedor)
            {
                AdministracionContacto contactoAutoCompra = await objPatrocinante(item, padreId, Usuario);
                var insert = await _administracionContactoRepository.InsertContacto(LogTransaccionId, contactoAutoCompra, true);
                var responseCliente = await _administracionContactoRepository.GetAdministracionContactoByDocId(LogTransaccionId, contactoAutoCompra.CedulaIdentidad ?? "");

                return responseCliente.Data.LContactoId;
            }
            responseGrd = await _administracionContactoRepository.GetAdministracionContactoByDocId(LogTransaccionId, responseCnx.Data.SCedulaIdentidadVendedor ?? "");
            if (responseGrd.Data == null)
            {
                padreId = await EnsureContactoExisteAsync(LogTransaccionId, responseCnx.Data.SCedulaIdentidadVendedor ?? "", item, Usuario);
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
        AdministracionContacto contacto = await objPatrocinante(item, padreId, Usuario);

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
        /*var  vtaCnx = await _ventasCnxRepository.GetVentaCnx("","","");
        int counter = 0;

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
            var responseExistContrato = await _administracionContratoRepository.GetContratoXNroVenta("", $"{item.IdVenta}-{item.Lote}", "", "");

            if(responseExistContrato.Data == null || responseExistContrato.Data.Count()<= 0)
            {
                var respSaveContrato = await _administracionContratoRepository.InsertContrato("", data);   
            }
            counter = counter+ 1;

            //var respSaveContrato = await _administracionContratoRepository.InsertContrato("", data);
            Console.WriteLine(item.Lote);
            var t = vtaCnx.Data.ToList();
            Console.WriteLine( JsonConvert.SerializeObject(t[counter], Formatting.Indented));
        }
        _logger.LogInformation("Quartz Job ejecutado: {time}", DateTime.Now);*/
       
        var t = await Proceso();
        await Procesar();
    }

    private Task Procesar()
    {
        return Task.CompletedTask;
    }

    public async Task<bool> Proceso()
    {
        var responseProceso = await _procesoComisionesRepository.GetProceso("", "VENTAS");
        var responseHomologacion = await _administracionComplejoRepository.GetHomologacionComplejoGrdCnx("");
        if (responseProceso.Data == null)
            return true;

        string inicio = responseProceso.Data.Inicio.ToString("yyyyMMdd");
        string fin = responseProceso.Data.Fin.ToString("yyyyMMdd");

        var  vtaCnx = await _ventasCnxRepository.GetVentaCnx("", inicio, fin);
        int counter = 0;

        foreach (var item in vtaCnx.Data)
        {
            // 🔹 Asegurar vendedor (árbol completo)
            var vendedorId = await EnsureContactoExisteAsync("", item.SCedulaIdentidadVendedor ?? "", item, "");

            // 🔹 Asegurar cliente (depende del vendedor)
            var responseCliente = await _administracionContactoRepository.GetAdministracionContactoByDocId("", item.SCedulaIdentidad ?? "");

            if (string.IsNullOrEmpty(responseCliente.Data?.SCedulaIdentidad))
            {
                AdministracionContacto cliente = await objCliennte(item, vendedorId, "");

                await _administracionContactoRepository.InsertContacto("", cliente);
                responseCliente = await _administracionContactoRepository.GetAdministracionContactoByDocId("", item.SCedulaIdentidad ?? "");

            }
            var homologacion = responseHomologacion.Data?.FirstOrDefault(x => x.LComplejoIdCX == item.LComplejoId);
            if (homologacion == null)
                continue;

            AdministracionContrato data = new AdministracionContrato
            {
                LContratoId = 0,
                Fecha = item.DFecha,
                NroVenta = $"{item.IdVenta}-{item.Lote}",
                LPropietarioId = (int)responseCliente.Data.LContactoId,
                LCopmlejoId = homologacion.LComplejoId, //Obtener la equivalecia
                Mzno = item.SManzano,
                Lote = $"{item.SLote}" ,
                Uv = item.SUV,
                PrecioInicial = item.PrecioInicial,
                CuotaInicial = item.SCuotaInicial,
                PrecioFinal = item.DPrecio,
                LEstadoContratoId = 1,
                LTipoContratoId = item.TipoVenta == 2 ? 1 : 2,
                LCiudadId =  0,
                ContratoEspecial = 0,
                LAsesorId = (int)vendedorId,
                Usuario = ".Net",
                PorcentajeCuotaInicial = item.PorcentajeCuotaInicial 
            };
            var responseExistContrato = await _administracionContratoRepository.GetContratoXNroVenta("", $"{item.IdVenta}-{item.Lote}", "", "");

            if(responseExistContrato.Data == null || responseExistContrato.Data.Count()<= 0)
            {
                var respSaveContrato = await _administracionContratoRepository.InsertContrato("", data);   
            }
            counter = counter+ 1;

            //var respSaveContrato = await _administracionContratoRepository.InsertContrato("", data);
            Console.WriteLine(item.Lote);
            var t = vtaCnx.Data.ToList();
            //Console.WriteLine( JsonConvert.SerializeObject(t[counter], Formatting.Indented));
        }
        //_logger.LogInformation("Quartz Job ejecutado: {time}", DateTime.Now);
        return true;
    }

    //string tipo = "JOB", bool rezagada = false, string paso = "", string usuario = "", int lCicloId = 0
    public async Task<bool> ProcesoPrincipal(string LogTransaccionId, RequestProcesoPrincipal Request,  List<ItemVentaCnx>? Lista = null){
        bool pasoIniciado = false;
        string inicio = string.Empty;
        string fin = string.Empty;

        var responseHomologacion = await _administracionComplejoRepository.GetHomologacionComplejoGrdCnx(LogTransaccionId);
        List<HomologacionComplejoGrdCnx> ListaComplejo = responseHomologacion.Data;

        if(Request.Tipo == "JOB")
        {
            var responseProceso = await _procesoComisionesRepository.GetProceso(LogTransaccionId, "VENTAS");
            if (responseProceso.Data == null)
                return true;
            
            inicio = responseProceso.Data.Inicio.ToString("yyyyMMdd");
            fin  = responseProceso.Data.Fin.ToString("yyyyMMdd");

            var vtaCnx = await _ventasCnxRepository.GetVentaCnx(LogTransaccionId, inicio, fin);
            Lista = vtaCnx.Data.ToList();

            var responseInicioPaso = await _controlProcesoRepository.IniciarPaso(
                LogTransaccionId,
                Request.Usuario,
                ProcesosDiccionario.COMISIONES,
                Request.LCicloId,
                Request.Paso
            );

            if (!responseInicioPaso.Success || !(responseInicioPaso.Data?.status ?? false))
                return false;

            pasoIniciado = true;
        }
        else
        {
            var responseCiclo = await _administracionCicloRepository.GetCiclo(LogTransaccionId, Request.LCicloId);
            if (!responseCiclo.Success || responseCiclo.Data.LCicloId <= 0)
            {
                _logger.LogWarning("ProcesoPrincipal cancelado porque no se encontro el ciclo. LCicloId: {LCicloId}", Request.LCicloId);
                return false;
            }

            inicio = responseCiclo.Data.DtFechaInicio ?? string.Empty;
            fin = responseCiclo.Data.DtFechaFin ?? string.Empty;

            if (string.IsNullOrWhiteSpace(inicio) || string.IsNullOrWhiteSpace(fin))
            {
                _logger.LogWarning("ProcesoPrincipal cancelado porque el ciclo no tiene fechas configuradas. LCicloId: {LCicloId}", Request.LCicloId);
                return false;
            }

            pasoIniciado = true;
        }

        bool procesado = false;

        try
        {
            procesado = await ProcesarVentas(LogTransaccionId, Request.Usuario, Lista, ListaComplejo, inicio, fin, Request.Rezagada, Request.LCicloId);

            if (!procesado)
            {
                _logger.LogWarning("ProcesoPrincipal cancelado porque ProcesarVentas devolvio false. Paso: {Paso}, LCicloId: {LCicloId}", Request.Paso, Request.LCicloId);

                if (pasoIniciado)
                {
                    await _controlProcesoRepository.CancelarPaso(LogTransaccionId, Request.Usuario, ProcesosDiccionario.COMISIONES, Request.LCicloId, Request.Paso);
                }

                return false;
            }

            await _controlProcesoRepository.UpdateControlProceso(LogTransaccionId, Request.Usuario, Request.Paso, Request.LCicloId);

            if (pasoIniciado)
            {
                await _controlProcesoRepository.FinalizarPaso(LogTransaccionId, Request.Usuario, ProcesosDiccionario.COMISIONES, Request.LCicloId, Request.Paso);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en ProcesoPrincipal. Paso: {Paso}, LCicloId: {LCicloId}", Request.Paso, Request.LCicloId);

            if (pasoIniciado)
            {
                await _controlProcesoRepository.CancelarPaso(LogTransaccionId, Request.Usuario, ProcesosDiccionario.COMISIONES, Request.LCicloId, Request.Paso);
            }

            throw;
        }
    }
    private async Task<bool> ProcesarVentas(string LogTransaccionId, string Usuario, List<ItemVentaCnx> Lista, List<HomologacionComplejoGrdCnx> ListaComplejo, string inicio, string fin, bool rezagada, int LCicloId)
    {
        int counter = 0;
        foreach (var item in Lista)
        {
            // 🔹 Asegurar vendedor (árbol completo)
            var vendedorId = await EnsureContactoExisteAsync(LogTransaccionId, item.SCedulaIdentidadVendedor ?? "", item, Usuario);

            // 🔹 Asegurar cliente (depende del vendedor)
            var responseCliente = await _administracionContactoRepository.GetAdministracionContactoByDocId(LogTransaccionId, item.SCedulaIdentidad ?? "");

            if (string.IsNullOrEmpty(responseCliente.Data?.SCedulaIdentidad))
            {
                AdministracionContacto cliente = await objCliennte(item, vendedorId, Usuario);

                await _administracionContactoRepository.InsertContacto(LogTransaccionId, cliente);
                responseCliente = await _administracionContactoRepository.GetAdministracionContactoByDocId(LogTransaccionId, item.SCedulaIdentidad ?? "");

            }
            AdministracionContrato data = new AdministracionContrato
            {
                LContratoId = 0,
                Fecha = item.DFecha,
                NroVenta = $"{item.IdVenta}-{item.Lote}",
                LPropietarioId = (int)responseCliente.Data.LContactoId,
                LCopmlejoId = ListaComplejo.FirstOrDefault(x => x.LComplejoIdCX == item.LComplejoId).LComplejoId, //Obtener la equivalecia
                Mzno = item.SManzano,
                Lote = $"{item.SLote}" ,
                Uv = item.SUV,
                PrecioInicial = item.PrecioInicial,
                CuotaInicial = item.SCuotaInicial,
                PrecioFinal = item.DPrecio,
                LEstadoContratoId = 4,
                LTipoContratoId = TiposContratosDiccionario.ObtenerGrd(item.TipoComisionable, item.TipoVenta == 1? true : false, item.TipoVenta == 2? true : false),//  item.TipoComisionable, // item.TipoVenta == 2 ? 1 : 2,
                LCiudadId =  0,
                ContratoEspecial = 0,
                LAsesorId = (int)vendedorId,
                Usuario = Usuario,
                PorcentajeCuotaInicial = item.PorcentajeCuotaInicial 
            };
            var responseExistContrato = await _administracionContratoRepository.GetContratoXNroVenta(LogTransaccionId, $"{item.IdVenta}-{item.Lote}", inicio, fin);

            if(responseExistContrato.Data == null || responseExistContrato.Data.Count()<= 0)
            {
                var respSaveContrato = await _administracionContratoRepository.InsertContrato(LogTransaccionId, data);   
            }
            counter = counter+ 1;
            Console.WriteLine(item.Lote);
            if (rezagada)
            {
                await _procesoComisionesRepository.UpdateVtaRezagadas(LogTransaccionId, item, Usuario, LCicloId);
            }
        }
        return true;
    }
}
