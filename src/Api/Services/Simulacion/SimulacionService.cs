using ApiGuardian.Application.Interfaces;
using ApiGuardian.Application.Interfaces.Simulacion;
using ApiGuardian.Infrastructure.Repositories.Simulacion;
using Microsoft.Extensions.Options;

namespace ApiGuardian.Api.Services.Simulacion;

// Servicio del proceso light. Fase 2 (Obtener Ventas) implementada en código.
// Lecturas: repositorios de solo lectura del proceso actual. Escrituras: SimulacionRepositorio (R2).
// Aun no conectado al job (fase 9).
public sealed partial class SimulacionService : ISimulacionService
{
    private const string NOMBRE_ARCHIVO = "SimulacionService.cs";

    private readonly IVentasCnxRepository _ventasCnx;
    private readonly IAdministracionContactoRepository _contactoLectura;
    private readonly IAdministracionContratoRepository _contratoLectura;
    private readonly IAdministracionComplejoRepository _complejoLectura;
    private readonly SimulacionRepositorio _repositorio;
    private readonly ILogService _log;
    private readonly ConfiguracionSimulacion _configuracion;
    private readonly IProcesoComisionesRepository _procesoLectura;
    private readonly IAdministracionHabilitacionComisionRepository _habilitacionLectura;
    private readonly HashSet<int> _noComprimir;
    private readonly IAdministracionSemanaCicloRepository _semanaLectura;
    private readonly IBonoResidualRepository _bonoLectura;
    private readonly IBrConfiguracionRepository _brConfiguracionLectura;

    public SimulacionService(
        IVentasCnxRepository ventasCnx,
        IAdministracionContactoRepository contactoLectura,
        IAdministracionContratoRepository contratoLectura,
        IAdministracionComplejoRepository complejoLectura,
        IProcesoComisionesRepository procesoLectura,
        IAdministracionHabilitacionComisionRepository habilitacionLectura,
        SimulacionRepositorio repositorio,
        ILogService log,
        IOptions<ConfiguracionSimulacion> configuracion,
        IConfiguration configurationGlobal,
        IAdministracionSemanaCicloRepository semanaLectura,
        IBonoResidualRepository bonoLectura,
        IBrConfiguracionRepository brConfiguracionLectura)
    {
        _semanaLectura = semanaLectura;
        _bonoLectura = bonoLectura;
        _brConfiguracionLectura = brConfiguracionLectura;
        _ventasCnx = ventasCnx;
        _contactoLectura = contactoLectura;
        _contratoLectura = contratoLectura;
        _complejoLectura = complejoLectura;
        _procesoLectura = procesoLectura;
        _habilitacionLectura = habilitacionLectura;
        _repositorio = repositorio;
        _log = log;
        _configuracion = configuracion.Value;
        // Misma clave y misma lectura que RedesService (HabilidacionesParaNoComprimirRed).
        _noComprimir = configurationGlobal
            .GetSection("HabilidacionesParaNoComprimirRed")
            .Get<int[]>()
            ?.Where(id => id > 0)
            .ToHashSet()
            ?? new HashSet<int>();
    }

    // F3-T1 y F3-T2: Comision Directa (ventas personales) y su base residual, como en SaveVtaPersonal.
    // Diferencia: no se compara la cantidad con una lista del frontend (el light no tiene frontend).
    public async Task<(bool Exito, string Mensaje)> ComisionDirectaAsync(string logId, string usuario, int lCicloId, string inicio, string fin)
    {
        var calculo = await _procesoLectura.GetCalculoVentaPersonal(logId, usuario, inicio, fin, lCicloId);
        var habilitaciones = await _habilitacionLectura.GetHabilitaciones(logId, usuario, lCicloId);
        if (!habilitaciones.Success)
            return (false, habilitaciones.Mensaje);

        var bloqueados = HabilitacionComisionHelper.GetContactosBloqueadosParaComision(habilitaciones.Data);
        var lista = calculo.Data
            .Where(x => !bloqueados.Contains(Convert.ToInt32(x.lcontacta_id)))
            .ToList();

        // Mismo ajuste que SaveVtaPersonal: 67% para los tipos especiales.
        foreach (var x in lista)
        {
            if (x.TipoContratoId == TiposContratosDiccionario.TiposContratosDiccionarioGrd.UPGRADE
                || x.TipoContratoId == TiposContratosDiccionario.TiposContratosDiccionarioGrd.RECOMPRA
                || x.TipoContratoId == TiposContratosDiccionario.TiposContratosDiccionarioGrd.RECUPERACION
                || x.TipoContratoId == TiposContratosDiccionario.TiposContratosDiccionarioGrd.CASOSESPECIALES)
            {
                x.dcomision = x.inicial * 67 / 100;
                x.dporcentajecomision = 67;
            }
        }

        var filas = lista.Select(x => new AdministracionVentaPersonal
        {
            susuarioadd = usuario,
            susuariomod = usuario,
            lciclo_id = lCicloId,
            lcontacto_id = x.lcontacta_id,
            dpreciolote = x.inicial,
            dporcentajecomision = x.dporcentajecomision,
            dcomision = x.dcomision,
            lcontrato_id = x.lcontrato_id,
            lnrosemana = 1,
            lsemana_id = 124,
        }).ToList();

        (bool Success, string Mensaje) guardado = filas.Count > 0
            ? await _repositorio.InsertVentaPersonalAsync(logId, filas)
            : (true, "No existen ventas personales que generen comisión para guardar.");
        if (!guardado.Success)
            return (false, guardado.Mensaje);

        if (!await CalculoVentaResidualAsync(logId, lCicloId, inicio, fin, usuario))
            return (false, "No se pudo calcular la venta residual asociada a Comisión Directa.");

        return (true, $"Comisión Directa: {filas.Count} ventas personales.");
    }

    // Copia de CalculoVentaResidualAsync (ProcesoComisionesService): base residual de la comisión directa.
    private async Task<bool> CalculoVentaResidualAsync(string logId, int lCicloId, string inicio, string fin, string usuario)
    {
        var contratos = await _contratoLectura.GetAdministracionContratoFechaVentaResidual(logId, inicio, fin);
        var habilitaciones = await _habilitacionLectura.GetHabilitaciones(logId, usuario, lCicloId);
        if (!habilitaciones.Success)
            return false;

        var bloqueados = HabilitacionComisionHelper.GetContactosBloqueadosParaComision(habilitaciones.Data);
        var membresia = new HashSet<int> { 85, 29, 58, 95, 98, 101, 102 };

        var lista = contratos.Data
            .Where(x => !HabilitacionComisionHelper.TiposContratoEspeciales.Contains(x.LTipoContratoId)
                        && !bloqueados.Contains(x.LAsesorId))
            .Select(x =>
            {
                var esMembresia = membresia.Contains(x.LComplejoId);
                var inicial10 = x.Precio * 10 / 100;
                var directa = x.CuotaInicial * (esMembresia ? 40 : 30) / 100;
                var nueva = esMembresia ? inicial10 : inicial10 * 30 / 100;
                var cuotas = esMembresia ? 12 : 6;
                return new ProductosPagarMensuales
                {
                    LcontratoId = x.LcontratoId,
                    LcomplejoId = x.LComplejoId,
                    Snroventa = x.NroVenta ?? "",
                    LcontactoId = x.LcontratoId,
                    LasesorId = x.LAsesorId,
                    Dtfecha = x.Fecha,
                    Precio = x.Precio,
                    CuotaInicial = x.CuotaInicial,
                    Porcentaje = x.PorcentajeInicial,
                    Comision = directa,
                    CuotAccPen = cuotas,
                    CuotPagadas = 0,
                    Inicial10 = inicial10,
                    MontPagar = nueva - directa,
                    MensPagar = (nueva - directa) / cuotas,
                };
            })
            .Where(x => x.MontPagar > 0 && x.Porcentaje < 100)
            .ToList();

        var resultado = await _repositorio.InsertProductosPagarMensualesAsync(logId, lista);
        return resultado.Success;
    }

    // F1-T6: candado por ciclo (D-21). Si el ciclo tiene una corrida EN_CURSO, no se ejecuta otra.
    public Task<(bool Iniciada, long Id, string Mensaje)> IniciarCorridaAsync(string logId, int lCicloId, string usuario)
    {
        return _repositorio.IniciarCorridaAsync(logId, lCicloId, usuario, _configuracion.HorasMaximasEnCurso);
    }

    public Task<bool> TerminarCorridaAsync(string logId, long id, bool exito, string? pasoFallido, string? mensajeError)
    {
        return _repositorio.TerminarCorridaAsync(logId, id, exito ? "OK" : "ERROR", pasoFallido, mensajeError);
    }

    // Corrida del job: candado (F1-T6), reset (F2-T2), Obtener Ventas (F2), Comision Directa (F3), Red (F4),
    // Comision Grupo (F5), Datos Residual (F6), Bono Residual (F7) y Bono Par (F8). Un fallo detiene la corrida en ese paso (R13).
    public async Task<(bool Exito, string Mensaje)> EjecutarCorridaAsync(string usuario, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var logId = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

        var ciclo = await _repositorio.ObtenerUltimoCicloAsync(logId);
        if (ciclo is null)
            return (false, "No se encontró el ciclo de referencia.");

        var candado = await IniciarCorridaAsync(logId, ciclo.LCicloId, usuario);
        if (!candado.Iniciada)
            return (false, candado.Mensaje);

        string paso = "RESET";
        try
        {
            var reset = await _repositorio.ResetCicloAsync(logId, ciclo.LCicloId, ciclo.Inicio, ciclo.Fin);
            if (!reset.Success)
                return await CerrarConErrorAsync(logId, candado.Id, paso, reset.Mensaje);

            paso = "OBTENER VENTAS";
            var ventas = await ObtenerVentasAsync(logId, usuario, ciclo.Inicio, ciclo.Fin);
            if (!ventas.Exito)
                return await CerrarConErrorAsync(logId, candado.Id, paso, ventas.Mensaje);

            paso = "COMISION DIRECTA";
            var comision = await ComisionDirectaAsync(logId, usuario, ciclo.LCicloId, ciclo.Inicio, ciclo.Fin);
            if (!comision.Exito)
                return await CerrarConErrorAsync(logId, candado.Id, paso, comision.Mensaje);

            paso = "RED COMPRIMIDA";
            var redComprimida = await RedComprimidaAsync(logId, usuario, ciclo.LCicloId, ciclo.Inicio, ciclo.Fin);
            if (!redComprimida.Exito)
                return await CerrarConErrorAsync(logId, candado.Id, paso, redComprimida.Mensaje);

            paso = "RED COMPLETA";
            var redCompleta = await RedCompletaAsync(logId);
            if (!redCompleta.Exito)
                return await CerrarConErrorAsync(logId, candado.Id, paso, redCompleta.Mensaje);

            paso = "COMISION GRUPO";
            var comisionGrupo = await ComisionGrupoAsync(logId, usuario, ciclo.LCicloId, ciclo.Inicio, ciclo.Fin);
            if (!comisionGrupo.Exito)
                return await CerrarConErrorAsync(logId, candado.Id, paso, comisionGrupo.Mensaje);

            paso = "DATOS RESIDUAL";
            var datosResidual = await DatosResidualAsync(logId, usuario, ciclo.LCicloId, ciclo.Inicio, ciclo.Fin);
            if (!datosResidual.Exito)
                return await CerrarConErrorAsync(logId, candado.Id, paso, datosResidual.Mensaje);

            paso = "COMISION RESIDUAL";
            var bonoResidual = await BonoResidualAsync(logId, usuario, ciclo.LCicloId, ciclo.Inicio, ciclo.Fin);
            if (!bonoResidual.Exito)
                return await CerrarConErrorAsync(logId, candado.Id, paso, bonoResidual.Mensaje);

            paso = "BONO PAR";
            var bonoPar = await BonoParAsync(logId, usuario, ciclo.LCicloId, ciclo.Inicio, ciclo.Fin);
            if (!bonoPar.Exito)
                return await CerrarConErrorAsync(logId, candado.Id, paso, bonoPar.Mensaje);

            // Los siete pasos terminaron: la corrida queda OK.
            await TerminarCorridaAsync(logId, candado.Id, true, null, "Corrida completa: los siete pasos terminaron.");
            return (true, "Corrida completa: los siete pasos terminaron.");
        }
        catch (Exception ex)
        {
            return await CerrarConErrorAsync(logId, candado.Id, paso, ex.Message);
        }
    }

    private async Task<(bool Exito, string Mensaje)> CerrarConErrorAsync(string logId, long idCorrida, string paso, string mensaje)
    {
        await TerminarCorridaAsync(logId, idCorrida, false, paso, mensaje);
        return (false, $"Corrida detenida en {paso}: {mensaje}");
    }

    // F2-T3 y F2-T4: Obtener Ventas normales del ciclo (rama JOB de ProcesoPrincipal, sin rezagadas ni especiales, D-09).
    public async Task<(bool Exito, string Mensaje)> ObtenerVentasAsync(string logId, string usuario, string inicio, string fin)
    {
        var ventas = await _ventasCnx.GetVentaCnx(logId, inicio, fin);
        if (!ventas.Success)
            return (false, ventas.Mensaje);

        var homologacion = await _complejoLectura.GetHomologacionComplejoGrdCnx(logId);
        if (!homologacion.Success)
            return (false, homologacion.Mensaje);

        var listaComplejo = homologacion.Data;
        int procesadas = 0;

        foreach (var item in ventas.Data)
        {
            // Vendedor (con su árbol de patrocinadores)
            var vendedorId = await EnsureContactoExisteAsync(logId, item.SCedulaIdentidadVendedor ?? "", item, usuario);

            // Cliente (depende del vendedor)
            var responseCliente = await _contactoLectura.GetAdministracionContactoByDocId(logId, item.SCedulaIdentidad ?? "");
            if (string.IsNullOrEmpty(responseCliente.Data?.SCedulaIdentidad))
            {
                var cliente = ObjCliente(item, vendedorId, usuario);
                await _repositorio.InsertContactoAsync(logId, cliente, false);
                responseCliente = await _contactoLectura.GetAdministracionContactoByDocId(logId, item.SCedulaIdentidad ?? "");
            }

            // Contrato de la venta
            var contrato = new AdministracionContrato
            {
                LContratoId = 0,
                Fecha = item.DFecha,
                NroVenta = $"{item.IdVenta}-{item.Lote}",
                LPropietarioId = (int)responseCliente.Data.LContactoId,
                LCopmlejoId = listaComplejo.FirstOrDefault(x => x.LComplejoIdCX == item.LComplejoId).LComplejoId,
                Mzno = item.SManzano,
                Lote = $"{item.SLote}",
                Uv = item.SUV,
                PrecioInicial = item.PrecioInicial,
                CuotaInicial = item.SCuotaInicial,
                PrecioFinal = item.DPrecio,
                LEstadoContratoId = 4,
                LTipoContratoId = TiposContratosDiccionario.ObtenerGrd(item.TipoComisionable, item.TipoVenta == 1, item.TipoVenta == 2),
                LCiudadId = 0,
                ContratoEspecial = 0,
                LAsesorId = (int)vendedorId,
                Usuario = usuario,
                PorcentajeCuotaInicial = item.PorcentajeCuotaInicial
            };

            var existente = await _contratoLectura.GetContratoXNroVenta(logId, $"{item.IdVenta}-{item.Lote}", inicio, fin);
            if (existente.Data == null || !existente.Data.Any())
            {
                await _repositorio.InsertContratoAsync(logId, contrato);
            }

            procesadas++;
        }

        _log.Info(logId, NOMBRE_ARCHIVO, nameof(ObtenerVentasAsync), $"Ventas procesadas: {procesadas}");
        return (true, $"Ventas procesadas: {procesadas}.");
    }

    // F4-T2: Red comprimida. Copia de RedesService.ObtenerRedComprimidaAsync con controlPaso = false (R5).
    // Diferencia: si no hay contactos, no falla; el reset ya dejó red_comprimida vacía del ciclo.
    // Diferencia (v42): los activos salen de los contratos de los ultimos 3 meses hasta el fin del ciclo, no solo del ciclo.
    public async Task<(bool Exito, string Mensaje)> RedComprimidaAsync(string logId, string usuario, int lCicloId, string inicio, string fin)
    {
        var habilitaciones = await _habilitacionLectura.GetHabilitaciones(logId, usuario, lCicloId);
        if (!habilitaciones.Success)
            return (false, habilitaciones.Mensaje);
        var personas = habilitaciones.Data.ToList();

        // Activos: contratos de los ultimos 3 meses, hasta el fin del ciclo (incluido).
        // Ej.: fin 20261031 -> ventana 20260801 a 20261031.
        var finFecha = DateTime.ParseExact(fin, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
        var inicioVentana = finFecha.AddMonths(-3).AddDays(1).ToString("yyyyMMdd");
        var activos = await _repositorio.GetContactosVentasMesAsync(logId, inicioVentana, fin);
        if (!activos.Success)
            return (false, activos.Mensaje);

        var red = await _repositorio.GetRedContactosAsync(logId);
        if (!red.Success)
            return (false, red.Mensaje);

        var activosPorId = activos.Lista
            .Select(x => new ItemContactoActivo
            {
                LContactoId = x.LContactoId > 0 ? x.LContactoId : x.LVendedorId,
                LVendedorId = x.LVendedorId > 0 ? x.LVendedorId : x.LContactoId,
            })
            .Where(x => x.LVendedorId > 0)
            .GroupBy(x => x.LVendedorId)
            .ToDictionary(x => x.Key, x => x.First());
        foreach (var h in personas)
            if (h.LContactoId > 0 && !activosPorId.ContainsKey(h.LContactoId))
                activosPorId[h.LContactoId] = new ItemContactoActivo { LContactoId = h.LContactoId, LVendedorId = h.LContactoId };

        var redPorContacto = red.Lista
            .Where(x => x.Hijo > 0)
            .GroupBy(x => x.Hijo)
            .ToDictionary(x => x.Key, x => x.First());
        foreach (var contacto in _noComprimir.Where(redPorContacto.ContainsKey))
            if (!activosPorId.ContainsKey(contacto))
                activosPorId[contacto] = new ItemContactoActivo { LContactoId = contacto, LVendedorId = contacto };

        var activoIds = activosPorId.Keys.ToHashSet();
        var lista = new List<ItemContactoRed>();
        foreach (var item in activosPorId.Values)
        {
            int contacto = item.LVendedorId, nivel = 1;
            var visitados = new HashSet<int> { contacto };
            while (nivel <= 7 && redPorContacto.TryGetValue(contacto, out var patrocinador))
            {
                int padre = patrocinador.Padre;
                if (padre <= 0 || !visitados.Add(padre))
                    break;
                if (activoIds.Contains(padre))
                    lista.Add(new ItemContactoRed
                    {
                        LContactoId = item.LVendedorId,
                        LPatrocinadorId = padre,
                        Nivel = nivel++,
                        LCicloId = lCicloId,
                        LContratoId = 0,
                        Usuario = usuario,
                    });
                contacto = padre;
            }
        }

        var guardado = await _repositorio.ReemplazarRedComprimidaAsync(logId, usuario, lCicloId, lista);
        if (!guardado.Success)
            return (false, guardado.Mensaje);
        return (true, $"Red comprimida: {lista.Count} registros.");
    }

    // F5-T2: Comision Grupo. Copia de GuardarVentaGrupoAsync sin control de proceso (R5) ni lista del frontend.
    // Diferencias: no se compara la cantidad con ListaComision (no hay frontend), igual que Comision Directa.
    // La lista sale de GetCalculoVentaGrupo, que lee red_comprimida del ciclo (ya escrita en F4-T2).
    public async Task<(bool Exito, string Mensaje)> ComisionGrupoAsync(string logId, string usuario, int lCicloId, string inicio, string fin)
    {
        var ventas = await _procesoLectura.GetCalculoVentaGrupo(logId, usuario, inicio, fin, lCicloId);

        var hab = await _habilitacionLectura.GetHabilitaciones(logId, usuario, lCicloId);
        if (!hab.Success)
            return (false, hab.Mensaje);

        var bloqueados = HabilitacionComisionHelper.GetContactosBloqueadosParaComision(hab.Data);
        // Excepcion de grupo (copiada de ProcesoComisionesService): los contactos configurados se guardan aunque
        // esten bloqueados, pero nunca cobran comision. Usa la misma clave de configuracion (HabilidacionesParaNoComprimirRed).
        var lista = ventas.Data
            .Where(x => !bloqueados.Contains(x.LGanadorId) || _noComprimir.Contains(x.LGanadorId))
            .ToList();
        foreach (var x in lista.Where(x => _noComprimir.Contains(x.LGanadorId)))
        {
            x.Comision = 0;
            x.EsCero = true;
            x.EsComisionGrupoCeroConfigurada = true;
        }

        var semanas = await _semanaLectura.GetSemanaCicloId(logId, lCicloId);
        if (!semanas.Success || !semanas.Semanas.Any())
            return (false, $"No hay semanas configuradas para el ciclo {lCicloId}.");
        var semana = semanas.Semanas.First();

        var rows = lista.Select(x => new ItemVentaGrupo
        {
            usuario = usuario,
            lciclo_id = lCicloId,
            lcontacto_id = x.LGanadorId,
            lgeneracion = x.Nivel,
            lasesor_id = x.LVendedorId,
            dporcentajecomision = x.Porcentaje,
            dcomision = x.Comision,
            dventapersonal = x.DCuotaInicial,
            dventapersonalinicial = x.DCuotaInicial,
            lcontrato_id = x.LContratoId,
            lnrosemana = semana.LNroSemana,
            lsemana_id = semana.LSemanaId,
        }).ToList();

        if (rows.Count == 0)
            return (true, "Comisión Grupo: no hay comisiones habilitadas para guardar.");

        var guardado = await _repositorio.InsertVentaGrupoAsync(logId, rows);
        if (!guardado.Success)
            return (false, guardado.Mensaje);
        return (true, $"Comisión Grupo: {rows.Count} registros.");
    }

    // F6-T2 a F6-T3: Datos Residual. Cartera (OBTENER CARTERA) y cuotas (OBTENER CUOTAS), sin excedente (D-03).
    // Lectura: BonoResidualRepository.GetCarteraAll (BDComisiones) y GetCuota (SQL Server, con conversion de dolar).
    // Escritura: SimulacionRepositorio (Cartera con guardia R-17; T_ACCIONESCUOTASGRL con DELETE por FECHA_PAGO).
    public async Task<(bool Exito, string Mensaje)> DatosResidualAsync(string logId, string usuario, int lCicloId, string inicio, string fin)
    {
        var cartera = await _bonoLectura.GetCarteraAll(logId, usuario);
        if (!cartera.Success)
            return (false, cartera.Mensaje);
        var guardadoCartera = await _repositorio.ReemplazarCarteraAsync(logId, lCicloId, cartera.ListaCartera.ToList());
        if (!guardadoCartera.Success)
            return (false, guardadoCartera.Mensaje);

        var cuotas = await _bonoLectura.GetCuota(logId, usuario, inicio, fin);
        if (!cuotas.Success)
            return (false, cuotas.Mensaje);
        var guardadoCuotas = await _repositorio.ReemplazarCuotasAsync(logId, inicio, fin, cuotas.ListaCuota.ToList());
        if (!guardadoCuotas.Success)
            return (false, guardadoCuotas.Mensaje);

        return (true, $"Datos residual: cartera {cartera.ListaCartera.Count()} registros; cuotas {cuotas.ListaCuota.Count()} registros.");
    }

    // F4-T3: Red completa (RedesService.ObtenerRedCuotasAsync sin control de paso). Carga tmp_residual_red,
    // que lee Bono Residual (BonoResidualRepository). No escribe en red_completa_cuotas: el código actual tampoco.
    public async Task<(bool Exito, string Mensaje)> RedCompletaAsync(string logId)
    {
        var red = await _repositorio.GetRedContactosAsync(logId);
        if (!red.Success)
            return (false, red.Mensaje);

        var lista = new List<ItemRedSieteNiveles>();
        var diccionario = red.Lista.ToDictionary(x => x.Hijo, x => x.Padre);
        foreach (var item in red.Lista)
        {
            var fila = new ItemRedSieteNiveles { Hijo = item.Hijo };
            int actual = item.Hijo;
            for (int nivel = 1; nivel <= 7; nivel++)
            {
                if (!diccionario.TryGetValue(actual, out int padre))
                    break;
                switch (nivel)
                {
                    case 1: fila.PadreN1 = padre; break;
                    case 2: fila.PadreN2 = padre; break;
                    case 3: fila.PadreN3 = padre; break;
                    case 4: fila.PadreN4 = padre; break;
                    case 5: fila.PadreN5 = padre; break;
                    case 6: fila.PadreN6 = padre; break;
                    case 7: fila.PadreN7 = padre; break;
                }
                actual = padre;
            }
            lista.Add(fila);
        }

        var guardado = await _repositorio.ReemplazarRedContactoTemporalAsync(logId, lista);
        if (!guardado.Success)
            return (false, guardado.Mensaje);
        return (true, $"Red completa: {lista.Count} contactos.");
    }

    // Copia de EnsureContactoExisteAsync (MiCronJob), misma lógica y mismos casos de autocompra.
    private async Task<long> EnsureContactoExisteAsync(string logId, string ci, ItemVentaCnx item, string usuario)
    {
        // 1. ¿Existe en GRD?
        var responseGrd = await _contactoLectura.GetAdministracionContactoByDocId(logId, ci);
        if (!string.IsNullOrEmpty(responseGrd.Data?.SCedulaIdentidad))
            return responseGrd.Data.LContactoId;

        // 2. Buscar en CNX
        var responseCnx = await _ventasCnx.GetClienteDocId(logId, ci);
        long padreId = 0;

        if (item.SCedulaIdentidad == item.SCedulaIdentidadVendedor)
        {
            return await InsertarAutoCompraAsync(logId, item, padreId, usuario);
        }

        // 3. Procesar padre (recursivo)
        if (responseCnx.Data != null)
        {
            if (responseCnx.Data.SCedulaIdentidad == responseCnx.Data.SCedulaIdentidadVendedor)
            {
                return await InsertarAutoCompraAsync(logId, item, padreId, usuario);
            }

            responseGrd = await _contactoLectura.GetAdministracionContactoByDocId(logId, responseCnx.Data.SCedulaIdentidadVendedor ?? "");
            padreId = responseGrd.Data == null
                ? await EnsureContactoExisteAsync(logId, responseCnx.Data.SCedulaIdentidadVendedor ?? "", item, usuario)
                : responseGrd.Data.LContactoId;
        }
        else
        {
            padreId = 1;
        }

        // 4. Crear contacto en GRD
        var contacto = ObjPatrocinante(item, padreId, usuario);
        responseGrd = await _contactoLectura.GetAdministracionContactoByDocId(logId, contacto.CedulaIdentidad ?? "");
        if (responseGrd.Data == null)
        {
            await _repositorio.InsertContactoAsync(logId, contacto, false);
            var responseCliente = await _contactoLectura.GetAdministracionContactoByDocId(logId, contacto.CedulaIdentidad ?? "");
            return responseCliente.Data.LContactoId;
        }

        return responseGrd.Data.LContactoId;
    }

    private async Task<long> InsertarAutoCompraAsync(string logId, ItemVentaCnx item, long padreId, string usuario)
    {
        var contactoAutoCompra = ObjPatrocinante(item, padreId, usuario);
        await _repositorio.InsertContactoAsync(logId, contactoAutoCompra, true);
        var responseCliente = await _contactoLectura.GetAdministracionContactoByDocId(logId, contactoAutoCompra.CedulaIdentidad ?? "");
        return responseCliente.Data.LContactoId;
    }

    // Copia de objPatrocinante (MiCronJob).
    private static AdministracionContacto ObjPatrocinante(ItemVentaCnx vtaCnx, long lPatrocinante, string usuario) => new()
    {
        Usuario = usuario,
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

    // Copia de objCliennte (MiCronJob).
    private static AdministracionContacto ObjCliente(ItemVentaCnx vtaCnx, long lPatrocinante, string usuario) => new()
    {
        Usuario = usuario,
        NombreCompleto = vtaCnx.SNombreCompleto,
        CedulaIdentidad = vtaCnx.SCedulaIdentidad,
        TelefonoFijo = vtaCnx.TelefonoFijo,
        TelefonoMovil = vtaCnx.TelefonoMovil,
        CorreoElectronico = vtaCnx.Correo,
        Ciudad = vtaCnx.SCiudad,
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
