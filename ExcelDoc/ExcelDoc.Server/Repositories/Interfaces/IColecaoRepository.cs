using ExcelDoc.Server.Models;

namespace ExcelDoc.Server.Repositories.Interfaces
{
    public interface IColecaoRepository
    {
        Task<IReadOnlyCollection<Colecao>> GetByEmpresaIdAsync(int? empresaId, bool includeAllCompanies, CancellationToken cancellationToken = default);

        Task<Colecao?> GetByIdWithMappingsAsync(int id, CancellationToken cancellationToken = default);

        Task<bool> ExistsByNomeAsync(string nomeColecao, TipoColecao tipoColecao, int? empresaId, int? ignoreId = null, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<Documento>> GetDocumentosByIdsAsync(IReadOnlyCollection<int> documentoIds, CancellationToken cancellationToken = default);

        Task AddAsync(Colecao colecao, CancellationToken cancellationToken = default);

        void Remove(Colecao colecao);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
