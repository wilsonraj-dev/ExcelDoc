using ExcelDoc.Server.Models;

namespace ExcelDoc.Server.Repositories.Interfaces
{
    public interface IPerfilMapeamentoRepository
    {
        Task<PerfilMapeamento?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<PerfilMapeamento?> GetForExecutionAsync(int id, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<PerfilMapeamento>> GetByDocumentoIdAsync(int documentoId, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<DocumentoColecao>> GetColecoesDoDocumentoAsync(int documentoId, CancellationToken cancellationToken = default);

        Task<Mapeamento?> GetMapeamentoByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<Documento?> GetDocumentoByIdAsync(int id, CancellationToken cancellationToken = default);

        Task AddAsync(PerfilMapeamento perfil, CancellationToken cancellationToken = default);

        void Remove(PerfilMapeamento perfil);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
