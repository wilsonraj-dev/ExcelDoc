using ExcelDoc.Server.DTOs.Documentos;

namespace ExcelDoc.Server.Services.Interfaces
{
    public interface IDocumentoService
    {
        Task<IReadOnlyCollection<DocumentoResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<DocumentoResponseDto> CriarAsync(DocumentoRequestDto request, CancellationToken cancellationToken = default);

        Task<DocumentoResponseDto> AtualizarAsync(int documentoId, DocumentoRequestDto request, CancellationToken cancellationToken = default);
    }
}
