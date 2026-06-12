using ExcelDoc.Server.Models;

namespace ExcelDoc.Server.Repositories.Interfaces
{
    public interface IUsuarioRepository
    {
        Task AddAsync(Usuario usuario, CancellationToken cancellationToken = default);

        Task<(IReadOnlyCollection<Usuario> Items, int TotalCount)> GetPagedAsync(string? termo, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

        Task<bool> ExistsByNomeUsuarioAsync(string nomeUsuario, CancellationToken cancellationToken = default);

        Task<Usuario?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

        Task<Usuario?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<Usuario?> GetTrackedByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<Usuario?> GetByLoginAsync(string login, CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
