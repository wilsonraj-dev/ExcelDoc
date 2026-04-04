using ExcelDoc.Server.Models;

namespace ExcelDoc.Server.Services.Interfaces
{
    public interface IUsuarioAcessoService
    {
        Task<Usuario> GetUsuarioAtualAsync(bool requerEmpresaVinculada = true, CancellationToken cancellationToken = default);

        Task<Usuario> ValidarAcessoEmpresaAsync(int empresaId, bool requerAdministrador, CancellationToken cancellationToken = default);
    }
}
