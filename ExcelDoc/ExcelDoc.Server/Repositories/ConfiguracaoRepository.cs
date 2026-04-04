using ExcelDoc.Server.Data;
using ExcelDoc.Server.Models;
using ExcelDoc.Server.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExcelDoc.Server.Repositories
{
    public class ConfiguracaoRepository : IConfiguracaoRepository
    {
        private readonly ExcelDocDbContext _context;

        public ConfiguracaoRepository(ExcelDocDbContext context)
        {
            _context = context;
        }

        public Task<Configuracao?> GetByEmpresaIdAsync(int empresaId, CancellationToken cancellationToken = default)
        {
            return _context.Configuracoes
                .FirstOrDefaultAsync(x => x.FK_IdEmpresa == empresaId, cancellationToken);
        }

        public async Task AddAsync(Configuracao configuracao, CancellationToken cancellationToken = default)
        {
            await _context.Configuracoes.AddAsync(configuracao, cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}
