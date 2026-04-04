using ExcelDoc.Server.DTOs.Documentos;
using ExcelDoc.Server.Repositories.Interfaces;
using ExcelDoc.Server.Services.Interfaces;

namespace ExcelDoc.Server.Services
{
    public class DocumentoService : IDocumentoService
    {
        private readonly IDocumentoRepository _documentoRepository;
        private readonly IUsuarioAcessoService _usuarioAcessoService;

        public DocumentoService(IDocumentoRepository documentoRepository, IUsuarioAcessoService usuarioAcessoService)
        {
            _documentoRepository = documentoRepository;
            _usuarioAcessoService = usuarioAcessoService;
        }

        public async Task<IReadOnlyCollection<DocumentoResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            await _usuarioAcessoService.GetUsuarioAtualAsync(true, cancellationToken);

            var documentos = await _documentoRepository.GetAllAsync(cancellationToken);

            return documentos
                .Select(x => new DocumentoResponseDto
                {
                    Id = x.Id,
                    NomeDocumento = x.NomeDocumento,
                    Endpoint = x.Endpoint
                })
                .ToList();
        }
    }
}
