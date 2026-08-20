using ApiGuardian.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace ApiGuardian.Infrastructure.Services;

public sealed class CambioDolarService
{
    private readonly HashSet<int> _idsComplejosProyectos;
    private readonly decimal _tipoCambio;

    public CambioDolarService(IConfiguration configuration)
    {
        var configuracion = configuration
            .GetSection("cambioDolar")
            .Get<CambioDolarConfiguracion>() ?? new CambioDolarConfiguracion();

        _idsComplejosProyectos = configuracion.IdsComplejosProyectos.ToHashSet();
        _tipoCambio = configuracion.TipoCambio;

        if (_idsComplejosProyectos.Count > 0 && _tipoCambio <= 0)
        {
            throw new InvalidOperationException(
                "La configuración cambioDolar.tipoCambio debe ser mayor a cero cuando existen complejos o proyectos configurados."
            );
        }
    }

    public decimal Convertir(decimal monto, int idComplejoProyecto)
    {
        return _idsComplejosProyectos.Contains(idComplejoProyecto)
            ? monto / _tipoCambio
            : monto;
    }
    public decimal SacarInicial(decimal monto, int idComplejoProyecto)
    {
        return _idsComplejosProyectos.Contains(idComplejoProyecto)
            ? monto / _tipoCambio
            : monto;
    }

    public void Convertir(ItemVentaCnx venta)
    {
        venta.DPrecio = Convertir(venta.DPrecio, venta.LComplejoId);
        venta.PrecioInicial = Convertir(venta.PrecioInicial, venta.LComplejoId);
        //venta.SCuotaInicial = Convertir(venta.SCuotaInicial, venta.LComplejoId);
        venta.SCuotaInicial = (venta.SCuotaInicial != venta.ValorCi)? venta.ValorCi: venta.SCuotaInicial;
        venta.SCuotaInicialOriginal = Convertir(venta.SCuotaInicialOriginal, venta.LComplejoId);
        venta.ValorCi = Convertir(venta.ValorCi, venta.LComplejoId);
        if(_idsComplejosProyectos.Contains(venta.LComplejoId))
        {
            venta.SCuotaInicial = venta.DPrecio * venta.PorcentajeCuotaInicial /100;
        }
    }

    public void Convertir(TCuota cuota)
    {
        var idComplejoProyecto = cuota.Idproyecto != 0
            ? cuota.Idproyecto
            : cuota.LComplejoId;

        cuota.Bono = Convertir(cuota.Bono, idComplejoProyecto);
        cuota.Amortizacion = Convertir(cuota.Amortizacion, idComplejoProyecto);
        cuota.Capital = Convertir(cuota.Capital, idComplejoProyecto);
        cuota.Interes = Convertir(cuota.Interes, idComplejoProyecto);
        cuota.Seguro = Convertir(cuota.Seguro, idComplejoProyecto);
        cuota.Expensa = Convertir(cuota.Expensa, idComplejoProyecto);
        cuota.Multa = Convertir(cuota.Multa, idComplejoProyecto);
        cuota.Acuenta = Convertir(cuota.Acuenta, idComplejoProyecto);
        cuota.Totalpago = Convertir(cuota.Totalpago, idComplejoProyecto);
        cuota.Montodeuda = Convertir(cuota.Montodeuda, idComplejoProyecto);
        cuota.Pagosacuenta = Convertir(cuota.Pagosacuenta, idComplejoProyecto);
    }

    public void Convertir(VentaResidual venta)
    {
        venta.ImporteTotal = Convertir(venta.ImporteTotal, venta.IdAlmacen);
    }

    public void Convertir(UpgradeSolicitudDto solicitud)
    {
        solicitud.MontoHold = Convertir(solicitud.MontoHold, solicitud.ProyectoHoldId);
        solicitud.PagadoHold = Convertir(solicitud.PagadoHold, solicitud.ProyectoHoldId);
        solicitud.DeudaHold = Convertir(solicitud.DeudaHold, solicitud.ProyectoHoldId);

        if (solicitud.ProyectoId.HasValue)
        {
            solicitud.Monto = Convertir(solicitud.Monto, solicitud.ProyectoId.Value);
            solicitud.Deuda = Convertir(solicitud.Deuda, solicitud.ProyectoId.Value);
            solicitud.Cuota = Convertir(solicitud.Cuota, solicitud.ProyectoId.Value);
        }
    }

    private sealed class CambioDolarConfiguracion
    {
        public List<int> IdsComplejosProyectos { get; set; } = new();
        public decimal TipoCambio { get; set; } = 1m;
    }
}
