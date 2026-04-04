using ExcelDoc.Server.Models;

namespace ExcelDoc.Server.Repositories.Interfaces
{
    public interface IUsuarioRepository
    {
        Task<Usuario?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<Usuario?> GetByLoginAsync(string login, CancellationToken cancellationToken = default);
    }
}
