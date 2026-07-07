using ExcelDoc.Server.Localization;
using ExcelDoc.Server.Models;
using ExcelDoc.Server.Repositories.Interfaces;
using ExcelDoc.Server.Services.Interfaces;

namespace ExcelDoc.Server.Services
{
    public class UsuarioAcessoService : IUsuarioAcessoService
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly IUsuarioRepository _usuarioRepository;

        public UsuarioAcessoService(ICurrentUserService currentUserService, IMessageService messageService, IUsuarioRepository usuarioRepository)
        {
            _currentUserService = currentUserService;
            _messageService = messageService;
            _usuarioRepository = usuarioRepository;
        }

        public async Task<Usuario> GetUsuarioAtualAsync(bool requerEmpresaVinculada = true, CancellationToken cancellationToken = default)
        {
            var usuarioId = _currentUserService.GetRequiredUserId();
            var usuario = await _usuarioRepository.GetByIdAsync(usuarioId, cancellationToken)
                ?? throw new KeyNotFoundException(_messageService.Get(MessageKeys.UserNotFound));

            if (!usuario.Ativo)
            {
                throw new UnauthorizedAccessException(_messageService.Get(MessageKeys.UserInactive));
            }

            if (requerEmpresaVinculada && !usuario.FK_IdEmpresa.HasValue)
            {
                throw new UnauthorizedAccessException(_messageService.Get(MessageKeys.UserWithoutCompanyCannotExecuteAction));
            }

            return usuario;
        }

        public async Task<Usuario> ValidarAcessoEmpresaAsync(int empresaId, bool requerAdministrador, CancellationToken cancellationToken = default)
        {
            var usuario = await GetUsuarioAtualAsync(true, cancellationToken);

            if (requerAdministrador && usuario.TipoUsuario != TipoUsuario.Administrador)
            {
                throw new UnauthorizedAccessException(_messageService.Get(MessageKeys.OnlyAdminsCanExecuteAction));
            }

            if (usuario.TipoUsuario != TipoUsuario.Administrador && usuario.FK_IdEmpresa != empresaId)
            {
                throw new UnauthorizedAccessException(_messageService.Get(MessageKeys.UserDoesNotHaveAccessToCompany));
            }

            return usuario;
        }
    }
}
