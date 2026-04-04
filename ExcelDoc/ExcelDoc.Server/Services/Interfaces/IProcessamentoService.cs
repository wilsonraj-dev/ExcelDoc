using ExcelDoc.Server.DTOs;
using ExcelDoc.Server.DTOs.Processamentos;

namespace ExcelDoc.Server.Services.Interfaces
{
    public interface IProcessamentoService
    {
        Task<ProcessamentoResponseDto> CriarEEnfileirarAsync(UploadProcessamentoRequestDto request, CancellationToken cancellationToken = default);

        Task<ProcessamentoResponseDto> GetByIdAsync(int processamentoId, CancellationToken cancellationToken = default);

        Task<PagedResultDto<ProcessamentoResponseDto>> GetPagedAsync(ProcessamentoQueryDto query, CancellationToken cancellationToken = default);

        Task<PagedResultDto<ProcessamentoItemResponseDto>> GetItemsPagedAsync(int processamentoId, ProcessamentoItensQueryDto query, CancellationToken cancellationToken = default);

        Task MarcarErroFinalAsync(int processamentoId, string erroDetalhado, CancellationToken cancellationToken = default);
    }
}
