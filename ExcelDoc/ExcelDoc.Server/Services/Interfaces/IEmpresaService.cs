using ExcelDoc.Server.DTOs.Empresas;

namespace ExcelDoc.Server.Services.Interfaces
{
    public interface IEmpresaService
    {
        Task<IReadOnlyCollection<EmpresaResponseDto>> GetDisponiveisAsync(CancellationToken cancellationToken = default);

        Task<EmpresaResponseDto> CriarAsync(EmpresaRequestDto request, CancellationToken cancellationToken = default);
    }
}
