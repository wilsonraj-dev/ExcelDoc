using ExcelDoc.Server.DTOs.Configuracoes;

namespace ExcelDoc.Server.Services.Interfaces
{
    public interface IConfiguracaoService
    {
        Task<ConfiguracaoResponseDto> GetByEmpresaIdAsync(int empresaId, int usuarioExecutorId, CancellationToken cancellationToken = default);

        Task<ConfiguracaoResponseDto> UpsertAsync(ConfiguracaoRequestDto request, CancellationToken cancellationToken = default);
    }
}
