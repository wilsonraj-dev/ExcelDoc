using ExcelDoc.Server.DTOs.Auth;

namespace ExcelDoc.Server.Services.Interfaces
{
    public interface IAuthService
    {
        Task ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken cancellationToken = default);

        Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);

        Task<RegisterUserResponseDto> RegisterAsync(RegisterUserRequestDto request, CancellationToken cancellationToken = default);

        Task ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken = default);
    }
}
