using ApiGuardian.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace ApiGuardian.Infrastructure.Services;

public sealed class CambioDolarService
{
    private readonly HashSet<int> _idsComplejosProyectos;
    private readonly DateTime _fechaCorte;
    private readonly decimal _tipoCambioHistorico;
    private readonly decimal _tipoCambioOficial;

    public CambioDolarService(IConfiguration configuration)
    {
        var configuracion = configuration
            .GetSection("cambioDolar")
            .Get<CambioDolarConfiguracion>() ?? new CambioDolarConfiguracion();

        _idsComplejosProyectos = configuracion.IdsComplejosProyectos.ToHashSet();
        _fechaCorte = configuracion.FechaCorte.Date;
        _tipoCambioHistorico = configuracion.TipoCambioHistorico;
        _tipoCambioOficial = configuracion.TipoCambioOficial;

        if (_tipoCambioHistorico <= 0 || _tipoCambioOficial <= 0)
        {
            throw new InvalidOperationException(
                "La configuración cambioDolar.tipoCambioHistorico y cambioDolar.tipoCambioOficial debe ser mayor a cero."
            );
        }
    }

    public decimal Convertir(decimal monto, int idComplejoProyecto)
    {
        return _idsComplejosProyectos.Contains(idComplejoProyecto)
            ? Redondear(monto / _tipoCambioOficial)
            : monto;
    }

    public decimal TipoCambioOficial => _tipoCambioOficial;
    public decimal SacarInicial(decimal monto, int idComplejoProyecto)
    {
        return _idsComplejosProyectos.Contains(idComplejoProyecto)
            ? Redondear(monto / _tipoCambioOficial)
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
            venta.SCuotaInicial = Redondear(venta.DPrecio * venta.PorcentajeCuotaInicial / 100);
        }
    }

    public void Convertir(TCuota cuota)
    {
        var idComplejoProyecto = cuota.Idproyecto != 0
            ? cuota.Idproyecto
            : cuota.LComplejoId;

        cuota.Bono = ConvertirCuota(cuota.Bono, idComplejoProyecto, cuota.Fecha_Venta);
        cuota.Amortizacion = ConvertirCuota(cuota.Amortizacion, idComplejoProyecto, cuota.Fecha_Venta);
        cuota.Capital = ConvertirCuota(cuota.Capital, idComplejoProyecto, cuota.Fecha_Venta);
        cuota.Interes = ConvertirCuota(cuota.Interes, idComplejoProyecto, cuota.Fecha_Venta);
        cuota.Seguro = ConvertirCuota(cuota.Seguro, idComplejoProyecto, cuota.Fecha_Venta);
        cuota.Expensa = ConvertirCuota(cuota.Expensa, idComplejoProyecto, cuota.Fecha_Venta);
        cuota.Multa = ConvertirCuota(cuota.Multa, idComplejoProyecto, cuota.Fecha_Venta);
        cuota.Acuenta = ConvertirCuota(cuota.Acuenta, idComplejoProyecto, cuota.Fecha_Venta);
        cuota.Totalpago = ConvertirCuota(cuota.Totalpago, idComplejoProyecto, cuota.Fecha_Venta);
        cuota.Montodeuda = ConvertirCuota(cuota.Montodeuda, idComplejoProyecto, cuota.Fecha_Venta);
        cuota.Pagosacuenta = ConvertirCuota(cuota.Pagosacuenta, idComplejoProyecto, cuota.Fecha_Venta);
    }

    public void Convertir(VentaResidual venta)
    {
        venta.ImporteTotal = ConvertirCuota(venta.ImporteTotal, venta.IdAlmacen, venta.Fecha);
    }

    private decimal ConvertirCuota(decimal monto, int idComplejoProyecto, DateTime fechaVenta)
    {
        // Los proyectos configurados se registran en BOB y siempre se expresan en USD al TC oficial.
        if (_idsComplejosProyectos.Contains(idComplejoProyecto))
            return Redondear(monto / _tipoCambioOficial);

        // Los proyectos no configurados se tratan como USD. Solo los importes anteriores
        // al corte se reexpresan: USD histórico -> BOB histórico -> USD oficial.
        return fechaVenta.Date < _fechaCorte
            ? Redondear(monto * _tipoCambioHistorico / _tipoCambioOficial)
            : monto;
    }

    // Todas las conversiones se redondean una sola vez, al resultado final, a 2 decimales.
    private static decimal Redondear(decimal valor) => decimal.Round(valor, 2, MidpointRounding.AwayFromZero);

    public decimal TipoCambioHistorico => _tipoCambioHistorico;

    // ===== Conversión según la moneda del ciclo =====
    // SUS: lógica actual (métodos sin moneda). BS: proyectos BOB sin cambio; proyectos USD × TC histórico,
    // sin importar la fecha de venta (no se usa fechaCorte ni tipoCambioOficial).

    public void Convertir(ItemVentaCnx venta, MonedaCiclo moneda)
    {
        if (moneda == MonedaCiclo.SUS)
        {
            Convertir(venta);
            return;
        }

        // Misma corrección de dato que en SUS, antes de convertir.
        venta.SCuotaInicial = (venta.SCuotaInicial != venta.ValorCi) ? venta.ValorCi : venta.SCuotaInicial;

        venta.DPrecio = ConvertirABolivianos(venta.DPrecio, venta.LComplejoId);
        venta.PrecioInicial = ConvertirABolivianos(venta.PrecioInicial, venta.LComplejoId);
        venta.SCuotaInicial = ConvertirABolivianos(venta.SCuotaInicial, venta.LComplejoId);
        venta.SCuotaInicialOriginal = ConvertirABolivianos(venta.SCuotaInicialOriginal, venta.LComplejoId);
        venta.ValorCi = ConvertirABolivianos(venta.ValorCi, venta.LComplejoId);
    }

    public void Convertir(TCuota cuota, MonedaCiclo moneda)
    {
        if (moneda == MonedaCiclo.SUS)
        {
            Convertir(cuota);
            return;
        }

        var idComplejoProyecto = cuota.Idproyecto != 0
            ? cuota.Idproyecto
            : cuota.LComplejoId;

        cuota.Bono = ConvertirABolivianos(cuota.Bono, idComplejoProyecto);
        cuota.Amortizacion = ConvertirABolivianos(cuota.Amortizacion, idComplejoProyecto);
        cuota.Capital = ConvertirABolivianos(cuota.Capital, idComplejoProyecto);
        cuota.Interes = ConvertirABolivianos(cuota.Interes, idComplejoProyecto);
        cuota.Seguro = ConvertirABolivianos(cuota.Seguro, idComplejoProyecto);
        cuota.Expensa = ConvertirABolivianos(cuota.Expensa, idComplejoProyecto);
        cuota.Multa = ConvertirABolivianos(cuota.Multa, idComplejoProyecto);
        cuota.Acuenta = ConvertirABolivianos(cuota.Acuenta, idComplejoProyecto);
        cuota.Totalpago = ConvertirABolivianos(cuota.Totalpago, idComplejoProyecto);
        cuota.Montodeuda = ConvertirABolivianos(cuota.Montodeuda, idComplejoProyecto);
        cuota.Pagosacuenta = ConvertirABolivianos(cuota.Pagosacuenta, idComplejoProyecto);
    }

    public void Convertir(VentaResidual venta, MonedaCiclo moneda)
    {
        if (moneda == MonedaCiclo.SUS)
        {
            Convertir(venta);
            return;
        }

        venta.ImporteTotal = ConvertirABolivianos(venta.ImporteTotal, venta.IdAlmacen);
    }

    public void Convertir(UpgradeSolicitudDto solicitud, MonedaCiclo moneda)
    {
        if (moneda == MonedaCiclo.SUS)
        {
            Convertir(solicitud);
            return;
        }

        solicitud.MontoHold = ConvertirABolivianos(solicitud.MontoHold, solicitud.ProyectoHoldId);
        solicitud.PagadoHold = ConvertirABolivianos(solicitud.PagadoHold, solicitud.ProyectoHoldId);
        solicitud.DeudaHold = ConvertirABolivianos(solicitud.DeudaHold, solicitud.ProyectoHoldId);

        if (solicitud.ProyectoId.HasValue)
        {
            solicitud.Monto = ConvertirABolivianos(solicitud.Monto, solicitud.ProyectoId.Value);
            solicitud.Deuda = ConvertirABolivianos(solicitud.Deuda, solicitud.ProyectoId.Value);
            solicitud.Cuota = ConvertirABolivianos(solicitud.Cuota, solicitud.ProyectoId.Value);
        }
    }

    // Ciclo BS: los proyectos BOB ya están en bolivianos; los USD se expresan en BOB al TC histórico.
    private decimal ConvertirABolivianos(decimal monto, int idComplejoProyecto)
    {
        return _idsComplejosProyectos.Contains(idComplejoProyecto)
            ? monto
            : Redondear(monto * _tipoCambioHistorico);
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
        public DateTime FechaCorte { get; set; } = new(2026, 6, 30);
        public decimal TipoCambioHistorico { get; set; } = 6.97m;
        public decimal TipoCambioOficial { get; set; } = 7m;
    }
}
