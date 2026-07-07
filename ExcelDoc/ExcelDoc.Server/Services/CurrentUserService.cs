using System.Security.Claims;
using ExcelDoc.Server.Localization;
using ExcelDoc.Server.Services.Interfaces;

namespace ExcelDoc.Server.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMessageService _messageService;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor, IMessageService messageService)
        {
            _httpContextAccessor = httpContextAccessor;
            _messageService = messageService;
        }

        public int GetRequiredUserId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                throw new UnauthorizedAccessException(_messageService.Get(MessageKeys.UserNotAuthenticated));
            }

            var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue(ClaimTypes.Name)
                ?? throw new UnauthorizedAccessException(_messageService.Get(MessageKeys.TokenInvalidForCurrentUser));

            if (!int.TryParse(value, out var userId))
            {
                throw new UnauthorizedAccessException(_messageService.Get(MessageKeys.UserIdentifierInvalidInToken));
            }

            return userId;
        }
    }
}
