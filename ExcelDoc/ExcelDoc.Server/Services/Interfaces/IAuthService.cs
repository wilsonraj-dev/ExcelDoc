using ExcelDoc.Server.DTOs.Auth;

namespace ExcelDoc.Server.Services.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    }
}
