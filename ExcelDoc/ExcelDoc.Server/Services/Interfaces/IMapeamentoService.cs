using ExcelDoc.Server.DTOs.Mapeamentos;

namespace ExcelDoc.Server.Services.Interfaces
{
    public interface IMapeamentoService
    {
        Task<IReadOnlyCollection<MapeamentoResumoResponseDto>> GetByColecaoAsync(int colecaoId, CancellationToken cancellationToken = default);

        Task<MapeamentoResumoResponseDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<MapeamentoResumoResponseDto> CriarAsync(MapeamentoRequestDto request, CancellationToken cancellationToken = default);

        Task<MapeamentoResumoResponseDto> ClonarAsync(int id, CancellationToken cancellationToken = default);

        Task<MapeamentoResumoResponseDto> AtualizarAsync(int id, MapeamentoRequestDto request, CancellationToken cancellationToken = default);

        Task ExcluirAsync(int id, CancellationToken cancellationToken = default);
    }
}
