using ExcelDoc.Server.DTOs.Documentos;

namespace ExcelDoc.Server.Services.Interfaces
{
    public interface IDocumentoService
    {
        Task<IReadOnlyCollection<DocumentoResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
    }
}
