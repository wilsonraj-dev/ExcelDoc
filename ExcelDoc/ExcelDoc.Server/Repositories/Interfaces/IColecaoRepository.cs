using ExcelDoc.Server.Models;

namespace ExcelDoc.Server.Repositories.Interfaces
{
    public interface IColecaoRepository
    {
        Task<IReadOnlyCollection<Colecao>> GetByEmpresaIdAsync(int empresaId, CancellationToken cancellationToken = default);

        Task<Colecao?> GetByIdWithMappingsAsync(int id, CancellationToken cancellationToken = default);

        Task AddAsync(Colecao colecao, CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
