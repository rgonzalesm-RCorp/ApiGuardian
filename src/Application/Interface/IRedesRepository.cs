using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IRedesRepository
{
    Task<(bool Success, string Mensaje, IEnumerable<ItemContactoActivo> ListadoContactosActivos)> GetObetenerContactoVentasMes(string LogTransaccionId, string Usuario, string Inicio, string Fin);
    Task<(bool Success, string Mensaje)> GuardarRedComprimida(string LogTransaccionId, string Usuario, List<ItemContactoRed> Listado); 
    Task<(bool Success, string Mensaje, int PatrocinadorId)> GetObetenerPatrocinador(string LogTransaccionId, string Usuario, int LContactoId);
    

    Task<(bool Success, string Mensaje, IEnumerable<ItemCuotasRed> ListadoContactosCuotas, IEnumerable<BrContacto> ListaContacto)> GetObtnerClientesCuotas(string LogTransaccionId, string Usuario);
    Task<(bool Success, string Mensaje, List<RedContacto> ListadoContactosCuotas)> GetRedCotactoAll(string LogTransaccionId, string Usuario);
    Task<(bool Success, string Mensaje)> GuardarRedContactoTemporal(string LogTransaccionId, string Usuario, List<ItemRedSieteNiveles> ListadoContactosCuotas);
}
