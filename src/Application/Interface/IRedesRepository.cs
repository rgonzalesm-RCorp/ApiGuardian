using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IRedesRepository
{
    Task<(bool Success, string Mensaje, IEnumerable<ItemContactoActivo> ListadoContactosActivos)> GetObetenerContactoVentasMes(string LogTransaccionId, string Usuario, string Inicio, string Fin);
    Task<(bool Success, string Mensaje)> GuardarRedComprimida(string LogTransaccionId, string Usuario, List<ItemContactoRedComprimida> Listado);
    Task<(bool Success, string Mensaje, int PatrocinadorId)> GetObetenerPatrocinador(string LogTransaccionId, string Usuario, int LContactoId);

}
