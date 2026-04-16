using ExcelDoc.Server.Models;

namespace ExcelDoc.Server.Repositories.Interfaces
{
    public interface IDocumentoRepository
    {
        Task<IReadOnlyCollection<Documento>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<Documento?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<Documento?> GetTrackedByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<Documento?> GetForProcessingAsync(int id, CancellationToken cancellationToken = default);

        Task<bool> ExistsByNomeOrEndpointAsync(string nomeDocumento, string endpoint, int? ignoreId = null, CancellationToken cancellationToken = default);

        Task AddAsync(Documento documento, CancellationToken cancellationToken = default);

        void Remove(Documento documento);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
