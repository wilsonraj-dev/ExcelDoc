using ExcelDoc.Server.Data;
using ExcelDoc.Server.Models;
using ExcelDoc.Server.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExcelDoc.Server.Repositories
{
    public class PerfilMapeamentoRepository : IPerfilMapeamentoRepository
    {
        private readonly ExcelDocDbContext _context;

        public PerfilMapeamentoRepository(ExcelDocDbContext context)
        {
            _context = context;
        }

        public Task<PerfilMapeamento?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.PerfilMapeamentos
                .Include(x => x.Itens)
                    .ThenInclude(x => x.Colecao)
                .Include(x => x.Itens)
                    .ThenInclude(x => x.Mapeamento)
                        .ThenInclude(x => x.Campos)
                .Include(x => x.Documento)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyCollection<PerfilMapeamento>> GetByDocumentoIdAsync(int documentoId, CancellationToken cancellationToken = default)
        {
            return await _context.PerfilMapeamentos
                .AsNoTracking()
                .Include(x => x.Itens)
                    .ThenInclude(x => x.Colecao)
                .Include(x => x.Itens)
                    .ThenInclude(x => x.Mapeamento)
                .Where(x => x.FK_IdDocumento == documentoId)
                .OrderByDescending(x => x.IsPadrao)
                .ThenBy(x => x.Nome)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyCollection<DocumentoColecao>> GetColecoesDoDocumentoAsync(int documentoId, CancellationToken cancellationToken = default)
        {
            return await _context.DocumentoColecoes
                .AsNoTracking()
                .Include(x => x.Colecao)
                .Where(x => x.FK_IdDocumento == documentoId)
                .ToListAsync(cancellationToken);
        }

        public Task<Mapeamento?> GetMapeamentoByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.Mapeamentos
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public Task<PerfilMapeamento?> GetForExecutionAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.PerfilMapeamentos
                .AsNoTracking()
                .Include(x => x.Itens)
                    .ThenInclude(x => x.Colecao)
                .Include(x => x.Itens)
                    .ThenInclude(x => x.Mapeamento)
                        .ThenInclude(x => x.Campos)
                .Include(x => x.Documento)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public Task<Documento?> GetDocumentoByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.Documentos
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task AddAsync(PerfilMapeamento perfil, CancellationToken cancellationToken = default)
        {
            await _context.PerfilMapeamentos.AddAsync(perfil, cancellationToken);
        }

        public void Remove(PerfilMapeamento perfil)
        {
            _context.PerfilMapeamentos.Remove(perfil);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}
