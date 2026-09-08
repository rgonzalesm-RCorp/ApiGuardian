

using ApiGuardian.Application.Interfaces;
using ApiGuardian.Domain.DTO;
using ApiGuardian.Infrastructure.Repositories;
using ApiGuardian.Infrastructure.Persistence;
using ApiGuardian.Infrastructure.Services;
using CleanDapperApi.Api.Services;
using Quartz;



var builder = WebApplication.CreateBuilder(args);
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;


if (builder.Environment.IsProduction())
{
    builder.WebHost.UseUrls("http://0.0.0.0:5000");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
/*
builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey("MiCronJob");

    q.AddJob<MiCronJob>(opts => opts.WithIdentity(jobKey));

    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("MiCronJob-trigger")
        .WithCronSchedule("0 0/5 * * * ?") // cada 5 minutos
    );
});

builder.Services.AddQuartzHostedService(q =>
{
    q.WaitForJobsToComplete = true;
});
*/


// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
//builder.Services.AddHostedService<MiCronJob>();

builder.Services.AddSingleton<DapperContext>();
builder.Services.AddSingleton<DapperContextSqlServer>();
builder.Services.AddSingleton<DapperContextSqlServer64>();
builder.Services.AddSingleton<CambioDolarService>();
builder.Services.Configure<MonteSionOpciones>(builder.Configuration.GetSection("MonteSion"));
builder.Services.Configure<PagoComisionOpciones>(builder.Configuration.GetSection("PagoComision"));
builder.Services.AddScoped<IAdministracionContactoRepository, AdministracionContactoRepository>();
builder.Services.AddScoped<IUtilsRepository, UtilsRepository>();
builder.Services.AddScoped<IAdministracionContratoRepository, AdministracionContratoRepository>();
builder.Services.AddScoped<IAdministracionCicloFacturaRepository, AdministracionCicloFacturaRepository>();
builder.Services.AddScoped<IAdministracionHabilitacionComisionRepository, AdministracionHabilitacionComisionRepository>();
builder.Services.AddScoped<IAdministracionObservacionComisionRepository, AdministracionObservacionComisionRepository>();
builder.Services.AddScoped<IAdministracionBuscarAsesorRepository, AdministracionBuscarAsesorRepository>();
builder.Services.AddScoped<IAdministracionCuentaBancoRepository, AdministracionCuentaBancoRepository>();
builder.Services.AddScoped<IAdministracionBancoRepository, AdministracionBancoRepository>();
builder.Services.AddScoped<IAdministracionDescuentoComisionRepository, AdministracionDescuentoComisionRepository>();
builder.Services.AddScoped<IMigracionAplicacionesProrrateoRepository, MigracionAplicacionesProrrateoRepository>();
builder.Services.AddScoped<IAdministracionNivelRepository, AdministracionNivelRepository>();
builder.Services.AddScoped<IAdministracionCicloRepository, AdministracionCicloRepository>();
builder.Services.AddScoped<IAdministracionComplejoRepository, AdministracionComplejoRepository>();
builder.Services.AddScoped<IAdministracionTipoContratoRepository, AdministracionTipoContratoRepository>();
builder.Services.AddScoped<IAdministracionDescuentoCicloTipoRepository, AdministracionDescuentoCicloTipoRepository>();
builder.Services.AddScoped<IAdministracionSemanaRepository, AdministracionSemanaRepository>();
builder.Services.AddScoped<IAdministracionEmpresaRepository, AdministracionEmpresaRepository>();
builder.Services.AddScoped<IAdministracionTipoContactoRepository, AdministracionTipoContactoRepository>();
builder.Services.AddScoped<IAdministracionSemanaCicloRepository, AdministracionSemanaCicloRepository>();
builder.Services.AddScoped<IAdministracionDetalleFacturaRepository, AdministracionDetalleFacturaRepository>();
builder.Services.AddScoped<IReportesRepository, ReportesRepository>();
builder.Services.AddScoped<IVentasCnxRepository, VentaCnxRepository>();
builder.Services.AddScoped<IProcesoComisionesRepository, ProcesoComisionesRepository>();
builder.Services.AddScoped<IConfiguracionProcesoComisionesRepository, ConfiguracionProcesoComisionesRepository>();
builder.Services.AddScoped<IAdministracionVentaPersonalRepository, AdministracionVentaPersonalRepository>();
builder.Services.AddScoped<IAdministracionVentaGrupoRepository, AdministracionVentaGrupoRepository>();
builder.Services.AddScoped<IControlProcesoRepository, ControlProcesoRepository>();
builder.Services.AddScoped<IBonoResidualRepository, BonoResidualRepository>();
builder.Services.AddScoped<IBrConfiguracionRepository, BrConfiguracionRepository>();
builder.Services.AddScoped<IAdministracionBonoResidualRepository, AdministracionBonoResidualRepository>();
builder.Services.AddScoped<IRetencionEmpresaRepository, RetencionEmpresaRepository>();
builder.Services.AddScoped<IRedesRepository, RedesRepository>();
builder.Services.AddScoped<IMonteSionRepository, MonteSionRepository>();
builder.Services.AddScoped<IBonoParRepository, BonoParRepository>();
builder.Services.AddScoped<ICuotasVentaResidualRepository, CuotasVentaResidualRepository>();
builder.Services.AddScoped<ICasosEspecialesRepository, CasosEspecialesRepository>();
builder.Services.AddScoped<IProcesoComisionesService, ProcesoComisionesService>();
builder.Services.AddScoped<IBonoResidualService, BonoResidualService>();
builder.Services.AddScoped<IAdministracionBuscarAsesorService, AdministracionBuscarAsesorService>();
builder.Services.AddScoped<IAdministracionCicloFacturaService, AdministracionCicloFacturaService>();
builder.Services.AddScoped<IAdministracionBancoService, AdministracionBancoService>();
builder.Services.AddScoped<IAdministracionComplejoService, AdministracionComplejoService>();
builder.Services.AddScoped<IAdministracionContactoService, AdministracionContactoService>();
builder.Services.AddScoped<IAdministracionCuentaBancoService, AdministracionCuentaBancoService>();
builder.Services.AddScoped<IAdministracionDescuentoCicloTipoService, AdministracionDescuentoCicloTipoService>();
builder.Services.AddScoped<IAdministracionDescuentoComisionService, AdministracionDescuentoComisionService>();
builder.Services.AddScoped<IAdministracionDetalleFacturaService, AdministracionDetalleFacturaService>();
builder.Services.AddScoped<IAdministracionEmpresaService, AdministracionEmpresaService>();
builder.Services.AddScoped<IAdministracionHabilitacionComisionService, AdministracionHabilitacionComisionService>();
builder.Services.AddScoped<IAdministracionNivelService, AdministracionNivelService>();
builder.Services.AddScoped<IAdministracionObservacionComisionService, AdministracionObservacionComisionService>();
builder.Services.AddScoped<IAdministracionSemanaCicloService, AdministracionSemanaCicloService>();
builder.Services.AddScoped<IAdministracionSemanaService, AdministracionSemanaService>();
builder.Services.AddScoped<IAdministracionTipoContactoService, AdministracionTipoContactoService>();
builder.Services.AddScoped<IAdministracionCicloService, AdministracionCicloService>();
builder.Services.AddScoped<IAdministracionContratoService, AdministracionContratoService>();
builder.Services.AddScoped<IAdministracionTipoContratoService, AdministracionTipoContratoService>();
builder.Services.AddScoped<ICuotasVentaResidualService, CuotasVentaResidualService>();
builder.Services.AddScoped<IRetencionEmpresaService, RetencionEmpresaService>();
builder.Services.AddScoped<IRedesService, RedesService>();
builder.Services.AddScoped<IMonteSionService, MonteSionService>();
builder.Services.AddScoped<IReportesService, ReportesService>();
builder.Services.AddScoped<ICasosObservadosService, CasosObservadosService>();
builder.Services.AddScoped<IControlProcesoService, ControlProcesoService>();
builder.Services.AddScoped<ICasosEspecialesService, CasosEspecialesService>();
builder.Services.AddScoped<IUtilsService, UtilsService>();
builder.Services.AddScoped<IBrConfiguracionService, BrConfiguracionService>();
builder.Services.AddScoped<IConfiguracionProcesoComisionesService, ConfiguracionProcesoComisionesService>();
builder.Services.AddScoped<IAplicacionesService, AplicacionesService>();
builder.Services.AddScoped<ICasosObservadosRepository, CasosObservadosRepository>();
builder.Services.AddScoped<IAplicacionesRepositorio, AplicacionesRepositorio>();

builder.Services.AddSingleton<ILogService, LogService>();

builder.Services.AddScoped<MiCronJob>();

var app = builder.Build();
// 2. Usar CORS
app.UseCors("AllowReactApp");
// Middleware
//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
