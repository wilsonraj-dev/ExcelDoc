using ExcelDoc.Server.Models;
using ExcelDoc.Server.Repositories.Interfaces;
using ExcelDoc.Server.Services.Interfaces;

namespace ExcelDoc.Server.Services
{
    public class UsuarioAcessoService : IUsuarioAcessoService
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IUsuarioRepository _usuarioRepository;

        public UsuarioAcessoService(ICurrentUserService currentUserService, IUsuarioRepository usuarioRepository)
        {
            _currentUserService = currentUserService;
            _usuarioRepository = usuarioRepository;
        }

        public async Task<Usuario> GetUsuarioAtualAsync(bool requerEmpresaVinculada = true, CancellationToken cancellationToken = default)
        {
            var usuarioId = _currentUserService.GetRequiredUserId();
            var usuario = await _usuarioRepository.GetByIdAsync(usuarioId, cancellationToken)
                ?? throw new KeyNotFoundException("Usuário não encontrado.");

            if (!usuario.Ativo)
            {
                throw new UnauthorizedAccessException("Usuário inativo.");
            }

            if (requerEmpresaVinculada && !usuario.FK_IdEmpresa.HasValue)
            {
                throw new UnauthorizedAccessException("Usuário sem empresa vinculada não pode executar ações.");
            }

            return usuario;
        }

        public async Task<Usuario> ValidarAcessoEmpresaAsync(int empresaId, bool requerAdministrador, CancellationToken cancellationToken = default)
        {
            var usuario = await GetUsuarioAtualAsync(true, cancellationToken);

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
