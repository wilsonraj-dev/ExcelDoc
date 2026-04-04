using ExcelDoc.Server.Models;
using ExcelDoc.Server.Repositories.Interfaces;
using ExcelDoc.Server.Services.Interfaces;

namespace ExcelDoc.Server.Services
{
    public class UsuarioAcessoService : IUsuarioAcessoService
    {
        private readonly IUsuarioRepository _usuarioRepository;

        public UsuarioAcessoService(IUsuarioRepository usuarioRepository)
        {
            _usuarioRepository = usuarioRepository;
        }

        public async Task<Usuario> ValidarAcessoEmpresaAsync(int usuarioExecutorId, int empresaId, bool requerAdministrador, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioRepository.GetByIdAsync(usuarioExecutorId, cancellationToken)
                ?? throw new KeyNotFoundException("Usuário não encontrado.");

            if (!usuario.Ativo)
            {
                throw new UnauthorizedAccessException("Usuário inativo.");
            }

            if (!usuario.FK_IdEmpresa.HasValue)
            {
                throw new UnauthorizedAccessException("Usuário sem empresa vinculada não pode executar ações.");
            }

            if (requerAdministrador && usuario.TipoUsuario != TipoUsuario.Administrador)
            {
                throw new UnauthorizedAccessException("Apenas administradores podem executar esta ação.");
            }

            if (usuario.TipoUsuario != TipoUsuario.Administrador && usuario.FK_IdEmpresa != empresaId)
            {
                throw new UnauthorizedAccessException("Usuário não possui acesso à empresa informada.");
            }

            return usuario;
        }
    }
}
