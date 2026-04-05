using ExcelDoc.Server.Data;
using ExcelDoc.Server.Models;
using ExcelDoc.Server.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExcelDoc.Server.Repositories
{
    public class DocumentoRepository : IDocumentoRepository
    {
        private readonly ExcelDocDbContext _context;

        public DocumentoRepository(ExcelDocDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyCollection<Documento>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Documentos
                .AsNoTracking()
                .OrderBy(x => x.NomeDocumento)
                .ToListAsync(cancellationToken);
        }

        public Task<Documento?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.Documentos
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public Task<Documento?> GetTrackedByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.Documentos
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public Task<Documento?> GetForProcessingAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.Documentos
                .Include(x => x.DocumentoColecoes)
                    .ThenInclude(x => x.Colecao)
                        .ThenInclude(x => x.MapeamentoCampos)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public Task<bool> ExistsByNomeOrEndpointAsync(string nomeDocumento, string endpoint, int? ignoreId = null, CancellationToken cancellationToken = default)
        {
            return _context.Documentos.AnyAsync(
                x => (!ignoreId.HasValue || x.Id != ignoreId.Value)
                    && (x.NomeDocumento == nomeDocumento || x.Endpoint == endpoint),
                cancellationToken);
        }

        public async Task AddAsync(Documento documento, CancellationToken cancellationToken = default)
        {
            await _context.Documentos.AddAsync(documento, cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}
