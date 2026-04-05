using ExcelDoc.Server.Data;
using ExcelDoc.Server.Models;
using ExcelDoc.Server.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExcelDoc.Server.Repositories
{
    public class EmpresaRepository : IEmpresaRepository
    {
        private readonly ExcelDocDbContext _context;

        public EmpresaRepository(ExcelDocDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyCollection<Empresa>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Empresas
                .AsNoTracking()
                .OrderBy(x => x.NomeEmpresa)
                .ToListAsync(cancellationToken);
        }

        public Task<Empresa?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.Empresas
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public Task<bool> ExistsByNameAsync(string nomeEmpresa, CancellationToken cancellationToken = default)
        {
            return _context.Empresas
                .AnyAsync(x => x.NomeEmpresa == nomeEmpresa, cancellationToken);
        }

        public async Task AddAsync(Empresa empresa, CancellationToken cancellationToken = default)
        {
            await _context.Empresas.AddAsync(empresa, cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}
