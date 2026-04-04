using ExcelDoc.Server.Data;
using ExcelDoc.Server.Models;
using ExcelDoc.Server.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExcelDoc.Server.Repositories
{
    public class ColecaoRepository : IColecaoRepository
    {
        private readonly ExcelDocDbContext _context;

        public ColecaoRepository(ExcelDocDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyCollection<Colecao>> GetByEmpresaIdAsync(int empresaId, CancellationToken cancellationToken = default)
        {
            return await _context.Colecoes
                .AsNoTracking()
                .Include(x => x.MapeamentoCampos)
                .Where(x => x.FK_IdEmpresa == null || x.FK_IdEmpresa == empresaId)
                .OrderBy(x => x.NomeColecao)
                .ToListAsync(cancellationToken);
        }

        public Task<Colecao?> GetByIdWithMappingsAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.Colecoes
                .Include(x => x.MapeamentoCampos)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task AddAsync(Colecao colecao, CancellationToken cancellationToken = default)
        {
            await _context.Colecoes.AddAsync(colecao, cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}
