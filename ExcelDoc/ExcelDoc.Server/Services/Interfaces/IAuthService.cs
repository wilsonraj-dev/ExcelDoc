using ExcelDoc.Server.DTOs.Auth;

namespace ExcelDoc.Server.Services.Interfaces
{
    public interface IAuthService
    {
        IReadOnlyCollection<SapBaseDto> GetBases();

        Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);

        Task LogoutAsync(CancellationToken cancellationToken = default);
    }
}
