using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Services;

public sealed class CuotasVentaResidualService : ICuotasVentaResidualService
{
    private readonly ICuotasVentaResidualRepository _repository;
    private readonly IAdministracionVentaPersonalRepository _ventaPersonal;
    private readonly IAdministracionHabilitacionComisionRepository _habilitacionRepository;
    private readonly IAdministracionCicloRepository _cicloRepository;
    private readonly IControlProcesoRepository _controlProcesoRepository;

    public CuotasVentaResidualService(
        ICuotasVentaResidualRepository repository,
        IAdministracionVentaPersonalRepository ventaPersonal,
        IAdministracionHabilitacionComisionRepository habilitacionRepository,
        IAdministracionCicloRepository cicloRepository,
        IControlProcesoRepository controlProcesoRepository
    )
    {
        _repository = repository;
        _ventaPersonal = ventaPersonal;
        _habilitacionRepository = habilitacionRepository;
        _cicloRepository = cicloRepository;
        _controlProcesoRepository = controlProcesoRepository;
    }

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerAsync(
        string usuario,
        int cicloId
    )
    {
        var id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        try
        {
            var fechas = await ObtenerFechasCicloAsync(id, cicloId);
            if (!fechas.Success)
                return (false, fechas.Mensaje, "");
            var cuotas = await _repository.GetCuotasVentasResidual(
                id,
                usuario,
                fechas.Inicio,
                fechas.Fin,
                cicloId
            );
            var productos = await _repository.GetProductosPagarMensuales(id, usuario);
            var ventasPersonal = await _ventaPersonal.GetVentaPersonal(id, usuario, cicloId);
            var habilitaciones = await _habilitacionRepository.GetHabilitaciones(
                id,
                usuario,
                cicloId
            );
            var siguiente = await _controlProcesoRepository.GetSiguientePaso(
                id,
                usuario,
                ProcesosDiccionario.COMISIONES,
                cicloId
            );
            if (!habilitaciones.Success)
                return (false, habilitaciones.Mensaje, "");

            var personas = habilitaciones.Data.ToList();
            var listado = await ConstruirListadoAsync(
                cuotas.ListadoCuotasVentasResidual,
                productos.ListadoProductosPagarMensuales,
                ventasPersonal.ListadoAdministracionVentaPersonal,
                personas
            );
            var xls = await new ComisionVentaResidualXls().GetComisionVentaResidualXlS(listado);
            return (
                cuotas.Success,
                cuotas.Mensaje,
                new
                {
                    ListadoComisionCuotaResidual = listado,
                    personasHabilitadas = personas,
                    base64Xls = xls.base64,
                    controlPasos = new
                    {
                        ejecutado = PasosDiccionario.COMISION_VENTA_RESIDUAL
                            != siguiente.Data.nombre,
                        data = siguiente.Data,
                    },
                }
            );
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje, object Data)> GuardarAsync(
        string usuario,
        int cicloId
    )
    {
        var id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        bool pasoIniciado = false;
        try
        {
            var fechas = await ObtenerFechasCicloAsync(id, cicloId);
            if (!fechas.Success)
                return (false, fechas.Mensaje, "");
            var siguiente = await _controlProcesoRepository.GetSiguientePaso(
                id,
                usuario,
                ProcesosDiccionario.COMISIONES,
                cicloId
            );
            if (PasosDiccionario.COMISION_VENTA_RESIDUAL != siguiente.Data.nombre)
                return (
                    false,
                    "Esta paso ya se encuentra ejecutado para este ciclo, si quieres volver a a procesar debes reinicar el proceso para el ciclo",
                    ""
                );

            var inicio = await _controlProcesoRepository.IniciarPaso(
                id,
                usuario,
                ProcesosDiccionario.COMISIONES,
                cicloId,
                PasosDiccionario.COMISION_VENTA_RESIDUAL
            );
            if (!inicio.Success || !(inicio.Data?.status ?? false))
                return (false, inicio.Data?.mensaje ?? inicio.Mensaje, "");
            pasoIniciado = true;

            var cuotas = await _repository.GetCuotasVentasResidual(
                id,
                usuario,
                fechas.Inicio,
                fechas.Fin,
                cicloId
            );
            if (!cuotas.Success)
                return await CancelarAsync(id, usuario, cicloId, cuotas.Mensaje);
            var productos = await _repository.GetProductosPagarMensuales(id, usuario);
            var ventasPersonal = await _ventaPersonal.GetVentaPersonal(id, usuario, cicloId);
            var habilitaciones = await _habilitacionRepository.GetHabilitaciones(
                id,
                usuario,
                cicloId
            );
            if (!habilitaciones.Success)
                return await CancelarAsync(id, usuario, cicloId, habilitaciones.Mensaje);

            await _repository.SaveCuotasVentasProductosPagarMensual(
                id,
                usuario,
                cuotas.ListadoCuotasVentasResidual.ToList()
            );
            var listado = await ConstruirListadoAsync(
                cuotas.ListadoCuotasVentasResidual,
                productos.ListadoProductosPagarMensuales,
                ventasPersonal.ListadoAdministracionVentaPersonal,
                habilitaciones.Data
            );
            var fechaActual = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var updates = listado
                .GroupBy(x => new
                {
                    x.IdProductoPagar,
                    x.LcontratoId,
                    x.NroVenta,
                    x.Recibe,
                    x.CuotPagadas,
                    x.CuotAccPen,
                    x.LasesorId,
                    x.MensPagar,
                    x.TotalComision,
                    x.TotalCuotasContabilizar,
                })
                .Select(g => new ProductosPagarMensualUpdate
                {
                    IdProductoPagar = g.Key.IdProductoPagar,
                    SNroVenta = g.Key.NroVenta,
                    CantidadNroCuotas = g.Sum(x => x.NroCuota),
                    ActivoMes = g.Key.Recibe,
                    CuotasPagadas = g.Key.CuotPagadas,
                    CuotasTotalesAPagar = g.Key.CuotAccPen,
                    LContratoId = g.Key.LcontratoId,
                    LContactoId = Convert.ToInt32(g.Key.LasesorId),
                    MontoPagarMes = Convert.ToDecimal(g.Key.MensPagar),
                    TotalComision = g.Key.TotalComision,
                    TotalCuotasContabilizar = g.Key.TotalCuotasContabilizar,
                    _ProductosDetalleCuotas = g.Select(x => new ProductosDetalleCuotas
                        {
                            IdProductoDetalle = 0,
                            UsuarioAdd = usuario,
                            FechaAdd = fechaActual,
                            FkIdProductoPagar = x.IdProductoPagar,
                            LcontratoId = x.LcontratoId,
                            CantCuotas = x.NroCuota,
                            ExcCuotas = 0,
                            Pagado = "1",
                            Habilitado = g.Key.Recibe ? "1" : "0",
                            LcicloId = cicloId,
                        })
                        .ToList(),
                })
                .ToList();

            var guardado = await _repository.SaveControlProductos(id, usuario, updates);
            if (!guardado.Success)
                return await CancelarAsync(id, usuario, cicloId, guardado.Mensaje);
            var fin = await _controlProcesoRepository.FinalizarPaso(
                id,
                usuario,
                ProcesosDiccionario.COMISIONES,
                cicloId,
                PasosDiccionario.COMISION_VENTA_RESIDUAL
            );
            if (!fin.Success || !(fin.Data?.status ?? false))
                return (false, fin.Data?.mensaje ?? fin.Mensaje, "");
            pasoIniciado = false;
            return (true, "Proceso ejecutado correctamente.", updates);
        }
        catch (Exception ex)
        {
            if (pasoIniciado)
                await _controlProcesoRepository.CancelarPaso(
                    id,
                    usuario,
                    ProcesosDiccionario.COMISIONES,
                    cicloId,
                    PasosDiccionario.COMISION_VENTA_RESIDUAL
                );
            return (false, ex.Message, "");
        }
    }

    private async Task<(
        bool Success,
        string Mensaje,
        string Inicio,
        string Fin
    )> ObtenerFechasCicloAsync(string id, int cicloId)
    {
        var r = await _cicloRepository.GetCiclo(id, cicloId);
        if (!r.Success || r.Data.LCicloId <= 0)
            return (false, $"No se encontró el ciclo {cicloId}.", "", "");
        if (
            string.IsNullOrWhiteSpace(r.Data.DtFechaInicio)
            || string.IsNullOrWhiteSpace(r.Data.DtFechaFin)
        )
            return (false, $"El ciclo {cicloId} no tiene fechas configuradas.", "", "");
        return (true, r.Mensaje, r.Data.DtFechaInicio, r.Data.DtFechaFin);
    }

    private static async Task<List<ListadoComisionCuotaResidual>> ConstruirListadoAsync(
        IEnumerable<VentaResidual> cuotas,
        IEnumerable<ProductosPagarMensuales> productos,
        IEnumerable<AdministracionVentaPersonal> ventasPersonal,
        IEnumerable<ItemHabilitacionComision> habilitaciones
    )
    {
        var bloqueados = HabilitacionComisionHelper.GetContactosBloqueadosParaComision(
            habilitaciones.ToList()
        );
        var habilitados = HabilitacionComisionHelper
            .GetContactosHabilitadosQueGeneranComision(habilitaciones.ToList())
            .Select(x => (long)x)
            .ToHashSet();
        var asesores = ventasPersonal
            .Select(x => x.lcontacto_id)
            .Where(id => !bloqueados.Contains(Convert.ToInt32(id)))
            .Concat(habilitados)
            .ToHashSet();
        var tareas = cuotas.Join(
            productos,
            v => v.NroVenta.Trim(),
            p => p.Snroventa.Trim(),
            async (venta, producto) =>
            {
                var comision = await GetComisionAsync(
                    Convert.ToInt32(producto.CuotAccPen),
                    Convert.ToInt32(producto.CuotPagadas),
                    venta.NroCuota,
                    venta.NroCuotaPagables,
                    Convert.ToDecimal(producto.MensPagar)
                );
                return new ListadoComisionCuotaResidual
                {
                    NroVenta = venta.NroVenta,
                    Empresa = venta.Empresa,
                    IdVenta = venta.IdVenta,
                    Fecha = venta.Fecha,
                    IdAlmacen = venta.IdAlmacen,
                    Proyecto = venta.Proyecto,
                    Lotes = venta.Lotes,
                    IdRecibo = venta.IdRecibo,
                    FechaRecibo = venta.FechaRecibo,
                    NroCuota = venta.NroCuota,
                    NroCuotaPagables = venta.NroCuotaPagables,
                    ImporteTotal = venta.ImporteTotal,
                    IdCliente = venta.IdCliente,
                    NombreCliente = venta.NombreCliente,
                    CiCliente = venta.CiCliente,
                    IdVendedor = venta.IdVendedor,
                    Vendedor = venta.Vendedor,
                    CiVendedor = venta.CiVendedor,
                    Concepto1 = venta.Concepto1,
                    LcicloId = venta.LcicloId,
                    IdProductoPagar = producto.IdProductoPagar,
                    LcontratoId = producto.LcontratoId,
                    LcomplejoId = producto.LcomplejoId,
                    Precio = producto.Precio,
                    CuotaInicial = producto.CuotaInicial,
                    Porcentaje = producto.Porcentaje,
                    Comision = producto.Comision,
                    CuotAccPen = producto.CuotAccPen,
                    CuotPagadas = producto.CuotPagadas,
                    Inicial10 = producto.Inicial10,
                    MontPagar = producto.MontPagar,
                    MensPagar = producto.MensPagar,
                    CiclosHabilitados = producto.CiclosHabilitados,
                    Terminado = producto.Terminado,
                    LasesorId = producto.LasesorId,
                    Recibe =
                        producto.LasesorId.HasValue && asesores.Contains(producto.LasesorId.Value),
                    EsHabilitado =
                        producto.LasesorId.HasValue
                        && habilitados.Contains(producto.LasesorId.Value),
                    TotalComision = comision.Comision,
                    TotalCuotasComisionables = comision.CuotasComisionables,
                    TotalCuotasContabilizar = comision.CuotasContabilizar,
                };
            }
        );
        return (await Task.WhenAll(tareas)).ToList();
    }

    private static Task<(
        decimal Comision,
        int CuotasComisionables,
        int CuotasContabilizar
    )> GetComisionAsync(
        int tope,
        int cantidadPagadas,
        int cuotasPagadas,
        int cuotasPagables,
        decimal mesComision
    )
    {
        int diferencia = Math.Max(0, cuotasPagadas - cuotasPagables);
        int contabilizar =
            cantidadPagadas + cuotasPagables > tope ? tope - cantidadPagadas : cuotasPagadas;
        int restante = tope - (cantidadPagadas + diferencia);
        if (restante <= 0)
            return Task.FromResult((0m, 0, contabilizar));
        int comisionables = Math.Min(restante, cuotasPagables);
        return Task.FromResult((comisionables * mesComision, comisionables, contabilizar));
    }

    private async Task<(bool Success, string Mensaje, object Data)> CancelarAsync(
        string id,
        string usuario,
        int cicloId,
        string mensaje
    )
    {
        await _controlProcesoRepository.CancelarPaso(
            id,
            usuario,
            ProcesosDiccionario.COMISIONES,
            cicloId,
            PasosDiccionario.COMISION_VENTA_RESIDUAL
        );
        return (false, mensaje, "");
    }
}
