using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IAdministracionHabilitacionComisionRepository
{
    Task<(IEnumerable<ItemHabilitacionComision> Data, bool Success, string Mensaje)> GetHabilitaciones(
        string LogTransaccionId,
        string Usuario,
        int LCicloId
    );

    Task<(bool Success, string Mensaje)> SaveHabilitaciones(
        string LogTransaccionId,
        string Usuario,
        int LCicloId,
        List<ItemHabilitacionComision> Listado
    );

    Task<(bool Success, string Mensaje)> UpdateHabilitacion(
        string LogTransaccionId,
        string Usuario,
        ItemHabilitacionComision Data
    );

    Task<(bool Success, string Mensaje)> DeleteHabilitacion(
        string LogTransaccionId,
        string Usuario,
        int LHabilitacionId
    );
}
