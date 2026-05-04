using ExcelDoc.Server.DTOs.PerfilMapeamentos;

namespace ExcelDoc.Server.Services.Interfaces
{
    public interface IPerfilMapeamentoService
    {
        Task<IReadOnlyCollection<PerfilMapeamentoResponseDto>> GetByDocumentoAsync(int documentoId, CancellationToken cancellationToken = default);

        Task<PerfilMapeamentoResponseDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<PerfilMapeamentoResponseDto> CriarAsync(PerfilMapeamentoRequestDto request, CancellationToken cancellationToken = default);

        Task<PerfilMapeamentoResponseDto> AtualizarAsync(int id, PerfilMapeamentoRequestDto request, CancellationToken cancellationToken = default);

        Task ExcluirAsync(int id, CancellationToken cancellationToken = default);

        Task<PerfilMapeamentoResponseDto> ClonarAsync(int id, CancellationToken cancellationToken = default);
    }
}
