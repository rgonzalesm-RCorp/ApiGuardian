using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface ICasosObservadosRepository
{
    Task<(IEnumerable<ItemCasoObservado> Data, CasosObservadosResumen Resumen, bool Success, string Mensaje)> GetCasosObservados(string LogTransaccionId, string Usuario, int LCicloId, DateTime fechaInicio, DateTime fechaFin);
    Task<(bool Success, string Mensaje)> ProcesarCasosObservados(string LogTransaccionId, string Usuario, int LCicloId);
}
