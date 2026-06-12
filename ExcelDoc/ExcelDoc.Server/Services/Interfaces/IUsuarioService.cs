using ExcelDoc.Server.DTOs;
using ExcelDoc.Server.DTOs.Auth;
using ExcelDoc.Server.DTOs.Usuarios;

namespace ExcelDoc.Server.Services.Interfaces
{
    public interface IUsuarioService
    {
        Task<PagedResultDto<UsuarioResponseDto>> GetPagedAsync(UsuarioQueryDto query, CancellationToken cancellationToken = default);

        Task<RegisterUserResponseDto> CriarAsync(UsuarioCreateRequestDto request, CancellationToken cancellationToken = default);

        Task<UsuarioResponseDto> VincularEmpresaAsync(int usuarioId, UsuarioEmpresaVinculoRequestDto request, CancellationToken cancellationToken = default);
    }
}
