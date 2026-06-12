using ExcelDoc.Server.Data;
using ExcelDoc.Server.Models;
using ExcelDoc.Server.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExcelDoc.Server.Repositories
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly ExcelDocDbContext _context;

        public UsuarioRepository(ExcelDocDbContext context)
        {
            _context = context;
        }

        public Task AddAsync(Usuario usuario, CancellationToken cancellationToken = default)
        {
            return _context.Usuarios.AddAsync(usuario, cancellationToken).AsTask();
        }

        public async Task<(IReadOnlyCollection<Usuario> Items, int TotalCount)> GetPagedAsync(string? termo, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _context.Usuarios
                .AsNoTracking()
                .Include(x => x.Empresa)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(termo))
            {
                var termoNormalizado = termo.Trim();
                query = query.Where(x =>
                    x.NomeUsuario.Contains(termoNormalizado) ||
                    (x.Email != null && x.Email.Contains(termoNormalizado)) ||
                    (x.Empresa != null && x.Empresa.NomeEmpresa.Contains(termoNormalizado)));
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderBy(x => x.NomeUsuario)
                .ThenBy(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return _context.Usuarios.AnyAsync(x => x.Email != null && x.Email == email, cancellationToken);
        }

        public Task<bool> ExistsByNomeUsuarioAsync(string nomeUsuario, CancellationToken cancellationToken = default)
        {
            return _context.Usuarios.AnyAsync(x => x.NomeUsuario == nomeUsuario, cancellationToken);
        }

        public Task<Usuario?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return _context.Usuarios
                .FirstOrDefaultAsync(x => x.Email != null && x.Email == email, cancellationToken);
        }

        public Task<Usuario?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.Usuarios
                .AsNoTracking()
                .Include(x => x.Empresa)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public Task<Usuario?> GetTrackedByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.Usuarios
                .Include(x => x.Empresa)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public Task<Usuario?> GetByLoginAsync(string login, CancellationToken cancellationToken = default)
        {
            return _context.Usuarios
                .Include(x => x.Empresa)
                .FirstOrDefaultAsync(x => x.NomeUsuario == login || (x.Email != null && x.Email == login), cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}
