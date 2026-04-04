using ExcelDoc.Server.Background;

namespace ExcelDoc.Server.Services.Interfaces
{
    public interface IProcessamentoWorkerService
    {
        Task ProcessAsync(ProcessamentoQueueItem item, CancellationToken cancellationToken = default);
    }
}
