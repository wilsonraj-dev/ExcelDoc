using ExcelDoc.Server.Models;

namespace ExcelDoc.Server.Repositories.Interfaces
{
    public interface IDocumentoRepository
    {
        Task<IReadOnlyCollection<Documento>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<Documento?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<Documento?> GetForProcessingAsync(int id, CancellationToken cancellationToken = default);
    }
}
