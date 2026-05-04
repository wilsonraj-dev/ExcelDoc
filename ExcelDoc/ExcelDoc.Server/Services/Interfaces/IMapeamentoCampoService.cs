using ExcelDoc.Server.DTOs.Mapeamentos;

namespace ExcelDoc.Server.Services.Interfaces
{
    public interface IMapeamentoCampoService
    {
        Task<IReadOnlyCollection<MapeamentoCampoResponseDto>> GetByMapeamentoAsync(int mapeamentoId, CancellationToken cancellationToken = default);

        Task<MapeamentoCampoResponseDto> CriarAsync(MapeamentoCampoRequestDto request, CancellationToken cancellationToken = default);

        Task<MapeamentoCampoResponseDto> AtualizarAsync(int id, MapeamentoCampoRequestDto request, CancellationToken cancellationToken = default);

        Task ExcluirAsync(int id, CancellationToken cancellationToken = default);
    }
}
