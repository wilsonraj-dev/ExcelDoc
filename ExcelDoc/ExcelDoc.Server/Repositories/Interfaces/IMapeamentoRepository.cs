using ExcelDoc.Server.Models;

namespace ExcelDoc.Server.Repositories.Interfaces
{
    public interface IMapeamentoRepository
    {
        Task<Colecao?> GetColecaoByIdAsync(int colecaoId, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<Mapeamento>> GetMapeamentosByColecaoIdAsync(int colecaoId, CancellationToken cancellationToken = default);

        Task<Mapeamento?> GetMapeamentoByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<MapeamentoCampo?> GetCampoByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<MapeamentoCampo>> GetCamposByMapeamentoIdAsync(int mapeamentoId, CancellationToken cancellationToken = default);

        Task<bool> ExistsIndiceNoMapeamentoAsync(int mapeamentoId, int indiceColuna, int? ignoreId = null, CancellationToken cancellationToken = default);

        Task AddMapeamentoAsync(Mapeamento mapeamento, CancellationToken cancellationToken = default);

        Task AddCampoAsync(MapeamentoCampo campo, CancellationToken cancellationToken = default);

        void RemoveMapeamento(Mapeamento mapeamento);

        void RemoveCampo(MapeamentoCampo campo);

        Task ReplaceCamposAsync(
            Mapeamento mapeamento,
            IReadOnlyCollection<MapeamentoCampo> campos,
            CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
