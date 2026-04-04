using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ExcelDoc.Server.DTOs.Auth;
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
        private readonly IPasswordHasherService _passwordHasherService;
        private readonly IUsuarioRepository _usuarioRepository;

        public AuthService(
            IOptions<JwtOptions> jwtOptions,
            IPasswordHasherService passwordHasherService,
            IUsuarioRepository usuarioRepository)
        {
            _jwtOptions = jwtOptions.Value;
            _passwordHasherService = passwordHasherService;
            _usuarioRepository = usuarioRepository;
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

            var expiresAt = DateTime.UtcNow.AddMinutes(Math.Max(1, _jwtOptions.ExpirationMinutes));
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
                EmpresaId = usuario.FK_IdEmpresa
            };
        }
    }
}
