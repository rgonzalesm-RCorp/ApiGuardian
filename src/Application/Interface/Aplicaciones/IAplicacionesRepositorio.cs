using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IAplicacionesRepositorio
{
    Task<(RespuestaVistaPreviaAplicaciones Datos, bool Exito, string Mensaje)> VistaPrevia(string logTransaccionId, int lCicloId);
    Task<(RespuestaEjecucionAplicaciones Datos, bool Exito, string Mensaje)> Aplicar(string logTransaccionId, int lCicloId);
}
