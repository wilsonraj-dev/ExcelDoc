using ExcelDoc.Server.Models;

namespace ExcelDoc.Server.Repositories.Interfaces
{
    public interface IConfiguracaoRepository
    {
        Task<Configuracao?> GetByEmpresaIdAsync(int empresaId, CancellationToken cancellationToken = default);

        Task AddAsync(Configuracao configuracao, CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
