using ExcelDoc.Server.Data;
using ExcelDoc.Server.Models;
using ExcelDoc.Server.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExcelDoc.Server.Repositories
{
    public class MapeamentoRepository : IMapeamentoRepository
    {
        private readonly ExcelDocDbContext _context;

        public MapeamentoRepository(ExcelDocDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyCollection<MapeamentoCampo>> GetByColecaoIdAsync(int colecaoId, CancellationToken cancellationToken = default)
        {
            return await _context.MapeamentoCampos
                .AsNoTracking()
                .Where(x => x.FK_IdColecao == colecaoId)
                .OrderBy(x => x.IndiceColuna)
                .ToListAsync(cancellationToken);
        }

        public Task<MapeamentoCampo?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.MapeamentoCampos
                .Include(x => x.Colecao)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public Task<bool> ExistsIndiceNaColecaoAsync(int colecaoId, int indiceColuna, int? ignoreId = null, CancellationToken cancellationToken = default)
        {
            return _context.MapeamentoCampos.AnyAsync(
                x => x.FK_IdColecao == colecaoId
                    && x.IndiceColuna == indiceColuna
                    && (!ignoreId.HasValue || x.Id != ignoreId.Value),
                cancellationToken);
        }

        public Task<Colecao?> GetColecaoByIdAsync(int colecaoId, CancellationToken cancellationToken = default)
        {
            return _context.Colecoes
                .FirstOrDefaultAsync(x => x.Id == colecaoId, cancellationToken);
        }

        public async Task AddAsync(MapeamentoCampo mapeamento, CancellationToken cancellationToken = default)
        {
            await _context.MapeamentoCampos.AddAsync(mapeamento, cancellationToken);
        }

        public void Remove(MapeamentoCampo mapeamento)
        {
            _context.MapeamentoCampos.Remove(mapeamento);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}
