using ApiGuardian.Application.Interfaces;

namespace CleanDapperApi.Api.Services;

public sealed class ContactosSoloEspecialesService : IContactosSoloEspecialesService
{
    private readonly IAdministracionCicloRepository _cicloRepository;
    private readonly IAdministracionContratoRepository _contratoRepository;
    private readonly IAdministracionHabilitacionComisionRepository _habilitacionRepository;

    // Primer ciclo al que se aplica la regla: Octubre 2026 (149), el ciclo en proceso cuando se aprobó.
    // Los ciclos anteriores ya están cerrados y no se recalculan; la clave SoloEspecialesDesdeCicloId permite cambiarlo.
    private const int CicloInicialPorDefecto = 149;
    private readonly int _desdeCicloId;

    public ContactosSoloEspecialesService(
        IAdministracionCicloRepository cicloRepository,
        IAdministracionContratoRepository contratoRepository,
        IAdministracionHabilitacionComisionRepository habilitacionRepository,
        IConfiguration configuration
    )
    {
        _desdeCicloId =
            configuration.GetValue<int?>("SoloEspecialesDesdeCicloId") ?? CicloInicialPorDefecto;
        _cicloRepository = cicloRepository;
        _contratoRepository = contratoRepository;
        _habilitacionRepository = habilitacionRepository;
    }

    public async Task<(bool Success, string Mensaje, HashSet<int> Contactos)> ObtenerAsync(
        string logTransaccionId,
        string usuario,
        int cicloId
    )
    {
        var vacio = new HashSet<int>();

        // Ciclos anteriores a la vigencia de la regla: no hace falta leer el ciclo.
        if (cicloId < _desdeCicloId)
            return (true, $"La regla solo especiales aplica desde el ciclo {_desdeCicloId}.", vacio);

        var ciclo = await _cicloRepository.GetCiclo(logTransaccionId, cicloId);
        if (!ciclo.Success || ciclo.Data.LCicloId <= 0)
            return (false, $"No se encontró el ciclo {cicloId}.", vacio);
        if (
            string.IsNullOrWhiteSpace(ciclo.Data.DtFechaInicio)
            || string.IsNullOrWhiteSpace(ciclo.Data.DtFechaFin)
        )
            return (false, $"El ciclo {cicloId} no tiene fechas configuradas.", vacio);

        return await ObtenerPorPeriodoAsync(
            logTransaccionId,
            usuario,
            cicloId,
            ciclo.Data.DtFechaInicio,
            ciclo.Data.DtFechaFin
        );
    }

    public async Task<(bool Success, string Mensaje, HashSet<int> Contactos)> ObtenerPorPeriodoAsync(
        string logTransaccionId,
        string usuario,
        int cicloId,
        string inicio,
        string fin
    )
    {
        var vacio = new HashSet<int>();

        // Ciclos anteriores a la vigencia de la regla: nadie es "solo especiales" y se calculan como antes.
        if (cicloId < _desdeCicloId)
            return (true, $"La regla solo especiales aplica desde el ciclo {_desdeCicloId}.", vacio);

        var soloEspeciales = await _contratoRepository.GetContactosSoloEspeciales(
            logTransaccionId,
            inicio,
            fin
        );
        if (!soloEspeciales.Success)
            return (false, soloEspeciales.Mensaje, vacio);

        var habilitaciones = await _habilitacionRepository.GetHabilitaciones(
            logTransaccionId,
            usuario,
            cicloId
        );
        if (!habilitaciones.Success)
            return (false, habilitaciones.Mensaje, vacio);

        // La habilitación manual manda: quien está habilitado para generar comisiones cobra todo.
        var contactos = HabilitacionComisionHelper.GetContactosSoloEspeciales(
            soloEspeciales.Data,
            habilitaciones.Data
        );
        return (true, $"Contactos solo especiales: {contactos.Count}.", contactos);
    }
}
