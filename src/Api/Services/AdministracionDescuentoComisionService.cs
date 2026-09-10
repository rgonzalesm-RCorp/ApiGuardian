using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;

namespace CleanDapperApi.Api.Services;

public sealed class AdministracionDescuentoComisionService : IAdministracionDescuentoComisionService
{
    private readonly IAdministracionDescuentoComisionRepository _repository;

    public AdministracionDescuentoComisionService(
        IAdministracionDescuentoComisionRepository repository
    ) => _repository = repository;

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerAsync(
        int contactoId,
        int cicloId,
        int semanaId,
        string id
    )
    {
        try
        {
            var comision = await _repository.GetComision(id, contactoId, cicloId, semanaId);
            var detalle = await _repository.GetDetalleDescuentoCiclo(id, cicloId, contactoId);
            return (
                comision.Success,
                comision.Mensaje,
                new { comision = comision.Data, detalleDescuento = detalle.Data }
            );
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje)> EliminarAsync(
        int descuentoDetalleId,
        int contactoId,
        int cicloId,
        string? usuario,
        string id
    )
    {
        try
        {
            var r = await _repository.EliminarDescuento(
                id,
                descuentoDetalleId,
                contactoId,
                cicloId,
                usuario
            );
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Mensaje)> InsertarAsync(DataDescuento data, string id)
    {
        try
        {
            var r = await _repository.InsertarDescuento(id, data);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
