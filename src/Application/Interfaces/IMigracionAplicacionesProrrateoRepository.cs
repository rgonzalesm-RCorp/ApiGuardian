using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IMigracionAplicacionesProrrateoRepository
{
    Task<(ResultadoMigracionAplicacionesProrrateo Datos, bool Exito, string Mensaje)> VistaPreviaAsync(SolicitudMigracionAplicacionesProrrateo solicitud);
    Task<(ResultadoMigracionAplicacionesProrrateo Datos, bool Exito, string Mensaje)> EjecutarAsync(SolicitudMigracionAplicacionesProrrateo solicitud);
    Task<(ResultadoMigracionAplicacionesProrrateo Datos, bool Exito, string Mensaje)> EjecutarDesdeCeroAsync(SolicitudMigracionAplicacionesProrrateo solicitud);
}
