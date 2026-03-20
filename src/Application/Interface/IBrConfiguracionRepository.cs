public interface IBrConfiguracionRepository
{
    Task<(IEnumerable<DetailsBrConfiguracion> Data, bool Success, string Mensaje)>GetConfiguracion(string LogTransaccionId, string Usuario);
    Task<(IEnumerable<BrNiveles> Data, bool Success, string Mensaje)>GetNivel(string LogTransaccionId, string Usuario);
    Task<(IEnumerable<BrTipoProducto> Data, bool Success, string Mensaje)>GetTipoProducto(string LogTransaccionId, string Usuario);
    Task<(bool Success, string Mensaje)>GuardarConfiguracion(string LogTransaccionId, string Usuario, BrConfiguracion data);
    Task<(bool Success, string Mensaje)>EliminarConfiguracion(string LogTransaccionId, string Usuario, int id);
}