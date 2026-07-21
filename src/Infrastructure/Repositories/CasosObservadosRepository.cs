using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Infrastructure.Repositories;

public class CasosObservadosRepository : ICasosObservadosRepository
{
    private readonly ILogService _log;
    private const string NOMBREARCHIVO = "CasosObservadosRepository.cs";

    public CasosObservadosRepository(ILogService log)
    {
        _log = log;
    }

    public Task<(IEnumerable<ItemCasoObservado> Data, CasosObservadosResumen Resumen, bool Success, string Mensaje)> GetCasosObservados(string LogTransaccionId, string Usuario, int LCicloId)
    {
        string nombreMetodo = "GetCasosObservados()";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [Usuario:{Usuario}, LCicloId:{LCicloId}]");

        var data = Enumerable.Empty<ItemCasoObservado>();
        var resumen = new CasosObservadosResumen
        {
            TotalCasos = 0,
            CasosPendientes = 0,
            CasosRevisados = 0
        };

        const string mensaje = "Casos observados obtenidos correctamente.";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Fin de metodo [mensaje:{mensaje}]");

        return Task.FromResult((data, resumen, true, mensaje));
    }

    public Task<(bool Success, string Mensaje)> ProcesarCasosObservados(string LogTransaccionId, string Usuario, int LCicloId)
    {
        string nombreMetodo = "ProcesarCasosObservados()";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Inicio de metodo [Usuario:{Usuario}, LCicloId:{LCicloId}]");

        // Punto de extension para conectar la logica real de casos observados.
        const string mensaje = "Paso de casos observados procesado correctamente.";

        _log.Info(LogTransaccionId, NOMBREARCHIVO, nombreMetodo, $"Fin de metodo [mensaje:{mensaje}]");

        return Task.FromResult((true, mensaje));
    }
}
