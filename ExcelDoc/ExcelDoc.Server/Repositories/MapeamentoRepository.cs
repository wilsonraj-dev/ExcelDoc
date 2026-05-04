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

        public Task<Colecao?> GetColecaoByIdAsync(int colecaoId, CancellationToken cancellationToken = default)
        {
            return _context.Colecoes
                .FirstOrDefaultAsync(x => x.Id == colecaoId, cancellationToken);
        }

        public async Task<IReadOnlyCollection<Mapeamento>> GetMapeamentosByColecaoIdAsync(int colecaoId, CancellationToken cancellationToken = default)
        {
            return await _context.Mapeamentos
                .AsNoTracking()
                .Include(x => x.Campos)
                .Where(x => x.FK_IdColecao == colecaoId)
                .OrderByDescending(x => x.IsPadrao)
                .ThenBy(x => x.Nome)
                .ToListAsync(cancellationToken);
        }

        public Task<Mapeamento?> GetMapeamentoByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.Mapeamentos
                .Include(x => x.Colecao)
                .Include(x => x.Campos)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public Task<MapeamentoCampo?> GetCampoByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.MapeamentoCampos
                .Include(x => x.Mapeamento)
                    .ThenInclude(x => x.Colecao)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyCollection<MapeamentoCampo>> GetCamposByMapeamentoIdAsync(int mapeamentoId, CancellationToken cancellationToken = default)
        {
            return await _context.MapeamentoCampos
                .AsNoTracking()
                .Where(x => x.FK_IdMapeamento == mapeamentoId)
                .OrderBy(x => x.IndiceColuna)
                .ToListAsync(cancellationToken);
        }

        public Task<bool> ExistsIndiceNoMapeamentoAsync(int mapeamentoId, int indiceColuna, int? ignoreId = null, CancellationToken cancellationToken = default)
        {
            return _context.MapeamentoCampos.AnyAsync(
                x => x.FK_IdMapeamento == mapeamentoId
                    && x.IndiceColuna == indiceColuna
                    && (!ignoreId.HasValue || x.Id != ignoreId.Value),
                cancellationToken);
        }

        public async Task AddMapeamentoAsync(Mapeamento mapeamento, CancellationToken cancellationToken = default)
        {
            await _context.Mapeamentos.AddAsync(mapeamento, cancellationToken);
        }

        public async Task AddCampoAsync(MapeamentoCampo campo, CancellationToken cancellationToken = default)
        {
            await _context.MapeamentoCampos.AddAsync(campo, cancellationToken);
        }

        public void RemoveMapeamento(Mapeamento mapeamento)
        {
            _context.Mapeamentos.Remove(mapeamento);
        }

        public void RemoveCampo(MapeamentoCampo campo)
        {
            _context.MapeamentoCampos.Remove(campo);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}
