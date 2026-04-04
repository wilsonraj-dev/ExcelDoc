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

        public Task<Usuario?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public Task<Usuario?> GetByLoginAsync(string login, CancellationToken cancellationToken = default)
        {
            return _context.Usuarios
                .FirstOrDefaultAsync(x => x.NomeUsuario == login || (x.Email != null && x.Email == login), cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}
