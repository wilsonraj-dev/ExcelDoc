using ExcelDoc.Server.Background.Interfaces;
using ExcelDoc.Server.Options;
using ExcelDoc.Server.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace ExcelDoc.Server.Background
{
    public class QueuedProcessingHostedService : BackgroundService
    {
        private readonly IBackgroundTaskQueue _queue;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<QueuedProcessingHostedService> _logger;
        private readonly ProcessingOptions _options;

        public QueuedProcessingHostedService(
            IBackgroundTaskQueue queue,
            IServiceScopeFactory serviceScopeFactory,
            IOptions<ProcessingOptions> options,
            ILogger<QueuedProcessingHostedService> logger)
        {
            _queue = queue;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var item = await _queue.DequeueAsync(stoppingToken);

                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var worker = scope.ServiceProvider.GetRequiredService<IProcessamentoWorkerService>();

                    await worker.ProcessAsync(item, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar job {ProcessamentoId} na tentativa {Attempt}", item.ProcessamentoId, item.Attempt + 1);

                    if (item.Attempt + 1 < _options.MaxRetries)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                        await _queue.EnqueueAsync(new ProcessamentoQueueItem
                        {
                            ProcessamentoId = item.ProcessamentoId,
                            FilePath = item.FilePath,
                            Attempt = item.Attempt + 1
                        }, stoppingToken);

                        continue;
                    }

                    using var scope = _serviceScopeFactory.CreateScope();
                    var processamentoService = scope.ServiceProvider.GetRequiredService<IProcessamentoService>();
                    await processamentoService.MarcarErroFinalAsync(item.ProcessamentoId, ex, stoppingToken);
                }
            }
        }
    }
}
