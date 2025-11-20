

using ApiGuardian.Application.Interfaces; // 👈 debe coincidir con tu namespace real
using ApiGuardian.Infrastructure.Repositories;
using ApiGuardian.Infrastructure.Persistence;
using ApiGuardian.Infrastructure.Services;


var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5000");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy
            .WithOrigins("http://10.2.10.41:5000") // URL de tu frontend
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
builder.Services.AddSingleton<ILogService, LogService>();

var app = builder.Build();
// 2. Usar CORS
app.UseCors("AllowReactApp");
// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
