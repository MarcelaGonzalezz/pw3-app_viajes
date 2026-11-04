using TpMiercoles.Servicios;

namespace TpMiercoles.Web.Infraestructura
{
    // Materializa el viaje de una salida cuando llega la hora, aunque no se haya
    // aceptado ningun envio. Se ejecuta periodicamente mientras la app esta levantada.
    public class MaterializacionDeViajesHostedService : BackgroundService
    {
        private static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(1);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MaterializacionDeViajesHostedService> _logger;

        public MaterializacionDeViajesHostedService(
            IServiceScopeFactory scopeFactory,
            ILogger<MaterializacionDeViajesHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using IServiceScope scope = _scopeFactory.CreateScope();
                    IMaterializadorDeViajes materializador =
                        scope.ServiceProvider.GetRequiredService<IMaterializadorDeViajes>();

                    await materializador.MaterializarSalidasAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al materializar viajes automaticamente.");
                }

                try
                {
                    await Task.Delay(Intervalo, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}
