using ExcelDoc.Server.Models;

namespace ExcelDoc.Server.Repositories.Interfaces
{
    public interface IEmpresaRepository
    {
        Task<IReadOnlyCollection<Empresa>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<Empresa?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<bool> ExistsByNameAsync(string nomeEmpresa, CancellationToken cancellationToken = default);

        Task AddAsync(Empresa empresa, CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
