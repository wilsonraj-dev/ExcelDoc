using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ExcelDoc.Server.Models;
using ExcelDoc.Server.Security;
using ExcelDoc.Server.Services.Interfaces;

namespace ExcelDoc.Server.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Usuario GetRequiredUser()
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("Usuário não autenticado.");
        }

        var userName = principal.FindFirstValue(ClaimTypes.GivenName)
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException(
                "O token não contém o usuário SAP.");
        var roleValue = principal.FindFirstValue(ClaimTypes.Role);

        if (!Enum.TryParse<TipoUsuario>(roleValue, true, out var role))
        {
            throw new UnauthorizedAccessException(
                "O token não contém o contexto SAP esperado.");
        }

        return new Usuario
        {
            Id = CreateStableUserId(userName),
            NomeUsuario = userName,
            TipoUsuario = role,
            Ativo = true,
            Idioma = "pt"
        };
    }

    private static int CreateStableUserId(string userName)
    {
        var hash = SHA256.HashData(
            Encoding.UTF8.GetBytes(userName.ToUpperInvariant()));
        return Math.Max(1, BitConverter.ToInt32(hash, 0) & int.MaxValue);
    }
}
