using System.Threading.Channels;
using System.Collections.Concurrent;
using ApiGuardian.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CleanDapperApi.Api.Services;

public interface IAplicacionesBackgroundQueue
{
    void Encolar(int cicloId);
    ValueTask<int> DesencolarAsync(CancellationToken cancellationToken);
    bool EstaEnProceso(int cicloId);
    void Finalizar(int cicloId);
}

public sealed class AplicacionesBackgroundQueue : IAplicacionesBackgroundQueue
{
    private readonly Channel<int> _cola = Channel.CreateUnbounded<int>(new UnboundedChannelOptions { SingleReader = true });
    private readonly ConcurrentDictionary<int, byte> _ciclosEnProceso = new();

    public void Encolar(int cicloId)
    {
        _ciclosEnProceso[cicloId] = 0;
        _cola.Writer.TryWrite(cicloId);
    }

    public ValueTask<int> DesencolarAsync(CancellationToken cancellationToken) =>
        _cola.Reader.ReadAsync(cancellationToken);

    public bool EstaEnProceso(int cicloId) => _ciclosEnProceso.ContainsKey(cicloId);

    public void Finalizar(int cicloId) => _ciclosEnProceso.TryRemove(cicloId, out _);
}

public sealed class AplicacionesBackgroundService : BackgroundService
{
    private readonly IAplicacionesBackgroundQueue _cola;
    private readonly IServiceScopeFactory _scopeFactory;

    public AplicacionesBackgroundService(IAplicacionesBackgroundQueue cola, IServiceScopeFactory scopeFactory)
    {
        _cola = cola;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var cicloId = await _cola.DesencolarAsync(stoppingToken);
            using var scope = _scopeFactory.CreateScope();
            var aplicaciones = scope.ServiceProvider.GetRequiredService<IAplicacionesService>();
            try
            {
                await aplicaciones.EjecutarEnSegundoPlanoAsync(cicloId);
            }
            finally
            {
                _cola.Finalizar(cicloId);
            }
        }
    }
}
