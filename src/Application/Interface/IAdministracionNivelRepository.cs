using System.Data.SqlTypes;
using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IAdministracionNivelRepository
{
    Task<(IEnumerable<AdministracionNivel> Nivel, bool Success, string Mensaje)> GetNivel(string LogTransaccionId);
    Task<(IEnumerable<AdministracionNivel> Nivel, bool Success, string Mensaje, int Total)> GetNivelPagination(string LogTransaccionId, int page, int pageSize, string? search);
    Task<(bool Success, string Mensaje)> GuardarNivel(string LogTransaccionId, AdministracionNivel Nivel);
    Task<(bool Success, string Mensaje)> ModificarNivel(string LogTransaccionId, AdministracionNivel Nivel);
    Task<(bool Success, string Mensaje)> EliminarNivel(string LogTransaccionId, int LNivelId);
}
