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

        public async Task<IReadOnlyCollection<Colecao>> GetByEmpresaIdAsync(int? empresaId, bool includeAllCompanies, CancellationToken cancellationToken = default)
        {
            var query = _context.Colecoes
                .AsNoTracking()
                .Include(x => x.MapeamentoCampos)
                .Include(x => x.DocumentoColecoes)
                    .ThenInclude(x => x.Documento)
                .AsQueryable();

            if (!includeAllCompanies)
            {
                query = query.Where(x => x.FK_IdEmpresa == null || x.FK_IdEmpresa == empresaId);
            }

            return await query
                .OrderBy(x => x.NomeColecao)
                .ToListAsync(cancellationToken);
        }

        public Task<Colecao?> GetByIdWithMappingsAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.Colecoes
                .Include(x => x.MapeamentoCampos)
                .Include(x => x.DocumentoColecoes)
                    .ThenInclude(x => x.Documento)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public Task<bool> ExistsByNomeAsync(string nomeColecao, TipoColecao tipoColecao, int? empresaId, int? ignoreId = null, CancellationToken cancellationToken = default)
        {
            return _context.Colecoes.AnyAsync(
                x => (!ignoreId.HasValue || x.Id != ignoreId.Value)
                    && x.NomeColecao == nomeColecao
                    && x.TipoColecao == tipoColecao
                    && x.FK_IdEmpresa == empresaId,
                cancellationToken);
        }

        public async Task<IReadOnlyCollection<Documento>> GetDocumentosByIdsAsync(IReadOnlyCollection<int> documentoIds, CancellationToken cancellationToken = default)
        {
            if (documentoIds.Count == 0)
            {
                return Array.Empty<Documento>();
            }

            return await _context.Documentos
                .Where(x => documentoIds.Contains(x.Id))
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(Colecao colecao, CancellationToken cancellationToken = default)
        {
            await _context.Colecoes.AddAsync(colecao, cancellationToken);
        }

        public void Remove(Colecao colecao)
        {
            _context.Colecoes.Remove(colecao);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}
