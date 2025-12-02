using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IAdministracionSemanaRepository
{
    Task<(IEnumerable<AdministracionSemana> Semana, bool Success, string Mensaje)> GetSemana(string LogTransaccionId);

    Task<(IEnumerable<AdministracionSemana> Semana, bool Success, string Mensaje, int Total)>
        GetSemanaPagination(string LogTransaccionId, int page, int pageSize, string? search);

    Task<(bool Success, string Mensaje)> GuardarSemana(string LogTransaccionId, AdministracionSemana data);
    Task<(bool Success, string Mensaje)> ModificarSemana(string LogTransaccionId, AdministracionSemana data);
    Task<(bool Success, string Mensaje)> EliminarSemana(string LogTransaccionId, int LSemanaId);
}
