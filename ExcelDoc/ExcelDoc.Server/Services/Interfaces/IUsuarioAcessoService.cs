using ExcelDoc.Server.Models;

namespace ExcelDoc.Server.Services.Interfaces
{
    public interface IUsuarioAcessoService
    {
        Task<Usuario> GetUsuarioAtualAsync(CancellationToken cancellationToken = default);
    }
}
