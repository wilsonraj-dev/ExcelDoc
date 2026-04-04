using System.Security.Claims;
using ExcelDoc.Server.Services.Interfaces;

namespace ExcelDoc.Server.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int GetRequiredUserId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                throw new UnauthorizedAccessException("Usuário não autenticado.");
            }

            var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue(ClaimTypes.Name)
                ?? throw new UnauthorizedAccessException("Token inválido para o usuário atual.");

            if (!int.TryParse(value, out var userId))
            {
                throw new UnauthorizedAccessException("Identificador do usuário inválido no token.");
            }

            return userId;
        }
    }
}
