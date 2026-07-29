using ExcelDoc.Server.Background.Interfaces;
using ExcelDoc.Server.Options;
using ExcelDoc.Server.Sap;
using ExcelDoc.Server.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace ExcelDoc.Server.Background;

public sealed class QueuedProcessingHostedService : BackgroundService
{
    private readonly IBackgroundTaskQueue _queue;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ISapServiceLayerClient _sapClient;
    private readonly ISapSessionStore _sessionStore;
    private readonly ILogger<QueuedProcessingHostedService> _logger;
    private readonly ProcessingOptions _options;

    public QueuedProcessingHostedService(
        IBackgroundTaskQueue queue,
        IServiceScopeFactory serviceScopeFactory,
        ISapServiceLayerClient sapClient,
        ISapSessionStore sessionStore,
        IOptions<ProcessingOptions> options,
        ILogger<QueuedProcessingHostedService> logger)
    {
        _queue = queue;
        _serviceScopeFactory = serviceScopeFactory;
        _sapClient = sapClient;
        _sessionStore = sessionStore;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            ProcessamentoQueueItem item;
            try
            {
                item = await _queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                var finalException = await ProcessWithRetriesAsync(
                    item,
                    stoppingToken);
                if (finalException is not null)
                {
                    await TryMarkFinalErrorAsync(
                        item,
                        finalException,
                        stoppingToken);
                }
            }
            finally
            {
                await DeleteUploadedFileAsync(item);
                await ReleaseLeaseAsync(item);
            }
        }
    }

    private async Task<Exception?> ProcessWithRetriesAsync(
        ProcessamentoQueueItem item,
        CancellationToken stoppingToken)
    {
        Exception? lastException = null;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var sessionAccessor = scope.ServiceProvider
                    .GetRequiredService<ISapSessionContextAccessor>();
                sessionAccessor.SetJobSessionKey(item.SessionKey);
                var worker = scope.ServiceProvider
                    .GetRequiredService<IProcessamentoWorkerService>();

                await worker.ProcessAsync(item, stoppingToken);
                return null;
            }
            catch (Exception exception)
            {
                lastException = exception;
                _logger.LogError(
                    exception,
                    "Erro ao processar job {ProcessamentoId} na tentativa {Attempt}.",
                    item.ProcessamentoId,
                    item.Attempt + 1);

                if (exception is SapSessionExpiredException ||
                    item.Attempt + 1 >= _options.MaxRetries)
                {
                    return exception;
                }

                item.Attempt++;
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    return lastException;
                }
            }
        }

        return lastException;
    }

    private async Task TryMarkFinalErrorAsync(
        ProcessamentoQueueItem item,
        Exception exception,
        CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var sessionAccessor = scope.ServiceProvider
                .GetRequiredService<ISapSessionContextAccessor>();
            sessionAccessor.SetJobSessionKey(item.SessionKey);
            var processingService = scope.ServiceProvider
                .GetRequiredService<IProcessamentoService>();
            await processingService.MarcarErroFinalAsync(
                item.ProcessamentoId,
                exception,
                stoppingToken);
        }
        catch (Exception finalizationException)
        {
            _logger.LogError(
                finalizationException,
                "Falha ao registrar o erro final do processamento {ProcessamentoId}; o worker continuará ativo.",
                item.ProcessamentoId);
        }
    }

    private async Task ReleaseLeaseAsync(ProcessamentoQueueItem item)
    {
        var sessionToLogout = _sessionStore.ReleaseJob(item.SessionKey);
        if (sessionToLogout is null)
        {
            return;
        }

        try
        {
            await _sapClient.LogoutAsync(sessionToLogout, CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogDebug(
                exception,
                "Falha no logout SAP adiado após finalizar o processamento {ProcessamentoId}.",
                item.ProcessamentoId);
        }
    }

    private async Task DeleteUploadedFileAsync(ProcessamentoQueueItem item)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var storage = scope.ServiceProvider
                .GetRequiredService<IArquivoStorageService>();
            await storage.DeleteAsync(item.FilePath, CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Falha ao remover o arquivo temporário do processamento {ProcessamentoId}: {FilePath}.",
                item.ProcessamentoId,
                item.FilePath);
        }
    }
}
