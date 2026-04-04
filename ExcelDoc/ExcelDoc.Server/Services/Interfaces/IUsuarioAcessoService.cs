using ExcelDoc.Server.Models;

namespace ExcelDoc.Server.Services.Interfaces
{
    public interface IUsuarioAcessoService
    {
        Task<Usuario> ValidarAcessoEmpresaAsync(int usuarioExecutorId, int empresaId, bool requerAdministrador, CancellationToken cancellationToken = default);
    }
}
