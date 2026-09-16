using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.Entities;
using ApiGuardian.Infrastructure.Services.Pdf;
using QuestPDF.Fluent;

namespace CleanDapperApi.Api.Services;

public sealed class AdministracionContratoService : IAdministracionContratoService
{
    private readonly IAdministracionContratoRepository _repository;

    public AdministracionContratoService(IAdministracionContratoRepository repository) =>
        _repository = repository;

    public async Task<(bool Success, string Mensaje, object Data)> ObtenerAsync(
        int page,
        int pageSize,
        string? search,
        int tipoBusqueda,
        DateTime? fechaInicio,
        DateTime? fechaFin,
        string id
    )
    {
        if (!fechaInicio.HasValue || !fechaFin.HasValue)
            return (false, "La fecha de inicio y la fecha de fin son obligatorias.", "");
        if (fechaInicio.Value.Date > fechaFin.Value.Date)
            return (false, "La fecha de inicio no puede ser mayor que la fecha de fin.", "");

        var inicio = fechaInicio.Value.Date;
        var fin = fechaFin.Value.Date;
        var tipoBusquedaNormalizado = tipoBusqueda == 2 ? 2 : 1;
        try
        {
            var contratos = await _repository.GetAllAdministracionContrato(
                id,
                page,
                pageSize,
                search,
                tipoBusquedaNormalizado,
                inicio,
                fin
            );
            if (!contratos.Success)
                return (
                    false,
                    contratos.Mensaje,
                    new
                    {
                        listaContrato = contratos.Data,
                        total = contratos.Total,
                        fileName = "",
                        fileBase64 = "",
                        contentType = "",
                        fileNameXls = "",
                        base64Xls = "",
                    }
                );

            var reporte = await _repository.GetReporteAdministracionContrato(
                id,
                search,
                tipoBusquedaNormalizado,
                inicio,
                fin
            );
            if (!reporte.Success)
                return (false, reporte.Mensaje, "");

            var datos = reporte.Data.ToList();
            var sufijo = $"{inicio:yyyyMMdd}-{fin:yyyyMMdd}";
            var pdf = Convert.ToBase64String(
                new ReporteContratos(datos, inicio, fin).GeneratePdf()
            );
            var xls = new ContratosXls().Generar(datos, inicio, fin);
            return (
                contratos.Success,
                contratos.Mensaje,
                new
                {
                    listaContrato = contratos.Data,
                    total = contratos.Total,
                    fileName = $"REPORTE DE CONTRATOS {sufijo}.pdf",
                    fileBase64 = pdf,
                    contentType = "application/pdf",
                    fileNameXls = $"REPORTE DE CONTRATOS {sufijo}.xlsx",
                    base64Xls = xls.Base64,
                }
            );
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }

    public async Task<(bool Success, string Mensaje)> InsertarAsync(
        AdministracionContrato data,
        string id
    )
    {
        try
        {
            var r = await _repository.InsertContrato(id, data);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Mensaje)> ActualizarAsync(
        AdministracionContrato data,
        string id
    )
    {
        try
        {
            var r = await _repository.UpdateContrato(id, data);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Mensaje)> EliminarAsync(int contratoId, string id)
    {
        if (contratoId <= 0)
            return (false, "El identificador del contrato es obligatorio.");
        try
        {
            var r = await _repository.DeleteContrato(id, contratoId);
            return (r.Success, r.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
