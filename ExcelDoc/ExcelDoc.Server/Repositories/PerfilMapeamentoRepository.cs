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
                        .ThenInclude(x => x.Campos)
                .Where(x => x.FK_IdDocumento == documentoId)
                .OrderByDescending(x => x.IsPadrao && !x.FK_IdEmpresa.HasValue)
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

        public async Task RemoveWithOrphanMappingsAsync(
            PerfilMapeamento perfil,
            CancellationToken cancellationToken = default)
        {
            var customMappingIds = perfil.Itens
                .Where(item => !item.Mapeamento.IsPadraoGlobal)
                .Select(item => item.FK_IdMapeamento)
                .Distinct()
                .ToList();

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                _context.PerfilMapeamentos.Remove(perfil);
                await _context.SaveChangesAsync(cancellationToken);

                if (customMappingIds.Count > 0)
                {
                    var orphanMappings = await _context.Mapeamentos
                        .Include(mapping => mapping.Campos)
                        .Where(mapping =>
                            customMappingIds.Contains(mapping.Id) &&
                            !(mapping.IsPadrao && !mapping.FK_IdEmpresa.HasValue) &&
                            !_context.PerfilMapeamentoItens.Any(item => item.FK_IdMapeamento == mapping.Id))
                        .ToListAsync(cancellationToken);

                    foreach (var mapping in orphanMappings)
                    {
                        _context.MapeamentoCampos.RemoveRange(mapping.Campos);
                    }

                    _context.Mapeamentos.RemoveRange(orphanMappings);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}
