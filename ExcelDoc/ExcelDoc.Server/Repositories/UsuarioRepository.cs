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
