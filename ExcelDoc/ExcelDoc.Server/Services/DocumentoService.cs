using ExcelDoc.Server.DTOs.Documentos;
using ExcelDoc.Server.Localization;
using ExcelDoc.Server.Models;
using ExcelDoc.Server.Repositories.Interfaces;
using ExcelDoc.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExcelDoc.Server.Services
{
    public class DocumentoService : IDocumentoService
    {
        private readonly IDocumentoRepository _documentoRepository;
        private readonly IMessageService _messageService;
        private readonly IUsuarioAcessoService _usuarioAcessoService;

        public DocumentoService(IDocumentoRepository documentoRepository, IMessageService messageService, IUsuarioAcessoService usuarioAcessoService)
        {
            _documentoRepository = documentoRepository;
            _messageService = messageService;
            _usuarioAcessoService = usuarioAcessoService;
        }

        public async Task<IReadOnlyCollection<DocumentoResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            await _usuarioAcessoService.GetUsuarioAtualAsync(true, cancellationToken);

            var documentos = await _documentoRepository.GetAllAsync(cancellationToken);

            return documentos
                .Select(Map)
                .ToList();
        }

        public async Task<DocumentoResponseDto> GetByIdAsync(int documentoId, CancellationToken cancellationToken = default)
        {
            await _usuarioAcessoService.GetUsuarioAtualAsync(true, cancellationToken);

            var documento = await _documentoRepository.GetByIdAsync(documentoId, cancellationToken)
                ?? throw new KeyNotFoundException(_messageService.Get(MessageKeys.DocumentNotFound));

            return Map(documento);
        }

        public async Task<DocumentoResponseDto> CriarAsync(DocumentoRequestDto request, CancellationToken cancellationToken = default)
        {
            await ValidarAdministradorAsync(cancellationToken);

            var nomeDocumento = request.NomeDocumento.Trim();
            var endpoint = request.Endpoint.Trim();

            ValidarCampos(nomeDocumento, endpoint);

            if (await _documentoRepository.ExistsByNomeOrEndpointAsync(nomeDocumento, endpoint, null, cancellationToken))
            {
                throw new InvalidOperationException(_messageService.Get(MessageKeys.DocumentAlreadyExistsWithNameOrEndpoint));
            }

            var documento = new Documento
            {
                NomeDocumento = nomeDocumento,
                Endpoint = endpoint
            };

            await _documentoRepository.AddAsync(documento, cancellationToken);
            await _documentoRepository.SaveChangesAsync(cancellationToken);

            return Map(documento);
        }

        public async Task ExcluirAsync(int documentoId, CancellationToken cancellationToken = default)
        {
            await ValidarAdministradorAsync(cancellationToken);

            var documento = await _documentoRepository.GetTrackedByIdAsync(documentoId, cancellationToken)
                ?? throw new KeyNotFoundException(_messageService.Get(MessageKeys.DocumentNotFound));

            try
            {
                _documentoRepository.Remove(documento);
                await _documentoRepository.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                throw new InvalidOperationException(_messageService.Get(MessageKeys.DocumentDeleteLinkedRecords));
            }
        }

        public async Task<DocumentoResponseDto> AtualizarAsync(int documentoId, DocumentoRequestDto request, CancellationToken cancellationToken = default)
        {
            await ValidarAdministradorAsync(cancellationToken);

            var documento = await _documentoRepository.GetTrackedByIdAsync(documentoId, cancellationToken)
                ?? throw new KeyNotFoundException(_messageService.Get(MessageKeys.DocumentNotFound));

            var nomeDocumento = request.NomeDocumento.Trim();
            var endpoint = request.Endpoint.Trim();

            ValidarCampos(nomeDocumento, endpoint);

            if (await _documentoRepository.ExistsByNomeOrEndpointAsync(nomeDocumento, endpoint, documentoId, cancellationToken))
            {
                throw new InvalidOperationException(_messageService.Get(MessageKeys.DocumentAlreadyExistsWithNameOrEndpoint));
            }

            documento.NomeDocumento = nomeDocumento;
            documento.Endpoint = endpoint;

            await _documentoRepository.SaveChangesAsync(cancellationToken);

            return Map(documento);
        }

        private async Task ValidarAdministradorAsync(CancellationToken cancellationToken)
        {
            var usuario = await _usuarioAcessoService.GetUsuarioAtualAsync(false, cancellationToken);

            if (usuario.TipoUsuario != TipoUsuario.Administrador)
            {
                throw new UnauthorizedAccessException(_messageService.Get(MessageKeys.OnlyAdminsCanChangeDocuments));
            }
        }

        private void ValidarCampos(string nomeDocumento, string endpoint)
        {
            if (string.IsNullOrWhiteSpace(nomeDocumento))
            {
                throw new InvalidOperationException(_messageService.Get(MessageKeys.DocumentNameRequired));
            }

            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new InvalidOperationException(_messageService.Get(MessageKeys.DocumentEndpointRequired));
            }
        }

        private static DocumentoResponseDto Map(Documento documento)
        {
            return new DocumentoResponseDto
            {
                Id = documento.Id,
                NomeDocumento = documento.NomeDocumento,
                Endpoint = documento.Endpoint,
                ColecaoIds = documento.DocumentoColecoes
                    .Select(dc => dc.FK_IdColecao)
                    .ToList()
            };
        }
    }
}
