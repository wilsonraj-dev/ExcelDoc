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
                .Include(x => x.Mapeamento)
                    .ThenInclude(x => x.Colecao)
                .Where(x => x.Mapeamento.FK_IdColecao == colecaoId && x.Mapeamento.IsPadrao)
                .OrderBy(x => x.IndiceColuna)
                .ToListAsync(cancellationToken);
        }

        public Task<MapeamentoCampo?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.MapeamentoCampos
                .Include(x => x.Mapeamento)
                    .ThenInclude(x => x.Colecao)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public Task<Colecao?> GetColecaoByIdWithMappingsAsync(int colecaoId, CancellationToken cancellationToken = default)
        {
            return _context.Colecoes
                .Include(x => x.Mapeamentos)
                    .ThenInclude(x => x.Campos)
                .Include(x => x.DocumentoColecoes)
                    .ThenInclude(x => x.Documento)
                .FirstOrDefaultAsync(x => x.Id == colecaoId, cancellationToken);
        }

        public Task<bool> ExistsIndiceNoMapeamentoAsync(int mapeamentoId, int indiceColuna, int? ignoreId = null, CancellationToken cancellationToken = default)
        {
            return _context.MapeamentoCampos.AnyAsync(
                x => x.FK_IdMapeamento == mapeamentoId
                    && x.IndiceColuna == indiceColuna
                    && (!ignoreId.HasValue || x.Id != ignoreId.Value),
                cancellationToken);
        }

        public Task<Colecao?> GetColecaoByIdAsync(int colecaoId, CancellationToken cancellationToken = default)
        {
            return _context.Colecoes
                .FirstOrDefaultAsync(x => x.Id == colecaoId, cancellationToken);
        }

        public Task<Mapeamento?> GetMapeamentoPadraoByColecaoIdAsync(int colecaoId, CancellationToken cancellationToken = default)
        {
            return _context.Mapeamentos
                .Include(x => x.Colecao)
                .FirstOrDefaultAsync(x => x.FK_IdColecao == colecaoId && x.IsPadrao, cancellationToken);
        }

        public async Task AddMapeamentoAsync(Mapeamento mapeamento, CancellationToken cancellationToken = default)
        {
            await _context.Mapeamentos.AddAsync(mapeamento, cancellationToken);
        }

        public async Task AddColecaoAsync(Colecao colecao, CancellationToken cancellationToken = default)
        {
            await _context.Colecoes.AddAsync(colecao, cancellationToken);
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
