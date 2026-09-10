using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IRetencionEmpresaService
{
    Task<(bool Success, string Mensaje, object Data)> VerAsync(int cicloId);
    Task<(bool Success, string Mensaje, object Data)> GuardarAsync(int cicloId);
}
