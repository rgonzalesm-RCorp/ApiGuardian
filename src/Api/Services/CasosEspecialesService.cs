using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Services;

public sealed class CasosEspecialesService : ICasosEspecialesService
{
    private readonly ICasosEspecialesRepository _repository;
    private readonly IAdministracionContratoRepository _contratoRepository;

    public CasosEspecialesService(
        ICasosEspecialesRepository repository,
        IAdministracionContratoRepository contratoRepository
    )
    {
        _repository = repository;
        _contratoRepository = contratoRepository;
    }

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerAsync(
        string usuario,
        int cicloId,
        string inicio,
        string fin
    )
    {
        try
        {
            var ventas = await _repository.GetVentasCasosEspeciales(Id(), usuario, inicio, fin);
            var contratos = await _contratoRepository.GetContratoFecha(Id(), inicio, fin);
            var upgradeIds = string.Join(
                ",",
                ventas
                    .VentasCasosEspeciales.Where(x =>
                        x.TipoComisionable
                        == TiposContratosDiccionario.TiposContratosDiccionarioCnx.UPGRADE
                    )
                    .Select(x => x.IdVenta)
            );
            var upgrades = await _repository.GetUpgradeSolicitudPorVentasCnx(
                Id(),
                usuario,
                upgradeIds
            );
            foreach (var item in ventas.VentasCasosEspeciales)
            {
                if (item.TipoComisionable != 8)
                {
                    //item.SCuotaInicial = item.DPrecio * 3 / 100;
                }
                item.SCuotaInicial = item.SCuotaInicialOriginal;
                if (
                    item.TipoComisionable
                    == TiposContratosDiccionario.TiposContratosDiccionarioCnx.UPGRADE
                )
                {
                    var solicitud = upgrades.Lista.FirstOrDefault(x => x.VentaId == item.IdVenta);
                    if (solicitud != null)
                    {
                        decimal diferencia = item.DPrecio - solicitud.MontoHold;
                        decimal tresPorciento = diferencia * 3 / 100;
                        item.SCuotaInicial =
                            item.SCuotaInicial < tresPorciento ? item.SCuotaInicial : tresPorciento;
                    }
                }
                if (
                    item.TipoComisionable
                    == TiposContratosDiccionario.TiposContratosDiccionarioCnx.RECOMPRA
                )
                    item.SCuotaInicial =
                        (item.DPrecio == item.SCuotaInicial || item.SCuotaInicial == 0)
                            ? item.DPrecio * 3 / 100
                            : item.SCuotaInicial;
                if (
                    item.TipoComisionable
                    == TiposContratosDiccionario.TiposContratosDiccionarioCnx.CASOSESPECIALES
                )
                {
                    decimal inicial = (item.DPrecio - item.SCuotaInicial) * 3 / 100;
                    item.SCuotaInicial = inicial + inicial * 49.25M / 100; // se aunmente un 49.25% por el tema de la retencion de impuestos
                }
            }
            var xls = await new CasosEspecialesXls().GetCasosEspecialesXls(
                ventas.VentasCasosEspeciales.ToList()
            );
            return (
                ventas.Success,
                ventas.Mensaje,
                new
                {
                    VentasCasosEspeciales = ventas.VentasCasosEspeciales,
                    VtaGrd = contratos.Data,
                    base64Xls = xls.base64,
                }
            );
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    private static string Id() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
}
