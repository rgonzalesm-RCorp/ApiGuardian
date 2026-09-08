namespace ApiGuardian.Application.Interfaces;

public interface IProcesoComisionesService
{
    Task<(bool Success, string Mensaje)> GuardarVentaAsync(
        RequestGuardarVentaGRD request,
        string logTransaccionId);
    Task<(bool Success, string Mensaje, object Data)> ObtenerVentaCnxAsync(int cicloId);
    Task<(bool Success, string Mensaje, object Data)> ObtenerVtaRezagadasAsync(string usuario, int cicloId);
    Task<(bool Success, string Mensaje, object Data)> ObtenerVentaPersonalAsync(string usuario, int cicloId);
    Task<(bool Success, string Mensaje)> GuardarVentaPersonalAsync(RequestSaveVtaPersonal request);
    Task<(bool Success, string Mensaje, object Data)> ObtenerVentaGrupoAsync(string usuario, int cicloId);
    Task<(bool Success, string Mensaje)> GuardarVentaGrupoAsync(RequestGuardarVentaGrupo request);
}

public interface IBonoResidualService
{
    Task<(bool Success, string Mensaje, DateTime? Inicio, DateTime? Fin)> GuardarCarteraAsync(
        string usuario,
        int cicloId,
        string logTransaccionId);

    Task<BonoResidualGuardadoResult> GuardarCuotaAsync(string usuario, int cicloId, string logTransaccionId);

    Task<(bool Success, string Mensaje, List<ItemVentaCnx> ListaExcedente)> GuardarExcedenteAsync(
        string usuario,
        int cicloId,
        string logTransaccionId);

    Task<BonoResidualGuardadoResult> GuardarBonoResidualAsync(
        string usuario,
        int cicloId,
        string logTransaccionId,
        BonoResidualCalculoResult calculo,
        DateTime inicio);
    Task<BonoResidualGuardadoResult> ProcesarBonoResidualAsync(string usuario, int cicloId, string logTransaccionId);

    Task<BonoResidualCalculoResult> ConstruirListadoResidualAsync(string usuario, int cicloId, string logTransaccionId);
    Task<BonoResidualGuardadoResult> ObtenerBonoResidualAsync(string usuario, int cicloId, string logTransaccionId);
    Task<BonoResidualGuardadoResult> ObtenerBonoParAsync(string usuario, int cicloId, string logTransaccionId);
    Task<BonoResidualGuardadoResult> ObtenerCuotaAsync(string usuario, int cicloId, string logTransaccionId);
    Task<BonoResidualGuardadoResult> ObtenerExcedenteAsync(string usuario, int cicloId, string logTransaccionId);
    Task<BonoResidualGuardadoResult> ObtenerCarteraAsync(string usuario, int cicloId, string logTransaccionId);

    Task<BonoResidualGuardadoResult> GuardarBonoParAsync(
        string usuario,
        int cicloId,
        string logTransaccionId);

}

public sealed class BonoResidualCalculoResult
{
    public bool Success { get; init; }
    public string Mensaje { get; init; } = string.Empty;
    public List<BrCalculoItem> ListadoResidual { get; init; } = new();
    public List<ItemHabilitacionComision> PersonasHabilitadas { get; init; } = new();
    public int TotalCuotas { get; init; }
    public int TotalContactos { get; init; }
}

public sealed class BonoResidualGuardadoResult
{
    public bool Success { get; init; }
    public string Mensaje { get; init; } = string.Empty;
    public object Data { get; init; } = string.Empty;
}
