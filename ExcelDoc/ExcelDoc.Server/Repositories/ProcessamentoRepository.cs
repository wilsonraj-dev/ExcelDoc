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

        public Task<bool> ExistsByHashAsync(int empresaId, string hashArquivo, CancellationToken cancellationToken = default)
        {
            return _context.Processamentos.AnyAsync(x => x.FK_IdEmpresa == empresaId && x.HashArquivo == hashArquivo, cancellationToken);
        }

        public async Task AddAsync(Processamento processamento, CancellationToken cancellationToken = default)
        {
            await _context.Processamentos.AddAsync(processamento, cancellationToken);
        }

        public Task<Processamento?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.Processamentos
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public Task<Processamento?> GetForExecutionAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.Processamentos
                .Include(x => x.Documento)
                    .ThenInclude(x => x.DocumentoColecoes)
                        .ThenInclude(x => x.Colecao)
                            .ThenInclude(x => x.MapeamentoCampos)
                .Include(x => x.Empresa)
                .Include(x => x.Usuario)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<(IReadOnlyCollection<Processamento> Items, int TotalCount)> GetPagedAsync(int empresaId, StatusProcessamento? status, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _context.Processamentos
                .AsNoTracking()
                .Where(x => x.FK_IdEmpresa == empresaId);

            if (status.HasValue)
            {
                query = query.Where(x => x.Status == status.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(x => x.DataExecucao)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<(IReadOnlyCollection<ProcessamentoItem> Items, int TotalCount)> GetItemsPagedAsync(int processamentoId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _context.ProcessamentoItens
                .AsNoTracking()
                .Where(x => x.FK_IdProcessamento == processamentoId);

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

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}
