using ExcelDoc.Server.DTOs.Colecoes;

namespace ExcelDoc.Server.Services.Interfaces
{
    public interface IColecaoService
    {
        Task<IReadOnlyCollection<ColecaoResponseDto>> GetByEmpresaIdAsync(int empresaId, CancellationToken cancellationToken = default);

        Task<ColecaoResponseDto> ClonePadraoAsync(CloneColecaoRequestDto request, CancellationToken cancellationToken = default);

        Task<ColecaoResponseDto> AtualizarMapeamentosAsync(int colecaoId, AtualizarMapeamentosRequestDto request, CancellationToken cancellationToken = default);
    }
}
