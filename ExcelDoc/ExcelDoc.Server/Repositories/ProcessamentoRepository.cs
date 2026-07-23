using ExcelDoc.Server.Data;
using ExcelDoc.Server.Models;
using ExcelDoc.Server.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExcelDoc.Server.Repositories
{
    public class ProcessamentoRepository : IProcessamentoRepository
    {
        private readonly ExcelDocDbContext _context;

        public ProcessamentoRepository(ExcelDocDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Processamento processamento, CancellationToken cancellationToken = default)
        {
            await _context.Processamentos.AddAsync(processamento, cancellationToken);
        }

        public Task<Processamento?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.Processamentos
                .AsNoTracking()
                .Include(x => x.Documento)
                .Include(x => x.PerfilMapeamento)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public Task<Processamento?> GetForExecutionAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.Processamentos
                .Include(x => x.Documento)
                    .ThenInclude(x => x.DocumentoColecoes)
                        .ThenInclude(x => x.Colecao)
                .Include(x => x.PerfilMapeamento)
                    .ThenInclude(x => x!.Itens)
                        .ThenInclude(x => x.Colecao)
                .Include(x => x.PerfilMapeamento)
                    .ThenInclude(x => x!.Itens)
                        .ThenInclude(x => x.Mapeamento)
                            .ThenInclude(x => x.Campos)
                .Include(x => x.Empresa)
                .Include(x => x.Usuario)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<(IReadOnlyCollection<Processamento> Items, int TotalCount)> GetPagedAsync(
            int empresaId,
            int? empresaUsuarioId,
            StatusProcessamento? status,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Processamentos
                .AsNoTracking()
                .Where(x =>
                    x.FK_IdEmpresa == empresaId &&
                    (x.PerfilMapeamento == null ||
                     (x.PerfilMapeamento.IsPadrao && !x.PerfilMapeamento.FK_IdEmpresa.HasValue) ||
                     (empresaUsuarioId.HasValue &&
                      x.PerfilMapeamento.FK_IdEmpresa == empresaUsuarioId.Value)));

            if (status.HasValue)
            {
                query = query.Where(x => x.Status == status.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Include(x => x.Documento)
                .Include(x => x.PerfilMapeamento)
                .OrderByDescending(x => x.DataExecucao)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<(IReadOnlyCollection<ProcessamentoItem> Items, int TotalCount)> GetItemsPagedAsync(int processamentoId, StatusProcessamentoItem? status, bool apenasComErro, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _context.ProcessamentoItens
                .AsNoTracking()
                .Where(x => x.FK_IdProcessamento == processamentoId);

            if (status.HasValue)
            {
                query = query.Where(x => x.Status == status.Value);
            }

            if (apenasComErro)
            {
                query = query.Where(x => x.Erro != null && x.Erro != string.Empty);
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderBy(x => x.LinhaExcel)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task AddItemAsync(ProcessamentoItem item, CancellationToken cancellationToken = default)
        {
            await _context.ProcessamentoItens.AddAsync(item, cancellationToken);
        }

        public Task<bool> HasDocumentoProcessadoComSucessoAsync(string idDocumentoUnico, CancellationToken cancellationToken = default)
        {
            return _context.ProcessamentoItens
                .AsNoTracking()
                .AnyAsync(x =>
                    x.IdDocumentoUnico == idDocumentoUnico &&
                    x.Status == StatusProcessamentoItem.Sucesso,
                    cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}
