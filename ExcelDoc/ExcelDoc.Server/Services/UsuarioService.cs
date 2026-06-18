using ExcelDoc.Server.DTOs;
using ExcelDoc.Server.DTOs.Auth;
using ExcelDoc.Server.DTOs.Usuarios;
using ExcelDoc.Server.Models;
using ExcelDoc.Server.Repositories.Interfaces;
using ExcelDoc.Server.Services.Interfaces;

namespace ExcelDoc.Server.Services
{
    public class UsuarioService : IUsuarioService
    {
        private const int MaxPageSize = 50;
        private readonly IEmpresaRepository _empresaRepository;
        private readonly ILogger<UsuarioService> _logger;
        private readonly IPasswordHasherService _passwordHasherService;
        private readonly IUsuarioAcessoService _usuarioAcessoService;
        private readonly IUsuarioRepository _usuarioRepository;

        public UsuarioService(
            IEmpresaRepository empresaRepository,
            ILogger<UsuarioService> logger,
            IPasswordHasherService passwordHasherService,
            IUsuarioAcessoService usuarioAcessoService,
            IUsuarioRepository usuarioRepository)
        {
            _empresaRepository = empresaRepository;
            _logger = logger;
            _passwordHasherService = passwordHasherService;
            _usuarioAcessoService = usuarioAcessoService;
            _usuarioRepository = usuarioRepository;
        }

        public async Task<RegisterUserResponseDto> CriarAsync(UsuarioCreateRequestDto request, CancellationToken cancellationToken = default)
        {
            await ValidarAdministradorAsync(cancellationToken);

            var nomeUsuario = request.NomeUsuario.Trim();
            var email = request.Email.Trim();

            if (await _usuarioRepository.ExistsByNomeUsuarioAsync(nomeUsuario, cancellationToken))
            {
                throw new InvalidOperationException("Nome de usuário já cadastrado.");
            }

            if (await _usuarioRepository.ExistsByEmailAsync(email, cancellationToken))
            {
                throw new InvalidOperationException("E-mail já cadastrado.");
            }

            Empresa? empresa = null;
            if (request.EmpresaId.HasValue)
            {
                empresa = await _empresaRepository.GetByIdAsync(request.EmpresaId.Value, cancellationToken)
                    ?? throw new KeyNotFoundException("Empresa não encontrada.");
            }

            var usuario = new Usuario
            {
                NomeUsuario = nomeUsuario,
                Email = email,
                SenhaHash = _passwordHasherService.Hash(request.Senha),
                TipoUsuario = TipoUsuario.Usuario,
                Ativo = true,
                FK_IdEmpresa = empresa?.Id,
                Empresa = empresa
            };

            await _usuarioRepository.AddAsync(usuario, cancellationToken);
            await _usuarioRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Usuário {UsuarioId} criado pelo administrador com empresa {EmpresaId}.", usuario.Id, usuario.FK_IdEmpresa);

            return new RegisterUserResponseDto
            {
                UsuarioId = usuario.Id,
                NomeUsuario = usuario.NomeUsuario,
                Email = usuario.Email ?? string.Empty
            };
        }

        public async Task<PagedResultDto<UsuarioResponseDto>> GetPagedAsync(UsuarioQueryDto query, CancellationToken cancellationToken = default)
        {
            await ValidarAdministradorAsync(cancellationToken);

            var pageNumber = Math.Max(1, query.PageNumber);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
            var result = await _usuarioRepository.GetPagedAsync(query.Termo, pageNumber, pageSize, cancellationToken);

            return new PagedResultDto<UsuarioResponseDto>
            {
                Items = result.Items.Select(Map).ToList(),
                TotalCount = result.TotalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<UsuarioResponseDto> VincularEmpresaAsync(int usuarioId, UsuarioEmpresaVinculoRequestDto request, CancellationToken cancellationToken = default)
        {
            await ValidarAdministradorAsync(cancellationToken);

            var usuario = await _usuarioRepository.GetTrackedByIdAsync(usuarioId, cancellationToken)
                ?? throw new KeyNotFoundException("Usuário não encontrado.");

            if (usuario.TipoUsuario == TipoUsuario.Administrador)
            {
                throw new InvalidOperationException("Usuários administradores não podem ser vinculados por esta funcionalidade.");
            }

            var empresa = await _empresaRepository.GetByIdAsync(request.EmpresaId, cancellationToken)
                ?? throw new KeyNotFoundException("Empresa não encontrada.");

            usuario.FK_IdEmpresa = empresa.Id;
            usuario.Empresa = empresa;

            await _usuarioRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Usuário {UsuarioId} vinculado à empresa {EmpresaId} pelo administrador.", usuario.Id, empresa.Id);

            return Map(usuario);
        }

        public async Task AtualizarIdioma(int usuarioId, string idioma, CancellationToken cancellationToken = default)
        {
            var valid = new[] { "pt", "en", "es" };
            if (!valid.Contains(idioma))
            {
                throw new FormatException("Idioma inválido.");
            }

            var usuario = await _usuarioRepository.GetTrackedByIdAsync(usuarioId, cancellationToken)
                ?? throw new KeyNotFoundException("Usuário não encontrado.");

            usuario.Idioma = idioma;
            await _usuarioRepository.SaveChangesAsync(cancellationToken);
        }

        private UsuarioResponseDto Map(Usuario usuario)
        {
            return new UsuarioResponseDto
            {
                Id = usuario.Id,
                NomeUsuario = usuario.NomeUsuario,
                Email = usuario.Email ?? string.Empty,
                TipoUsuario = usuario.TipoUsuario.ToString(),
                Ativo = usuario.Ativo,
                EmpresaId = usuario.FK_IdEmpresa,
                NomeEmpresa = usuario.Empresa?.NomeEmpresa,                 
                Idioma = usuario.Idioma
            };
        }

        private async Task ValidarAdministradorAsync(CancellationToken cancellationToken)
        {
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(false, cancellationToken);

            if (usuario.TipoUsuario != TipoUsuario.Administrador)
            {
                throw new UnauthorizedAccessException("Apenas administradores podem executar esta ação.");
            }
        }
    }
}
