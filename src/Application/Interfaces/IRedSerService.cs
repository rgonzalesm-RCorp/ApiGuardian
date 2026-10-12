namespace ApiGuardian.Application.Interfaces;

public interface IRedSerService
{
    Task<(bool Success, string Mensaje, object Data)> ArmarAsync(
        string usuario,
        DateTime inicio,
        DateTime fin
    );

    Task<(bool Success, string Mensaje, object Data)> ObtenerAsync(DateTime? inicio, DateTime? fin);
}
