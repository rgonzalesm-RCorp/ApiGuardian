using ApiGuardian.Domain.Entities;

namespace ApiGuardian.Application.Interfaces;

public interface IAdministracionBuscarAsesorService
{
    Task<(bool Success, string Mensaje, object Data)> ObtenerAsesoresAsync(int contactoId, string logTransaccionId);
}

public interface IAdministracionCicloFacturaService
{
    Task<(bool Success, string Mensaje, object Data)> ObtenerAsync(int page, int pageSize, int cicloId, string logTransaccionId);
    Task<(bool Success, string Mensaje)> InsertarAsync(AdministracionCicloFactura data, string logTransaccionId);
    Task<(bool Success, string Mensaje)> EliminarAsync(int cicloFacturaId, string? usuario, string logTransaccionId);
}

public interface IAdministracionBancoService
{
    Task<(bool Success, string Mensaje, object Data)> ObtenerBancosAsync(string logTransaccionId);
    Task<(bool Success, string Mensaje, object Data)> ObtenerMonedasAsync(string logTransaccionId);
    Task<(bool Success, string Mensaje)> InsertarAsync(AdministracionBanco data, string logTransaccionId);
    Task<(bool Success, string Mensaje)> ActualizarAsync(AdministracionBanco data, string logTransaccionId);
    Task<(bool Success, string Mensaje)> EliminarAsync(int bancoId, string? usuario, string logTransaccionId);
}

public interface IAdministracionComplejoService
{
    Task<(bool Success, string Mensaje, object Data)> ObtenerAsync(string id);
    Task<(bool Success, string Mensaje, object Data)> ObtenerPaginadoAsync(int page, int pageSize, string? search, string id);
    Task<(bool Success, string Mensaje)> InsertarAsync(AdministracionComplejoABM data, string id);
    Task<(bool Success, string Mensaje)> ActualizarAsync(AdministracionComplejoABM data, string id);
    Task<(bool Success, string Mensaje)> EliminarAsync(int complejoId, string id);
}

public interface IAdministracionContactoService
{
    Task<(bool Success, string Mensaje, object Data)> ObtenerAsync(int page, int pageSize, string? search, string id);
    Task<(bool Success, string Mensaje)> InsertarAsync(AdministracionContacto data, string id);
    Task<(bool Success, string Mensaje)> ActualizarAsync(AdministracionContacto data, string id);
    Task<(bool Success, string Mensaje)> DarDeBajaAsync(AdministracionContactoBaja data, string id);
    Task<(bool Success, string Mensaje, object Data)> VerificarEstadoAsync(string usuario, string documento, string id);
}

public interface IAdministracionCuentaBancoService
{
    Task<(bool Success, string Mensaje, object Data)> ObtenerAsync(int contactoId, string id);
    Task<(bool Success, string Mensaje, object Data)> ObtenerPaginadoAsync(int page, int pageSize, string? search, string id);
    Task<(bool Success, string Mensaje)> ActualizarAsync(DataCuentaBanco data, string id);
}

public interface IAdministracionDescuentoCicloTipoService
{
    Task<(bool Success, string Mensaje, object Data)> ObtenerPaginadoAsync(int page, int pageSize, string? search, string id);
    Task<(bool Success, string Mensaje)> InsertarAsync(AdministracionDescuentoCicloTipo data, string id);
    Task<(bool Success, string Mensaje)> ActualizarAsync(AdministracionDescuentoCicloTipo data, string id);
    Task<(bool Success, string Mensaje)> EliminarAsync(int descuentoCicloTipoId, string id);
}

public interface IAdministracionDescuentoComisionService
{
    Task<(bool Success, string Mensaje, object Data)> ObtenerAsync(int contactoId, int cicloId, int semanaId, string id);
    Task<(bool Success, string Mensaje)> EliminarAsync(int descuentoDetalleId, int contactoId, int cicloId, string? usuario, string id);
    Task<(bool Success, string Mensaje)> InsertarAsync(DataDescuento data, string id);
}

public interface IAdministracionDetalleFacturaService
{
    Task<(bool Success, string Mensaje, object Data)> ObtenerPaginadoAsync(int page, int pageSize, string id);
    Task<(bool Success, string Mensaje)> InsertarAsync(AdministracionDetalleFactura data, string id);
    Task<(bool Success, string Mensaje)> ActualizarAsync(AdministracionDetalleFactura data, string id);
    Task<(bool Success, string Mensaje)> EliminarAsync(int detalleFacturaId, string id);
    Task<(bool Success, string Mensaje, object Data)> ObtenerTiposComisionAsync(string id);
}

public interface IAdministracionEmpresaService
{
    Task<(bool Success, string Mensaje, object Data)> ObtenerAsync(string id);
    Task<(bool Success, string Mensaje, object Data)> ObtenerPaginadoAsync(int page, int pageSize, string? search, string id);
    Task<(bool Success, string Mensaje)> InsertarAsync(AdministracionEmpresa data, string id);
    Task<(bool Success, string Mensaje)> ActualizarAsync(AdministracionEmpresa data, string id);
    Task<(bool Success, string Mensaje)> EliminarAsync(int empresaId, string id);
}

public interface IAdministracionHabilitacionComisionService
{
    Task<(bool Success, string Mensaje, object Data)> ObtenerAsync(string? logId, string usuario, int cicloId);
    Task<(bool Success, string Mensaje)> GuardarAsync(string? logId, string usuario, int cicloId, List<ItemHabilitacionComision> listado);
    Task<(bool Success, string Mensaje)> ActualizarAsync(string? logId, string usuario, ItemHabilitacionComision data);
    Task<(bool Success, string Mensaje)> EliminarAsync(string? logId, string usuario, int habilitacionId);
}

public interface IAdministracionNivelService
{
    Task<(bool Success, string Mensaje, object Data)> ObtenerAsync(string id);
    Task<(bool Success, string Mensaje, object Data)> ObtenerPaginadoAsync(int page, int pageSize, string? search, string id);
    Task<(bool Success, string Mensaje)> InsertarAsync(AdministracionNivel data, string id);
    Task<(bool Success, string Mensaje)> ActualizarAsync(AdministracionNivel data, string id);
    Task<(bool Success, string Mensaje)> EliminarAsync(int nivelId, string id);
}

public interface IAdministracionObservacionComisionService
{
    Task<(bool Success, string Mensaje, object Data)> ObtenerPaginadoAsync(int page, int pageSize, string? search, int cicloId, string id);
    Task<(bool Success, string Mensaje)> InsertarAsync(AdministracionObservacionComision data, string id);
    Task<(bool Success, string Mensaje)> ActualizarAsync(AdministracionObservacionComision data, string id);
    Task<(bool Success, string Mensaje)> EliminarAsync(int observacionId, string? usuario, string id);
}

public interface IAdministracionSemanaCicloService
{
    Task<(bool Success, string Mensaje, object Data)> ObtenerPaginadoAsync(int page, int pageSize, string? search, string id);
    Task<(bool Success, string Mensaje)> InsertarAsync(AdministracionSemanaCicloABM data, string id);
    Task<(bool Success, string Mensaje)> ActualizarAsync(AdministracionSemanaCicloABM data, string id);
    Task<(bool Success, string Mensaje)> EliminarAsync(int semanaId, string id);
}

public interface IAdministracionSemanaService
{
    Task<(bool Success, string Mensaje, object Data)> ObtenerAsync(string id);
    Task<(bool Success, string Mensaje, object Data)> ObtenerPaginadoAsync(int page, int pageSize, string? search, string id);
    Task<(bool Success, string Mensaje)> InsertarAsync(AdministracionSemana data, string id);
    Task<(bool Success, string Mensaje)> ActualizarAsync(AdministracionSemana data, string id);
    Task<(bool Success, string Mensaje)> EliminarAsync(int semanaId, string id);
}

public interface IAdministracionTipoContactoService
{
    Task<(bool Success, string Mensaje, object Data)> ObtenerAsync(string id);
    Task<(bool Success, string Mensaje, object Data)> ObtenerPaginadoAsync(int page, int pageSize, string? search, string id);
    Task<(bool Success, string Mensaje)> InsertarAsync(AdministracionTipoContacto data, string id);
    Task<(bool Success, string Mensaje)> ActualizarAsync(AdministracionTipoContacto data, string id);
    Task<(bool Success, string Mensaje)> EliminarAsync(int tipoContactoId, string id);
}

public interface IAdministracionCicloService
{
    Task<(bool Success, string Mensaje, object Data)> ObtenerAsync(string id);
    Task<(bool Success, string Mensaje, object Data)> ObtenerPaginadoAsync(int page, int pageSize, string? search, string id);
    Task<(bool Success, string Mensaje)> InsertarAsync(AdministracionCicloABM data, string id);
    Task<(bool Success, string Mensaje)> ActualizarAsync(AdministracionCicloABM data, string id);
    Task<(bool Success, string Mensaje)> EliminarAsync(int cicloId, string id);
}

public interface IAdministracionContratoService
{
    Task<(bool Success, string Mensaje, object Data)> ObtenerAsync(int page, int pageSize, string? search, DateTime? fechaInicio, DateTime? fechaFin, string id);
    Task<(bool Success, string Mensaje)> InsertarAsync(AdministracionContrato data, string id);
    Task<(bool Success, string Mensaje)> ActualizarAsync(AdministracionContrato data, string id);
    Task<(bool Success, string Mensaje)> EliminarAsync(int contratoId, string id);
}

public interface IAdministracionTipoContratoService
{
    Task<(bool Success, string Mensaje, object Data)> ObtenerAsync(string id);
    Task<(bool Success, string Mensaje, object Data)> ObtenerPaginadoAsync(int page, int pageSize, string? search, string id);
    Task<(bool Success, string Mensaje)> InsertarAsync(AdministracionTipoContratoABM data, string id);
    Task<(bool Success, string Mensaje)> ActualizarAsync(AdministracionTipoContratoABM data, string id);
    Task<(bool Success, string Mensaje)> EliminarAsync(int tipoContratoId, string id);
}

public interface ICuotasVentaResidualService
{
    Task<(bool Success, string Mensaje, object Data)> ObtenerAsync(string usuario, int cicloId);
    Task<(bool Success, string Mensaje, object Data)> GuardarAsync(string usuario, int cicloId);
}

public interface IRedesService
{
    Task<(bool Success, string Mensaje, object Data)> ObtenerRedComprimidaAsync(string usuario, int cicloId, string inicio, string fin, bool controlPaso = true);
    Task<(bool Success, string Mensaje, object Data)> ObtenerRedCuotasAsync(string usuario, int cicloId);
}

public interface IReportesService
{
    Task<(bool Success, string Mensaje, object Data)> ReporteComisionesAsync(int cicloId, int contactoId);
    Task<(bool Success, string Mensaje, object Data)> ReporteAplicacionesAsync(int cicloId, int contactoId);
    Task<(bool Success, string Mensaje, object Data)> ReporteDescuentoEmpresaAsync(int cicloId, int empresaId);
    Task<(bool Success, string Mensaje, object Data)> ReporteFacturacionAsync(int cicloId, int contactoId);
    Task<(bool Success, string Mensaje, object Data)> ReporteProrrateoAsync(int cicloId);
    Task<(bool Success, string Mensaje, object Data)> ReporteComisionServicioAsync(int cicloId, int empresaId);
    Task<(bool Success, string Mensaje, object Data)> ReportePagarComisionAsync(int cicloId);
    Task<(bool Success, string Mensaje, object Data)> ReportePlanCarreraAsync(int cicloId);
    Task<(bool Success, string Mensaje, object Data)> ReporteAscensoRangoAsync(int cicloId);
}

public interface ICasosObservadosService
{
    Task<(bool Success, string Mensaje, object Data)> ObtenerAsync(string usuario, int cicloId, DateTime? inicio, DateTime? fin);
    Task<(bool Success, string Mensaje, object Data)> ProcesarAsync(string usuario, int cicloId);
}

public interface IControlProcesoService
{
    Task<(bool Success, string Mensaje, object Data)> ObtenerConfiguracionAsync(string usuario);
    Task<(bool Success, string Mensaje, object Data)> GuardarConfiguracionAsync(string usuario, ControlProcesoConfiguracion data);
    Task<(bool Success, string Mensaje, object Data)> EliminarConfiguracionAsync(string usuario, int procesoId);
    Task<(bool Success, string Mensaje, object Data)> ObtenerCicloAsync(string usuario, int cicloId);
    Task<(bool Success, string Mensaje, object Data)> ReiniciarCicloAsync(string usuario, int cicloId, string inicio, string fin);
    Task<(bool Success, string Mensaje, object Data)> CerrarCicloAsync(string usuario, int cicloId);
}

public interface IProcesoFacturacionService
{
    Task<(bool Success, string Mensaje, object Data)> GuardarAsesoresAsync(
        SolicitudGuardarAsesoresFacturacion solicitud
    );
}

public interface ICasosEspecialesService
{
    Task<(bool Success, string Mensaje, object Data)> ObtenerAsync(string usuario, int cicloId, string inicio, string fin);
}

public interface IUtilsService
{
    Task<(bool Success, string Mensaje, object Data)> ObtenerSemanaCicloAsync(int cicloId);
    Task<(bool Success, string Mensaje, object Data)> ObtenerDepartamentoAsync(int paisId);
    Task<(bool Success, string Mensaje, object Data)> ObtenerTipoContratoAsync();
    Task<(bool Success, string Mensaje, object Data)> ObtenerEstadoContratoAsync();
    Task<(bool Success, string Mensaje, object Data)> ObtenerTipoBajaAsync();
    Task<(bool Success, string Mensaje, object Data)> ObtenerPaisAsync();
    Task<(bool Success, string Mensaje, object Data)> ObtenerTipoDescuentoAsync();
}

public interface IBrConfiguracionService
{
    Task<(bool Success, string Mensaje, object Data)> ObtenerDatosAsync(string usuario);
    Task<(bool Success, string Mensaje, object Data)> ObtenerConfiguracionAsync();
    Task<(bool Success, string Mensaje)> GuardarAsync(BrConfiguracion data);
    Task<(bool Success, string Mensaje)> EliminarAsync(string usuario, int configuracionId);
}

public interface IConfiguracionProcesoComisionesService
{
    Task<(bool Success, string Mensaje, object Data, bool Swall)> GuardarAsync(PC_ConfigVtaPersonal data);
    Task<(bool Success, string Mensaje, object Data)> ObtenerAsync();
    Task<(bool Success, string Mensaje)> EliminarAsync(string usuario, int configuracionId);
}

public interface IAplicacionesService
{
    Task<(bool Exito, string Mensaje, object Datos)> VistaPreviaAsync(int cicloId);
    Task<(bool Exito, string Mensaje, object Datos)> IniciarAplicacionAsync(int cicloId);
    Task<(bool Exito, string Mensaje, object Datos)> ReprocesarGrupoSionAsync(int cicloId);
    Task EjecutarEnSegundoPlanoAsync(int cicloId);
    Task<(bool Exito, string Mensaje, object Datos)> ObtenerComisionadosAsync(int cicloId);
}
