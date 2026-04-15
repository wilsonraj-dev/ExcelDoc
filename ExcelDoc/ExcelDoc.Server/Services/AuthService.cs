using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ExcelDoc.Server.DTOs.Auth;
using ExcelDoc.Server.Models;
using ExcelDoc.Server.Options;
using ExcelDoc.Server.Repositories.Interfaces;
using ExcelDoc.Server.Security;
using ExcelDoc.Server.Services.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ExcelDoc.Server.Services
{
    public class AuthService : IAuthService
    {
        private readonly JwtOptions _jwtOptions;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthService> _logger;
        private readonly IPasswordHasherService _passwordHasherService;
        private readonly ISystemClock _systemClock;
        private readonly IUsuarioRepository _usuarioRepository;

        public AuthService(
            IOptions<JwtOptions> jwtOptions,
            IEmailService emailService,
            ILogger<AuthService> logger,
            IPasswordHasherService passwordHasherService,
            ISystemClock systemClock,
            IUsuarioRepository usuarioRepository)
        {
            _jwtOptions = jwtOptions.Value;
            _emailService = emailService;
            _logger = logger;
            _passwordHasherService = passwordHasherService;
            _systemClock = systemClock;
            _usuarioRepository = usuarioRepository;
        }

        public async Task ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken cancellationToken = default)
        {
            var email = request.Email.Trim();
            var usuario = await _usuarioRepository.GetByEmailAsync(email, cancellationToken);

            if (usuario is null || !usuario.Ativo)
            {
                _logger.LogInformation("Solicitação de recuperação ignorada para e-mail não encontrado ou inativo: {Email}.", email);
                return;
            }

            var expiresAt = _systemClock.UtcNow.AddMinutes(10);
            var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

            usuario.ResetPasswordCode = code;
            usuario.ResetPasswordCodeExpiresAtUtc = expiresAt;

            await _usuarioRepository.SaveChangesAsync(cancellationToken);

            var body = $"Olá, {usuario.NomeUsuario}!\n\nSeu código para redefinição de senha é: {code}\n\nEsse código expira em 10 minutos ({expiresAt:dd/MM/yyyy HH:mm:ss} UTC).\n\nSe você não solicitou a redefinição, ignore este e-mail.";
            await _emailService.SendAsync(email, "Código de redefinição de senha - ExcelDoc", body, cancellationToken);
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
        {
            var usuario = await _usuarioRepository.GetByLoginAsync(request.Login.Trim(), cancellationToken)
                ?? throw new UnauthorizedAccessException("Credenciais inválidas.");

            if (!usuario.Ativo)
            {
                throw new UnauthorizedAccessException("Usuário inativo.");
            }

            if (!_passwordHasherService.Verify(request.Senha, usuario.SenhaHash))
            {
                throw new UnauthorizedAccessException("Credenciais inválidas.");
            }

            if (_passwordHasherService.NeedsRehash(usuario.SenhaHash))
            {
                usuario.SenhaHash = _passwordHasherService.Hash(request.Senha);
                await _usuarioRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Senha do usuário {UsuarioId} migrada automaticamente para BCrypt.", usuario.Id);
            }

            var expiresAt = _systemClock.UtcNow.AddMinutes(Math.Max(1, _jwtOptions.ExpirationMinutes));
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new(ClaimTypes.Name, usuario.Id.ToString()),
                new(ClaimTypes.GivenName, usuario.NomeUsuario),
                new(ClaimTypes.Role, usuario.TipoUsuario.ToString())
            };

            if (usuario.FK_IdEmpresa.HasValue)
            {
                claims.Add(new Claim(CustomClaimTypes.EmpresaId, usuario.FK_IdEmpresa.Value.ToString()));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var tokenDescriptor = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials);

            var token = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);

            return new LoginResponseDto
            {
                Token = token,
                ExpiresAtUtc = expiresAt,
                UsuarioId = usuario.Id,
                NomeUsuario = usuario.NomeUsuario,
                TipoUsuario = usuario.TipoUsuario.ToString(),
                NomeEmpresa = usuario.Empresa?.NomeEmpresa,
                EmpresaId = usuario.FK_IdEmpresa
            };
        }

        public async Task<RegisterUserResponseDto> RegisterAsync(RegisterUserRequestDto request, CancellationToken cancellationToken = default)
        {
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

            var usuario = new Usuario
            {
                NomeUsuario = nomeUsuario,
                Email = email,
                SenhaHash = _passwordHasherService.Hash(request.Senha),
                TipoUsuario = TipoUsuario.Usuario,
                Ativo = true
            };

            await _usuarioRepository.AddAsync(usuario, cancellationToken);
            await _usuarioRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Usuário {UsuarioId} criado via autoatendimento.", usuario.Id);

            return new RegisterUserResponseDto
            {
                UsuarioId = usuario.Id,
                NomeUsuario = usuario.NomeUsuario,
                Email = usuario.Email ?? string.Empty
            };
        }

        public async Task ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken = default)
        {
            var email = request.Email.Trim();
            var usuario = await _usuarioRepository.GetByEmailAsync(email, cancellationToken)
                ?? throw new InvalidOperationException("Código inválido ou expirado.");

            if (!usuario.Ativo ||
                string.IsNullOrWhiteSpace(usuario.ResetPasswordCode) ||
                usuario.ResetPasswordCodeExpiresAtUtc is null ||
                usuario.ResetPasswordCode != request.Codigo.Trim() ||
                usuario.ResetPasswordCodeExpiresAtUtc <= _systemClock.UtcNow)
            {
                throw new InvalidOperationException("Código inválido ou expirado.");
            }

            usuario.SenhaHash = _passwordHasherService.Hash(request.NovaSenha);
            usuario.ResetPasswordCode = null;
            usuario.ResetPasswordCodeExpiresAtUtc = null;

            await _usuarioRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Senha redefinida com sucesso para o usuário {UsuarioId}.", usuario.Id);
        }
    }
}
