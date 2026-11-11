using ApiGuardian.Application.Interfaces;
using ApiGuardian.Infrastructure.Persistence;

namespace ApiGuardian.Infrastructure.Repositories.Simulacion;

// Repositorio del proceso light. Una parte por proceso (fases 2 a 8).
// El SQL se copia de los repositorios actuales; no se inyectan repositorios de escritura del proceso actual (R2).
public sealed partial class SimulacionRepositorio
{
    private const string NOMBRE_ARCHIVO = "SimulacionRepositorio.cs";

    private readonly DapperContext _context;
    private readonly ILogService _log;

    public SimulacionRepositorio(DapperContext context, ILogService log)
    {
        _context = context;
        _log = log;
    }
}
