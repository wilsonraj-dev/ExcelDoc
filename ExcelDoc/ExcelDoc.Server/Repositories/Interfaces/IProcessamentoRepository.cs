using ExcelDoc.Server.Models;

namespace ExcelDoc.Server.Repositories.Interfaces
{
    public interface IProcessamentoRepository
    {
        Task<bool> ExistsByHashAsync(int empresaId, string hashArquivo, CancellationToken cancellationToken = default);

        Task AddAsync(Processamento processamento, CancellationToken cancellationToken = default);

        Task<Processamento?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<Processamento?> GetForExecutionAsync(int id, CancellationToken cancellationToken = default);

        Task<(IReadOnlyCollection<Processamento> Items, int TotalCount)> GetPagedAsync(int empresaId, StatusProcessamento? status, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        Task<(IReadOnlyCollection<ProcessamentoItem> Items, int TotalCount)> GetItemsPagedAsync(int processamentoId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        Task AddItemAsync(ProcessamentoItem item, CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
