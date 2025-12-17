

using ApiGuardian.Application.Interfaces; // 👈 debe coincidir con tu namespace real
using ApiGuardian.Infrastructure.Repositories;
using ApiGuardian.Infrastructure.Persistence;
using ApiGuardian.Infrastructure.Services;



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


// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<DapperContext>();
builder.Services.AddScoped<IAdministracionContactoRepository, AdministracionContactoRepository>();
builder.Services.AddScoped<IUtilsRepository, UtilsRepository>();
builder.Services.AddScoped<IAdministracionContratoRepository, AdministracionContratoRepository>();
builder.Services.AddScoped<IAdministracionCicloFacturaRepository, AdministracionCicloFacturaRepository>();
builder.Services.AddScoped<IAdministracionObservacionComisionRepository, AdministracionObservacionComisionRepository>();
builder.Services.AddScoped<IAdministracionBuscarAsesorRepository, AdministracionBuscarAsesorRepository>();
builder.Services.AddScoped<IAdministracionCuentaBancoRepository, AdministracionCuentaBancoRepository>();
builder.Services.AddScoped<IAdministracionBancoRepository, AdministracionBancoRepository>();
builder.Services.AddScoped<IAdministracionDescuentoComisionRepository, AdministracionDescuentoComisionRepository>();
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
builder.Services.AddSingleton<ILogService, LogService>();

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
