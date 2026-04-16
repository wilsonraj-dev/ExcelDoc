using ExcelDoc.Server.DTOs.Mapeamentos;

namespace ExcelDoc.Server.Services.Interfaces
{
    public interface IMapeamentoService
    {
        Task<IReadOnlyCollection<MapeamentoResponseDto>> GetByColecaoAsync(int colecaoId, CancellationToken cancellationToken = default);

        Task<MapeamentoResponseDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<MapeamentoResponseDto> CriarAsync(MapeamentoRequestDto request, CancellationToken cancellationToken = default);

        Task<MapeamentoResponseDto> AtualizarAsync(int id, MapeamentoRequestDto request, CancellationToken cancellationToken = default);

        Task ExcluirAsync(int id, CancellationToken cancellationToken = default);
    }
}
