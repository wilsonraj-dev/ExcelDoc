namespace ExcelDoc.Server.Background.Interfaces
{
    public interface IBackgroundTaskQueue
    {
        ValueTask EnqueueAsync(ProcessamentoQueueItem item, CancellationToken cancellationToken = default);

        ValueTask<ProcessamentoQueueItem> DequeueAsync(CancellationToken cancellationToken);
    }
}
