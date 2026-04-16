using ExcelDoc.Server.Models;

namespace ExcelDoc.Server.Repositories.Interfaces
{
    public interface IMapeamentoRepository
    {
        Task<IReadOnlyCollection<MapeamentoCampo>> GetByColecaoIdAsync(int colecaoId, CancellationToken cancellationToken = default);

        Task<MapeamentoCampo?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<bool> ExistsIndiceNaColecaoAsync(int colecaoId, int indiceColuna, int? ignoreId = null, CancellationToken cancellationToken = default);

        Task<Colecao?> GetColecaoByIdAsync(int colecaoId, CancellationToken cancellationToken = default);

        Task AddAsync(MapeamentoCampo mapeamento, CancellationToken cancellationToken = default);

        void Remove(MapeamentoCampo mapeamento);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
